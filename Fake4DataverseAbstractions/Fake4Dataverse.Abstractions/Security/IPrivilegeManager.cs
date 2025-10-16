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
        /// </summary>
        bool HasPrivilege(Guid userId, string privilegeName, int requiredDepth);

        /// <summary>
        /// Grants all standard privileges to the System Administrator role.
        /// </summary>
        void GrantAllPrivilegesToSystemAdministrator();
    }
}
