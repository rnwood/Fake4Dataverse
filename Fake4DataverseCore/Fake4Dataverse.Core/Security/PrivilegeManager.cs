using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.Security
{
    /// <summary>
    /// Manages privileges and their assignment to roles.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
    /// 
    /// Dataverse automatically creates certain privileges (prvCreateXYZ, prvReadXYZ, prvWriteXYZ, prvDeleteXYZ, prvAppendXYZ, prvAppendToXYZ)
    /// when an entity is created. These privileges control CRUD operations on the entity.
    /// </summary>
    public class PrivilegeManager : IPrivilegeManager
    {
        private readonly IXrmFakedContext _context;

        // Privilege depth constants matching Dataverse
        // Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges#privilege-depth
        public const int PrivilegeDepthBasic = 1;      // User's own records
        public const int PrivilegeDepthLocal = 2;      // User's business unit
        public const int PrivilegeDepthDeep = 4;       // User's business unit and child business units
        public const int PrivilegeDepthGlobal = 8;     // Organization-wide

        public PrivilegeManager(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Creates standard CRUD privileges for an entity.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// 
        /// Dataverse creates these privileges automatically:
        /// - prvCreate{EntityName} - Create privilege
        /// - prvRead{EntityName} - Read privilege
        /// - prvWrite{EntityName} - Write/Update privilege
        /// - prvDelete{EntityName} - Delete privilege
        /// - prvAppend{EntityName} - Append privilege
        /// - prvAppendTo{EntityName} - AppendTo privilege
        /// - prvAssign{EntityName} - Assign privilege (not for organization-owned entities)
        /// - prvShare{EntityName} - Share privilege (not for organization-owned entities)
        /// 
        /// Organization-owned entities (like system tables) have no owner and support only Organization scope.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security#organization-owned-entities
        /// </summary>
        public void CreateStandardPrivilegesForEntity(string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
            {
                throw new ArgumentNullException(nameof(entityLogicalName));
            }

            var entityMetadata = _context.GetEntityMetadataByName(entityLogicalName);
            if (entityMetadata == null)
            {
                return; // Entity metadata not found
            }

            var entityName = entityMetadata.LogicalName;
            var privilegePrefix = "prv";
            
            // Determine if entity is organization-owned
            // Organization-owned entities don't have ownerid/owninguser attributes
            // Common examples: systemuser, businessunit, role, privilege, organization
            bool isOrganizationOwned = IsOrganizationOwnedEntity(entityName);

            // Define standard privileges
            var standardPrivileges = new[]
            {
                new { Name = $"{privilegePrefix}Create{ToPascalCase(entityName)}", AccessRight = AccessRights.CreateAccess, SkipForOrgOwned = false },
                new { Name = $"{privilegePrefix}Read{ToPascalCase(entityName)}", AccessRight = AccessRights.ReadAccess, SkipForOrgOwned = false },
                new { Name = $"{privilegePrefix}Write{ToPascalCase(entityName)}", AccessRight = AccessRights.WriteAccess, SkipForOrgOwned = false },
                new { Name = $"{privilegePrefix}Delete{ToPascalCase(entityName)}", AccessRight = AccessRights.DeleteAccess, SkipForOrgOwned = false },
                new { Name = $"{privilegePrefix}Append{ToPascalCase(entityName)}", AccessRight = AccessRights.AppendAccess, SkipForOrgOwned = true },
                new { Name = $"{privilegePrefix}AppendTo{ToPascalCase(entityName)}", AccessRight = AccessRights.AppendToAccess, SkipForOrgOwned = true },
                new { Name = $"{privilegePrefix}Assign{ToPascalCase(entityName)}", AccessRight = AccessRights.AssignAccess, SkipForOrgOwned = true },
                new { Name = $"{privilegePrefix}Share{ToPascalCase(entityName)}", AccessRight = AccessRights.ShareAccess, SkipForOrgOwned = true }
            };

            foreach (var priv in standardPrivileges)
            {
                // Skip Assign/Share/Append/AppendTo privileges for organization-owned entities
                if (isOrganizationOwned && priv.SkipForOrgOwned)
                {
                    continue;
                }
                
                CreatePrivilegeIfNotExists(priv.Name, entityName, (int)priv.AccessRight, isOrganizationOwned);
            }
        }

        /// <summary>
        /// Determines if an entity is organization-owned (has no owner).
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security#organization-owned-entities
        /// </summary>
        private bool IsOrganizationOwnedEntity(string entityName)
        {
            // System tables are organization-owned
            var organizationOwnedEntities = new[]
            {
                "organization",
                "businessunit", 
                "systemuser",
                "team",
                "role",
                "privilege",
                "roleprivileges",
                "entitydefinition",
                "attribute",
                "solution",
                "publisher",
                "webresource",
                "sitemap",
                "appmodule",
                "appmodulecomponent",
                "savedquery",
                "systemform"
            };

            return organizationOwnedEntities.Contains(entityName.ToLowerInvariant());
        }

        /// <summary>
        /// Creates a privilege if it doesn't already exist.
        /// Organization-owned entities support only Organization scope.
        /// </summary>
        private void CreatePrivilegeIfNotExists(string privilegeName, string entityName, int accessRight, bool isOrganizationOwned = false)
        {
            // Check if privilege already exists
            var existingPrivilege = _context.CreateQuery("privilege")
                .Where(p => p.GetAttributeValue<string>("name") == privilegeName)
                .FirstOrDefault();

            if (existingPrivilege != null)
            {
                return; // Privilege already exists
            }

            // Create the privilege
            // Organization-owned entities support only Organization (Global) scope
            // User-owned entities support Basic, Local, Deep, and Global scopes
            var privilege = new Entity("privilege")
            {
                Id = Guid.NewGuid(),
                ["name"] = privilegeName,
                ["accessright"] = accessRight,
                ["canbebasic"] = !isOrganizationOwned,
                ["canbelocal"] = !isOrganizationOwned,
                ["canbedeep"] = !isOrganizationOwned,
                ["canbeglobal"] = true,  // All entities support global/organization scope
                ["canbeprivate"] = !isOrganizationOwned
            };

            _context.AddEntity(privilege);
        }

        /// <summary>
        /// Checks if a user has a specific privilege through their assigned roles.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// 
        /// System Administrator role grants all privileges implicitly (not materialized).
        /// </summary>
        public bool HasPrivilege(Guid userId, string privilegeName, int requiredDepth = PrivilegeDepthBasic)
        {
            // System Administrator has all privileges implicitly (not materialized in database)
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security#system-administrator-role
            if (_context.SecurityManager.IsSystemAdministrator(userId))
            {
                return true;
            }

            // Get user's roles
            var userRoles = _context.SecurityManager.GetUserRoles(userId);
            if (userRoles == null || userRoles.Length == 0)
            {
                return false;
            }

            // Check each role for the privilege
            foreach (var roleId in userRoles)
            {
                if (RoleHasPrivilege(roleId, privilegeName, requiredDepth))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a role has a specific privilege with the required depth.
        /// </summary>
        private bool RoleHasPrivilege(Guid roleId, string privilegeName, int requiredDepth)
        {
            // Get the privilege
            var privilege = _context.CreateQuery("privilege")
                .Where(p => p.GetAttributeValue<string>("name") == privilegeName)
                .FirstOrDefault();

            if (privilege == null)
            {
                return false;
            }

            // Check if the role has this privilege through roleprivileges junction
            var rolePrivilege = _context.CreateQuery("roleprivileges")
                .Where(rp => rp.GetAttributeValue<Guid>("roleid") == roleId && 
                             rp.GetAttributeValue<Guid>("privilegeid") == privilege.Id)
                .FirstOrDefault();

            if (rolePrivilege == null)
            {
                return false;
            }

            // Check privilege depth
            var privilegeDepthMask = rolePrivilege.GetAttributeValue<int>("privilegedepthmask");
            return (privilegeDepthMask & requiredDepth) == requiredDepth;
        }

        /// <summary>
        /// Grants all standard privileges to the System Administrator role.
        /// </summary>
        public void GrantAllPrivilegesToSystemAdministrator()
        {
            var sysAdminRoleId = _context.SecurityManager.SystemAdministratorRoleId;

            // Get all privileges
            var allPrivileges = _context.CreateQuery("privilege").ToList();

            foreach (var privilege in allPrivileges)
            {
                // Check if role already has this privilege
                var existingRolePrivilege = _context.CreateQuery("roleprivileges")
                    .Where(rp => rp.GetAttributeValue<Guid>("roleid") == sysAdminRoleId &&
                                 rp.GetAttributeValue<Guid>("privilegeid") == privilege.Id)
                    .FirstOrDefault();

                if (existingRolePrivilege == null)
                {
                    // Grant the privilege with global depth
                    var rolePrivilege = new Entity("roleprivileges")
                    {
                        Id = Guid.NewGuid(),
                        ["roleid"] = sysAdminRoleId,
                        ["privilegeid"] = privilege.Id,
                        ["privilegedepthmask"] = PrivilegeDepthGlobal
                    };

                    _context.AddEntity(rolePrivilege);
                }
            }
        }

        /// <summary>
        /// Converts a logical name to PascalCase for privilege naming.
        /// Example: "new_customentity" becomes "NewCustomentity"
        /// </summary>
        private string ToPascalCase(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return logicalName;
            }

            // Handle underscore-separated names
            var parts = logicalName.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }
            }

            return string.Join("", parts);
        }
    }
}
