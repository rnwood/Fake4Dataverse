# Auditing in Fake4Dataverse

Testing audit functionality is essential for ensuring your Dataverse applications properly track changes to data. This guide covers enabling auditing, testing audit records, and retrieving audit history.


**Reference:** [Dataverse Auditing Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Microsoft documentation on the auditing system in Dataverse, including audit entity structure, audit details, and auditing configuration.

## Table of Contents

- [Overview](#overview)
- [Enabling Auditing](#enabling-auditing)
- [Testing CRUD Auditing](#testing-crud-auditing)
  - [Create Operations](#create-operations)
  - [Update Operations](#update-operations)
  - [Delete Operations](#delete-operations)
- [Retrieving Audit History](#retrieving-audit-history)
  - [Record Change History](#record-change-history)
  - [Attribute Change History](#attribute-change-history)
  - [Audit Details](#audit-details)
- [Querying Audit Records](#querying-audit-records)
- [User Tracking](#user-tracking)
- [Clearing Audit Data](#clearing-audit-data)
- [Complete Examples](#complete-examples)
- [Key Differences from FakeXrmEasy v2](#key-differences-from-fakexrmeasy-v2)
- [Best Practices](#best-practices)
- [See Also](#see-also)

## Overview

Dataverse auditing tracks changes to records over time, capturing:

- **Create operations**: When records are created
- **Update operations**: When records are modified, including old and new attribute values
- **Delete operations**: When records are deleted
- **User information**: Which user performed each operation
- **Timestamps**: When each operation occurred

In Dataverse, auditing must be explicitly enabled at both the organization and entity level. In Fake4Dataverse, auditing is disabled by default to match this behavior.

## Enabling Auditing

To enable auditing in your tests:

```csharp
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Middleware;

[Fact]
public void Should_TrackChanges_When_AuditingIsEnabled()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Enable auditing
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Act - Create a record
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Contoso"
    });
    
    // Assert - Audit record was created
    var auditRecords = auditRepository.GetAllAuditRecords();
    Assert.Single(auditRecords);
}
```

**Note:** Auditing only captures changes made after it is enabled.

## Testing CRUD Auditing

### Create Operations

**Reference:** [Auditing Create Operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview#audit-operations) - In Dataverse, Create operations are recorded in the audit log with action = 1 (Create).

```csharp
[Fact]
public void Should_AuditCreateOperation()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Act
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Test Account",
        ["revenue"] = new Money(100000)
    });
    
    // Assert
    var auditRecords = auditRepository.GetAllAuditRecords().ToList();
    Assert.Single(auditRecords);
    
    var auditRecord = auditRecords.First();
    Assert.Equal(AuditAction.Create, auditRecord.GetAttributeValue<int>("action"));
    Assert.Equal("Create", auditRecord.GetAttributeValue<string>("operation"));
    
    var objectId = auditRecord.GetAttributeValue<EntityReference>("objectid");
    Assert.Equal("account", objectId.LogicalName);
    Assert.Equal(accountId, objectId.Id);
}
```

### Update Operations

**Reference:** [Auditing Update Operations](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.attributeauditdetail) - Update operations track which attributes changed, storing both old and new values in AttributeAuditDetail.

```csharp
[Fact]
public void Should_AuditUpdateOperation_WithAttributeChanges()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create account
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Original Name",
        ["revenue"] = new Money(100000)
    });
    
    // Clear creation audit
    auditRepository.ClearAuditData();
    
    // Act - Update the account
    service.Update(new Entity("account", accountId)
    {
        ["name"] = "Updated Name",
        ["revenue"] = new Money(200000)
    });
    
    // Assert
    var auditRecords = auditRepository.GetAllAuditRecords().ToList();
    Assert.Single(auditRecords);
    
    var auditRecord = auditRecords.First();
    Assert.Equal(AuditAction.Update, auditRecord.GetAttributeValue<int>("action"));
    
    // Check audit details for old/new values
    var auditId = auditRecord.GetAttributeValue<Guid>("auditid");
    var auditDetail = (AttributeAuditDetail)auditRepository.GetAuditDetails(auditId);
    
    Assert.Equal("Original Name", auditDetail.OldValue.GetAttributeValue<string>("name"));
    Assert.Equal("Updated Name", auditDetail.NewValue.GetAttributeValue<string>("name"));
    Assert.Equal(100000m, auditDetail.OldValue.GetAttributeValue<Money>("revenue").Value);
    Assert.Equal(200000m, auditDetail.NewValue.GetAttributeValue<Money>("revenue").Value);
}
```

### Delete Operations

**Reference:** [Auditing Delete Operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview#audit-operations) - Delete operations are recorded with action = 3 (Delete).

```csharp
[Fact]
public void Should_AuditDeleteOperation()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    
    // Create account first
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Enable auditing after creation
    auditRepository.ClearAuditData();
    auditRepository.IsAuditEnabled = true;
    
    // Act - Delete the account
    service.Delete("account", accountId);
    
    // Assert
    var auditRecords = auditRepository.GetAllAuditRecords().ToList();
    Assert.Single(auditRecords);
    
    var auditRecord = auditRecords.First();
    Assert.Equal(AuditAction.Delete, auditRecord.GetAttributeValue<int>("action"));
}
```

## Retrieving Audit History

### Record Change History

**Reference:** [RetrieveRecordChangeHistoryRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieverecordchangehistoryrequest) - Retrieves all audit records for a specific entity, showing the complete change history.

```csharp
[Fact]
public void Should_RetrieveRecordChangeHistory()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create and update account multiple times
    var accountId = service.Create(new Entity("account") { ["name"] = "V1" });
    service.Update(new Entity("account", accountId) { ["name"] = "V2" });
    service.Update(new Entity("account", accountId) { ["name"] = "V3" });
    
    // Act - Retrieve change history
    var request = new RetrieveRecordChangeHistoryRequest
    {
        Target = new EntityReference("account", accountId)
    };
    
    var response = (RetrieveRecordChangeHistoryResponse)service.Execute(request);
    
    // Assert
    Assert.Equal(3, response.AuditDetailCollection.AuditDetails.Count);
    
    // First is Create, then two Updates
    var audits = response.AuditDetailCollection.AuditDetails;
    Assert.All(audits, audit => Assert.NotNull(audit.AuditRecord));
}
```

### Attribute Change History

**Reference:** [RetrieveAttributeChangeHistoryRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveattributechangehistoryrequest) - Retrieves audit history for a specific attribute, useful for tracking changes to individual fields.

```csharp
[Fact]
public void Should_RetrieveAttributeChangeHistory()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create account and update different attributes
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Test",
        ["revenue"] = new Money(1000)
    });
    
    service.Update(new Entity("account", accountId) { ["name"] = "Updated" });
    service.Update(new Entity("account", accountId) { ["revenue"] = new Money(2000) });
    service.Update(new Entity("account", accountId) { ["name"] = "Final" });
    
    // Act - Get history for "name" attribute only
    var request = new RetrieveAttributeChangeHistoryRequest
    {
        Target = new EntityReference("account", accountId),
        AttributeLogicalName = "name"
    };
    
    var response = (RetrieveAttributeChangeHistoryResponse)service.Execute(request);
    
    // Assert - Should have 2 updates where name changed (not revenue-only)
    Assert.Equal(2, response.AuditDetailCollection.AuditDetails.Count);
}
```

### Audit Details

**Reference:** [RetrieveAuditDetailsRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest) - Retrieves the full audit details for a specific audit record, including old and new values.

```csharp
[Fact]
public void Should_RetrieveAuditDetails()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create and update account
    var accountId = service.Create(new Entity("account") { ["name"] = "Before" });
    service.Update(new Entity("account", accountId) { ["name"] = "After" });
    
    // Get the update audit record
    var accountRef = new EntityReference("account", accountId);
    var auditRecords = auditRepository.GetAuditRecordsForEntity(accountRef).ToList();
    var updateAuditId = auditRecords.Last().GetAttributeValue<Guid>("auditid");
    
    // Act - Retrieve audit details
    var request = new RetrieveAuditDetailsRequest
    {
        AuditId = updateAuditId
    };
    
    var response = (RetrieveAuditDetailsResponse)service.Execute(request);
    
    // Assert
    Assert.NotNull(response.AuditDetail);
    var attrDetail = (AttributeAuditDetail)response.AuditDetail;
    Assert.Equal("Before", attrDetail.OldValue.GetAttributeValue<string>("name"));
    Assert.Equal("After", attrDetail.NewValue.GetAttributeValue<string>("name"));
}
```

## Querying Audit Records

You can query audit records directly through the repository:

```csharp
[Fact]
public void Should_QueryAuditRecords()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create multiple accounts
    var account1 = service.Create(new Entity("account") { ["name"] = "Account 1" });
    var account2 = service.Create(new Entity("account") { ["name"] = "Account 2" });
    
    // Act - Get audit records for specific account
    var account1Ref = new EntityReference("account", account1);
    var account1Audits = auditRepository.GetAuditRecordsForEntity(account1Ref).ToList();
    
    // Assert
    Assert.Single(account1Audits);
    
    var objectId = account1Audits[0].GetAttributeValue<EntityReference>("objectid");
    Assert.Equal(account1, objectId.Id);
}
```

## User Tracking

**Reference:** [User Auditing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview#audit-user-access) - Audit records track which user performed each operation using the userid field.

```csharp
[Fact]
public void Should_TrackUserInAuditRecords()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Set the calling user
    var userId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // Act
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Assert - Audit record contains user ID
    var auditRecords = auditRepository.GetAllAuditRecords().ToList();
    var auditUserId = auditRecords[0].GetAttributeValue<EntityReference>("userid");
    Assert.Equal(userId, auditUserId.Id);
}
```

## Clearing Audit Data

**Reference:** [DeleteAuditDataRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.deleteauditdatarequest) - In Dataverse, DeleteAuditDataRequest allows administrators to delete audit records. In Fake4Dataverse, use ClearAuditData() for testing.

```csharp
[Fact]
public void Should_ClearAuditData()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Create audit records
    service.Create(new Entity("account") { ["name"] = "Account 1" });
    service.Create(new Entity("account") { ["name"] = "Account 2" });
    
    Assert.Equal(2, auditRepository.GetAllAuditRecords().Count());
    
    // Act
    auditRepository.ClearAuditData();
    
    // Assert
    Assert.Empty(auditRepository.GetAllAuditRecords());
}
```

## Complete Examples

### Testing Audit Trail for Compliance

```csharp
[Fact]
public void Should_MaintainCompleteAuditTrail()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    auditRepository.IsAuditEnabled = true;
    
    // Set up users
    var user1 = Guid.NewGuid();
    var user2 = Guid.NewGuid();
    
    // Act - Simulate lifecycle
    context.CallerProperties.CallerId = new EntityReference("systemuser", user1);
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Contoso",
        ["revenue"] = new Money(100000)
    });
    
    System.Threading.Thread.Sleep(100); // Ensure different timestamps
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", user2);
    service.Update(new Entity("account", accountId)
    {
        ["revenue"] = new Money(150000)
    });
    
    // Assert - Verify complete audit trail
    var accountRef = new EntityReference("account", accountId);
    var audits = auditRepository.GetAuditRecordsForEntity(accountRef).ToList();
    
    Assert.Equal(2, audits.Count);
    
    // Verify Create operation
    Assert.Equal(AuditAction.Create, audits[0].GetAttributeValue<int>("action"));
    Assert.Equal(user1, audits[0].GetAttributeValue<EntityReference>("userid").Id);
    
    // Verify Update operation
    Assert.Equal(AuditAction.Update, audits[1].GetAttributeValue<int>("action"));
    Assert.Equal(user2, audits[1].GetAttributeValue<EntityReference>("userid").Id);
    
    // Verify timestamps are ordered
    var createTime = audits[0].GetAttributeValue<DateTime>("createdon");
    var updateTime = audits[1].GetAttributeValue<DateTime>("createdon");
    Assert.True(updateTime > createTime);
}
```

## Key Differences from FakeXrmEasy v2

**Important**: The audit implementation in Fake4Dataverse differs from FakeXrmEasy v2+ in several ways:

### Setup and Configuration

| Feature | FakeXrmEasy v2+ | Fake4Dataverse |
|---------|----------------|----------------|
| **Enable Auditing** | `context.AuditingEnabled = true` | `context.GetProperty<IAuditRepository>().IsAuditEnabled = true` |
| **Access Audits** | `context.GetAuditRecords()` | `context.GetProperty<IAuditRepository>().GetAllAuditRecords()` |
| **Clear Audits** | `context.ClearAudits()` | `context.GetProperty<IAuditRepository>().ClearAuditData()` |

### Key Differences:

1. **Property-based Access**: Fake4Dataverse uses the property system for audit repository access, following its architecture pattern
2. **Interface-based**: Uses `IAuditRepository` interface for better testability and extensibility
3. **SDK Compatibility**: Uses SDK `AttributeAuditDetail` class directly instead of custom classes

### Migration Example:

**FakeXrmEasy v2:**
```csharp
context.AuditingEnabled = true;
var audits = context.GetAuditRecords();
```

**Fake4Dataverse:**
```csharp
var auditRepo = context.GetProperty<IAuditRepository>();
auditRepo.IsAuditEnabled = true;
var audits = auditRepo.GetAllAuditRecords();
```

## Best Practices

### 1. Enable Auditing Selectively

Only enable auditing when testing audit-specific functionality to avoid performance overhead:

```csharp
// Don't do this for all tests
auditRepository.IsAuditEnabled = true;

// Instead, enable only in audit-specific tests
[Trait("Category", "Audit")]
public class AuditTests
{
    [Fact]
    public void Should_TestAuditing()
    {
        var auditRepository = context.GetProperty<IAuditRepository>();
        auditRepository.IsAuditEnabled = true;
        // Test logic
    }
}
```

### 2. Clear Audit Data Between Test Sections

When testing multiple operations, clear audit data to isolate test sections:

```csharp
// Create operation
service.Create(entity);
auditRepository.ClearAuditData();

// Now test update operation in isolation
service.Update(entity);
var updateAudits = auditRepository.GetAllAuditRecords();
```

### 3. Set User Context for Realistic Tests

Always set the calling user to test user tracking:

```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
service.Create(entity);
```

### 4. Verify Timestamps

Check that audit records have realistic timestamps:

```csharp
var audit = auditRepository.GetAllAuditRecords().First();
var createTime = audit.GetAttributeValue<DateTime>("createdon");
Assert.True(createTime <= DateTime.UtcNow);
Assert.True(createTime > DateTime.UtcNow.AddMinutes(-1));
```

## See Also

- [Dataverse Auditing Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Official Microsoft documentation
- [Configure Auditing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure) - Audit configuration details
- [Security and Permissions](./security-permissions.md) - Related security testing
- [Testing Plugins](./testing-plugins.md) - Test plugins with auditing
- [Message Executors](../messages/audit.md) - Audit message reference

---

**Questions?** Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
