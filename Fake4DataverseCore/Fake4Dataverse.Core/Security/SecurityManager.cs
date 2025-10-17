using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Fake4Dataverse.Security
{
    /// <summary>
    /// Manages the initialization and configuration of security-related entities in the context.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
    /// </summary>
    public class SecurityManager : ISecurityManager
    {
        private readonly IXrmFakedContext _context;
        
        // Store IDs as instance variables for easy retrieval
        private Guid? _rootOrganizationId;
        private Guid? _rootBusinessUnitId;
        private Guid? _systemAdministratorRoleId;
        
        private PrivilegeManager _privilegeManager;
        private RoleLifecycleManager _roleLifecycleManager;

        public SecurityManager(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _privilegeManager = new PrivilegeManager(context);
            _roleLifecycleManager = new RoleLifecycleManager(context);
        }

        /// <summary>
        /// Gets the privilege manager for managing privileges and role assignments.
        /// </summary>
        public IPrivilegeManager PrivilegeManager => _privilegeManager;

        /// <summary>
        /// Gets the role lifecycle manager for managing role shadow copies and business unit lifecycle.
        /// </summary>
        public IRoleLifecycleManager RoleLifecycleManager => _roleLifecycleManager;

        /// <summary>
        /// Gets the root organization ID. Creates the organization if it doesn't exist.
        /// </summary>
        public Guid RootOrganizationId
        {
            get
            {
                if (_rootOrganizationId == null)
                {
                    _rootOrganizationId = EnsureRootOrganization();
                }
                return _rootOrganizationId.Value;
            }
        }

        /// <summary>
        /// Gets the root business unit ID. Creates the business unit if it doesn't exist.
        /// </summary>
        public Guid RootBusinessUnitId
        {
            get
            {
                if (_rootBusinessUnitId == null)
                {
                    _rootBusinessUnitId = EnsureRootBusinessUnit();
                }
                return _rootBusinessUnitId.Value;
            }
        }

        /// <summary>
        /// Gets the System Administrator role ID. Creates the role if it doesn't exist.
        /// The ID varies per context but is easily retrievable through this property.
        /// </summary>
        public Guid SystemAdministratorRoleId
        {
            get
            {
                if (_systemAdministratorRoleId == null)
                {
                    InitializeSystemAdministratorRole();
                }
                return _systemAdministratorRoleId.Value;
            }
        }

        /// <summary>
        /// Initializes the default System Administrator role with all privileges.
        /// This is a well-known role in Dataverse that grants full access to the system.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// 
        /// The System Administrator role ID varies per context instance to avoid hardcoding bugs.
        /// Use SecurityManager.SystemAdministratorRoleId to retrieve it.
        /// </summary>
        public void InitializeSystemAdministratorRole()
        {
            // Check if already initialized
            if (_systemAdministratorRoleId != null)
            {
                return;
            }

            // Try to find existing System Administrator role
            var existingRole = _context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<string>("name") == "System Administrator")
                .FirstOrDefault();
                
            if (existingRole != null)
            {
                _systemAdministratorRoleId = existingRole.Id;
                return;
            }

            // Generate a new ID for this instance
            var roleId = Guid.NewGuid();
            _systemAdministratorRoleId = roleId;

            // Create the root business unit if it doesn't exist
            var rootBusinessUnitId = RootBusinessUnitId;

            // Create the System Administrator role
            var systemAdminRole = new Entity("role")
            {
                Id = roleId,
                ["name"] = "System Administrator",
                ["businessunitid"] = new EntityReference("businessunit", rootBusinessUnitId),
                ["iscustomizable"] = false,
                ["ismanaged"] = true,
                ["parentroleid"] = null, // This is the root/master role
                ["parentrootroleid"] = null // Root roles don't have a parent root
            };

            _context.AddEntity(systemAdminRole);
            
            // Update the parentrootroleid to point to itself after creation
            systemAdminRole["parentrootroleid"] = new EntityReference("role", roleId);
            _context.UpdateEntity(systemAdminRole);
        }

        /// <summary>
        /// Ensures the root organization and business unit exist.
        /// Returns the root business unit ID.
        /// </summary>
        private Guid EnsureRootBusinessUnit()
        {
            // Check if already cached
            if (_rootBusinessUnitId != null)
            {
                return _rootBusinessUnitId.Value;
            }

            // Check for existing root business unit
            var existingBU = _context.CreateQuery("businessunit")
                .Where(bu => bu.GetAttributeValue<EntityReference>("parentbusinessunitid") == null)
                .FirstOrDefault();
                
            if (existingBU != null)
            {
                _rootBusinessUnitId = existingBU.Id;
                return existingBU.Id;
            }

            // Create root organization if it doesn't exist
            var orgId = RootOrganizationId;

            // Create root business unit
            var rootBUId = Guid.NewGuid();
            _rootBusinessUnitId = rootBUId;
            
            var rootBusinessUnit = new Entity("businessunit")
            {
                Id = rootBUId,
                ["name"] = "Default Business Unit",
                ["organizationid"] = new EntityReference("organization", orgId),
                ["parentbusinessunitid"] = null,
                ["isdisabled"] = false
            };

            _context.AddEntity(rootBusinessUnit);
            return rootBUId;
        }

        /// <summary>
        /// Ensures the root organization exists.
        /// Returns the organization ID.
        /// </summary>
        private Guid EnsureRootOrganization()
        {
            // Check if already cached
            if (_rootOrganizationId != null)
            {
                return _rootOrganizationId.Value;
            }

            // Check for existing organization
            var existingOrg = _context.CreateQuery("organization").FirstOrDefault();
            if (existingOrg != null)
            {
                _rootOrganizationId = existingOrg.Id;
                return existingOrg.Id;
            }

            // Create root organization
            var orgId = Guid.NewGuid();
            _rootOrganizationId = orgId;
            
            var organization = new Entity("organization")
            {
                Id = orgId,
                ["name"] = "Default Organization",
                ["isdisabled"] = false
            };

            _context.AddEntity(organization);
            return orgId;
        }

        /// <summary>
        /// Checks if a user has the System Administrator role assigned.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user has the System Administrator role</returns>
        public bool IsSystemAdministrator(Guid userId)
        {
            if (!_context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges)
            {
                return false;
            }

            var roleId = SystemAdministratorRoleId;

            // Query the systemuserroles intersect entity directly
            // In Dataverse, N:N relationships are stored in intersect entities
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/associate-disassociate-entities-using-web-api
            try
            {
                var userRole = _context.CreateQuery("systemuserroles")
                    .Where(ur => ur.GetAttributeValue<Guid>("systemuserid") == userId && 
                                 ur.GetAttributeValue<Guid>("roleid") == roleId)
                    .FirstOrDefault();
                
                return userRole != null;
            }
            catch
            {
                // If systemuserroles entity doesn't exist, return false
                return false;
            }
        }

        /// <summary>
        /// Gets all role IDs assigned to a user.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>Array of role IDs assigned to the user</returns>
        public Guid[] GetUserRoles(Guid userId)
        {
            try
            {
                // Query the systemuserroles intersect entity directly
                // In Dataverse, N:N relationships are stored in intersect entities
                var userRoles = _context.CreateQuery("systemuserroles")
                    .ToList()
                    .Where(ur => ur.GetAttributeValue<EntityReference>("systemuserid")?.Id == userId)
                    .ToList();

                return userRoles.Select(ur => ur.GetAttributeValue<EntityReference>("roleid")?.Id ?? Guid.Empty).ToArray();
            }
            catch
            {
                // If systemuserroles entity doesn't exist, return empty array
                return Array.Empty<Guid>();
            }
        }

        /// <summary>
        /// Gets all role IDs assigned to a team.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security#teams
        /// </summary>
        /// <param name="teamId">The team ID</param>
        /// <returns>Array of role IDs assigned to the team</returns>
        public Guid[] GetTeamRoles(Guid teamId)
        {
            try
            {
                // Query the teamroles intersect entity directly
                // In Dataverse, N:N relationships are stored in intersect entities
                var teamRoles = _context.CreateQuery("teamroles")
                    .ToList()
                    .Where(tr => tr.GetAttributeValue<EntityReference>("teamid")?.Id == teamId)
                    .ToList();

                return teamRoles.Select(tr => tr.GetAttributeValue<EntityReference>("roleid")?.Id ?? Guid.Empty).ToArray();
            }
            catch
            {
                // If teamroles entity doesn't exist, return empty array
                return Array.Empty<Guid>();
            }
        }

    }
}
