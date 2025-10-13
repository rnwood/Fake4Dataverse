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
            SetProperty<IAuditRepository>(new AuditRepository());
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
        /// Records an audit entry for a Create operation
        /// </summary>
        internal void RecordCreateAudit(Entity entity)
        {
            var auditRepository = GetProperty<IAuditRepository>();
            if (auditRepository?.IsAuditEnabled == true)
            {
                var userId = CallerProperties?.CallerId?.Id ?? Guid.Empty;
                var objectRef = new EntityReference(entity.LogicalName, entity.Id);
                
                auditRepository.CreateAuditRecord(
                    AuditAction.Create,
                    "Create",
                    objectRef,
                    userId);
            }
        }

        /// <summary>
        /// Records an audit entry for an Update operation
        /// </summary>
        internal void RecordUpdateAudit(Entity oldEntity, Entity newEntity)
        {
            var auditRepository = GetProperty<IAuditRepository>();
            if (auditRepository?.IsAuditEnabled == true)
            {
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

                if (attributeChanges.Any())
                {
                    auditRepository.CreateAuditRecord(
                        AuditAction.Update,
                        "Update",
                        objectRef,
                        userId,
                        attributeChanges);
                }
            }
        }

        /// <summary>
        /// Records an audit entry for a Delete operation
        /// </summary>
        internal void RecordDeleteAudit(EntityReference entityRef)
        {
            var auditRepository = GetProperty<IAuditRepository>();
            if (auditRepository?.IsAuditEnabled == true)
            {
                var userId = CallerProperties?.CallerId?.Id ?? Guid.Empty;
                
                auditRepository.CreateAuditRecord(
                    AuditAction.Delete,
                    "Delete",
                    entityRef,
                    userId);
            }
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
