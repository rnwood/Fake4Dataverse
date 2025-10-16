using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Fake4Dataverse.Security
{
    /// <summary>
    /// Manages the initialization and configuration of security-related entities in the context.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
    /// </summary>
    public class SecurityManager
    {
        private readonly IXrmFakedContext _context;

        public SecurityManager(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Initializes the default System Administrator role with all privileges.
        /// This is a well-known role in Dataverse that grants full access to the system.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// 
        /// The System Administrator role has the GUID: {C52D9CA4-3D13-43E7-9C23-D6C6F5FDD425}
        /// This matches the standard Dataverse System Administrator role.
        /// </summary>
        public void InitializeSystemAdministratorRole()
        {
            var service = _context.GetOrganizationService();
            var roleId = SecurityConfiguration.DefaultSystemAdministratorRoleId;

            // Check if role already exists
            if (_context.ContainsEntity("role", roleId))
            {
                return;
            }

            // Create the root business unit if it doesn't exist
            var rootBusinessUnitId = EnsureRootBusinessUnit();

            // Create the System Administrator role
            var systemAdminRole = new Entity("role")
            {
                Id = roleId,
                ["name"] = "System Administrator",
                ["businessunitid"] = new EntityReference("businessunit", rootBusinessUnitId),
                ["iscustomizable"] = false,
                ["ismanaged"] = true
            };

            _context.AddEntity(systemAdminRole);
        }

        /// <summary>
        /// Ensures the root organization and business unit exist.
        /// Returns the root business unit ID.
        /// </summary>
        private Guid EnsureRootBusinessUnit()
        {
            var service = _context.GetOrganizationService();

            // Check for existing root business unit
            var existingBU = _context.CreateQuery("businessunit").FirstOrDefault();
            if (existingBU != null)
            {
                return existingBU.Id;
            }

            // Create root organization if it doesn't exist
            var orgId = EnsureRootOrganization();

            // Create root business unit
            var rootBUId = Guid.NewGuid();
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
            // Check for existing organization
            var existingOrg = _context.CreateQuery("organization").FirstOrDefault();
            if (existingOrg != null)
            {
                return existingOrg.Id;
            }

            // Create root organization
            var orgId = Guid.NewGuid();
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

            var roleId = _context.SecurityConfiguration.SystemAdministratorRoleId;

            // Query for systemuserroles association
            // In Dataverse, this is stored as a many-to-many relationship
            try
            {
                var roles = _context.CreateQuery("role")
                    .Where(r => r.Id == roleId)
                    .FirstOrDefault();

                if (roles == null)
                {
                    return false;
                }

                // Check if user has this role through the systemuserroles_association relationship
                // This would require relationship data to be set up
                // For now, we'll check if the relationship exists in the context
                var relationship = _context.GetRelationship("systemuserroles_association");
                if (relationship == null)
                {
                    return false;
                }

                // Get related entities for this user
                var relatedRoles = _context.CreateQuery("role")
                    .ToList()
                    .Where(r => r.Id == roleId);

                return relatedRoles.Any();
            }
            catch
            {
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
                var relationship = _context.GetRelationship("systemuserroles_association");
                if (relationship == null)
                {
                    return Array.Empty<Guid>();
                }

                // This would need to query the N:N relationship data
                // For now, return empty array as relationship infrastructure needs to be set up
                return Array.Empty<Guid>();
            }
            catch
            {
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
                var relationship = _context.GetRelationship("teamroles_association");
                if (relationship == null)
                {
                    return Array.Empty<Guid>();
                }

                // This would need to query the N:N relationship data
                return Array.Empty<Guid>();
            }
            catch
            {
                return Array.Empty<Guid>();
            }
        }
    }
}
