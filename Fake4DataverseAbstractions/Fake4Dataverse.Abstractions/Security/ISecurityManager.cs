using System;

namespace Fake4Dataverse.Abstractions.Security
{
    /// <summary>
    /// Provides access to security infrastructure and management.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
    /// </summary>
    public interface ISecurityManager
    {
        /// <summary>
        /// Gets the root organization ID. Creates the organization if it doesn't exist.
        /// The ID varies per context instance to avoid hardcoded GUID bugs.
        /// </summary>
        Guid RootOrganizationId { get; }

        /// <summary>
        /// Gets the root business unit ID. Creates the business unit if it doesn't exist.
        /// The ID varies per context instance to avoid hardcoded GUID bugs.
        /// </summary>
        Guid RootBusinessUnitId { get; }

        /// <summary>
        /// Gets the System Administrator role ID. Creates the role if it doesn't exist.
        /// The ID varies per context instance to avoid hardcoded GUID bugs.
        /// </summary>
        Guid SystemAdministratorRoleId { get; }

        /// <summary>
        /// Initializes the default System Administrator role with all privileges.
        /// This is automatically called during context initialization if AutoGrantSystemAdministratorPrivileges is true.
        /// </summary>
        void InitializeSystemAdministratorRole();

        /// <summary>
        /// Checks if a user has the System Administrator role assigned.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user has the System Administrator role</returns>
        bool IsSystemAdministrator(Guid userId);

        /// <summary>
        /// Gets all role IDs assigned to a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Array of role IDs assigned to the user</returns>
        Guid[] GetUserRoles(Guid userId);

        /// <summary>
        /// Gets all role IDs assigned to a team.
        /// </summary>
        /// <param name="teamId">The team ID</param>
        /// <returns>Array of role IDs assigned to the team</returns>
        Guid[] GetTeamRoles(Guid teamId);

        /// <summary>
        /// Gets the privilege manager for managing privileges and role assignments.
        /// </summary>
        IPrivilegeManager PrivilegeManager { get; }

        /// <summary>
        /// Gets the role lifecycle manager for managing role shadow copies and business unit lifecycle.
        /// </summary>
        IRoleLifecycleManager RoleLifecycleManager { get; }
    }
}
