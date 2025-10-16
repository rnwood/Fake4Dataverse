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
        /// - prvAssign{EntityName} - Assign privilege
        /// - prvShare{EntityName} - Share privilege
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

            // Define standard privileges
            var standardPrivileges = new[]
            {
                new { Name = $"{privilegePrefix}Create{ToPascalCase(entityName)}", AccessRight = AccessRights.CreateAccess },
                new { Name = $"{privilegePrefix}Read{ToPascalCase(entityName)}", AccessRight = AccessRights.ReadAccess },
                new { Name = $"{privilegePrefix}Write{ToPascalCase(entityName)}", AccessRight = AccessRights.WriteAccess },
                new { Name = $"{privilegePrefix}Delete{ToPascalCase(entityName)}", AccessRight = AccessRights.DeleteAccess },
                new { Name = $"{privilegePrefix}Append{ToPascalCase(entityName)}", AccessRight = AccessRights.AppendAccess },
                new { Name = $"{privilegePrefix}AppendTo{ToPascalCase(entityName)}", AccessRight = AccessRights.AppendToAccess },
                new { Name = $"{privilegePrefix}Assign{ToPascalCase(entityName)}", AccessRight = AccessRights.AssignAccess },
                new { Name = $"{privilegePrefix}Share{ToPascalCase(entityName)}", AccessRight = AccessRights.ShareAccess }
            };

            foreach (var priv in standardPrivileges)
            {
                CreatePrivilegeIfNotExists(priv.Name, entityName, (int)priv.AccessRight);
            }
        }

        /// <summary>
        /// Creates a privilege if it doesn't already exist.
        /// </summary>
        private void CreatePrivilegeIfNotExists(string privilegeName, string entityName, int accessRight)
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
            var privilege = new Entity("privilege")
            {
                Id = Guid.NewGuid(),
                ["name"] = privilegeName,
                ["accessright"] = accessRight,
                ["canbebasic"] = true,
                ["canbelocal"] = true,
                ["canbedeep"] = true,
                ["canbeglobal"] = true,
                ["canbeprivate"] = true
            };

            _context.AddEntity(privilege);
        }

        /// <summary>
        /// Checks if a user has a specific privilege through their assigned roles.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// </summary>
        public bool HasPrivilege(Guid userId, string privilegeName, int requiredDepth = PrivilegeDepthBasic)
        {
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
