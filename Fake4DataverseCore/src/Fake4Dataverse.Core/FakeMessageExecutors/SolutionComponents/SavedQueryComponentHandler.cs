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
    /// 
    /// SavedQueries (system views) are stored in the "savedquery" table.
    /// This handler processes view definitions and uses CRUD operations to manage them.
    /// </summary>
    public class SavedQueryComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 26; // SavedQuery

        public string ComponentTypeName => "SavedQuery";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract SavedQueries folder from ZIP
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
            // SavedQuery files are typically stored in SavedQueries/ folder within the solution ZIP
            var savedQueryEntries = zipArchive.Entries
                .Where(e => e.FullName.StartsWith("SavedQueries/", StringComparison.OrdinalIgnoreCase) && 
                           e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var entry in savedQueryEntries)
            {
                using (var stream = entry.Open())
                {
                    var savedQueryXml = XDocument.Load(stream);
                    ProcessSavedQuery(savedQueryXml, service, solution);
                }
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

            foreach (var savedQuery in savedQueries.Entities)
            {
                var savedQueryXml = GenerateSavedQueryXml(savedQuery);
                var fileName = $"SavedQueries/{savedQuery.GetAttributeValue<Guid>("savedqueryid")}.xml";
                
                var entry = zipArchive.CreateEntry(fileName);
                using (var entryStream = entry.Open())
                using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    savedQueryXml.Save(writer);
                }
            }
        }

        /// <summary>
        /// Processes a SavedQuery XML and creates/updates the savedquery record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
        /// </summary>
        private void ProcessSavedQuery(XDocument savedQueryXml, IOrganizationService service, Entity solution)
        {
            var root = savedQueryXml.Root;
            if (root == null || root.Name.LocalName != "savedquery")
            {
                return;
            }

            var savedQueryId = Guid.Parse(root.Element("savedqueryid")?.Value ?? Guid.NewGuid().ToString());
            
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
            
            // Extract standard SavedQuery attributes from XML
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/savedquery
            if (root.Element("name")?.Value != null)
                savedQuery["name"] = root.Element("name").Value;
            
            if (root.Element("returnedtypecode")?.Value != null)
                savedQuery["returnedtypecode"] = root.Element("returnedtypecode").Value;
            
            if (root.Element("fetchxml")?.Value != null)
                savedQuery["fetchxml"] = root.Element("fetchxml").Value;
            
            if (root.Element("layoutxml")?.Value != null)
                savedQuery["layoutxml"] = root.Element("layoutxml").Value;
            
            if (root.Element("querytype")?.Value != null && int.TryParse(root.Element("querytype").Value, out int queryType))
                savedQuery["querytype"] = queryType;
            
            if (root.Element("isdefault")?.Value != null && bool.TryParse(root.Element("isdefault").Value, out bool isDefault))
                savedQuery["isdefault"] = isDefault;
            
            if (root.Element("iscustomizable")?.Value != null && bool.TryParse(root.Element("iscustomizable").Value, out bool isCustomizable))
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
        /// Generates SavedQuery XML for export.
        /// </summary>
        private XDocument GenerateSavedQueryXml(Entity savedQuery)
        {
            var root = new XElement("savedquery");
            
            root.Add(new XElement("savedqueryid", savedQuery.GetAttributeValue<Guid>("savedqueryid")));
            
            if (savedQuery.Contains("name"))
                root.Add(new XElement("name", savedQuery.GetAttributeValue<string>("name")));
            
            if (savedQuery.Contains("returnedtypecode"))
                root.Add(new XElement("returnedtypecode", savedQuery.GetAttributeValue<string>("returnedtypecode")));
            
            if (savedQuery.Contains("fetchxml"))
                root.Add(new XElement("fetchxml", savedQuery.GetAttributeValue<string>("fetchxml")));
            
            if (savedQuery.Contains("layoutxml"))
                root.Add(new XElement("layoutxml", savedQuery.GetAttributeValue<string>("layoutxml")));
            
            if (savedQuery.Contains("querytype"))
                root.Add(new XElement("querytype", savedQuery.GetAttributeValue<int>("querytype")));
            
            if (savedQuery.Contains("isdefault"))
                root.Add(new XElement("isdefault", savedQuery.GetAttributeValue<bool>("isdefault")));
            
            if (savedQuery.Contains("iscustomizable"))
                root.Add(new XElement("iscustomizable", savedQuery.GetAttributeValue<bool>("iscustomizable")));

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }
    }
}
