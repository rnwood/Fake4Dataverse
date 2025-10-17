using System;
using Microsoft.Xrm.Sdk;
using Fake4Dataverse.Extensions;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;

namespace Fake4Dataverse.Services
{
    public enum EntityInitializationLevel
    {
        Default = 0,  //Minimal initialization of common attributes
        PerEntity = 1 //More detailed initialization of entities, on an entity per entity basis
    }
    public class DefaultEntityInitializerService : IEntityInitializerService
    {
        public Dictionary<string, IEntityInitializerService> InitializerServiceDictionary { get; set; }
        
        // Shared auto number service for all entities
        private static readonly AutoNumberFormatService _autoNumberService = new AutoNumberFormatService();

        public DefaultEntityInitializerService()
        {
            InitializerServiceDictionary = new Dictionary<string, IEntityInitializerService>()
            {
                { InvoiceDetailInitializerService.EntityLogicalName, new InvoiceDetailInitializerService() },
                { InvoiceInitializerService.EntityLogicalName, new InvoiceInitializerService() }
            };
        }

        public Entity Initialize(Entity e, Guid gCallerId, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false)
        {
            //Validate primary key for dynamic entities
            var primaryKey = string.Format("{0}id", e.LogicalName);
            if (!e.Attributes.ContainsKey(primaryKey))
            {
                e[primaryKey] = e.Id;
            }

            if (isManyToManyRelationshipEntity)
            {
                return e;
            }

            var CallerId = new EntityReference("systemuser", gCallerId); //Create a new instance by default
            
            // Handle impersonation for audit fields
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // When impersonating:
            // - createdby/modifiedby = impersonated user
            // - createdonbehalfof/modifiedonbehalfof = actual calling user
            var callerProperties = ctx.CallerProperties as CallerProperties;
            var effectiveUser = callerProperties?.GetEffectiveUser() ?? CallerId;
            var isImpersonating = callerProperties?.ImpersonatedUserId != null;

            var now = DateTime.UtcNow;

            e.SetValueIfEmpty("createdon", now);

            //Overriden created on should replace created on
            if (e.Contains("overriddencreatedon"))
            {
                e["createdon"] = e["overriddencreatedon"];
            }

            e.SetValueIfEmpty("modifiedon", now);
            e.SetValueIfEmpty("createdby", effectiveUser);
            e.SetValueIfEmpty("modifiedby", effectiveUser);
            e.SetValueIfEmpty("ownerid", effectiveUser);
            
            // Set createdonbehalfof when impersonating
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // The createdonbehalfof field stores the actual user who initiated the operation (the impersonator)
            if (isImpersonating && callerProperties?.CallerId != null)
            {
                e.SetValueIfEmpty("createdonbehalfof", callerProperties.CallerId);
                e.SetValueIfEmpty("modifiedonbehalfof", callerProperties.CallerId);
            }
            
            e.SetValueIfEmpty("statecode", new OptionSetValue(0)); //Active by default

            // Process auto number fields
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Auto number fields automatically generate values when a record is created
            ProcessAutoNumberFields(e, ctx);

            if (ctx.InitializationLevel == EntityInitializationLevel.PerEntity)
            {
                if (!string.IsNullOrEmpty(e.LogicalName) && InitializerServiceDictionary.ContainsKey(e.LogicalName))
                    InitializerServiceDictionary[e.LogicalName].Initialize(e, gCallerId, ctx, isManyToManyRelationshipEntity);
            }

            return e;
        }

        public Entity Initialize(Entity e, XrmFakedContext ctx, bool isManyToManyRelationshipEntity = false)
        {
            return this.Initialize(e, Guid.NewGuid(), ctx, isManyToManyRelationshipEntity);
        }

        /// <summary>
        /// Processes auto number fields for an entity based on metadata.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
        /// Auto number fields are string attributes with an AutoNumberFormat property that defines the pattern.
        /// </summary>
        private void ProcessAutoNumberFields(Entity e, XrmFakedContext ctx)
        {
            // Get entity metadata
            var entityMetadata = ctx.GetEntityMetadataByName(e.LogicalName);
            if (entityMetadata == null || entityMetadata.Attributes == null)
            {
                return;
            }

            // Find string attributes with AutoNumberFormat
            var autoNumberAttributes = entityMetadata.Attributes
                .OfType<StringAttributeMetadata>()
                .Where(attr => !string.IsNullOrEmpty(attr.AutoNumberFormat))
                .ToList();

            foreach (var attribute in autoNumberAttributes)
            {
                // Only generate if the attribute is not already set
                if (!e.Contains(attribute.LogicalName) || e[attribute.LogicalName] == null)
                {
                    var autoNumberValue = _autoNumberService.GenerateAutoNumber(
                        e.LogicalName,
                        attribute.LogicalName,
                        attribute.AutoNumberFormat);
                    
                    e[attribute.LogicalName] = autoNumberValue;
                }
            }
        }
    }
}