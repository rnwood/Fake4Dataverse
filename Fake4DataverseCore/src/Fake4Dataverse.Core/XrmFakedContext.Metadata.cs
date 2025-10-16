using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Extensions;
using System.Reflection;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk;
using Fake4Dataverse.Metadata;
using Fake4Dataverse.Abstractions;
using System.Threading.Tasks;

namespace Fake4Dataverse
{
    public partial class XrmFakedContext : IXrmFakedContext
    {
        /// <summary>
        /// Stores some minimal metadata info if dynamic entities are used and no injected metadata was used
        /// </summary>
        protected internal Dictionary<string, Dictionary<string, string>> AttributeMetadataNames { get; set; }

        /// <summary>
        /// Stores fake entity metadata
        /// </summary>
        protected internal Dictionary<string, EntityMetadata> EntityMetadata { get; set; }


        public void InitializeMetadata(IEnumerable<EntityMetadata> entityMetadataList)
        {
            if (entityMetadataList == null)
            {
                throw new Exception("Entity metadata parameter can not be null");
            }

            //  this.EntityMetadata = new Dictionary<string, EntityMetadata>();
            foreach (var eMetadata in entityMetadataList)
            {
                if (string.IsNullOrWhiteSpace(eMetadata.LogicalName))
                {
                    throw new Exception("An entity metadata record must have a LogicalName property.");
                }

                if (EntityMetadata.ContainsKey(eMetadata.LogicalName))
                {
                    // Skip if entity is already present (e.g., system entities loaded in constructor)
                    // Update existing metadata instead of throwing an error
                    EntityMetadata[eMetadata.LogicalName] = eMetadata.Copy();
                }
                else
                {
                    EntityMetadata.Add(eMetadata.LogicalName, eMetadata.Copy());
                }
                
                // Persist metadata to standard Dataverse tables
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata
                PersistMetadataToTables(eMetadata);
            }
        }

        public void InitializeMetadata(EntityMetadata entityMetadata)
        {
            this.InitializeMetadata(new List<EntityMetadata>() { entityMetadata });
        }

        public void InitializeMetadata(Assembly earlyBoundEntitiesAssembly)
        {
            IEnumerable<EntityMetadata> entityMetadatas = MetadataGenerator.FromEarlyBoundEntities(earlyBoundEntitiesAssembly);
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
        }

        /// <summary>
        /// Initialize metadata from a CDM (Common Data Model) JSON file.
        /// Reference: https://github.com/microsoft/CDM
        /// 
        /// CDM is Microsoft's standard schema definition format that provides a shared data language
        /// across business applications and data sources. This method allows initializing entity metadata
        /// from CDM JSON files exported from Power Platform, Dynamics 365, or other CDM-compliant systems.
        /// </summary>
        /// <param name="cdmJsonFilePath">Path to the CDM JSON file</param>
        public void InitializeMetadataFromCdmFile(string cdmJsonFilePath)
        {
            IEnumerable<EntityMetadata> entityMetadatas = MetadataGenerator.FromCdmJsonFile(cdmJsonFilePath);
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
        }

        /// <summary>
        /// Initialize metadata from multiple CDM JSON files.
        /// </summary>
        /// <param name="cdmJsonFilePaths">Collection of paths to CDM JSON files</param>
        public void InitializeMetadataFromCdmFiles(IEnumerable<string> cdmJsonFilePaths)
        {
            IEnumerable<EntityMetadata> entityMetadatas = MetadataGenerator.FromCdmJsonFiles(cdmJsonFilePaths);
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
        }

        /// <summary>
        /// Initialize metadata from standard CDM schema groups by downloading them from Microsoft's CDM repository.
        /// Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        /// 
        /// This method downloads standard schema groups (crmcommon, sales, service, portals, customerInsights) directly from
        /// Microsoft's official CDM repository on GitHub. It follows the imports in each schema file to recursively
        /// load all dependent entity definitions. This is useful for quickly setting up tests with standard 
        /// Dynamics 365/Dataverse entities without needing local CDM files.
        /// 
        /// Available standard schemas: crmcommon, sales, service, portals, customerInsights
        /// </summary>
        /// <param name="schemaNames">Names of standard schemas to load (e.g., "crmcommon", "sales")</param>
        public async Task InitializeMetadataFromStandardCdmSchemasAsync(IEnumerable<string> schemaNames)
        {
            IEnumerable<EntityMetadata> entityMetadatas = await MetadataGenerator.FromStandardCdmSchemasAsync(schemaNames);
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
        }
        
