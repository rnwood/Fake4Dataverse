using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.FakeMessageExecutors.SolutionComponents
{
    /// <summary>
    /// Handles import/export of Entity components (Component Type 1).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
    /// 
    /// Entities in Dataverse are stored in the "entity" table (accessed via REST as EntityDefinition).
    /// This handler processes entity metadata from customizations.xml and creates/updates entity records.
    /// </summary>
    public class EntityComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 1; // Entity

        public string ComponentTypeName => "Entity";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // Find customizations.xml in the ZIP
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-xml-reference
            var customizationsEntry = zipArchive.Entries.FirstOrDefault(e => 
                e.FullName.Equals("customizations.xml", System.StringComparison.OrdinalIgnoreCase));

            if (customizationsEntry == null)
            {
                // No customizations file - solution may only contain other components
                return;
            }

            XDocument customizationsXml;
            using (var stream = customizationsEntry.Open())
            {
                customizationsXml = XDocument.Load(stream);
            }

            // Process entities from customizations.xml
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-xml-reference#entities-element
            var entities = customizationsXml.Root?.Element("Entities")?.Elements("Entity");
            if (entities == null)
            {
                return;
            }

            foreach (var entityElement in entities)
            {
                ProcessEntityMetadata(entityElement, service, solution);
            }
        }

        public void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // TODO: Implement entity export to customizations.xml
            // This would query the "entity" table and generate the customizations.xml content
        }

        /// <summary>
        /// Processes entity metadata from customizations.xml and creates/updates entity records.
        /// Uses CRUD operations on the "entity" table.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-xml-reference#entity-element
        /// </summary>
        private void ProcessEntityMetadata(XElement entityElement, IOrganizationService service, Entity solution)
        {
            // Extract entity information
            var entityInfo = entityElement.Element("EntityInfo");
            if (entityInfo == null)
            {
                return;
            }

            var logicalName = entityInfo.Element("Name")?.Value;
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return;
            }

            // Check if entity already exists using CRUD operations on "entity" table
            // Note: The table is called "entity" but accessed via REST API as "EntityDefinition"
            var query = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet("metadataid", "logicalname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("logicalname", ConditionOperator.Equal, logicalName.ToLower())
                    }
                }
            };

            var existingEntities = service.RetrieveMultiple(query);

            if (existingEntities.Entities.Count == 0)
            {
                // Create new entity record via CRUD
                var entityRecord = new Entity("entity")
                {
                    Id = System.Guid.NewGuid()
                };
                entityRecord["metadataid"] = entityRecord.Id;
                entityRecord["logicalname"] = logicalName.ToLower();
                entityRecord["schemaname"] = entityInfo.Element("OriginalName")?.Value ?? logicalName;
                entityRecord["issolutionaware"] = true;

                service.Create(entityRecord);
            }
            else
            {
                // Entity already exists - could update if needed
                // For now, we'll just track it in the solution component
            }
        }
    }
}
