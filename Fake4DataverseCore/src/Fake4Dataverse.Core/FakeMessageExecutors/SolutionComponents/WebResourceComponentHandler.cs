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
    /// Handles import/export of WebResource components (Component Type 61).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
    /// 
    /// WebResources are stored in the "webresource" table.
    /// In Power Platform solutions, webresource definitions are in the customizations.xml file under the WebResources element.
    /// The actual file content is stored inline in the XML as base64-encoded content.
    /// This handler processes web resource files and uses CRUD operations to manage them.
    /// </summary>
    public class WebResourceComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 61; // WebResource

        public string ComponentTypeName => "WebResource";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract webresources from customizations.xml
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

            // WebResources are under ImportExportXml/WebResources element
            var webResources = customizationsXml.Root?.Element("WebResources")?.Elements("WebResource");
            if (webResources == null)
            {
                return;
            }

            foreach (var webResourceElement in webResources)
            {
                ProcessWebResource(webResourceElement, service, solution);
            }
        }

        public void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Query webresource table via CRUD to find web resources in this solution
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet(true), // Get all columns for export
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Filter by solution - typically through solutioncomponent relationship
                        // For now, export all webresources as a simple implementation
                    }
                }
            };

            var webResources = service.RetrieveMultiple(query);

            if (webResources.Entities.Count == 0)
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

            // Add or update WebResources element
            var webResourcesElement = customizationsXml.Root.Element("WebResources");
            if (webResourcesElement == null)
            {
                webResourcesElement = new XElement("WebResources");
                customizationsXml.Root.Add(webResourcesElement);
            }

            foreach (var webResource in webResources.Entities)
            {
                webResourcesElement.Add(GenerateWebResourceElement(webResource));
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
        /// Processes a WebResource element from customizations.xml and creates/updates the webresource record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema
        /// </summary>
        private void ProcessWebResource(XElement webResourceElement, IOrganizationService service, Entity solution)
        {
            var webResourceIdStr = webResourceElement.Element("WebResourceId")?.Value;
            if (string.IsNullOrEmpty(webResourceIdStr))
            {
                return;
            }

            var webResourceId = Guid.Parse(webResourceIdStr);
            
            // Check if webresource already exists
            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("webresourceid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("webresourceid", ConditionOperator.Equal, webResourceId)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query);

            var webResource = new Entity("webresource")
            {
                Id = webResourceId
            };
            webResource["webresourceid"] = webResourceId;
            
            // Extract standard WebResource attributes from XML element
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
            if (webResourceElement.Element("Name")?.Value != null)
                webResource["name"] = webResourceElement.Element("Name").Value;
            
            if (webResourceElement.Element("DisplayName")?.Value != null)
                webResource["displayname"] = webResourceElement.Element("DisplayName").Value;
            
            if (webResourceElement.Element("WebResourceType")?.Value != null && int.TryParse(webResourceElement.Element("WebResourceType").Value, out int webResourceType))
                webResource["webresourcetype"] = webResourceType;
            
            if (webResourceElement.Element("Description")?.Value != null)
                webResource["description"] = webResourceElement.Element("Description").Value;
            
            // Content is base64-encoded in the XML
            if (webResourceElement.Element("Content")?.Value != null)
                webResource["content"] = webResourceElement.Element("Content").Value;
            
            if (webResourceElement.Element("IsCustomizable")?.Value != null && bool.TryParse(webResourceElement.Element("IsCustomizable").Value, out bool isCustomizable))
                webResource["iscustomizable"] = isCustomizable;
            
            if (webResourceElement.Element("IsHidden")?.Value != null && bool.TryParse(webResourceElement.Element("IsHidden").Value, out bool isHidden))
                webResource["ishidden"] = isHidden;

            if (existing.Entities.Count > 0)
            {
                service.Update(webResource);
            }
            else
            {
                service.Create(webResource);
            }
        }

        /// <summary>
        /// Generates WebResource XML element for customizations.xml.
        /// </summary>
        private XElement GenerateWebResourceElement(Entity webResource)
        {
            var element = new XElement("WebResource");
            
            element.Add(new XElement("WebResourceId", webResource.GetAttributeValue<Guid>("webresourceid")));
            
            if (webResource.Contains("name"))
                element.Add(new XElement("Name", webResource.GetAttributeValue<string>("name")));
            
            if (webResource.Contains("displayname"))
                element.Add(new XElement("DisplayName", webResource.GetAttributeValue<string>("displayname")));
            
            if (webResource.Contains("webresourcetype"))
                element.Add(new XElement("WebResourceType", webResource.GetAttributeValue<int>("webresourcetype")));
            
            if (webResource.Contains("description"))
                element.Add(new XElement("Description", webResource.GetAttributeValue<string>("description")));
            
            // Content is stored as base64-encoded string
            if (webResource.Contains("content"))
                element.Add(new XElement("Content", webResource.GetAttributeValue<string>("content")));
            
            if (webResource.Contains("iscustomizable"))
                element.Add(new XElement("IsCustomizable", webResource.GetAttributeValue<bool>("iscustomizable")));
            
            if (webResource.Contains("ishidden"))
                element.Add(new XElement("IsHidden", webResource.GetAttributeValue<bool>("ishidden")));

            return element;
        }

        /// <summary>
        /// Gets the webresource type based on file extension.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource#webresourcetype-choicesoptions
        /// </summary>
        private int GetWebResourceType(string extension)
        {
            // WebResourceType values from Microsoft documentation:
            // 1 = HTML, 2 = CSS, 3 = JavaScript, 4 = XML, 5 = PNG, 6 = JPG, 7 = GIF,
            // 8 = XAP, 9 = XSL, 10 = ICO, 11 = SVG, 12 = RESX
            return extension switch
            {
                ".htm" or ".html" => 1,
                ".css" => 2,
                ".js" => 3,
                ".xml" => 4,
                ".png" => 5,
                ".jpg" or ".jpeg" => 6,
                ".gif" => 7,
                ".xap" => 8,
                ".xsl" or ".xslt" => 9,
                ".ico" => 10,
                ".svg" => 11,
                ".resx" => 12,
                _ => 3 // Default to JavaScript
            };
        }

        /// <summary>
        /// Gets file extension from webresource type.
        /// </summary>
        private string GetFileExtensionFromType(int webResourceType)
        {
            return webResourceType switch
            {
                1 => ".html",
                2 => ".css",
                3 => ".js",
                4 => ".xml",
                5 => ".png",
                6 => ".jpg",
                7 => ".gif",
                8 => ".xap",
                9 => ".xsl",
                10 => ".ico",
                11 => ".svg",
                12 => ".resx",
                _ => ".txt"
            };
        }
    }
}