        /// <summary>
        /// Initializes entity metadata from standard CDM entities by downloading them from Microsoft's CDM repository.
        /// Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        /// 
        /// This method downloads specific standard entities (account, contact, lead, etc.) directly from
        /// Microsoft's official CDM repository on GitHub. Useful for tests or when only specific entities are needed.
        /// 
        /// Available standard entities: account, contact, lead, opportunity, quote, order, invoice, incident (case)
        /// </summary>
        /// <param name="entityNames">Names of standard entities to load (e.g., "account", "contact", "lead")</param>
        public async Task InitializeMetadataFromStandardCdmEntitiesAsync(IEnumerable<string> entityNames)
        {
            IEnumerable<EntityMetadata> entityMetadatas = await MetadataGenerator.FromStandardCdmEntitiesAsync(entityNames);
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
        }

        /// <summary>
        /// Initialize system entity metadata from embedded CDM resources.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/about-entity-reference
        /// 
        /// This method loads system entity metadata from embedded CDM schema files in the Fake4Dataverse.Core assembly. 
        /// These system entities are required for Model-Driven App functionality, solution management, and metadata persistence in tests.
        /// 
        /// System entities included:
        /// - Metadata virtual tables: entity, attribute, relationship, optionset, entitykey
        /// - Solution entities: solution, solutioncomponent, componentdefinition
        /// - Solution-aware entities: appmodule, sitemap, savedquery, systemform, webresource, appmodulecomponent
        /// 
        /// The metadata virtual tables enable querying metadata as data, matching real Dataverse behavior.
        /// Note: The entity table is accessed via REST API as EntityDefinition.
        /// Solution-aware entities automatically get special columns (solutionid, overwritetime, componentstate, ismanaged).
        /// See metadata-persistence.md for details.
        /// </summary>
        public void InitializeSystemEntityMetadata()
        {
            IEnumerable<EntityMetadata> entityMetadatas = MetadataGenerator.FromEmbeddedSystemEntities();
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
            }
            
            // Initialize componentdefinition with default solution-aware entities
            // This marks system entities as solution-aware and adds the required columns
            SolutionAwareManager.InitializeComponentDefinitions(this);
            
            // Update entity metadata to include solution-aware columns
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
            foreach (var entityName in new[] { "systemform", "savedquery", "webresource", "sitemap", "appmodule", "appmodulecomponent" })
            {
                var entityMetadata = GetEntityMetadataByName(entityName);
                if (entityMetadata != null)
                {
                    SolutionAwareManager.EnsureSolutionAwareColumns(entityMetadata, this);
                    SetEntityMetadata(entityMetadata);
                }
            }
        }

        public IQueryable<EntityMetadata> CreateMetadataQuery()
        {
            return this.EntityMetadata.Values
                    .Select(em => em.Copy())
                    .ToList()
                    .AsQueryable();
        }

        public EntityMetadata GetEntityMetadataByName(string sLogicalName)
        {
            if (EntityMetadata.ContainsKey(sLogicalName))
                return EntityMetadata[sLogicalName].Copy();

            return null;
        }

        public void SetEntityMetadata(EntityMetadata em)
        {
            if (this.EntityMetadata.ContainsKey(em.LogicalName))
                this.EntityMetadata[em.LogicalName] = em.Copy();
            else
                this.EntityMetadata.Add(em.LogicalName, em.Copy());
            
            // Persist metadata to standard Dataverse tables
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata
            PersistMetadataToTables(em);
        }

        public AttributeMetadata GetAttributeMetadataFor(string sEntityName, string sAttributeName, Type attributeType)
        {
            if (EntityMetadata.ContainsKey(sEntityName))
            {
                var entityMetadata = GetEntityMetadataByName(sEntityName);
                var attribute = entityMetadata.Attributes
                                .Where(a => a.LogicalName.Equals(sAttributeName))
                                .FirstOrDefault();

                if (attribute != null)
                    return attribute;
            }

            if (attributeType == typeof(string))
            {
                return new StringAttributeMetadata(sAttributeName);
            }
            //Default
            return new StringAttributeMetadata(sAttributeName);
        }
        
