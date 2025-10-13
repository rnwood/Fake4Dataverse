namespace Fake4Dataverse.Abstractions.Audit
{
    /// <summary>
    /// Represents the audit action types in Dataverse
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
    /// 
    /// Audit actions represent the types of operations that are tracked in the audit log.
    /// These values correspond to the 'action' field in the audit entity.
    /// </summary>
    public static class AuditAction
    {
        /// <summary>
        /// Create operation - A new record was created
        /// </summary>
        public const int Create = 1;

        /// <summary>
        /// Update operation - An existing record was modified
        /// </summary>
        public const int Update = 2;

        /// <summary>
        /// Delete operation - A record was deleted
        /// </summary>
        public const int Delete = 3;

        /// <summary>
        /// Access operation - A user accessed a record (user access auditing)
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure#user-access-auditing
        /// </summary>
        public const int Access = 64;

        /// <summary>
        /// Assign operation - Record ownership was transferred
        /// </summary>
        public const int Assign = 101;

        /// <summary>
        /// Share operation - Record was shared with a user or team
        /// </summary>
        public const int Share = 102;

        /// <summary>
        /// Unshare operation - Record sharing was revoked
        /// </summary>
        public const int Unshare = 103;

        /// <summary>
        /// Merge operation - Records were merged
        /// </summary>
        public const int Merge = 104;
    }
}
