using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Fake4Dataverse.Security
{
    /// <summary>
    /// Manages the lifecycle of roles and their shadow copies across business units.
    /// 
    /// In Dataverse, roles are granted at a per-BU level with shadow copies automatically
    /// maintained across the business unit hierarchy.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/create-edit-business-units
    /// 
    /// Key behaviors:
    /// - When a role is created, it becomes a "root" role (parentrootroleid points to itself)
    /// - Shadow copies are created for each business unit (parentroleid points to root)
    /// - Shadow copies cannot be modified or deleted directly
    /// - When a BU is created, shadow copies of all root roles are created
    /// - When a BU is deleted, shadow copies are automatically deleted
    /// - Users/teams can only be assigned roles from their own BU (unless modern BUs enabled)
    /// </summary>
    public class RoleLifecycleManager : IRoleLifecycleManager
    {
        private readonly IXrmFakedContext _context;

        public RoleLifecycleManager(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Called when a new role is created.
        /// Creates shadow copies for all existing business units.
        /// </summary>
        public void OnRoleCreated(Entity role)
        {
            if (role == null || role.LogicalName != "role")
            {
                throw new ArgumentException("Entity must be a role", nameof(role));
            }

            // Check if this is already a shadow copy (has parentroleid)
            if (role.Contains("parentroleid") && role.GetAttributeValue<EntityReference>("parentroleid") != null)
            {
                // This is a shadow copy, don't create more shadows
                return;
            }

            // This is a root/master role - set parentrootroleid to itself
            if (!role.Contains("parentrootroleid") || role.GetAttributeValue<EntityReference>("parentrootroleid") == null)
            {
                role["parentrootroleid"] = new EntityReference("role", role.Id);
                _context.UpdateEntity(role);
            }

            // Get the business unit this role belongs to
            var roleBU = role.GetAttributeValue<EntityReference>("businessunitid");
            if (roleBU == null)
            {
                return; // Role must have a business unit
            }

            // Get all business units except the one this role belongs to
            var allBusinessUnits = _context.CreateQuery("businessunit")
                .Where(bu => bu.Id != roleBU.Id)
                .ToArray();

            // Create shadow copies for each business unit
            foreach (var businessUnit in allBusinessUnits)
            {
                CreateShadowRole(role, businessUnit.Id);
            }
        }

        /// <summary>
        /// Called when a business unit is created.
        /// Creates shadow copies of all root roles for this business unit.
        /// </summary>
        public void OnBusinessUnitCreated(Entity businessUnit)
        {
            if (businessUnit == null || businessUnit.LogicalName != "businessunit")
            {
                throw new ArgumentException("Entity must be a businessunit", nameof(businessUnit));
            }

            // Get all root roles (where parentroleid is null or equals roletemplateid conceptually)
            // A root role has parentrootroleid pointing to itself
            var rootRoles = _context.CreateQuery("role")
                .ToArray()
                .Where(r => 
                {
                    var parentRole = r.GetAttributeValue<EntityReference>("parentroleid");
                    var parentRootRole = r.GetAttributeValue<EntityReference>("parentrootroleid");
                    
                    // Root role has no parent or parent root points to itself
                    return parentRole == null && parentRootRole != null && parentRootRole.Id == r.Id;
                })
                .ToArray();

            // Create shadow copies for each root role
            foreach (var rootRole in rootRoles)
            {
                // Don't create shadow if this is the role's own BU
                var roleBU = rootRole.GetAttributeValue<EntityReference>("businessunitid");
                if (roleBU != null && roleBU.Id == businessUnit.Id)
                {
                    continue;
                }

                CreateShadowRole(rootRole, businessUnit.Id);
            }
        }

        /// <summary>
        /// Called when a business unit is deleted.
        /// Deletes all shadow roles for this business unit and removes role assignments.
        /// </summary>
        public void OnBusinessUnitDeleted(Guid businessUnitId)
        {
            // Get all roles for this business unit that are shadow copies
            var shadowRoles = _context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("businessunitid") != null &&
                           r.GetAttributeValue<EntityReference>("businessunitid").Id == businessUnitId)
                .ToArray()
                .Where(r =>
                {
                    var parentRole = r.GetAttributeValue<EntityReference>("parentroleid");
                    return parentRole != null; // Shadow roles have a parent
                })
                .ToArray();

            // Delete all shadow roles
            foreach (var shadowRole in shadowRoles)
            {
                // Remove role assignments first
                RemoveAllRoleAssignments(shadowRole.Id);
                
                // Delete the shadow role
                _context.DeleteEntity(new EntityReference("role", shadowRole.Id));
            }
        }

        /// <summary>
        /// Called when a root role is deleted.
        /// Deletes all shadow copies of this role.
        /// </summary>
        public void OnRoleDeleted(Guid roleId)
        {
            var role = _context.GetEntityById("role", roleId);
            if (role == null)
            {
                return;
            }

            // Check if this is a root role
            var parentRole = role.GetAttributeValue<EntityReference>("parentroleid");
            if (parentRole != null)
            {
                // This is a shadow copy, can't delete it directly
                throw new InvalidOperationException("Shadow role copies cannot be deleted directly. Delete the root role instead.");
            }

            // Get all shadow copies (roles with this role as parent)
            var shadowCopies = _context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("parentroleid") != null &&
                           r.GetAttributeValue<EntityReference>("parentroleid").Id == roleId)
                .ToArray();

            // Delete all shadow copies
            foreach (var shadow in shadowCopies)
            {
                RemoveAllRoleAssignments(shadow.Id);
                _context.DeleteEntity(new EntityReference("role", shadow.Id));
            }
        }

        /// <summary>
        /// Called when a user/team's business unit is updated.
        /// Removes all role assignments as they are no longer valid.
        /// </summary>
        public void OnUserTeamBusinessUnitChanged(string entityName, Guid entityId)
        {
            if (entityName != "systemuser" && entityName != "team")
            {
                throw new ArgumentException("Entity must be systemuser or team", nameof(entityName));
            }

            // Remove all role assignments for this user/team
            // In Dataverse, this is done through the systemuserroles_association or teamroles_association
            RemoveAllRoleAssignmentsForPrincipal(entityName, entityId);
        }

        /// <summary>
        /// Validates that a role can be assigned to a user/team.
        /// Role can only be assigned to the same BU the user/team is in unless modern BUs feature is on.
        /// </summary>
        public void ValidateRoleAssignment(Guid roleId, string principalType, Guid principalId)
        {
            // Get the modern BUs setting
            bool useModernBusinessUnits = _context.SecurityConfiguration.UseModernBusinessUnits;

            if (!useModernBusinessUnits)
            {
                // Traditional mode: role and principal must be in the same BU
                var role = _context.GetEntityById("role", roleId);
                if (role == null)
                {
                    throw new InvalidOperationException($"Role {roleId} not found.");
                }

                var principal = _context.GetEntityById(principalType, principalId);
                if (principal == null)
                {
                    throw new InvalidOperationException($"{principalType} {principalId} not found.");
                }

                var roleBU = role.GetAttributeValue<EntityReference>("businessunitid");
                var principalBU = principal.GetAttributeValue<EntityReference>("businessunitid");

                if (roleBU != null && principalBU != null && roleBU.Id != principalBU.Id)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign role from business unit {roleBU.Id} to {principalType} in business unit {principalBU.Id}. " +
                        "Roles can only be assigned to users/teams in the same business unit unless modern business units are enabled.");
                }
            }
            // Modern BUs mode: roles can be assigned across BUs (matrix-based security)
        }

        /// <summary>
        /// Creates a shadow copy of a role for a specific business unit.
        /// </summary>
        private void CreateShadowRole(Entity rootRole, Guid businessUnitId)
        {
            // Check if shadow already exists
            var existing = _context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("parentrootroleid") != null &&
                           r.GetAttributeValue<EntityReference>("parentrootroleid").Id == rootRole.Id &&
                           r.GetAttributeValue<EntityReference>("businessunitid") != null &&
                           r.GetAttributeValue<EntityReference>("businessunitid").Id == businessUnitId)
                .FirstOrDefault();

            if (existing != null)
            {
                return; // Shadow already exists
            }

            // Create shadow copy
            var shadowRole = new Entity("role")
            {
                Id = Guid.NewGuid(),
                ["name"] = rootRole.GetAttributeValue<string>("name"),
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["parentroleid"] = new EntityReference("role", rootRole.Id),
                ["parentrootroleid"] = new EntityReference("role", rootRole.Id),
                ["roletemplateid"] = rootRole.Contains("roletemplateid") ? rootRole.GetAttributeValue<EntityReference>("roletemplateid") : null,
                ["ismanaged"] = rootRole.GetAttributeValue<bool>("ismanaged"),
                ["iscustomizable"] = false // Shadow copies are not customizable
            };

            _context.AddEntity(shadowRole);

            // Copy privileges from root role to shadow
            CopyRolePrivileges(rootRole.Id, shadowRole.Id);
        }

        /// <summary>
        /// Copies privileges from a root role to a shadow role.
        /// </summary>
        private void CopyRolePrivileges(Guid sourceRoleId, Guid targetRoleId)
        {
            var sourcePrivileges = _context.CreateQuery("roleprivileges")
                .Where(rp => rp.GetAttributeValue<EntityReference>("roleid") != null &&
                            rp.GetAttributeValue<EntityReference>("roleid").Id == sourceRoleId)
                .ToArray();

            foreach (var priv in sourcePrivileges)
            {
                var newPriv = new Entity("roleprivileges")
                {
                    Id = Guid.NewGuid(),
                    ["roleid"] = new EntityReference("role", targetRoleId),
                    ["privilegeid"] = priv.GetAttributeValue<EntityReference>("privilegeid"),
                    ["privilegedepthmask"] = priv.GetAttributeValue<int>("privilegedepthmask")
                };

                _context.AddEntity(newPriv);
            }
        }

        /// <summary>
        /// Removes all role assignments for a specific role.
        /// </summary>
        private void RemoveAllRoleAssignments(Guid roleId)
        {
            // Remove from systemuserroles_association
            var userRoles = _context.CreateQuery("systemuserroles_association")
                .Where(ur => ur.GetAttributeValue<EntityReference>("roleid") != null &&
                            ur.GetAttributeValue<EntityReference>("roleid").Id == roleId)
                .ToArray();

            foreach (var userRole in userRoles)
            {
                _context.DeleteEntity(new EntityReference("systemuserroles_association", userRole.Id));
            }

            // Remove from teamroles_association
            var teamRoles = _context.CreateQuery("teamroles_association")
                .Where(tr => tr.GetAttributeValue<EntityReference>("roleid") != null &&
                            tr.GetAttributeValue<EntityReference>("roleid").Id == roleId)
                .ToArray();

            foreach (var teamRole in teamRoles)
            {
                _context.DeleteEntity(new EntityReference("teamroles_association", teamRole.Id));
            }
        }

        /// <summary>
        /// Removes all role assignments for a specific user or team.
        /// </summary>
        private void RemoveAllRoleAssignmentsForPrincipal(string principalType, Guid principalId)
        {
            if (principalType == "systemuser")
            {
                var userRoles = _context.CreateQuery("systemuserroles_association")
                    .Where(ur => ur.GetAttributeValue<EntityReference>("systemuserid") != null &&
                                ur.GetAttributeValue<EntityReference>("systemuserid").Id == principalId)
                    .ToArray();

                foreach (var userRole in userRoles)
                {
                    _context.DeleteEntity(new EntityReference("systemuserroles_association", userRole.Id));
                }
            }
            else if (principalType == "team")
            {
                var teamRoles = _context.CreateQuery("teamroles_association")
                    .Where(tr => tr.GetAttributeValue<EntityReference>("teamid") != null &&
                                tr.GetAttributeValue<EntityReference>("teamid").Id == principalId)
                    .ToArray();

                foreach (var teamRole in teamRoles)
                {
                    _context.DeleteEntity(new EntityReference("teamroles_association", teamRole.Id));
                }
            }
        }
    }
}