        /// <summary>
        /// Persists entity metadata to standard Dataverse metadata tables (EntityDefinition and Attribute).
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata
        /// 
        /// In Dataverse, metadata is accessible through special virtual entities:
        /// - EntityDefinition (entitydefinition) - Contains entity metadata
        /// - Attribute (attribute) - Contains attribute metadata
        /// - Relationship (relationship) - Contains relationship metadata
        /// - OptionSet (optionset) - Contains optionset metadata
        /// - EntityKey (entitykey) - Contains entity key metadata
        /// 
        /// This method stores metadata in these tables so it can be queried like regular entity data.
        /// The metadata tables are always initialized in the constructor, so they should always be available.
        /// </summary>
        private void PersistMetadataToTables(EntityMetadata metadata)
        {
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.LogicalName))
                return;
            
            // Don't try to persist the metadata tables themselves to avoid circular dependency
            // Support both old names (entity, optionset, entitykey) and new names (entitydefinition, optionsetdefinition, entitykeydefinition)
            var metadataTables = new[] { "entity", "entitydefinition", "attribute", "relationship", "optionset", "optionsetdefinition", "entitykey", "entitykeydefinition", "relationshipdefinition" };
            if (metadataTables.Contains(metadata.LogicalName))
                return;
            
            // Metadata tables should always be present (initialized in constructor)
            // Support both "entitydefinition" (new name) and "entity" (old alias) for backward compatibility
            if (!this.EntityMetadata.ContainsKey("entitydefinition") && !this.EntityMetadata.ContainsKey("entity"))
            {
                throw new InvalidOperationException(
                    "EntityDefinition metadata table (entitydefinition or entity) is not initialized. " +
                    "System entity metadata should be automatically loaded in the constructor.");
            }
            
            if (!this.EntityMetadata.ContainsKey("attribute"))
            {
                throw new InvalidOperationException(
                    "Attribute metadata table is not initialized. " +
                    "System entity metadata should be automatically loaded in the constructor.");
            }
            
            // Convert EntityMetadata to EntityDefinition record
            // Always store in "entitydefinition" table (prefer new name over old alias "entity")
            var entityTableName = "entitydefinition";
            var entityDefRecord = MetadataPersistenceManager.EntityMetadataToEntityDefinition(metadata);
            // Update the entity record's logical name to match the table it's being stored in
            entityDefRecord.LogicalName = entityTableName;
            
            // Check if EntityDefinition record already exists
            var existingEntityDef = this.Data.ContainsKey(entityTableName) 
                ? this.Data[entityTableName].Values.FirstOrDefault(e => 
                    e.GetAttributeValue<string>("logicalname") == metadata.LogicalName)
                : null;
            
            if (existingEntityDef != null)
            {
                // Update existing record
                entityDefRecord.Id = existingEntityDef.Id;
                this.Data[entityTableName][existingEntityDef.Id] = entityDefRecord;
            }
            else
            {
                // Create new record
                if (!this.Data.ContainsKey(entityTableName))
                    this.Data[entityTableName] = new Dictionary<Guid, Entity>();
                
                this.Data[entityTableName][entityDefRecord.Id] = entityDefRecord;
            }
            
            // Persist attributes if they exist
            if (metadata.Attributes != null && metadata.Attributes.Length > 0)
            {
                foreach (var attrMetadata in metadata.Attributes)
                {
                    var attrRecord = MetadataPersistenceManager.AttributeMetadataToAttribute(attrMetadata, metadata.LogicalName);
                    
                    // Check if Attribute record already exists
                    var existingAttr = this.Data.ContainsKey("attribute")
                        ? this.Data["attribute"].Values.FirstOrDefault(a =>
                            a.GetAttributeValue<string>("entitylogicalname") == metadata.LogicalName &&
                            a.GetAttributeValue<string>("logicalname") == attrMetadata.LogicalName)
                        : null;
                    
                    if (existingAttr != null)
                    {
                        // Update existing record
                        attrRecord.Id = existingAttr.Id;
                        this.Data["attribute"][existingAttr.Id] = attrRecord;
                    }
                    else
                    {
                        // Create new record
                        if (!this.Data.ContainsKey("attribute"))
                            this.Data["attribute"] = new Dictionary<Guid, Entity>();
                        
                        this.Data["attribute"][attrRecord.Id] = attrRecord;
                    }
                }
            }
        }

    }
}