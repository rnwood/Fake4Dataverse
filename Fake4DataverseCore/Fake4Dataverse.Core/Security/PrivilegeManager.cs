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
        /// 
        /// Note: This method checks if the user has the privilege but does not consider
        /// the business unit context. For business unit-aware privilege checking, use
        /// HasPrivilegeForRecord() which considers role BU context.
        /// </summary>
        public bool HasPrivilege(Guid userId, string privilegeName, int requiredDepth = PrivilegeDepthBasic)
        {
            // System Administrator has all privileges implicitly (not materialized in database)
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security#system-administrator-role
            if (_context.SecurityManager.IsSystemAdministrator(userId))
            {
                return true;
            }

            // Get user's direct roles
            var userRoles = _context.SecurityManager.GetUserRoles(userId);
            
            // Get user's team roles
            var user = _context.GetEntityById("systemuser", userId);
            var teamRoles = Array.Empty<Guid>();
            if (user != null)
            {
                // Get all teams the user belongs to and their roles
                try
                {
                    var teamMemberships = _context.CreateQuery("teammembership")
                        .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == userId)
                        .ToList();
                    
                    var allTeamRoles = new List<Guid>();
                    foreach (var membership in teamMemberships)
                    {
                        var teamId = membership.GetAttributeValue<Guid>("teamid");
                        var rolesForTeam = _context.SecurityManager.GetTeamRoles(teamId);
                        allTeamRoles.AddRange(rolesForTeam);
                    }
                    teamRoles = allTeamRoles.ToArray();
                }
                catch
                {
                    // teammembership entity may not exist
                }
            }
            
            // Combine direct and team roles
            var allRoles = userRoles.Concat(teamRoles).Distinct().ToArray();
            
            if (allRoles.Length == 0)
            {
                return false;
            }

            // Check each role for the privilege
            foreach (var roleId in allRoles)
            {
                if (RoleHasPrivilege(roleId, privilegeName, requiredDepth))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if a user has a specific privilege for a record, considering business unit context.
        /// This is the correct method for privilege checking in Dataverse security model.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// 
        /// A user may have roles from multiple business units (directly or through teams).
        /// Each role is associated with one business unit. The privilege depth must be evaluated
        /// in the context of the role's business unit, not just the user's primary business unit.
        /// </summary>
        public bool HasPrivilegeForRecord(Guid userId, string privilegeName, Entity record, int baseDepth)
        {
            // System Administrator has all privileges implicitly
            if (_context.SecurityManager.IsSystemAdministrator(userId))
            {
                return true;
            }

            // Get user's direct roles
            var userRoles = _context.SecurityManager.GetUserRoles(userId);
            
            // Get user's team roles
            var user = _context.GetEntityById("systemuser", userId);
            var teamRoles = Array.Empty<Guid>();
            if (user != null)
            {
                try
                {
                    var teamMemberships = _context.CreateQuery("teammembership")
                        .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == userId)
                        .ToList();
                    
                    var allTeamRoles = new List<Guid>();
                    foreach (var membership in teamMemberships)
                    {
                        var teamId = membership.GetAttributeValue<Guid>("teamid");
                        var rolesForTeam = _context.SecurityManager.GetTeamRoles(teamId);
                        allTeamRoles.AddRange(rolesForTeam);
                    }
                    teamRoles = allTeamRoles.ToArray();
                }
                catch
                {
                    // teammembership entity may not exist
                }
            }
            
            // Combine direct and team roles
            var allRoles = userRoles.Concat(teamRoles).Distinct().ToArray();
            
            if (allRoles.Length == 0)
            {
                return false;
            }

            // Check each role for the privilege, considering the role's business unit context
            foreach (var roleId in allRoles)
            {
                if (RoleHasPrivilegeForRecord(roleId, privilegeName, userId, record, baseDepth))
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
        /// Checks if a role has a specific privilege for a record, considering the role's business unit context.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// 
        /// The privilege depth is evaluated relative to the role's business unit, not the user's primary business unit.
        /// </summary>
        private bool RoleHasPrivilegeForRecord(Guid roleId, string privilegeName, Guid userId, Entity record, int baseDepth)
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

            var privilegeDepthMask = rolePrivilege.GetAttributeValue<int>("privilegedepthmask");
            
            // Get the role's business unit
            var role = _context.GetEntityById("role", roleId);
            if (role == null || !role.Contains("businessunitid"))
            {
                return false;
            }
            var roleBU = role.GetAttributeValue<EntityReference>("businessunitid");
            
            // Check Global depth - always valid regardless of business unit
            if ((privilegeDepthMask & PrivilegeDepthGlobal) == PrivilegeDepthGlobal)
            {
                return true;
            }
            
            // For organization-owned entities, only Global scope is valid
            if (!record.Contains("ownerid"))
            {
                return false;  // Need Global scope for org-owned entities
            }
            
            // Check Basic depth - user must own the record
            if ((privilegeDepthMask & PrivilegeDepthBasic) == PrivilegeDepthBasic)
            {
                var ownerId = record.GetAttributeValue<EntityReference>("ownerid");
                if (ownerId != null && ownerId.Id == userId)
                {
                    return true;  // User owns the record
                }
            }
            
            // Check Local depth - record's business unit must match role's business unit
            if ((privilegeDepthMask & PrivilegeDepthLocal) == PrivilegeDepthLocal)
            {
                if (record.Contains("owningbusinessunit"))
                {
                    var recordBU = record.GetAttributeValue<EntityReference>("owningbusinessunit");
                    if (recordBU != null && recordBU.Id == roleBU.Id)
                    {
                        return true;  // Record belongs to role's business unit
                    }
                }
            }
            
            // Check Deep depth - record's business unit must be in role's business unit hierarchy
            if ((privilegeDepthMask & PrivilegeDepthDeep) == PrivilegeDepthDeep)
            {
                if (record.Contains("owningbusinessunit"))
                {
                    var recordBU = record.GetAttributeValue<EntityReference>("owningbusinessunit");
                    if (recordBU != null && IsInBusinessUnitHierarchy(recordBU.Id, roleBU.Id))
                    {
                        return true;  // Record's BU is in role's BU hierarchy
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if a business unit is within another business unit's hierarchy.
        /// Used for Deep privilege depth checking.
        /// </summary>
        private bool IsInBusinessUnitHierarchy(Guid childBUId, Guid parentBUId)
        {
            if (childBUId == parentBUId)
            {
                return true;
            }

            var currentBU = _context.GetEntityById("businessunit", childBUId);
            while (currentBU != null && currentBU.Contains("parentbusinessunitid"))
            {
                var parentBURef = currentBU.GetAttributeValue<EntityReference>("parentbusinessunitid");
                if (parentBURef == null)
                {
                    break;
                }

                if (parentBURef.Id == parentBUId)
                {
                    return true;
                }

                currentBU = _context.GetEntityById("businessunit", parentBURef.Id);
            }

            return false;
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
