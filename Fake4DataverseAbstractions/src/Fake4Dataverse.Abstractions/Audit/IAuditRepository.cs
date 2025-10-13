using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.Audit
{
    /// <summary>
    /// Repository for managing audit records in Fake4Dataverse
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
    /// 
    /// In Dataverse, auditing tracks changes to records over time, including:
    /// - Create, Update, Delete operations
    /// - Attribute value changes (old and new values)
    /// - User access to records
    /// - Metadata changes
    /// - Security operations (Assign, Share, etc.)
    /// </summary>
    public interface IAuditRepository
    {
        /// <summary>
        /// Records an audit entry for an operation
        /// </summary>
        /// <param name="action">Audit action (Create=1, Update=2, Delete=3, Access=64, etc.)</param>
        /// <param name="operation">Operation name</param>
        /// <param name="objectId">Entity reference to the audited record</param>
        /// <param name="userId">User who performed the operation</param>
        /// <param name="attributeChanges">Dictionary of attribute changes (attribute name -> old/new value pair)</param>
        /// <returns>The created audit record</returns>
        Entity CreateAuditRecord(
            int action,
            string operation,
            EntityReference objectId,
            Guid userId,
            Dictionary<string, (object oldValue, object newValue)> attributeChanges = null);

        /// <summary>
        /// Retrieves audit records for a specific entity
        /// </summary>
        /// <param name="objectId">Entity reference to get audit history for</param>
        /// <returns>List of audit records ordered by creation date</returns>
        IEnumerable<Entity> GetAuditRecordsForEntity(EntityReference objectId);

        /// <summary>
        /// Retrieves audit records for a specific attribute of an entity
        /// </summary>
        /// <param name="objectId">Entity reference to get audit history for</param>
        /// <param name="attributeName">Attribute name to filter by</param>
        /// <returns>List of audit records for the specific attribute</returns>
        IEnumerable<Entity> GetAuditRecordsForAttribute(EntityReference objectId, string attributeName);

        /// <summary>
        /// Retrieves audit details for a specific audit record
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest
        /// 
        /// AuditDetails contain the specific changes made in an audit record, including:
        /// - AttributeAuditDetail: Old and new values for changed attributes
        /// - RelationshipAuditDetail: Association/disassociation changes
        /// - UserAccessAuditDetail: User access information
        /// </summary>
        /// <param name="auditId">ID of the audit record</param>
        /// <returns>AuditDetail object with change information</returns>
        object GetAuditDetails(Guid auditId);

        /// <summary>
        /// Gets all audit records in the repository
        /// </summary>
        /// <returns>All audit records</returns>
        IEnumerable<Entity> GetAllAuditRecords();

        /// <summary>
        /// Clears all audit records (for testing purposes)
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.deleteauditdatarequest
        /// 
        /// In Dataverse, DeleteAuditDataRequest deletes audit records for a specified date range.
        /// This method clears all audit data for testing scenarios.
        /// </summary>
        void ClearAuditData();

        /// <summary>
        /// Gets or sets whether auditing is enabled at the organization level.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
        /// 
        /// In Dataverse, this is the global audit setting (IsAuditEnabled on Organization entity).
        /// When false, no auditing occurs regardless of entity or attribute settings.
        /// When true, entity and attribute level settings are also checked.
        /// </summary>
        bool IsAuditEnabled { get; set; }
    }
}
