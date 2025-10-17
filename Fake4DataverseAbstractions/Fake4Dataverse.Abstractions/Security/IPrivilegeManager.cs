using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Abstractions.Security
{
    /// <summary>
    /// Manages privileges and their assignment to roles.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
    /// </summary>
    public interface IPrivilegeManager
    {
        /// <summary>
        /// Creates standard CRUD privileges for an entity.
        /// </summary>
        void CreateStandardPrivilegesForEntity(string entityLogicalName);

        /// <summary>
        /// Checks if a user has a specific privilege through their assigned roles.
        /// Does not consider business unit context - use HasPrivilegeForRecord for BU-aware checking.
        /// </summary>
        bool HasPrivilege(Guid userId, string privilegeName, int requiredDepth);

        /// <summary>
        /// Checks if a user has a specific privilege for a record, considering business unit context.
        /// This is the correct method for privilege checking in Dataverse security model.
        /// A user may have roles from multiple business units (directly or through teams),
        /// and the privilege depth is evaluated relative to each role's business unit.
        /// </summary>
        bool HasPrivilegeForRecord(Guid userId, string privilegeName, Entity record, int baseDepth);

        /// <summary>
        /// Grants all standard privileges to the System Administrator role.
        /// </summary>
        void GrantAllPrivilegesToSystemAdministrator();
    }
}
