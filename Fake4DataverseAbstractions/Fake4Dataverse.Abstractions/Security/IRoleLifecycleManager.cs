using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Abstractions.Security
{
    /// <summary>
    /// Manages the lifecycle of roles and their shadow copies across business units.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
    /// </summary>
    public interface IRoleLifecycleManager
    {
        /// <summary>
        /// Called when a new role is created.
        /// Creates shadow copies for all existing business units.
        /// </summary>
        void OnRoleCreated(Entity role);

        /// <summary>
        /// Called when a business unit is created.
        /// Creates shadow copies of all root roles for this business unit.
        /// </summary>
        void OnBusinessUnitCreated(Entity businessUnit);

        /// <summary>
        /// Called when a business unit is deleted.
        /// Deletes all shadow roles for this business unit.
        /// </summary>
        void OnBusinessUnitDeleted(Guid businessUnitId);

        /// <summary>
        /// Called when a root role is deleted.
        /// Deletes all shadow copies of this role.
        /// </summary>
        void OnRoleDeleted(Guid roleId);

        /// <summary>
        /// Called when a user/team's business unit is updated.
        /// Removes all role assignments as they are no longer valid.
        /// </summary>
        void OnUserTeamBusinessUnitChanged(string entityName, Guid entityId);

        /// <summary>
        /// Validates that a role can be assigned to a user/team.
        /// Role can only be assigned to the same BU unless modern BUs feature is on.
        /// </summary>
        void ValidateRoleAssignment(Guid roleId, string principalType, Guid principalId);
    }
}
