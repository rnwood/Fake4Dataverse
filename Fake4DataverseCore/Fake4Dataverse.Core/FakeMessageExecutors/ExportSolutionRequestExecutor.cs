using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executes ExportSolutionRequest to export solutions from the faked context.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest
    /// 
    /// ExportSolutionRequest exports a solution as a ZIP file containing:
    /// - solution.xml: Manifest with solution metadata and component references
    /// - [Content_Types].xml: MIME types for package parts
    /// - Component-specific files based on what's included in the solution
    /// 
    /// The export process:
    /// 1. Retrieves the solution record by unique name
    /// 2. Queries solution components associated with the solution
    /// 3. Builds solution.xml manifest
    /// 4. Creates ZIP archive with all required files
    /// 5. Returns the ZIP file as a byte array
    /// </summary>
    public class ExportSolutionRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ExportSolutionRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var exportRequest = (ExportSolutionRequest)request;

            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest.solutionname
            // SolutionName is a required property containing the unique name of the solution to export
            if (string.IsNullOrWhiteSpace(exportRequest.SolutionName))
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    ErrorCodes.ExportSolutionError,
                    "SolutionName is required for solution export.");
            }

            var service = ctx.GetOrganizationService();

            // Retrieve the solution
            var solutions = service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, exportRequest.SolutionName)
                    }
                }
            });

            if (solutions.Entities.Count == 0)
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    ErrorCodes.ExportSolutionError,
                    $"Solution with unique name '{exportRequest.SolutionName}' not found.");
            }

            var solution = solutions.Entities[0];

            // Retrieve solution components
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
            // SolutionComponent records track which components are included in each solution
            var solutionComponents = service.RetrieveMultiple(new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "rootcomponentbehavior"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solution.Id)
                    }
                }
            });

            // Create solution ZIP file
            byte[] solutionFile;
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Add solution.xml
                    var solutionXml = GenerateSolutionXml(solution, solutionComponents, exportRequest, service);
                    var solutionXmlEntry = zipArchive.CreateEntry("solution.xml");
                    using (var entryStream = solutionXmlEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        solutionXml.Save(writer);
                    }

                    // Add [Content_Types].xml
                    // Reference: https://learn.microsoft.com/en-us/openspecs/office_standards/ms-opc/6c1afe62-4a8e-4d0e-9c61-d7b81a4d5b82
                    // Content Types file defines MIME types for parts in the package
                    var contentTypesXml = GenerateContentTypesXml();
                    var contentTypesEntry = zipArchive.CreateEntry("[Content_Types].xml");
                    using (var entryStream = contentTypesEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        contentTypesXml.Save(writer);
                    }
                }

                solutionFile = memoryStream.ToArray();
            }

            // Return response with solution file
            return new ExportSolutionResponse
            {
                Results = new ParameterCollection
                {
                    { "ExportSolutionFile", solutionFile }
                }
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(ExportSolutionRequest);
        }

        /// <summary>
        /// Generates solution.xml manifest
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
        /// </summary>
        private XDocument GenerateSolutionXml(Entity solution, EntityCollection solutionComponents, 
            ExportSolutionRequest exportRequest, IOrganizationService service)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("ImportExportXml",
                    new XAttribute("version", "9.0.0.0"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement("SolutionManifest",
                        new XElement("UniqueName", solution.GetAttributeValue<string>("uniquename")),
                        GenerateLocalizedNames(solution),
                        GenerateDescriptions(solution),
                        new XElement("Version", solution.GetAttributeValue<string>("version") ?? "1.0.0.0"),
                        GeneratePublisher(solution, service),
                        GenerateManaged(solution, exportRequest),
                        GenerateRootComponents(solutionComponents)
                    )
                )
            );

            return doc;
        }

        /// <summary>
        /// Generates LocalizedNames element for solution.xml
        /// </summary>
        private XElement GenerateLocalizedNames(Entity solution)
        {
            var friendlyName = solution.GetAttributeValue<string>("friendlyname") ?? 
                             solution.GetAttributeValue<string>("uniquename");

            return new XElement("LocalizedNames",
                new XElement("LocalizedName",
                    new XAttribute("description", friendlyName),
                    new XAttribute("languagecode", "1033")
                )
            );
        }

        /// <summary>
        /// Generates Descriptions element for solution.xml
        /// </summary>
        private XElement GenerateDescriptions(Entity solution)
        {
            var description = solution.GetAttributeValue<string>("description") ?? string.Empty;

            return new XElement("Descriptions",
                new XElement("Description",
                    new XAttribute("description", description),
                    new XAttribute("languagecode", "1033")
                )
            );
        }

        /// <summary>
        /// Generates Publisher element for solution.xml
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/publisher
        /// </summary>
        private XElement GeneratePublisher(Entity solution, IOrganizationService service)
        {
            var publisherRef = solution.GetAttributeValue<EntityReference>("publisherid");
            
            if (publisherRef != null)
            {
                try
                {
                    var publisher = service.Retrieve("publisher", publisherRef.Id, new ColumnSet(true));
                    
                    return new XElement("Publisher",
                        new XElement("UniqueName", publisher.GetAttributeValue<string>("uniquename") ?? "DefaultPublisher"),
                        new XElement("LocalizedNames",
                            new XElement("LocalizedName",
                                new XAttribute("description", publisher.GetAttributeValue<string>("friendlyname") ?? "Default Publisher"),
                                new XAttribute("languagecode", "1033")
                            )
                        ),
                        new XElement("Descriptions"),
                        new XElement("EMailAddress", publisher.GetAttributeValue<string>("emailaddress") ?? string.Empty),
                        new XElement("SupportingWebsiteUrl", publisher.GetAttributeValue<string>("supportingwebsiteurl") ?? string.Empty),
                        new XElement("CustomizationPrefix", publisher.GetAttributeValue<string>("customizationprefix") ?? "new"),
                        new XElement("CustomizationOptionValuePrefix", publisher.GetAttributeValue<int>("customizationoptionvalueprefix"))
                    );
                }
                catch
                {
                    // Fall through to default publisher if retrieval fails
                }
            }

            // Return default publisher if none specified
            return new XElement("Publisher",
                new XElement("UniqueName", "DefaultPublisher"),
                new XElement("LocalizedNames",
                    new XElement("LocalizedName",
                        new XAttribute("description", "Default Publisher"),
                        new XAttribute("languagecode", "1033")
                    )
                ),
                new XElement("Descriptions"),
                new XElement("EMailAddress"),
                new XElement("SupportingWebsiteUrl"),
                new XElement("CustomizationPrefix", "new"),
                new XElement("CustomizationOptionValuePrefix", "10000")
            );
        }

        /// <summary>
        /// Generates Managed element for solution.xml
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.exportsolutionrequest.managed
        /// </summary>
        private XElement GenerateManaged(Entity solution, ExportSolutionRequest exportRequest)
        {
            // Managed property from request overrides solution setting
            bool isManaged = exportRequest.Managed || solution.GetAttributeValue<bool>("ismanaged");
            return new XElement("Managed", isManaged ? "1" : "0");
        }

        /// <summary>
        /// Generates RootComponents element for solution.xml
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
        /// </summary>
        private XElement GenerateRootComponents(EntityCollection solutionComponents)
        {
            var rootComponents = new XElement("RootComponents");

            foreach (var component in solutionComponents.Entities)
            {
                var objectId = component.GetAttributeValue<Guid>("objectid");
                var componentType = component.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0;
                var rootBehavior = component.GetAttributeValue<OptionSetValue>("rootcomponentbehavior")?.Value ?? 0;

                // Only include root components (not dependencies)
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#rootcomponentbehavior-choicesoptions
                // RootComponentBehavior values: 0=Include Subcomponents, 1=Do Not Include Subcomponents, 2=Include As Shell Only
                if (rootBehavior == 0 || rootBehavior == 1 || rootBehavior == 2)
                {
                    rootComponents.Add(new XElement("RootComponent",
                        new XAttribute("type", componentType),
                        new XAttribute("id", objectId.ToString("B").ToUpper()),
                        new XAttribute("behavior", rootBehavior)
                    ));
                }
            }

            return rootComponents;
        }

        /// <summary>
        /// Generates [Content_Types].xml for the solution package
        /// Reference: https://learn.microsoft.com/en-us/openspecs/office_standards/ms-opc/6c1afe62-4a8e-4d0e-9c61-d7b81a4d5b82
        /// </summary>
        private XDocument GenerateContentTypesXml()
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Types",
                    new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Default",
                        new XAttribute("Extension", "xml"),
                        new XAttribute("ContentType", "application/xml")
                    )
                )
            );

            return doc;
        }
    }
}
