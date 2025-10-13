using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Microsoft.Crm.Sdk.Messages;
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
        private readonly Dictionary<Guid, AuditDetail> _auditDetails;
        private readonly IXrmFakedContext _context;

        public bool IsAuditEnabled { get; set; }

        public AuditRepository(IXrmFakedContext context)
        {
            _auditRecords = new List<Entity>();
            _auditDetails = new Dictionary<Guid, AuditDetail>();
            _context = context;
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

            // Store audit details
            // For Create operations, store the new entity values
            // For Update operations, store old and new values if there are changes
            // For Delete operations, no detail needed
            if (action == AuditAction.Create)
            {
                var auditDetail = new AttributeAuditDetail();
                auditDetail.AuditRecord = auditRecord;
                auditDetail.OldValue = new Entity(objectId.LogicalName, objectId.Id);
                auditDetail.NewValue = new Entity(objectId.LogicalName, objectId.Id);
                _auditDetails[auditId] = auditDetail;
            }
            else if (attributeChanges != null && attributeChanges.Any())
            {
                var auditDetail = new AttributeAuditDetail();
                auditDetail.AuditRecord = auditRecord;
                auditDetail.OldValue = new Entity(objectId.LogicalName, objectId.Id);
                auditDetail.NewValue = new Entity(objectId.LogicalName, objectId.Id);

                foreach (var change in attributeChanges)
                {
                    auditDetail.OldValue[change.Key] = change.Value.oldValue;
                    auditDetail.NewValue[change.Key] = change.Value.newValue;
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
                        return attrDetail.OldValue.Contains(attributeName) || 
                               attrDetail.NewValue.Contains(attributeName);
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
}
