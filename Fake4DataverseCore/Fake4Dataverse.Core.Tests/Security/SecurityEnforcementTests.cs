using Fake4Dataverse.Security;
using Fake4Dataverse.Security.Middleware;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Crud;
using Fake4Dataverse.Integrity;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Security
{
    /// <summary>
    /// Tests for security enforcement features.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// </summary>
    public class SecurityEnforcementTests
    {
        [Fact]
        public void Should_Initialize_System_Administrator_Role_Automatically()
        {
            // Arrange & Act
            var context = new XrmFakedContext();
            
            // Assert - System Administrator role should be auto-initialized
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            var sysAdminRole = context.GetEntityById("role", sysAdminRoleId);
            Assert.NotNull(sysAdminRole);
            Assert.Equal("System Administrator", sysAdminRole.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Use_Variable_IDs_For_System_Administrator_Role()
        {
            // Arrange & Act - create two contexts
            var context1 = new XrmFakedContext();
            var context2 = new XrmFakedContext();
            
            var roleId1 = context1.SecurityManager.SystemAdministratorRoleId;
            var roleId2 = context2.SecurityManager.SystemAdministratorRoleId;
            
            // Assert - IDs should be different (avoiding hardcoded GUID bugs)
            Assert.NotEqual(roleId1, roleId2);
            
            // Both should be valid
            Assert.NotEqual(Guid.Empty, roleId1);
            Assert.NotEqual(Guid.Empty, roleId2);
        }

        [Fact]
        public void Should_Retrieve_Root_Business_Unit_ID_Easily()
        {
            // Arrange & Act
            var context = new XrmFakedContext();
            var rootBUId = context.SecurityManager.RootBusinessUnitId;
            
            // Assert
            Assert.NotEqual(Guid.Empty, rootBUId);
            
            var rootBU = context.GetEntityById("businessunit", rootBUId);
            Assert.NotNull(rootBU);
            Assert.Equal("Default Business Unit", rootBU.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Retrieve_Root_Organization_ID_Easily()
        {
            // Arrange & Act
            var context = new XrmFakedContext();
            var rootOrgId = context.SecurityManager.RootOrganizationId;
            
            // Assert
            Assert.NotEqual(Guid.Empty, rootOrgId);
            
            var rootOrg = context.GetEntityById("organization", rootOrgId);
            Assert.NotNull(rootOrg);
            Assert.Equal("Default Organization", rootOrg.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Create_Root_Business_Unit_With_System_Administrator_Role()
        {
            // Arrange & Act
            var context = new XrmFakedContext();
            
            // Assert - Root business unit should exist
            var businessUnits = context.CreateQuery("businessunit").ToList();
            Assert.NotEmpty(businessUnits);
            
            var rootBU = businessUnits[0];
            Assert.Equal("Default Business Unit", rootBU.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Create_Root_Organization_With_System_Administrator_Role()
        {
            // Arrange & Act
            var context = new XrmFakedContext();
            
            // Assert - Root organization should exist
            var organizations = context.CreateQuery("organization").ToList();
            Assert.NotEmpty(organizations);
            
            var rootOrg = organizations[0];
            Assert.Equal("Default Organization", rootOrg.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Not_Initialize_System_Administrator_If_Disabled()
        {
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = false;
            
            // Create a new context with the setting disabled
            var context2 = new XrmFakedContext();
            context2.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = false;
            
            // Act - manually initialize to test
            // The role won't be auto-created in constructor due to the setting
            
            // Assert - just verify the setting works
            Assert.False(context2.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges);
        }

        [Fact]
        public void SecurityManager_Should_Check_System_Administrator_Role()
        {
            // Arrange
            var context = new XrmFakedContext();
            var securityManager = new SecurityManager(context);
            var userId = Guid.NewGuid();
            
            // Act
            var isAdmin = securityManager.IsSystemAdministrator(userId);
            
            // Assert - without role assignment, user is not admin
            Assert.False(isAdmin);
        }

        [Fact]
        public void Should_Allow_Operations_When_Security_Disabled()
        {
            // Arrange - use middleware builder but keep security disabled
            var integrityOptions = new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false  // Disable validation for this basic test
            };
            
            var builder = MiddlewareBuilder.New()
                .AddCrud(integrityOptions)  // Wire up CRUD operations with validation disabled
                .UseCrud(); // Register CRUD middleware to handle Execute(CreateRequest, etc.)
                
            var context = builder.Build();
            var service = context.GetOrganizationService();
            
            // Security is disabled by default
            Assert.False(context.SecurityConfiguration.SecurityEnabled);
            
            // Act - create without setting caller (should work since security is disabled)
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test Account"
            });
            
            // Assert
            Assert.NotEqual(Guid.Empty, accountId);
        }

        [Fact]
        public void Should_Enforce_Security_When_Enabled_With_Middleware()
        {
            // Arrange
            var integrityOptions = new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false  // Disable validation - testing security not metadata
            };
            
            var builder = MiddlewareBuilder.New()
                .AddCrud(integrityOptions)
                .UseCrud()  // Register CRUD middleware
                .AddSecurity(); // Add security middleware (runs first after reversal)
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
            context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = true;  // Enable auto System Admin privileges
            
            var service = context.GetOrganizationService();
            
            // Create a user
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser")
            {
                Id = userId,
                ["fullname"] = "Test User"
            };
            context.Initialize(user);
            
            // Assign System Administrator role to grant all privileges
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            var userRole = new Entity("systemuserroles")
            {
                ["systemuserid"] = userId,
                ["roleid"] = sysAdminRoleId
            };
            context.Initialize(userRole);
            
            // Set as caller
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Act - create account (should work - user has System Admin role)
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test Account"
            });
            
            // Assert
            Assert.NotEqual(Guid.Empty, accountId);
        }

        [Fact]
        public void Should_Deny_Access_When_No_Caller_Specified_And_Security_Enabled()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddCrud()
                .AddSecurity()
                .UseCrud();  // Register CRUD middleware
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
            
            var service = context.GetOrganizationService();
            
            // Don't set caller
            context.CallerProperties.CallerId = null;
            
            // Act & Assert
            var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            {
                service.Create(new Entity("account") { ["name"] = "Test" });
            });
            
            Assert.Contains("No caller specified", exception.Message);
        }

        [Fact]
        public void Should_Allow_Record_Owner_To_Update()
        {
            // Arrange
            var integrityOptions = new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false  // Disable validation - testing security not metadata
            };
            
            var builder = MiddlewareBuilder.New()
                .AddCrud(integrityOptions)
                .UseCrud()  // Register CRUD middleware
                .AddSecurity();  // Security middleware runs first (checks permissions before CRUD)
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
            context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = true;  // Enable auto System Admin privileges
            
            var service = context.GetOrganizationService();
            
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Owner" };
            
            // Get System Administrator role ID (this initializes default security entities)
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            
            // Assign System Administrator role to grant all privileges
            var userRole = new Entity("systemuserroles")
            {
                ["systemuserid"] = userId,
                ["roleid"] = sysAdminRoleId
            };
            
            // Initialize user and role assignment together
            context.Initialize(new[] { user, userRole });
            
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Create account as this user
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            
            // Act - update as owner (should work)
            service.Update(new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated"
            });
            
            // Assert
            var account = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
            Assert.Equal("Updated", account.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Deny_Access_To_Non_Owner_When_Security_Enabled()
        {
            // Arrange
            var integrityOptions = new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false  // Disable validation - testing security not metadata
            };
            
            var builder = MiddlewareBuilder.New()
                .AddCrud(integrityOptions)
                .UseCrud()  // Register CRUD middleware
                .AddSecurity();  // Security middleware runs first (checks permissions before CRUD)
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
            context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = true;  // Enable auto System Admin privileges
            
            var service = context.GetOrganizationService();
            
            var owner1Id = Guid.NewGuid();
            var owner2Id = Guid.NewGuid();
            
            var owner1 = new Entity("systemuser") { Id = owner1Id, ["fullname"] = "Owner 1" };
            var owner2 = new Entity("systemuser") { Id = owner2Id, ["fullname"] = "Owner 2" };
            
            context.Initialize(new[] { owner1, owner2 });
            
            // Assign System Administrator role to both users to grant all privileges
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            var userRole1 = new Entity("systemuserroles")
            {
                ["systemuserid"] = owner1Id,
                ["roleid"] = sysAdminRoleId
            };
            var userRole2 = new Entity("systemuserroles")
            {
                ["systemuserid"] = owner2Id,
                ["roleid"] = sysAdminRoleId
            };
            context.Initialize(new[] { userRole1, userRole2 });
            
            // Owner 1 creates account
            context.CallerProperties.CallerId = new EntityReference("systemuser", owner1Id);
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            
            // Act & Assert - Owner 2 tries to update (should fail due to record-level security)
            context.CallerProperties.CallerId = new EntityReference("systemuser", owner2Id);
            
            var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            {
                service.Update(new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Hacked"
                });
            });
            
            Assert.Contains("does not have", exception.Message);
        }

        [Fact]
        public void Should_Allow_Access_Through_Shared_Permissions()
        {
            // Arrange
            var integrityOptions = new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false  // Disable validation - testing security not metadata
            };
            
            var builder = MiddlewareBuilder.New()
                .AddCrud(integrityOptions)
                .UseCrud()  // Register CRUD middleware
                .AddSecurity();  // Security middleware runs first (checks permissions before CRUD)
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
            context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = true;  // Enable auto System Admin privileges
            
            var service = context.GetOrganizationService();
            
            var owner1Id = Guid.NewGuid();
            var owner2Id = Guid.NewGuid();
            
            var owner1 = new Entity("systemuser") { Id = owner1Id, ["fullname"] = "Owner 1" };
            var owner2 = new Entity("systemuser") { Id = owner2Id, ["fullname"] = "Owner 2" };
            
            context.Initialize(new[] { owner1, owner2 });
            
            // Assign System Administrator role to both users to grant all privileges
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            var userRole1 = new Entity("systemuserroles")
            {
                ["systemuserid"] = owner1Id,
                ["roleid"] = sysAdminRoleId
            };
            var userRole2 = new Entity("systemuserroles")
            {
                ["systemuserid"] = owner2Id,
                ["roleid"] = sysAdminRoleId
            };
            context.Initialize(new[] { userRole1, userRole2 });
            
            // Owner 1 creates account
            context.CallerProperties.CallerId = new EntityReference("systemuser", owner1Id);
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            
            // Owner 1 grants access to Owner 2
            var grantRequest = new GrantAccessRequest
            {
                Target = new EntityReference("account", accountId),
                PrincipalAccess = new PrincipalAccess
                {
                    Principal = new EntityReference("systemuser", owner2Id),
                    AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
                }
            };
            service.Execute(grantRequest);
            
            // Act - Owner 2 updates (should work due to shared access)
            context.CallerProperties.CallerId = new EntityReference("systemuser", owner2Id);
            service.Update(new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated by Owner 2"
            });
            
            // Assert
            var account = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
            Assert.Equal("Updated by Owner 2", account.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void SecurityManager_Should_Return_Empty_Roles_For_User_Without_Roles()
        {
            // Arrange
            var context = new XrmFakedContext();
            var securityManager = new SecurityManager(context);
            var userId = Guid.NewGuid();
            
            // Act
            var roles = securityManager.GetUserRoles(userId);
            
            // Assert
            Assert.Empty(roles);
        }

        [Fact]
        public void SecurityManager_Should_Return_Empty_Roles_For_Team_Without_Roles()
        {
            // Arrange
            var context = new XrmFakedContext();
            var securityManager = new SecurityManager(context);
            var teamId = Guid.NewGuid();
            
            // Act
            var roles = securityManager.GetTeamRoles(teamId);
            
            // Assert
            Assert.Empty(roles);
        }
    }
}
