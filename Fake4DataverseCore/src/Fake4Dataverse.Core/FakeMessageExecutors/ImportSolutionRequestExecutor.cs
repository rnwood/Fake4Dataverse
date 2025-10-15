using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceModel;
using System.Xml.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Exceptions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executes ImportSolutionRequest to import solutions into the faked context.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
    /// 
    /// ImportSolutionRequest imports a solution into the organization. The solution file is a ZIP archive
    /// containing solution.xml manifest and component files. The import process:
    /// 1. Validates the solution file format
    /// 2. Reads solution metadata from solution.xml
    /// 3. Creates/updates the solution record
    /// 4. Processes solution components based on componentdefinition table
    /// 5. Creates solutioncomponent records to track components
    /// </summary>
    public class ImportSolutionRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ImportSolutionRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var importRequest = (ImportSolutionRequest)request;

            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.customizationfile
            // CustomizationFile is a required property containing the solution ZIP file as a byte array
            if (importRequest.CustomizationFile == null || importRequest.CustomizationFile.Length == 0)
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    ErrorCodes.ImportSolutionError,
                    "CustomizationFile is required for solution import.");
            }

            var service = ctx.GetOrganizationService();
            Entity solution = null;
            Guid importJobId = importRequest.ImportJobId != Guid.Empty ? importRequest.ImportJobId : Guid.NewGuid();

            try
            {
                // Extract and parse the solution ZIP file
                using (var memoryStream = new MemoryStream(importRequest.CustomizationFile))
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                {
                    // Find and read solution.xml
                    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
                    // The solution.xml file contains the solution manifest with metadata and component references
                    var solutionXmlEntry = zipArchive.Entries.FirstOrDefault(e => 
                        e.FullName.Equals("solution.xml", StringComparison.OrdinalIgnoreCase));

                    if (solutionXmlEntry == null)
                    {
                        throw FakeOrganizationServiceFaultFactory.New(
                            ErrorCodes.ImportSolutionError,
                            "Invalid solution file: solution.xml not found.");
                    }

                    XDocument solutionXml;
                    using (var stream = solutionXmlEntry.Open())
                    {
                        solutionXml = XDocument.Load(stream);
                    }

                    // Parse solution metadata
                    solution = ParseSolutionMetadata(solutionXml, importRequest, ctx);

                    // Check if solution already exists
                    var existingSolutions = service.RetrieveMultiple(new Microsoft.Xrm.Sdk.Query.QueryExpression("solution")
                    {
                        ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("solutionid", "uniquename", "ismanaged"),
                        Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                        {
                            Conditions =
                            {
                                new Microsoft.Xrm.Sdk.Query.ConditionExpression("uniquename", 
                                    Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, 
                                    solution.GetAttributeValue<string>("uniquename"))
                            }
                        }
                    });

                    if (existingSolutions.Entities.Count > 0)
                    {
                        // Update existing solution
                        var existingSolution = existingSolutions.Entities[0];
                        solution.Id = existingSolution.Id;
                        solution["solutionid"] = solution.Id;

                        // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.overwriteunmanagedcustomizations
                        // OverwriteUnmanagedCustomizations controls whether to overwrite unmanaged customizations
                        bool isExistingManaged = existingSolution.GetAttributeValue<bool>("ismanaged");
                        bool isImportingManaged = solution.GetAttributeValue<bool>("ismanaged");

                        if (isExistingManaged && !isImportingManaged)
                        {
                            throw FakeOrganizationServiceFaultFactory.New(
                                ErrorCodes.ImportSolutionManagedToUnmanagedMismatch,
                                "Cannot import unmanaged solution over a managed solution.");
                        }

                        service.Update(solution);
                    }
                    else
                    {
                        // Create new solution
                        if (solution.Id == Guid.Empty)
                        {
                            solution.Id = Guid.NewGuid();
                            solution["solutionid"] = solution.Id;
                        }
                        service.Create(solution);
                    }

                    // Process solution components
                    ProcessSolutionComponents(solutionXml, solution, ctx, service, importRequest);
                }

                // Return successful response with import job ID
                return new ImportSolutionResponse
                {
                    Results = new ParameterCollection
                    {
                        { "ImportJobId", importJobId }
                    }
                };
            }
            catch (InvalidDataException ex)
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    ErrorCodes.ImportSolutionError,
                    $"Invalid solution file format: {ex.Message}");
            }
            catch (Exception ex) when (!(ex is FaultException<OrganizationServiceFault>))
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    ErrorCodes.ImportSolutionError,
                    $"Solution import failed: {ex.Message}");
            }
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(ImportSolutionRequest);
        }

        /// <summary>
        /// Parses solution metadata from solution.xml
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
        /// </summary>
        private Entity ParseSolutionMetadata(XDocument solutionXml, ImportSolutionRequest importRequest, IXrmFakedContext ctx)
        {
            var root = solutionXml.Root;
            if (root == null || !root.Name.LocalName.Equals("ImportExportXml", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Invalid solution.xml: root element must be ImportExportXml");
            }

            var solutionManifest = root.Element("SolutionManifest");
            if (solutionManifest == null)
            {
                throw new InvalidDataException("Invalid solution.xml: SolutionManifest element not found");
            }

            var solution = new Entity("solution")
            {
                Id = Guid.NewGuid()
            };

            solution["solutionid"] = solution.Id;
            solution["uniquename"] = solutionManifest.Element("UniqueName")?.Value ?? 
                throw new InvalidDataException("UniqueName is required in solution manifest");
            
            var localizedNames = solutionManifest.Element("LocalizedNames");
            if (localizedNames != null)
            {
                var localizedName = localizedNames.Elements("LocalizedName").FirstOrDefault();
                if (localizedName != null)
                {
                    solution["friendlyname"] = localizedName.Attribute("description")?.Value ?? solution["uniquename"];
                }
            }

            var version = solutionManifest.Element("Version");
            if (version != null)
            {
                solution["version"] = version.Value;
            }

            var publisher = solutionManifest.Element("Publisher");
            if (publisher != null)
            {
                var uniqueName = publisher.Element("UniqueName")?.Value;
                if (!string.IsNullOrWhiteSpace(uniqueName))
                {
                    // Try to find the publisher
                    var svc = ctx.GetOrganizationService();
                    var publishers = svc.RetrieveMultiple(new Microsoft.Xrm.Sdk.Query.QueryExpression("publisher")
                    {
                        ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("publisherid"),
                        Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                        {
                            Conditions =
                            {
                                new Microsoft.Xrm.Sdk.Query.ConditionExpression("uniquename", 
                                    Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, uniqueName)
                            }
                        }
                    });

                    if (publishers.Entities.Count > 0)
                    {
                        solution["publisherid"] = publishers.Entities[0].ToEntityReference();
                    }
                }
            }

            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest.converttomanaged
            // ConvertToManaged property controls whether to import as managed solution
            bool isManaged = solutionManifest.Element("Managed")?.Value == "1";
            if (importRequest.ConvertToManaged)
            {
                isManaged = true;
            }
            solution["ismanaged"] = isManaged;

            solution["installedon"] = DateTime.UtcNow;
            solution["isvisible"] = true;

            return solution;
        }

        /// <summary>
        /// Processes solution components from solution.xml
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
        /// </summary>
        private void ProcessSolutionComponents(XDocument solutionXml, Entity solution, IXrmFakedContext ctx, 
            IOrganizationService service, ImportSolutionRequest importRequest)
        {
            var root = solutionXml.Root;
            var rootComponents = root?.Element("SolutionManifest")?.Element("RootComponents");
            
            if (rootComponents == null)
            {
                // No components to process
                return;
            }

            foreach (var rootComponent in rootComponents.Elements("RootComponent"))
            {
                var componentType = int.Parse(rootComponent.Attribute("type")?.Value ?? "0");
                var componentId = Guid.Parse(rootComponent.Attribute("id")?.Value ?? Guid.Empty.ToString());
                var schemaName = rootComponent.Attribute("schemaName")?.Value;

                if (componentId == Guid.Empty)
                {
                    continue;
                }

                // Check if this component type is supported
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/componentdefinition-entity
                // The componentdefinition table stores information about which entities are solution-aware
                if (!IsComponentTypeSupported(ctx, componentType, out string entityLogicalName))
                {
                    throw FakeOrganizationServiceFaultFactory.New(
                        ErrorCodes.ImportSolutionError,
                        $"Component type {componentType} ({schemaName}) is not supported. " +
                        $"Please ensure the corresponding entity is marked as solution-aware in the componentdefinition table.");
                }

                // Create or update solution component record
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
                // SolutionComponent entity tracks which components belong to which solutions
                var existingComponents = service.RetrieveMultiple(new Microsoft.Xrm.Sdk.Query.QueryExpression("solutioncomponent")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("solutioncomponentid"),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                        {
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression("solutionid", 
                                Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solution.Id),
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression("objectid", 
                                Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, componentId),
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression("componenttype", 
                                Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, componentType)
                        }
                    }
                });

                if (existingComponents.Entities.Count == 0)
                {
                    var solutionComponent = new Entity("solutioncomponent")
                    {
                        Id = Guid.NewGuid()
                    };
                    solutionComponent["solutioncomponentid"] = solutionComponent.Id;
                    solutionComponent["solutionid"] = solution.ToEntityReference();
                    solutionComponent["objectid"] = componentId;
                    solutionComponent["componenttype"] = new OptionSetValue(componentType);
                    solutionComponent["rootcomponentbehavior"] = new OptionSetValue(0); // Include
                    solutionComponent["ismetadata"] = false;

                    service.Create(solutionComponent);
                }
            }
        }

        /// <summary>
        /// Checks if a component type is supported based on componentdefinition table
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/componentdefinition-entity
        /// </summary>
        private bool IsComponentTypeSupported(IXrmFakedContext ctx, int componentType, out string entityLogicalName)
        {
            entityLogicalName = null;

            var xrmCtx = ctx as XrmFakedContext;
            if (xrmCtx == null || !xrmCtx.Data.ContainsKey("componentdefinition"))
            {
                return false;
            }

            // Look up the component type in componentdefinition
            var componentDef = xrmCtx.Data["componentdefinition"].Values
                .FirstOrDefault(e => 
                    e.GetAttributeValue<int?>("objecttypecode") == componentType &&
                    e.GetAttributeValue<bool?>("issolutionaware") == true);

            if (componentDef != null)
            {
                entityLogicalName = componentDef.GetAttributeValue<string>("logicalname");
                return true;
            }

            return false;
        }
    }
}
