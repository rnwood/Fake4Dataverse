using Fake4Dataverse.Security;
using Fake4Dataverse.Security.Middleware;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Crud;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Security
{
    /// <summary>
    /// Comprehensive tests for the complete Dataverse security model implementation.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// </summary>
    public class SecurityModelComprehensiveTests
    {
        #region Privilege-Based Security Tests
        
        [Fact]
        public void Should_Auto_Create_Privileges_For_User_Owned_Entities()
        {
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            
            // Load account metadata (user-owned entity)
            context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "sales" }).Wait();
            
            // Act
            var privileges = context.CreateQuery("privilege")
                .Where(p => p.GetAttributeValue<string>("name").Contains("Account"))
                .ToList();
            
            // Assert - should have all 8 privileges for user-owned entities
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvCreateAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvReadAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvWriteAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvDeleteAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvAppendAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvAppendToAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvAssignAccount");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvShareAccount");
        }
        
        [Fact]
        public void Should_Auto_Create_Limited_Privileges_For_Organization_Owned_Entities()
        {
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            
            // Load systemuser metadata (organization-owned entity)
            context.InitializeMetadataFromCdmFiles(new[] { "/home/runner/work/Fake4Dataverse/Fake4Dataverse/Fake4DataverseCore/system-edm-files/SystemUser.cdm.json" });
            
            // Act - systemuser is organization-owned
            var privileges = context.CreateQuery("privilege")
                .Where(p => p.GetAttributeValue<string>("name").Contains("Systemuser"))
                .ToList();
            
            // Assert - should have only 4 privileges for org-owned entities (no Assign/Share/Append/AppendTo)
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvCreateSystemuser");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvReadSystemuser");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvWriteSystemuser");
            Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvDeleteSystemuser");
            
            // These should NOT exist for organization-owned entities
            Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvAssignSystemuser");
            Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvShareSystemuser");
            Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvAppendSystemuser");
            Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvAppendToSystemuser");
        }
        
        [Fact]
        public void Should_Grant_Access_Based_On_Privilege_Depth_Basic()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddCrud()
                .UseCrud()
                .AddSecurity();
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforcePrivilegeDepth = true;
            
            var service = context.GetOrganizationService();
            
            // Grant Basic depth privilege (user can only access their own records)
            // Load metadata first (this initializes default security entities)
            context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "sales" }).Wait();
            
            // Create user and role
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var buId = context.SecurityManager.RootBusinessUnitId;
            
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Test User", ["businessunitid"] = new EntityReference("businessunit", buId) };
            var role = new Entity("role") { Id = roleId, ["name"] = "Test Role", ["businessunitid"] = new EntityReference("businessunit", buId) };
            
            var prvReadAccount = context.CreateQuery("privilege")
                .FirstOrDefault(p => p.GetAttributeValue<string>("name") == "prvReadAccount");
            
            var rolePrivilege = new Entity("roleprivileges")
            {
                Id = Guid.NewGuid(),
                ["roleid"] = new EntityReference("role", roleId),
                ["privilegeid"] = new EntityReference("privilege", prvReadAccount.Id),
                ["privilegedepthmask"] = 1 // Basic depth
            };
            
            // Assign role to user via systemuserroles entity
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = userId,
                ["roleid"] = roleId
            };
            
            // Initialize all entities together
            context.Initialize(new[] { user, role, rolePrivilege, userRole });
            
            // Set caller
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Act - user creates and reads their own account
            var accountId = service.Create(new Entity("account") { ["name"] = "My Account" });
            var account = service.Retrieve("account", accountId, new ColumnSet("name"));
            
            // Assert
            Assert.NotNull(account);
            Assert.Equal("My Account", account.GetAttributeValue<string>("name"));
        }
        
        #endregion
        
        #region System Administrator Tests
        
        [Fact]
        public void System_Administrator_Should_Have_All_Privileges_Implicitly()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddCrud()
                .UseCrud()
                .AddSecurity();
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            
            var service = context.GetOrganizationService();
            
            // Load account metadata (needed for validation)
            context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "sales" }).Wait();
            
            // Create System Administrator user
            var userId = Guid.NewGuid();
            
            // Get System Administrator role ID (this initializes default security entities)
            var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
            
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Admin User" };
            
            // Assign System Administrator role via systemuserroles entity
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = userId,
                ["roleid"] = sysAdminRoleId
            };
            
            // Initialize user and role assignment together
            context.Initialize(new[] { user, userRole });
            
            // Set caller
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Act - create account without any explicit privilege grants
            var accountId = service.Create(new Entity("account") { ["name"] = "Admin Account" });
            
            // Assert - System Administrator can do anything
            Assert.NotEqual(Guid.Empty, accountId);
            var account = service.Retrieve("account", accountId, new ColumnSet("name"));
            Assert.Equal("Admin Account", account.GetAttributeValue<string>("name"));
        }
        
        #endregion
        
        #region Organization-Owned Entity Tests
        
        [Fact]
        public void Organization_Owned_Entities_Should_Support_Only_Global_Scope()
        {
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            
            // Act - check systemuser privileges (organization-owned)
            var privileges = context.CreateQuery("privilege")
                .Where(p => p.GetAttributeValue<string>("name").Contains("Systemuser"))
                .ToList();
            
            // Assert - all privileges should have only canbeglobal=true
            foreach (var privilege in privileges)
            {
                Assert.False(privilege.GetAttributeValue<bool>("canbebasic"));
                Assert.False(privilege.GetAttributeValue<bool>("canbelocal"));
                Assert.False(privilege.GetAttributeValue<bool>("canbedeep"));
                Assert.True(privilege.GetAttributeValue<bool>("canbeglobal"));
            }
        }
        
        [Fact]
        public void System_Tables_Should_Be_Readable_By_Everyone()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddCrud()
                .UseCrud()
                .AddSecurity();
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            
            var service = context.GetOrganizationService();
            
            // Create a regular user without any special privileges
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Regular User" };
            context.Initialize(user);
            
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Act - read system tables without any privilege grants
            var roles = service.RetrieveMultiple(new QueryExpression("role")).Entities;
            var businessUnits = service.RetrieveMultiple(new QueryExpression("businessunit")).Entities;
            var users = service.RetrieveMultiple(new QueryExpression("systemuser")).Entities;
            
            // Assert - everyone can read system tables
            Assert.NotEmpty(roles);
            Assert.NotEmpty(businessUnits);
            Assert.NotEmpty(users);
        }
        
        #endregion
        
        #region Role Shadow Copies Tests
        
        [Fact]
        public void Should_Create_Shadow_Copies_When_Role_Is_Created()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            var service = context.GetOrganizationService();
            
            // Create a second business unit
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit")
            {
                Id = bu2Id,
                ["name"] = "Sales Department",
                ["parentbusinessunitid"] = new EntityReference("businessunit", bu1Id)
            };
            service.Create(bu2);
            
            // Act - create a role in BU1
            var roleId = Guid.NewGuid();
            var role = new Entity("role")
            {
                Id = roleId,
                ["name"] = "Sales Manager",
                ["businessunitid"] = new EntityReference("businessunit", bu1Id)
            };
            service.Create(role);
            
            // Assert - shadow copy should exist for BU2
            var allRoles = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<string>("name") == "Sales Manager")
                .ToList();
                
            Assert.Equal(2, allRoles.Count); // Root role + 1 shadow copy
            
            var rootRole = allRoles.First(r => r.Id == roleId);
            Assert.Equal(roleId, rootRole.GetAttributeValue<EntityReference>("parentrootroleid").Id);
            
            var shadowRole = allRoles.First(r => r.Id != roleId);
            Assert.Equal(roleId, shadowRole.GetAttributeValue<EntityReference>("parentroleid").Id);
            Assert.Equal(bu2Id, shadowRole.GetAttributeValue<EntityReference>("businessunitid").Id);
        }
        
        [Fact]
        public void Should_Create_Shadow_Copies_When_Business_Unit_Is_Created()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            var service = context.GetOrganizationService();
            
            // Create a role first
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var roleId = Guid.NewGuid();
            var role = new Entity("role")
            {
                Id = roleId,
                ["name"] = "Sales Manager",
                ["businessunitid"] = new EntityReference("businessunit", bu1Id)
            };
            service.Create(role);
            
            // Act - create a new business unit
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit")
            {
                Id = bu2Id,
                ["name"] = "Sales Department",
                ["parentbusinessunitid"] = new EntityReference("businessunit", bu1Id)
            };
            service.Create(bu2);
            
            // Assert - shadow copy of the role should be created for new BU
            var rolesInBU2 = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("businessunitid").Id == bu2Id)
                .ToList();
                
            Assert.NotEmpty(rolesInBU2);
            var shadowRole = rolesInBU2.First(r => r.GetAttributeValue<string>("name") == "Sales Manager");
            Assert.Equal(roleId, shadowRole.GetAttributeValue<EntityReference>("parentroleid").Id);
        }
        
        [Fact]
        public void Should_Delete_All_Shadow_Copies_When_Root_Role_Is_Deleted()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            var service = context.GetOrganizationService();
            
            // Create a second business unit
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit") { Id = bu2Id, ["name"] = "Sales" };
            service.Create(bu2);
            
            // Create a role (creates shadow copy)
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Sales Manager", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(role);
            
            // Act - delete root role
            service.Delete("role", roleId);
            
            // Assert - all copies should be deleted
            var allRoles = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<string>("name") == "Sales Manager")
                .ToList();
                
            Assert.Empty(allRoles);
        }
        
        [Fact]
        public void Should_Prevent_Direct_Deletion_Of_Shadow_Roles()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            var service = context.GetOrganizationService();
            
            // Create a second business unit
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit") { Id = bu2Id, ["name"] = "Sales" };
            service.Create(bu2);
            
            // Create a role (creates shadow copy)
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Sales Manager", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(role);
            
            // Find shadow role
            var shadowRole = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("parentroleid") != null)
                .Where(r => r.GetAttributeValue<EntityReference>("parentroleid").Id == roleId)
                .First();
            
            // Act & Assert - cannot delete shadow role directly
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                service.Delete("role", shadowRole.Id);
            });
            
            Assert.Contains("Shadow role", exception.Message);
        }
        
        #endregion
        
        #region Role Assignment Rules Tests
        
        [Fact]
        public void Should_Enforce_Same_BU_Role_Assignment_In_Traditional_Mode()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud()
                .AddSecurity();
                
            var context = builder.Build();
            context.SecurityConfiguration.UseModernBusinessUnits = false; // Traditional mode
            
            // Load metadata for systemuserroles (many-to-many relationship entity)
            context.InitializeMetadataFromCdmFiles(new[] { "/home/runner/work/Fake4Dataverse/Fake4Dataverse/Fake4DataverseCore/system-edm-files/SystemUserRoles.cdm.json" });
            
            var service = context.GetOrganizationService();
            
            // Create two business units
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit") { Id = bu2Id, ["name"] = "Sales" };
            service.Create(bu2);
            
            // Create user in BU1
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(user);
            
            // Create role in BU1
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Role 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(role);
            
            // Find shadow role in BU2
            var shadowRoleInBU2 = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("businessunitid").Id == bu2Id)
                .Where(r => r.GetAttributeValue<string>("name") == "Role 1")
                .First();
            
            // Act & Assert - cannot assign role from different BU (create systemuserroles with cross-BU role)
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = new EntityReference("systemuser", userId),
                ["roleid"] = new EntityReference("role", shadowRoleInBU2.Id)
            };
            
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                service.Create(userRole);
            });
            
            Assert.Contains("same business unit", exception.Message);
        }
        
        [Fact]
        public void Should_Allow_Cross_BU_Role_Assignment_In_Modern_BU_Mode()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            context.SecurityConfiguration.UseModernBusinessUnits = true; // Modern mode
            
            // Load metadata for systemuserroles (many-to-many relationship entity)
            context.InitializeMetadataFromCdmFiles(new[] { "/home/runner/work/Fake4Dataverse/Fake4Dataverse/Fake4DataverseCore/system-edm-files/SystemUserRoles.cdm.json" });
            
            var service = context.GetOrganizationService();
            
            // Create two business units
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit") { Id = bu2Id, ["name"] = "Sales" };
            service.Create(bu2);
            
            // Create user in BU1
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(user);
            
            // Create role in BU1
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Role 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(role);
            
            // Find shadow role in BU2
            var shadowRoleInBU2 = context.CreateQuery("role")
                .Where(r => r.GetAttributeValue<EntityReference>("businessunitid").Id == bu2Id)
                .Where(r => r.GetAttributeValue<string>("name") == "Role 1")
                .First();
            
            // Act - assign role from different BU (should work in modern mode)
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = new EntityReference("systemuser", userId),
                ["roleid"] = new EntityReference("role", shadowRoleInBU2.Id)
            };
            service.Create(userRole);
            
            // Assert - no exception thrown
            var userRoles = context.SecurityManager.GetUserRoles(userId);
            Assert.Contains(shadowRoleInBU2.Id, userRoles);
        }
        
        [Fact]
        public void Should_Remove_Role_Assignments_When_User_BU_Changes()
        {
            // Arrange
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud();
                
            var context = builder.Build();
            
            // Load metadata for systemuserroles (many-to-many relationship entity)
            context.InitializeMetadataFromCdmFiles(new[] { "/home/runner/work/Fake4Dataverse/Fake4Dataverse/Fake4DataverseCore/system-edm-files/SystemUserRoles.cdm.json" });
            
            var service = context.GetOrganizationService();
            
            // Create two business units
            var bu1Id = context.SecurityManager.RootBusinessUnitId;
            var bu2Id = Guid.NewGuid();
            var bu2 = new Entity("businessunit") { Id = bu2Id, ["name"] = "Sales" };
            service.Create(bu2);
            
            // Create user in BU1
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(user);
            
            // Create and assign role
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Role 1", ["businessunitid"] = new EntityReference("businessunit", bu1Id) };
            service.Create(role);
            
            // Assign role to user via systemuserroles entity
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = new EntityReference("systemuser", userId),
                ["roleid"] = new EntityReference("role", roleId)
            };
            service.Create(userRole);
            
            // Verify role is assigned
            Assert.NotEmpty(context.SecurityManager.GetUserRoles(userId));
            
            // Act - update user's business unit
            user["businessunitid"] = new EntityReference("businessunit", bu2Id);
            service.Update(user);
            
            // Assert - role assignments should be removed
            Assert.Empty(context.SecurityManager.GetUserRoles(userId));
        }
        
        #endregion
        
        #region Integration Tests
        
        [Fact]
        public void Complete_Security_Scenario_With_Privilege_Checking()
        {
            // Arrange - set up complete security environment
            var builder = MiddlewareBuilder.New()
                .AddRoleLifecycle()
                .AddCrud()
                .UseCrud()
                .AddSecurity();
                
            var context = builder.Build();
            context.SecurityConfiguration.SecurityEnabled = true;
            context.SecurityConfiguration.EnforcePrivilegeDepth = true;
            
            var service = context.GetOrganizationService();
            
            // Load account metadata
            context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "sales" }).Wait();
            
            // Create business unit
            var buId = context.SecurityManager.RootBusinessUnitId;
            
            // Create user
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Sales Rep", ["businessunitid"] = new EntityReference("businessunit", buId) };
            
            // Create role
            var roleId = Guid.NewGuid();
            var role = new Entity("role") { Id = roleId, ["name"] = "Sales Representative", ["businessunitid"] = new EntityReference("businessunit", buId) };
            
            // Grant privileges to role
            var prvCreate = context.CreateQuery("privilege").First(p => p.GetAttributeValue<string>("name") == "prvCreateAccount");
            var prvRead = context.CreateQuery("privilege").First(p => p.GetAttributeValue<string>("name") == "prvReadAccount");
            var prvWrite = context.CreateQuery("privilege").First(p => p.GetAttributeValue<string>("name") == "prvWriteAccount");
            
            // Assign role to user via systemuserroles entity
            var userRole = new Entity("systemuserroles")
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = userId,
                ["roleid"] = roleId
            };
            
            // Initialize all entities together
            context.Initialize(new[]
            {
                user,
                role,
                new Entity("roleprivileges") { Id = Guid.NewGuid(), ["roleid"] = new EntityReference("role", roleId), ["privilegeid"] = new EntityReference("privilege", prvCreate.Id), ["privilegedepthmask"] = 1 },
                new Entity("roleprivileges") { Id = Guid.NewGuid(), ["roleid"] = new EntityReference("role", roleId), ["privilegeid"] = new EntityReference("privilege", prvRead.Id), ["privilegedepthmask"] = 1 },
                new Entity("roleprivileges") { Id = Guid.NewGuid(), ["roleid"] = new EntityReference("role", roleId), ["privilegeid"] = new EntityReference("privilege", prvWrite.Id), ["privilegedepthmask"] = 1 },
                userRole
            });
            
            // Set caller
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
            
            // Act - perform operations
            var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
            var account = service.Retrieve("account", accountId, new ColumnSet("name"));
            service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated Account" });
            
            // Assert - all operations succeeded with proper privileges
            Assert.NotEqual(Guid.Empty, accountId);
            Assert.NotNull(account);
        }
        
        #endregion
    }
}
