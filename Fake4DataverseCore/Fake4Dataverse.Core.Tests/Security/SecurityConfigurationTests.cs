using Fake4Dataverse.Abstractions.Security;
using Fake4Dataverse.Security;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Security
{
    /// <summary>
    /// Tests for the Dataverse security model configuration.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// </summary>
    public class SecurityConfigurationTests
    {
        [Fact]
        public void Should_Create_SecurityConfiguration_With_Security_Disabled_By_Default()
        {
            // Arrange & Act
            var config = new SecurityConfiguration();

            // Assert
            Assert.False(config.SecurityEnabled);
            Assert.False(config.UseModernBusinessUnits);
            Assert.False(config.EnforcePrivilegeDepth);
            Assert.False(config.EnforceRecordLevelSecurity);
            Assert.False(config.EnforceFieldLevelSecurity);
            Assert.True(config.AutoGrantSystemAdministratorPrivileges);
            Assert.Equal(SecurityConfiguration.DefaultSystemAdministratorRoleId, config.SystemAdministratorRoleId);
        }

        [Fact]
        public void Should_Create_Fully_Secured_Configuration()
        {
            // Arrange & Act
            var config = SecurityConfiguration.CreateFullySecured();

            // Assert
            Assert.True(config.SecurityEnabled);
            Assert.True(config.EnforcePrivilegeDepth);
            Assert.True(config.EnforceRecordLevelSecurity);
            Assert.True(config.EnforceFieldLevelSecurity);
            Assert.True(config.AutoGrantSystemAdministratorPrivileges);
        }

        [Fact]
        public void Should_Create_Basic_Security_Configuration()
        {
            // Arrange & Act
            var config = SecurityConfiguration.CreateBasicSecurity();

            // Assert
            Assert.True(config.SecurityEnabled);
            Assert.False(config.EnforcePrivilegeDepth);
            Assert.False(config.EnforceRecordLevelSecurity);
            Assert.False(config.EnforceFieldLevelSecurity);
            Assert.True(config.AutoGrantSystemAdministratorPrivileges);
        }

        [Fact]
        public void Should_Allow_Custom_System_Administrator_Role_Id()
        {
            // Arrange
            var customRoleId = Guid.NewGuid();
            var config = new SecurityConfiguration
            {
                SystemAdministratorRoleId = customRoleId
            };

            // Assert
            Assert.Equal(customRoleId, config.SystemAdministratorRoleId);
        }

        [Fact]
        public void XrmFakedContext_Should_Have_SecurityConfiguration_Property()
        {
            // Arrange & Act
            var context = new XrmFakedContext();

            // Assert
            Assert.NotNull(context.SecurityConfiguration);
            Assert.False(context.SecurityConfiguration.SecurityEnabled);
        }

        [Fact]
        public void Should_Load_Security_Entity_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act - security entities are loaded automatically during initialization
            var systemUserMetadata = context.GetEntityMetadataByName("systemuser");
            var businessUnitMetadata = context.GetEntityMetadataByName("businessunit");
            var teamMetadata = context.GetEntityMetadataByName("team");
            var roleMetadata = context.GetEntityMetadataByName("role");
            var privilegeMetadata = context.GetEntityMetadataByName("privilege");
            var organizationMetadata = context.GetEntityMetadataByName("organization");

            // Assert
            Assert.NotNull(systemUserMetadata);
            Assert.Equal("systemuser", systemUserMetadata.LogicalName);
            Assert.NotNull(businessUnitMetadata);
            Assert.Equal("businessunit", businessUnitMetadata.LogicalName);
            Assert.NotNull(teamMetadata);
            Assert.Equal("team", teamMetadata.LogicalName);
            Assert.NotNull(roleMetadata);
            Assert.Equal("role", roleMetadata.LogicalName);
            Assert.NotNull(privilegeMetadata);
            Assert.Equal("privilege", privilegeMetadata.LogicalName);
            Assert.NotNull(organizationMetadata);
            Assert.Equal("organization", organizationMetadata.LogicalName);
        }

        [Fact]
        public void Should_Create_SystemUser_Entity()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act
            var userId = Guid.NewGuid();
            var user = new Entity("systemuser")
            {
                Id = userId,
                ["fullname"] = "Test User",
                ["domainname"] = "DOMAIN\\testuser",
                ["internalemailaddress"] = "testuser@example.com"
            };

            context.Initialize(user);

            // Assert - use GetEntityById which works without middleware
            var retrievedUser = context.GetEntityById("systemuser", userId);
            Assert.NotNull(retrievedUser);
            Assert.Equal("Test User", retrievedUser.GetAttributeValue<string>("fullname"));
            Assert.Equal("DOMAIN\\testuser", retrievedUser.GetAttributeValue<string>("domainname"));
        }

        [Fact]
        public void Should_Create_BusinessUnit_Entity()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act
            var buId = Guid.NewGuid();
            var businessUnit = new Entity("businessunit")
            {
                Id = buId,
                ["name"] = "Test Business Unit"
            };

            context.Initialize(businessUnit);

            // Assert - use GetEntityById which works without middleware
            var retrievedBU = context.GetEntityById("businessunit", buId);
            Assert.NotNull(retrievedBU);
            Assert.Equal("Test Business Unit", retrievedBU.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Create_Team_Entity()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act
            var teamId = Guid.NewGuid();
            var buId = Guid.NewGuid();
            
            var businessUnit = new Entity("businessunit")
            {
                Id = buId,
                ["name"] = "Test Business Unit"
            };
            
            var team = new Entity("team")
            {
                Id = teamId,
                ["name"] = "Test Team",
                ["businessunitid"] = new EntityReference("businessunit", buId)
            };

            context.Initialize(new[] { businessUnit, team });

            // Assert - use GetEntityById which works without middleware
            var retrievedTeam = context.GetEntityById("team", teamId);
            Assert.NotNull(retrievedTeam);
            Assert.Equal("Test Team", retrievedTeam.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_Create_Role_Entity()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Act
            var roleId = Guid.NewGuid();
            var buId = Guid.NewGuid();
            
            var businessUnit = new Entity("businessunit")
            {
                Id = buId,
                ["name"] = "Test Business Unit"
            };
            
            var role = new Entity("role")
            {
                Id = roleId,
                ["name"] = "Test Role",
                ["businessunitid"] = new EntityReference("businessunit", buId)
            };

            context.Initialize(new[] { businessUnit, role });

            // Assert - use GetEntityById which works without middleware
            var retrievedRole = context.GetEntityById("role", roleId);
            Assert.NotNull(retrievedRole);
            Assert.Equal("Test Role", retrievedRole.GetAttributeValue<string>("name"));
        }
    }
}
