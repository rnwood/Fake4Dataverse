using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.AuditTests
{
    /// <summary>
    /// Tests for audit functionality
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
    /// 
    /// Dataverse auditing tracks changes to records including Create, Update, Delete operations
    /// and captures old/new values for changed attributes.
    /// </summary>
    public class AuditRepositoryTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Not_CreateAuditRecords_When_AuditingIsDisabled()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure
            // Auditing must be explicitly enabled in Dataverse. By default, it is disabled.
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();

            // Act - Create an account without audit enabled
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test Account"
            });

            // Assert - No audit records should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Empty(auditRecords);
        }

        [Fact]
        public void Should_CreateAuditRecord_When_EntityIsCreated()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
            // When auditing is enabled, Create operations are tracked in the audit log
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Act - Create an account
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test Account"
            });

            // Assert - Audit record should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);

            var auditRecord = auditRecords.First();
            Assert.Equal("audit", auditRecord.LogicalName);
            Assert.Equal(AuditAction.Create, auditRecord.GetAttributeValue<int>("action"));
            Assert.Equal("Create", auditRecord.GetAttributeValue<string>("operation"));
            
            var objectId = auditRecord.GetAttributeValue<EntityReference>("objectid");
            Assert.Equal("account", objectId.LogicalName);
            Assert.Equal(accountId, objectId.Id);
        }

        [Fact]
        public void Should_CreateAuditRecord_When_EntityIsUpdated()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
            // Update operations track which attributes changed and their old/new values
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();

            // Create account first
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Original Name",
                ["revenue"] = new Money(1000)
            });

            // Clear any creation audit records and enable auditing
            auditRepository.ClearAuditData();
            auditRepository.IsAuditEnabled = true;

            // Act - Update the account
            service.Update(new Entity("account", accountId)
            {
                ["name"] = "Updated Name",
                ["revenue"] = new Money(2000)
            });

            // Assert - Audit record should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);

            var auditRecord = auditRecords.First();
            Assert.Equal(AuditAction.Update, auditRecord.GetAttributeValue<int>("action"));
            Assert.Equal("Update", auditRecord.GetAttributeValue<string>("operation"));
        }

        [Fact]
        public void Should_CreateAuditRecord_When_EntityIsDeleted()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
            // Delete operations are tracked in the audit log
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();

            // Create account first
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Test Account"
            });

            // Clear any creation audit records and enable auditing
            auditRepository.ClearAuditData();
            auditRepository.IsAuditEnabled = true;

            // Act - Delete the account
            service.Delete("account", accountId);

            // Assert - Audit record should be created
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);

            var auditRecord = auditRecords.First();
            Assert.Equal(AuditAction.Delete, auditRecord.GetAttributeValue<int>("action"));
            Assert.Equal("Delete", auditRecord.GetAttributeValue<string>("operation"));
            
            var objectId = auditRecord.GetAttributeValue<EntityReference>("objectid");
            Assert.Equal("account", objectId.LogicalName);
            Assert.Equal(accountId, objectId.Id);
        }

        [Fact]
        public void Should_TrackAttributeChanges_When_EntityIsUpdated()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.attributeauditdetail
            // AttributeAuditDetail contains old and new values for changed attributes
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create account first
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Original Name",
                ["revenue"] = new Money(1000)
            });

            // Clear creation audit
            auditRepository.ClearAuditData();

            // Act - Update specific attributes
            service.Update(new Entity("account", accountId)
            {
                ["name"] = "Updated Name"
            });

            // Assert - Audit details should contain attribute changes
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);

            var auditId = auditRecords.First().GetAttributeValue<Guid>("auditid");
            var auditDetail = auditRepository.GetAuditDetails(auditId);
            
            Assert.NotNull(auditDetail);
            Assert.IsType<AttributeAuditDetail>(auditDetail);
            
            var attrDetail = (AttributeAuditDetail)auditDetail;
            Assert.Equal("Original Name", attrDetail.OldValue.GetAttributeValue<string>("name"));
            Assert.Equal("Updated Name", attrDetail.NewValue.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Should_RetrieveAuditRecordsForEntity()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create and update an account multiple times
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            service.Update(new Entity("account", accountId) { ["name"] = "Test 1" });
            service.Update(new Entity("account", accountId) { ["name"] = "Test 2" });

            // Act - Get audit records for the account
            var accountRef = new EntityReference("account", accountId);
            var auditRecords = auditRepository.GetAuditRecordsForEntity(accountRef).ToList();

            // Assert - Should have 3 audit records (1 create + 2 updates)
            Assert.Equal(3, auditRecords.Count);
            Assert.Equal(AuditAction.Create, auditRecords[0].GetAttributeValue<int>("action"));
            Assert.Equal(AuditAction.Update, auditRecords[1].GetAttributeValue<int>("action"));
            Assert.Equal(AuditAction.Update, auditRecords[2].GetAttributeValue<int>("action"));
        }

        [Fact]
        public void Should_RetrieveAuditRecordsForAttribute()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create account and update different attributes
            var accountId = service.Create(new Entity("account") 
            { 
                ["name"] = "Test",
                ["revenue"] = new Money(1000)
            });
            
            service.Update(new Entity("account", accountId) { ["name"] = "Updated Name" });
            service.Update(new Entity("account", accountId) { ["revenue"] = new Money(2000) });
            service.Update(new Entity("account", accountId) { ["name"] = "Final Name" });

            // Act - Get audit records for the "name" attribute
            var accountRef = new EntityReference("account", accountId);
            var nameAuditRecords = auditRepository.GetAuditRecordsForAttribute(accountRef, "name").ToList();

            // Assert - Should have 3 audit records where name is present:
            // 1. Create (name was set)
            // 2. First Update (name changed)
            // 3. Second Update (name changed)
            // The revenue-only update should not be included
            Assert.Equal(3, nameAuditRecords.Count);
        }

        [Fact]
        public void Should_ClearAuditData()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.deleteauditdatarequest
            // DeleteAuditDataRequest allows clearing audit data for testing or maintenance
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create some audit records
            service.Create(new Entity("account") { ["name"] = "Test 1" });
            service.Create(new Entity("account") { ["name"] = "Test 2" });

            var auditRecordsBefore = auditRepository.GetAllAuditRecords().ToList();
            Assert.Equal(2, auditRecordsBefore.Count);

            // Act - Clear audit data
            auditRepository.ClearAuditData();

            // Assert - All audit records should be removed
            var auditRecordsAfter = auditRepository.GetAllAuditRecords().ToList();
            Assert.Empty(auditRecordsAfter);
        }

        [Fact]
        public void Should_RecordUserInAuditRecord()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
            // Audit records track which user performed the operation
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            var userId = Guid.NewGuid();
            context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

            // Act - Create an account
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });

            // Assert - Audit record should contain the user ID
            var auditRecords = auditRepository.GetAllAuditRecords().ToList();
            Assert.Single(auditRecords);

            var auditRecord = auditRecords.First();
            var auditUserId = auditRecord.GetAttributeValue<EntityReference>("userid");
            Assert.Equal(userId, auditUserId.Id);
        }
    }
}
