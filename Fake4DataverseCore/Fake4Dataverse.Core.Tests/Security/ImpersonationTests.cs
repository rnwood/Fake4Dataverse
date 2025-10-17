using Fake4Dataverse.Security;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Security
{
    /// <summary>
    /// Tests for impersonation functionality.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
    /// 
    /// Impersonation allows a user with the prvActOnBehalfOfAnotherUser privilege to perform operations
    /// as if they were another user. The impersonated user's identity is used for security checks and audit fields,
    /// while the actual calling user is recorded in createdonbehalfof/modifiedonbehalfof fields.
    /// </summary>
    public class ImpersonationTests
    {
        [Fact]
        public void Should_Allow_SystemAdministrator_To_Impersonate()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // The System Administrator role has the prvActOnBehalfOfAnotherUser privilege by default.
            
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            var service = context.GetOrganizationService();
            
            var adminUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var businessUnitId = context.SecurityManager.RootBusinessUnitId;

            // Create admin user and assign System Administrator role
            var adminUser = new Entity("systemuser")
            {
                Id = adminUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Admin User"
            };
            context.AddEntity(adminUser);

            // Create target user
            var targetUser = new Entity("systemuser")
            {
                Id = targetUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Target User"
            };
            context.AddEntity(targetUser);

            // Assign System Administrator role to admin
            context.SecurityManager.AssignRole(adminUserId, context.SecurityManager.SystemAdministratorRoleId);

            // Set caller as admin
            context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
            
            // Set impersonation
            context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

            // Act - Create an account (should succeed with no exception)
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Assert - Should not throw
            var exception = Record.Exception(() => service.Create(account));
            Assert.Null(exception);
        }

        [Fact]
        public void Should_Set_CreatedBy_To_ImpersonatedUser()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // When impersonating, createdby should be set to the impersonated user.
            
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            var service = context.GetOrganizationService();

            var adminUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var businessUnitId = context.SecurityManager.RootBusinessUnitId;

            // Create users
            var adminUser = new Entity("systemuser")
            {
                Id = adminUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Admin User"
            };
            context.AddEntity(adminUser);

            var targetUser = new Entity("systemuser")
            {
                Id = targetUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Target User"
            };
            context.AddEntity(targetUser);

            // Assign System Administrator role
            context.SecurityManager.AssignRole(adminUserId, context.SecurityManager.SystemAdministratorRoleId);

            // Set impersonation
            context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
            context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

            // Act - Create an account
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            service.Create(account);

            // Assert - createdby should be target user
            var retrieved = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.NotNull(retrieved.GetAttributeValue<EntityReference>("createdby"));
            Assert.Equal(targetUserId, retrieved.GetAttributeValue<EntityReference>("createdby").Id);
        }

        [Fact]
        public void Should_Set_CreatedOnBehalfOf_To_CallingUser()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // When impersonating, createdonbehalfof should be set to the actual calling user (the impersonator).
            
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            var service = context.GetOrganizationService();

            var adminUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var businessUnitId = context.SecurityManager.RootBusinessUnitId;

            // Create users
            var adminUser = new Entity("systemuser")
            {
                Id = adminUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Admin User"
            };
            context.AddEntity(adminUser);

            var targetUser = new Entity("systemuser")
            {
                Id = targetUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Target User"
            };
            context.AddEntity(targetUser);

            // Assign System Administrator role
            context.SecurityManager.AssignRole(adminUserId, context.SecurityManager.SystemAdministratorRoleId);

            // Set impersonation
            context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
            context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

            // Act - Create an account
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            service.Create(account);

            // Assert - createdonbehalfof should be admin user (the impersonator)
            var retrieved = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.True(retrieved.Contains("createdonbehalfof"));
            Assert.NotNull(retrieved.GetAttributeValue<EntityReference>("createdonbehalfof"));
            Assert.Equal(adminUserId, retrieved.GetAttributeValue<EntityReference>("createdonbehalfof").Id);
        }

        [Fact]
        public void Should_Deny_Impersonation_Without_Privilege()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
            // A user without the prvActOnBehalfOfAnotherUser privilege cannot impersonate.
            
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            var service = context.GetOrganizationService();
            
            var regularUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var businessUnitId = context.SecurityManager.RootBusinessUnitId;

            // Create regular user (no special privileges)
            var regularUser = new Entity("systemuser")
            {
                Id = regularUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Regular User"
            };
            context.AddEntity(regularUser);

            // Create target user
            var targetUser = new Entity("systemuser")
            {
                Id = targetUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Target User"
            };
            context.AddEntity(targetUser);

            // Set caller as regular user
            context.CallerProperties.CallerId = new EntityReference("systemuser", regularUserId);
            
            // Set impersonation
            context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

            // Act & Assert - Should throw UnauthorizedAccessException
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            var exception = Assert.Throws<UnauthorizedAccessException>(() => service.Create(account));
            Assert.Contains("prvActOnBehalfOfAnotherUser", exception.Message);
        }

        [Fact]
        public void Should_Not_Set_CreatedOnBehalfOf_Without_Impersonation()
        {
            // When not impersonating, createdonbehalfof should not be set.
            
            // Arrange
            var context = new XrmFakedContext();
            context.SecurityConfiguration.SecurityEnabled = true;
            var service = context.GetOrganizationService();

            var adminUserId = Guid.NewGuid();
            var businessUnitId = context.SecurityManager.RootBusinessUnitId;

            // Create admin user
            var adminUser = new Entity("systemuser")
            {
                Id = adminUserId,
                ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
                ["fullname"] = "Admin User"
            };
            context.AddEntity(adminUser);
            context.SecurityManager.AssignRole(adminUserId, context.SecurityManager.SystemAdministratorRoleId);

            // Set caller (no impersonation)
            context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
            context.CallerProperties.ImpersonatedUserId = null; // Explicitly no impersonation

            // Act - Create an account
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            service.Create(account);

            // Assert - createdonbehalfof should NOT be set
            var retrieved = service.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.False(retrieved.Contains("createdonbehalfof"));
        }
    }
}
