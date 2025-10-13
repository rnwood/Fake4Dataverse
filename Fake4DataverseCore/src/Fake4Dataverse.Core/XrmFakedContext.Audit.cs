using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Audit;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse
{
    /// <summary>
    /// Audit-related functionality for XrmFakedContext
    /// </summary>
    public partial class XrmFakedContext : IXrmFakedContext
    {
        /// <summary>
        /// Initializes audit repository
        /// This is called from the main constructor
        /// </summary>
        private void InitializeAuditRepository()
        {
            SetProperty<IAuditRepository>(new AuditRepository(this));
        }

        /// <summary>
        /// Gets the audit repository
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
        /// 
        /// The audit repository stores audit records created during CRUD operations when auditing is enabled.
        /// Use this property to:
        /// - Enable/disable auditing: context.GetProperty&lt;IAuditRepository&gt;().IsAuditEnabled = true
        /// - Query audit records: context.GetProperty&lt;IAuditRepository&gt;().GetAuditRecordsForEntity()
        /// - Clear audit data: context.GetProperty&lt;IAuditRepository&gt;().ClearAuditData()
        /// </summary>
        public IAuditRepository AuditRepository
        {
            get
            {
                if (!HasProperty<IAuditRepository>())
                {
                    InitializeAuditRepository();
                }
                return GetProperty<IAuditRepository>();
            }
        }

        /// <summary>
        /// Checks if auditing should occur for a specific entity and its attributes
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
        /// 
        /// In Dataverse, auditing requires:
        /// 1. Organization-level IsAuditEnabled = true
        /// 2. Entity-level IsAuditEnabled = true (from EntityMetadata)
        /// 3. Attribute-level IsAuditEnabled = true (from AttributeMetadata) for attribute changes
        /// </summary>
        private bool ShouldAuditEntity(string entityLogicalName)
        {
            var auditRepository = GetProperty<IAuditRepository>();
            if (auditRepository?.IsAuditEnabled != true)
            {
                return false;
            }

            // Check entity-level audit setting
            if (EntityMetadata.ContainsKey(entityLogicalName))
            {
                var entityMetadata = EntityMetadata[entityLogicalName];
                // IsAuditEnabled is a nullable bool, so we check for true explicitly
                if (entityMetadata.IsAuditEnabled?.Value != true)
                {
                    return false;
                }
            }
            // If no metadata exists, we allow auditing (matches behavior for dynamic entities)

            return true;
        }

        /// <summary>
        /// Filters attribute changes to only include audited attributes
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
        /// </summary>
        private Dictionary<string, (object oldValue, object newValue)> FilterAuditedAttributes(
            string entityLogicalName,
            Dictionary<string, (object oldValue, object newValue)> attributeChanges)
        {
            if (attributeChanges == null || !attributeChanges.Any())
            {
                return attributeChanges;
            }

            // If no metadata exists, audit all attributes
            if (!EntityMetadata.ContainsKey(entityLogicalName))
            {
                return attributeChanges;
            }

            var entityMetadata = EntityMetadata[entityLogicalName];
            var filteredChanges = new Dictionary<string, (object oldValue, object newValue)>();

            foreach (var change in attributeChanges)
            {
                var attributeMetadata = entityMetadata.Attributes?
                    .FirstOrDefault(a => a.LogicalName.Equals(change.Key, StringComparison.OrdinalIgnoreCase));

                // If attribute metadata exists, check its audit setting
                if (attributeMetadata != null)
                {
                    // IsAuditEnabled is a BooleanManagedProperty
                    if (attributeMetadata.IsAuditEnabled?.Value == true)
                    {
                        filteredChanges[change.Key] = change.Value;
                    }
                }
                else
                {
                    // If no attribute metadata, include the change
                    filteredChanges[change.Key] = change.Value;
                }
            }

            return filteredChanges;
        }

        /// <summary>
        /// Records an audit entry for a Create operation
        /// </summary>
        internal void RecordCreateAudit(Entity entity)
        {
            if (!ShouldAuditEntity(entity.LogicalName))
            {
                return;
            }

            var auditRepository = GetProperty<IAuditRepository>();
            var userId = CallerProperties?.CallerId?.Id ?? Guid.Empty;
            var objectRef = new EntityReference(entity.LogicalName, entity.Id);
            
            auditRepository.CreateAuditRecord(
                AuditAction.Create,
                "Create",
                objectRef,
                userId);
        }

        /// <summary>
        /// Records an audit entry for an Update operation
        /// </summary>
        internal void RecordUpdateAudit(Entity oldEntity, Entity newEntity)
        {
            if (!ShouldAuditEntity(newEntity.LogicalName))
            {
                return;
            }

            var auditRepository = GetProperty<IAuditRepository>();
            var userId = CallerProperties?.CallerId?.Id ?? Guid.Empty;
            var objectRef = new EntityReference(newEntity.LogicalName, newEntity.Id);

            // Track attribute changes
            var attributeChanges = new Dictionary<string, (object oldValue, object newValue)>();
            
            foreach (var attr in newEntity.Attributes.Keys)
            {
                var newValue = newEntity.Attributes[attr];
                var oldValue = oldEntity.Contains(attr) ? oldEntity.Attributes[attr] : null;
                
                // Only track if value changed
                if (!AreValuesEqual(oldValue, newValue))
                {
                    attributeChanges[attr] = (oldValue, newValue);
                }
            }

            // Filter to only audited attributes
            var auditedChanges = FilterAuditedAttributes(newEntity.LogicalName, attributeChanges);

            if (auditedChanges.Any())
            {
                auditRepository.CreateAuditRecord(
                    AuditAction.Update,
                    "Update",
                    objectRef,
                    userId,
                    auditedChanges);
            }
        }

        /// <summary>
        /// Records an audit entry for a Delete operation
        /// </summary>
        internal void RecordDeleteAudit(EntityReference entityRef)
        {
            if (!ShouldAuditEntity(entityRef.LogicalName))
            {
                return;
            }

            var auditRepository = GetProperty<IAuditRepository>();
            var userId = CallerProperties?.CallerId?.Id ?? Guid.Empty;
            
            auditRepository.CreateAuditRecord(
                AuditAction.Delete,
                "Delete",
                entityRef,
                userId);
        }

        /// <summary>
        /// Helper method to compare attribute values
        /// </summary>
        private bool AreValuesEqual(object val1, object val2)
        {
            if (val1 == null && val2 == null) return true;
            if (val1 == null || val2 == null) return false;
            
            // Handle EntityReference comparison
            if (val1 is EntityReference ref1 && val2 is EntityReference ref2)
            {
                return ref1.LogicalName == ref2.LogicalName && ref1.Id == ref2.Id;
            }
            
            // Handle OptionSetValue comparison
            if (val1 is OptionSetValue opt1 && val2 is OptionSetValue opt2)
            {
                return opt1.Value == opt2.Value;
            }
            
            // Handle Money comparison
            if (val1 is Money money1 && val2 is Money money2)
            {
                return money1.Value == money2.Value;
            }
            
            return val1.Equals(val2);
        }
    }
}
