using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.AuditTests
{
    /// <summary>
    /// Tests for entity and attribute-level audit settings
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
    /// 
    /// In Dataverse, auditing requires multiple levels to be enabled:
    /// 1. Organization-level (IsAuditEnabled on Organization)
    /// 2. Entity-level (IsAuditEnabled on EntityMetadata)
    /// 3. Attribute-level (IsAuditEnabled on AttributeMetadata)
    /// </summary>
    public class AuditMetadataSettingsTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_NotAudit_When_EntityAuditingIsDisabled()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
            // Entity-level auditing must be enabled for auditing to occur on that entity
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Create entity metadata with auditing DISABLED
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(false)  // Entity auditing disabled
            };
            context.InitializeMetadata(entityMetadata);
            
            // Act - Create an account
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            
            // Assert - No audit should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Empty(auditRecords);
        }

        [Fact]
        public void Should_Audit_When_EntityAuditingIsEnabled()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Create entity metadata with auditing ENABLED
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(true)  // Entity auditing enabled
            };
            context.InitializeMetadata(entityMetadata);
            
            // Act - Create an account
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            
            // Assert - Audit should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);
            Assert.Equal(AuditAction.Create, auditRecords[0].GetAttributeValue<int>("action"));
        }

        [Fact]
        public void Should_OnlyAuditEnabledAttributes_When_AttributeAuditingIsConfigured()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
            // Attribute-level auditing controls which fields are tracked in updates
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Create entity metadata with selective attribute auditing
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };
            var nameAttribute = new StringAttributeMetadata
            {
                LogicalName = "name",
                IsAuditEnabled = new BooleanManagedProperty(true)  // Name is audited
            };
            var descriptionAttribute = new StringAttributeMetadata
            {
                LogicalName = "description",
                IsAuditEnabled = new BooleanManagedProperty(false)  // Description is NOT audited
            };
            entityMetadata.SetAttributeCollection(new[] { nameAttribute, descriptionAttribute });
            context.InitializeMetadata(entityMetadata);
            
            // Create account
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Original Name",
                ["description"] = "Original Description"
            });
            
            // Clear creation audit
            auditRepository.ClearAuditData();
            
            // Act - Update both name and description
            service.Update(new Entity("account", accountId)
            {
                ["name"] = "Updated Name",
                ["description"] = "Updated Description"
            });
            
            // Assert - Only name change should be audited
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);
            
            var auditId = auditRecords[0].GetAttributeValue<Guid>("auditid");
            var auditDetail = (Microsoft.Crm.Sdk.Messages.AttributeAuditDetail)auditRepository.GetAuditDetails(auditId);
            
            // Name should be in audit
            Assert.True(auditDetail.NewValue.Contains("name"));
            Assert.Equal("Original Name", auditDetail.OldValue.GetAttributeValue<string>("name"));
            Assert.Equal("Updated Name", auditDetail.NewValue.GetAttributeValue<string>("name"));
            
            // Description should NOT be in audit
            Assert.False(auditDetail.NewValue.Contains("description"));
        }

        [Fact]
        public void Should_AuditAllAttributes_When_NoMetadataIsDefined()
        {
            // Arrange
            // When no metadata is defined (dynamic entities), all attributes should be audited
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // No metadata defined for "account" - should audit all attributes
            
            // Create account
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Original",
                ["description"] = "Original Desc"
            });
            
            // Clear creation audit
            auditRepository.ClearAuditData();
            
            // Act - Update both attributes
            service.Update(new Entity("account", accountId)
            {
                ["name"] = "Updated",
                ["description"] = "Updated Desc"
            });
            
            // Assert - Both attributes should be audited
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);
            
            var auditId = auditRecords[0].GetAttributeValue<Guid>("auditid");
            var auditDetail = (Microsoft.Crm.Sdk.Messages.AttributeAuditDetail)auditRepository.GetAuditDetails(auditId);
            
            // Both should be in audit
            Assert.True(auditDetail.NewValue.Contains("name"));
            Assert.True(auditDetail.NewValue.Contains("description"));
        }

        [Fact]
        public void Should_AuditAttribute_When_NoAttributeMetadataExists()
        {
            // Arrange
            // If entity metadata exists but specific attribute metadata doesn't, audit that attribute
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Create entity metadata with only "name" attribute defined
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };
            var nameAttribute = new StringAttributeMetadata
            {
                LogicalName = "name",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };
            entityMetadata.SetAttributeCollection(new[] { nameAttribute });
            // "description" attribute metadata not defined
            context.InitializeMetadata(entityMetadata);
            
            // Create account
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Original",
                ["description"] = "Original Desc"
            });
            
            // Clear creation audit
            auditRepository.ClearAuditData();
            
            // Act - Update both attributes
            service.Update(new Entity("account", accountId)
            {
                ["name"] = "Updated",
                ["description"] = "Updated Desc"
            });
            
            // Assert - Both should be audited (undefined attributes are audited)
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);
            
            var auditId = auditRecords[0].GetAttributeValue<Guid>("auditid");
            var auditDetail = (Microsoft.Crm.Sdk.Messages.AttributeAuditDetail)auditRepository.GetAuditDetails(auditId);
            
            Assert.True(auditDetail.NewValue.Contains("name"));
            Assert.True(auditDetail.NewValue.Contains("description"));
        }

        [Fact]
        public void Should_NotCreateAuditRecord_When_NoAuditedAttributesChanged()
        {
            // Arrange
            // If only non-audited attributes change, no audit record should be created
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Create entity metadata with only "name" audited
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };
            var nameAttribute = new StringAttributeMetadata
            {
                LogicalName = "name",
                IsAuditEnabled = new BooleanManagedProperty(true)  // Audited
            };
            var descriptionAttribute = new StringAttributeMetadata
            {
                LogicalName = "description",
                IsAuditEnabled = new BooleanManagedProperty(false)  // NOT audited
            };
            entityMetadata.SetAttributeCollection(new[] { nameAttribute, descriptionAttribute });
            context.InitializeMetadata(entityMetadata);
            
            // Create account
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test",
                ["description"] = "Original Description"
            });
            
            // Clear creation audit
            auditRepository.ClearAuditData();
            
            // Act - Update ONLY description (non-audited attribute)
            service.Update(new Entity("account", accountId)
            {
                ["description"] = "Updated Description"
            });
            
            // Assert - No audit record should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Empty(auditRecords);
        }

        [Fact]
        public void Should_RespectMultipleEntityAuditSettings()
        {
            // Arrange
            // Different entities can have different audit settings
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            
            // Enable organization-level auditing
            auditRepository.IsAuditEnabled = true;
            
            // Account: auditing enabled
            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };
            
            // Contact: auditing disabled
            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact",
                IsAuditEnabled = new BooleanManagedProperty(false)
            };
            
            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });
            
            // Act - Create both entities
            var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
            var contactId = service.Create(new Entity("contact") { ["firstname"] = "Test Contact" });
            
            // Assert - Only account should be audited
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);
            Assert.Equal("account", auditRecords[0].GetAttributeValue<EntityReference>("objectid").LogicalName);
        }
    }
}
