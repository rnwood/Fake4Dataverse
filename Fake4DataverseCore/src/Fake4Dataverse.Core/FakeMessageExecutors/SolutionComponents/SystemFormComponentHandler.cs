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
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
    /// 
    /// SystemForms are stored in the "systemform" table.
    /// In Power Platform solutions, systemform definitions are in the customizations.xml file under the systemforms element.
    /// This handler processes form definitions and uses CRUD operations to manage them.
    /// </summary>
    public class SystemFormComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 60; // SystemForm

        public string ComponentTypeName => "SystemForm";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract systemforms from customizations.xml
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

            // SystemForms are under ImportExportXml/systemforms element
            var systemForms = customizationsXml.Root?.Element("systemforms")?.Elements("systemform");
            if (systemForms == null)
            {
                return;
            }

            foreach (var systemFormElement in systemForms)
            {
                ProcessSystemForm(systemFormElement, service, solution);
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

            if (systemForms.Entities.Count == 0)
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

            // Add or update systemforms element
            var systemFormsElement = customizationsXml.Root.Element("systemforms");
            if (systemFormsElement == null)
            {
                systemFormsElement = new XElement("systemforms");
                customizationsXml.Root.Add(systemFormsElement);
            }

            foreach (var systemForm in systemForms.Entities)
            {
                systemFormsElement.Add(GenerateSystemFormElement(systemForm));
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
        /// Processes a SystemForm element from customizations.xml and creates/updates the systemform record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
        /// </summary>
        private void ProcessSystemForm(XElement systemFormElement, IOrganizationService service, Entity solution)
        {
            var formIdStr = systemFormElement.Element("formid")?.Value;
            if (string.IsNullOrEmpty(formIdStr))
            {
                return;
            }

            var formId = Guid.Parse(formIdStr);
            
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
            
            // Extract standard SystemForm attributes from XML element
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemform
            if (systemFormElement.Element("name")?.Value != null)
                systemForm["name"] = systemFormElement.Element("name").Value;
            
            if (systemFormElement.Element("objecttypecode")?.Value != null)
                systemForm["objecttypecode"] = systemFormElement.Element("objecttypecode").Value;
            
            if (systemFormElement.Element("formxml")?.Value != null)
                systemForm["formxml"] = systemFormElement.Element("formxml").Value;
            
            if (systemFormElement.Element("type")?.Value != null && int.TryParse(systemFormElement.Element("type").Value, out int formType))
                systemForm["type"] = formType;
            
            if (systemFormElement.Element("description")?.Value != null)
                systemForm["description"] = systemFormElement.Element("description").Value;
            
            if (systemFormElement.Element("iscustomizable")?.Value != null && bool.TryParse(systemFormElement.Element("iscustomizable").Value, out bool isCustomizable))
                systemForm["iscustomizable"] = isCustomizable;
            
            if (systemFormElement.Element("isdefault")?.Value != null && bool.TryParse(systemFormElement.Element("isdefault").Value, out bool isDefault))
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
        /// Generates SystemForm XML element for customizations.xml.
        /// </summary>
        private XElement GenerateSystemFormElement(Entity systemForm)
        {
            var element = new XElement("systemform");
            
            element.Add(new XElement("formid", systemForm.GetAttributeValue<Guid>("formid")));
            
            if (systemForm.Contains("name"))
                element.Add(new XElement("name", systemForm.GetAttributeValue<string>("name")));
            
            if (systemForm.Contains("objecttypecode"))
                element.Add(new XElement("objecttypecode", systemForm.GetAttributeValue<string>("objecttypecode")));
            
            if (systemForm.Contains("formxml"))
                element.Add(new XElement("formxml", new XCData(systemForm.GetAttributeValue<string>("formxml"))));
            
            if (systemForm.Contains("type"))
                element.Add(new XElement("type", systemForm.GetAttributeValue<int>("type")));
            
            if (systemForm.Contains("description"))
                element.Add(new XElement("description", systemForm.GetAttributeValue<string>("description")));
            
            if (systemForm.Contains("iscustomizable"))
                element.Add(new XElement("iscustomizable", systemForm.GetAttributeValue<bool>("iscustomizable")));
            
            if (systemForm.Contains("isdefault"))
                element.Add(new XElement("isdefault", systemForm.GetAttributeValue<bool>("isdefault")));

            return element;
        }
    }
}
