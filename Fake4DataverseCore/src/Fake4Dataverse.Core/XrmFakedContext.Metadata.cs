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
                    throw new Exception("An entity metadata record with the same logical name was previously added. ");
                }
                EntityMetadata.Add(eMetadata.LogicalName, eMetadata.Copy());
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
        /// This method loads system entity metadata (solution, appmodule, sitemap, savedquery, systemform, webresource, appmodulecomponent)
        /// from embedded CDM schema files in the Fake4Dataverse.Core assembly. These system entities are required for
        /// Model-Driven App functionality and solution management in tests.
        /// 
        /// System entities included: solution, appmodule, sitemap, savedquery, systemform, webresource, appmodulecomponent
        /// </summary>
        public void InitializeSystemEntityMetadata()
        {
            IEnumerable<EntityMetadata> entityMetadatas = MetadataGenerator.FromEmbeddedSystemEntities();
            if (entityMetadatas.Any())
            {
                this.InitializeMetadata(entityMetadatas);
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

    }
}