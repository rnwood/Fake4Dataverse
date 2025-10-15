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
    /// Handles import/export of SystemForm components (Component Type 60).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
    /// 
    /// SystemForms are stored in the "systemform" table.
    /// This handler processes form definitions and uses CRUD operations to manage them.
    /// </summary>
    public class SystemFormComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 60; // SystemForm

        public string ComponentTypeName => "SystemForm";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract SystemForms folder from ZIP
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
            // SystemForm files are typically stored in SystemForms/ folder within the solution ZIP
            var systemFormEntries = zipArchive.Entries
                .Where(e => e.FullName.StartsWith("SystemForms/", StringComparison.OrdinalIgnoreCase) && 
                           e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var entry in systemFormEntries)
            {
                using (var stream = entry.Open())
                {
                    var systemFormXml = XDocument.Load(stream);
                    ProcessSystemForm(systemFormXml, service, solution);
                }
            }
        }

        public void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Query systemform table via CRUD to find forms in this solution
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
            var query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet(true), // Get all columns for export
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Filter by solution - typically through solutioncomponent relationship
                        // For now, export all systemforms as a simple implementation
                    }
                }
            };

            var systemForms = service.RetrieveMultiple(query);

            foreach (var systemForm in systemForms.Entities)
            {
                var systemFormXml = GenerateSystemFormXml(systemForm);
                var fileName = $"SystemForms/{systemForm.GetAttributeValue<Guid>("formid")}.xml";
                
                var entry = zipArchive.CreateEntry(fileName);
                using (var entryStream = entry.Open())
                using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    systemFormXml.Save(writer);
                }
            }
        }

        /// <summary>
        /// Processes a SystemForm XML and creates/updates the systemform record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
        /// </summary>
        private void ProcessSystemForm(XDocument systemFormXml, IOrganizationService service, Entity solution)
        {
            var root = systemFormXml.Root;
            if (root == null || root.Name.LocalName != "systemform")
            {
                return;
            }

            var formId = Guid.Parse(root.Element("formid")?.Value ?? Guid.NewGuid().ToString());
            
            // Check if systemform already exists
            var query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("formid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("formid", ConditionOperator.Equal, formId)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query);

            var systemForm = new Entity("systemform")
            {
                Id = formId
            };
            systemForm["formid"] = formId;
            
            // Extract standard SystemForm attributes from XML
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
            if (root.Element("name")?.Value != null)
                systemForm["name"] = root.Element("name").Value;
            
            if (root.Element("objecttypecode")?.Value != null)
                systemForm["objecttypecode"] = root.Element("objecttypecode").Value;
            
            if (root.Element("formxml")?.Value != null)
                systemForm["formxml"] = root.Element("formxml").Value;
            
            if (root.Element("type")?.Value != null && int.TryParse(root.Element("type").Value, out int formType))
                systemForm["type"] = formType;
            
            if (root.Element("description")?.Value != null)
                systemForm["description"] = root.Element("description").Value;
            
            if (root.Element("iscustomizable")?.Value != null && bool.TryParse(root.Element("iscustomizable").Value, out bool isCustomizable))
                systemForm["iscustomizable"] = isCustomizable;
            
            if (root.Element("isdefault")?.Value != null && bool.TryParse(root.Element("isdefault").Value, out bool isDefault))
                systemForm["isdefault"] = isDefault;

            if (existing.Entities.Count > 0)
            {
                service.Update(systemForm);
            }
            else
            {
                service.Create(systemForm);
            }
        }

        /// <summary>
        /// Generates SystemForm XML for export.
        /// </summary>
        private XDocument GenerateSystemFormXml(Entity systemForm)
        {
            var root = new XElement("systemform");
            
            root.Add(new XElement("formid", systemForm.GetAttributeValue<Guid>("formid")));
            
            if (systemForm.Contains("name"))
                root.Add(new XElement("name", systemForm.GetAttributeValue<string>("name")));
            
            if (systemForm.Contains("objecttypecode"))
                root.Add(new XElement("objecttypecode", systemForm.GetAttributeValue<string>("objecttypecode")));
            
            if (systemForm.Contains("formxml"))
                root.Add(new XElement("formxml", systemForm.GetAttributeValue<string>("formxml")));
            
            if (systemForm.Contains("type"))
                root.Add(new XElement("type", systemForm.GetAttributeValue<int>("type")));
            
            if (systemForm.Contains("description"))
                root.Add(new XElement("description", systemForm.GetAttributeValue<string>("description")));
            
            if (systemForm.Contains("iscustomizable"))
                root.Add(new XElement("iscustomizable", systemForm.GetAttributeValue<bool>("iscustomizable")));
            
            if (systemForm.Contains("isdefault"))
                root.Add(new XElement("isdefault", systemForm.GetAttributeValue<bool>("isdefault")));

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        }
    }
}
