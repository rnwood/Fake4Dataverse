using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.FakeMessageExecutors.SolutionComponents
{
    /// <summary>
    /// Handles import/export of SavedQuery (View) components (Component Type 26).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
    /// 
    /// SavedQueries (system views) are stored in the "savedquery" table.
    /// In Power Platform solutions, savedquery definitions are in the customizations.xml file under the savedqueries element.
    /// This handler processes view definitions and uses CRUD operations to manage them.
    /// </summary>
    public class SavedQueryComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 26; // SavedQuery

        public string ComponentTypeName => "SavedQuery";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract savedqueries from customizations.xml
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
            // All component definitions are in customizations.xml, not separate folders
            var customizationsEntry = zipArchive.Entries.FirstOrDefault(e => 
                e.FullName.Equals("customizations.xml", StringComparison.OrdinalIgnoreCase));

            if (customizationsEntry == null)
            {
                return;
            }

            XDocument customizationsXml;
            using (var stream = customizationsEntry.Open())
            {
                customizationsXml = XDocument.Load(stream);
            }

            // SavedQueries are under ImportExportXml/savedqueries element
            var savedQueries = customizationsXml.Root?.Element("savedqueries")?.Elements("savedquery");
            if (savedQueries == null)
            {
                return;
            }

            foreach (var savedQueryElement in savedQueries)
            {
                ProcessSavedQuery(savedQueryElement, service, solution);
            }
        }

        public void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Query savedquery table via CRUD to find views in this solution
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
            var query = new QueryExpression("savedquery")
            {
                ColumnSet = new ColumnSet(true), // Get all columns for export
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Filter by solution - typically through solutioncomponent relationship
                        // For now, export all savedqueries as a simple implementation
                    }
                }
            };

            var savedQueries = service.RetrieveMultiple(query);

            if (savedQueries.Entities.Count == 0)
            {
                return;
            }

            // Find or create customizations.xml in the ZIP
            var customizationsEntry = zipArchive.Entries.FirstOrDefault(e => 
                e.FullName.Equals("customizations.xml", StringComparison.OrdinalIgnoreCase));

            XDocument customizationsXml;
            if (customizationsEntry != null)
            {
                using (var stream = customizationsEntry.Open())
                {
                    customizationsXml = XDocument.Load(stream);
                }
            }
            else
            {
                // Create new customizations.xml structure
                customizationsXml = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("ImportExportXml",
                        new XAttribute("version", "9.0.0.0"),
                        new XAttribute("SolutionPackageVersion", "9.0"),
                        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    )
                );
            }

            // Add or update savedqueries element
            var savedQueriesElement = customizationsXml.Root.Element("savedqueries");
            if (savedQueriesElement == null)
            {
                savedQueriesElement = new XElement("savedqueries");
                customizationsXml.Root.Add(savedQueriesElement);
            }

            foreach (var savedQuery in savedQueries.Entities)
            {
                savedQueriesElement.Add(GenerateSavedQueryElement(savedQuery));
            }

            // Write back to ZIP
            if (customizationsEntry != null)
            {
                customizationsEntry.Delete();
            }

            var entry = zipArchive.CreateEntry("customizations.xml");
            using (var entryStream = entry.Open())
            using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
            {
                customizationsXml.Save(writer);
            }
        }

        /// <summary>
        /// Processes a SavedQuery element from customizations.xml and creates/updates the savedquery record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
        /// </summary>
        private void ProcessSavedQuery(XElement savedQueryElement, IOrganizationService service, Entity solution)
        {
            var savedQueryIdStr = savedQueryElement.Element("savedqueryid")?.Value;
            if (string.IsNullOrEmpty(savedQueryIdStr))
            {
                return;
            }

            var savedQueryId = Guid.Parse(savedQueryIdStr);
            
            // Check if savedquery already exists
            var query = new QueryExpression("savedquery")
            {
                ColumnSet = new ColumnSet("savedqueryid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("savedqueryid", ConditionOperator.Equal, savedQueryId)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query);

            var savedQuery = new Entity("savedquery")
            {
                Id = savedQueryId
            };
            savedQuery["savedqueryid"] = savedQueryId;
            
            // Extract standard SavedQuery attributes from XML element
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
            if (savedQueryElement.Element("name")?.Value != null)
                savedQuery["name"] = savedQueryElement.Element("name").Value;
            
            if (savedQueryElement.Element("returnedtypecode")?.Value != null)
                savedQuery["returnedtypecode"] = savedQueryElement.Element("returnedtypecode").Value;
            
            if (savedQueryElement.Element("fetchxml")?.Value != null)
                savedQuery["fetchxml"] = savedQueryElement.Element("fetchxml").Value;
            
            if (savedQueryElement.Element("layoutxml")?.Value != null)
                savedQuery["layoutxml"] = savedQueryElement.Element("layoutxml").Value;
            
            if (savedQueryElement.Element("querytype")?.Value != null && int.TryParse(savedQueryElement.Element("querytype").Value, out int queryType))
                savedQuery["querytype"] = queryType;
            
            if (savedQueryElement.Element("isdefault")?.Value != null && bool.TryParse(savedQueryElement.Element("isdefault").Value, out bool isDefault))
                savedQuery["isdefault"] = isDefault;
            
            if (savedQueryElement.Element("iscustomizable")?.Value != null && bool.TryParse(savedQueryElement.Element("iscustomizable").Value, out bool isCustomizable))
                savedQuery["iscustomizable"] = isCustomizable;

            if (existing.Entities.Count > 0)
            {
                service.Update(savedQuery);
            }
            else
            {
                service.Create(savedQuery);
            }
        }

        /// <summary>
        /// Generates SavedQuery XML element for customizations.xml.
        /// </summary>
        private XElement GenerateSavedQueryElement(Entity savedQuery)
        {
            var element = new XElement("savedquery");
            
            element.Add(new XElement("savedqueryid", savedQuery.GetAttributeValue<Guid>("savedqueryid")));
            
            if (savedQuery.Contains("name"))
                element.Add(new XElement("name", savedQuery.GetAttributeValue<string>("name")));
            
            if (savedQuery.Contains("returnedtypecode"))
                element.Add(new XElement("returnedtypecode", savedQuery.GetAttributeValue<string>("returnedtypecode")));
            
            if (savedQuery.Contains("fetchxml"))
                element.Add(new XElement("fetchxml", new XCData(savedQuery.GetAttributeValue<string>("fetchxml"))));
            
            if (savedQuery.Contains("layoutxml"))
                element.Add(new XElement("layoutxml", new XCData(savedQuery.GetAttributeValue<string>("layoutxml"))));
            
            if (savedQuery.Contains("querytype"))
                element.Add(new XElement("querytype", savedQuery.GetAttributeValue<int>("querytype")));
            
            if (savedQuery.Contains("isdefault"))
                element.Add(new XElement("isdefault", savedQuery.GetAttributeValue<bool>("isdefault")));
            
            if (savedQuery.Contains("iscustomizable"))
                element.Add(new XElement("iscustomizable", savedQuery.GetAttributeValue<bool>("iscustomizable")));

            return element;
        }
    }
}
