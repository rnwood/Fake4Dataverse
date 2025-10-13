using Fake4Dataverse.Abstractions.Audit;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.Audit
{
    /// <summary>
    /// Implementation of audit repository for Fake4Dataverse
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
    /// </summary>
    public class AuditRepository : IAuditRepository
    {
        private readonly List<Entity> _auditRecords;
        private readonly Dictionary<Guid, object> _auditDetails;

        public bool IsAuditEnabled { get; set; }

        public AuditRepository()
        {
            _auditRecords = new List<Entity>();
            _auditDetails = new Dictionary<Guid, object>();
            IsAuditEnabled = false; // Disabled by default to match Dataverse behavior
        }

        /// <summary>
        /// Creates an audit record for an operation
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
        /// 
        /// In Dataverse, audit records contain:
        /// - auditid: Unique identifier for the audit record
        /// - action: The type of operation (Create=1, Update=2, Delete=3, etc.)
        /// - operation: The operation name (Create, Update, Delete, etc.)
        /// - objectid: EntityReference to the audited record
        /// - objecttypecode: Entity logical name
        /// - userid: User who performed the operation
        /// - createdon: Timestamp when the audit was created
        /// </summary>
        public Entity CreateAuditRecord(
            int action,
            string operation,
            EntityReference objectId,
            Guid userId,
            Dictionary<string, (object oldValue, object newValue)> attributeChanges = null)
        {
            if (!IsAuditEnabled)
            {
                return null;
            }

            var auditId = Guid.NewGuid();
            var auditRecord = new Entity("audit", auditId);

            auditRecord["auditid"] = auditId;
            auditRecord["action"] = action;
            auditRecord["operation"] = operation;
            auditRecord["objectid"] = objectId;
            auditRecord["objecttypecode"] = objectId.LogicalName;
            auditRecord["userid"] = new EntityReference("systemuser", userId);
            auditRecord["createdon"] = DateTime.UtcNow;

            _auditRecords.Add(auditRecord);

            // Store audit details if there are attribute changes
            if (attributeChanges != null && attributeChanges.Any())
            {
                var auditDetail = new AttributeAuditDetail
                {
                    AuditRecord = auditRecord,
                    OldValues = new Entity(objectId.LogicalName, objectId.Id),
                    NewValues = new Entity(objectId.LogicalName, objectId.Id)
                };

                foreach (var change in attributeChanges)
                {
                    auditDetail.OldValues[change.Key] = change.Value.oldValue;
                    auditDetail.NewValues[change.Key] = change.Value.newValue;
                }

                _auditDetails[auditId] = auditDetail;
            }

            return auditRecord;
        }

        /// <summary>
        /// Gets audit records for a specific entity
        /// </summary>
        public IEnumerable<Entity> GetAuditRecordsForEntity(EntityReference objectId)
        {
            return _auditRecords
                .Where(a => 
                {
                    var objRef = a.GetAttributeValue<EntityReference>("objectid");
                    return objRef != null && 
                           objRef.LogicalName == objectId.LogicalName && 
                           objRef.Id == objectId.Id;
                })
                .OrderBy(a => a.GetAttributeValue<DateTime>("createdon"))
                .ToList();
        }

        /// <summary>
        /// Gets audit records for a specific attribute
        /// </summary>
        public IEnumerable<Entity> GetAuditRecordsForAttribute(EntityReference objectId, string attributeName)
        {
            var entityAudits = GetAuditRecordsForEntity(objectId);

            return entityAudits.Where(audit =>
            {
                var auditId = audit.GetAttributeValue<Guid>("auditid");
                if (_auditDetails.TryGetValue(auditId, out var detail))
                {
                    if (detail is AttributeAuditDetail attrDetail)
                    {
                        return attrDetail.OldValues.Contains(attributeName) || 
                               attrDetail.NewValues.Contains(attributeName);
                    }
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Gets audit details for a specific audit record
        /// </summary>
        public object GetAuditDetails(Guid auditId)
        {
            if (_auditDetails.TryGetValue(auditId, out var detail))
            {
                return detail;
            }
            return null;
        }

        /// <summary>
        /// Gets all audit records
        /// </summary>
        public IEnumerable<Entity> GetAllAuditRecords()
        {
            return _auditRecords.OrderBy(a => a.GetAttributeValue<DateTime>("createdon")).ToList();
        }

        /// <summary>
        /// Clears all audit data
        /// </summary>
        public void ClearAuditData()
        {
            _auditRecords.Clear();
            _auditDetails.Clear();
        }
    }

    /// <summary>
    /// Represents attribute audit details
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.attributeauditdetail
    /// 
    /// AttributeAuditDetail contains the old and new values for attributes that changed during an operation.
    /// This allows tracking what specific values changed from one update to another.
    /// </summary>
    public class AttributeAuditDetail
    {
        /// <summary>
        /// The audit record this detail belongs to
        /// </summary>
        public Entity AuditRecord { get; set; }

        /// <summary>
        /// Entity containing the old attribute values before the change
        /// </summary>
        public Entity OldValues { get; set; }

        /// <summary>
        /// Entity containing the new attribute values after the change
        /// </summary>
        public Entity NewValues { get; set; }
    }
}
