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
    /// 
    /// WebResources are stored in the "webresource" table.
    /// This handler processes web resource files and uses CRUD operations to manage them.
    /// </summary>
    public class WebResourceComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 61; // WebResource

        public string ComponentTypeName => "WebResource";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Extract WebResources folder from ZIP
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/solution-file-reference
            // WebResource files are typically stored in WebResources/ folder within the solution ZIP
            var webResourceEntries = zipArchive.Entries
                .Where(e => e.FullName.StartsWith("WebResources/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var entry in webResourceEntries)
            {
                // WebResources can be various file types (JS, CSS, HTML, images, etc.)
                // The file extension determines the webresource type
                ProcessWebResource(entry, service, solution);
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

            foreach (var webResource in webResources.Entities)
            {
                var fileName = GetWebResourceFileName(webResource);
                var content = webResource.GetAttributeValue<string>("content");
                
                if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(content))
                {
                    var entry = zipArchive.CreateEntry(fileName);
                    using (var entryStream = entry.Open())
                    {
                        // Content is typically base64 encoded
                        var bytes = Convert.FromBase64String(content);
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a WebResource file and creates/updates the webresource record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource
        /// </summary>
        private void ProcessWebResource(ZipArchiveEntry entry, IOrganizationService service, Entity solution)
        {
            // Extract webresource ID from filename or generate new one
            var fileName = entry.Name;
            var webResourceId = Guid.NewGuid();
            
            // Check if webresource already exists by name
            var name = entry.FullName.Replace("WebResources/", "").Replace("/", "_");
            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("webresourceid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, name)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query);
            if (existing.Entities.Count > 0)
            {
                webResourceId = existing.Entities[0].Id;
            }

            var webResource = new Entity("webresource")
            {
                Id = webResourceId
            };
            webResource["webresourceid"] = webResourceId;
            webResource["name"] = name;
            
            // Read content and encode as base64
            using (var stream = entry.Open())
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                webResource["content"] = Convert.ToBase64String(bytes);
            }
            
            // Determine webresource type from file extension
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource#webresourcetype-choicesoptions
            var extension = Path.GetExtension(fileName).ToLower();
            int webResourceType = GetWebResourceType(extension);
            webResource["webresourcetype"] = webResourceType;
            
            // Set display name
            webResource["displayname"] = Path.GetFileNameWithoutExtension(fileName);

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
        /// Gets the filename for a webresource for export.
        /// </summary>
        private string GetWebResourceFileName(Entity webResource)
        {
            var name = webResource.GetAttributeValue<string>("name");
            var webResourceType = webResource.GetAttributeValue<int>("webresourcetype");
            
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            
            // Determine file extension from type
            var extension = GetFileExtensionFromType(webResourceType);
            
            // Construct filename
            return $"WebResources/{name}{extension}";
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
