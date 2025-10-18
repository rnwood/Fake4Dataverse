# Auditing in Fake4Dataverse

Testing audit functionality is essential for ensuring your Dataverse applications properly track changes to data. This guide covers enabling auditing, testing audit records, and retrieving audit history.


**Reference:** [Dataverse Auditing Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Microsoft documentation on the auditing system in Dataverse, including audit entity structure, audit details, and auditing configuration.

## Table of Contents

- [Overview](#overview)
- [Enabling Auditing](#enabling-auditing)
- [Metadata-Based Audit Configuration](#metadata-based-audit-configuration)
  - [Organization-Level Settings](#organization-level-settings)
  - [Entity-Level Settings](#entity-level-settings)
  - [Attribute-Level Settings](#attribute-level-settings)
  - [Complete Three-Level Example](#complete-three-level-example)
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

## Metadata-Based Audit Configuration

**Reference:** [Configure Auditing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure) - Microsoft documentation on configuring auditing at organization, entity, and attribute levels in Dataverse.

Fake4Dataverse supports Dataverse's three-level audit configuration system, matching real Dataverse behavior exactly. All three levels must be enabled for auditing to occur.

### Organization-Level Settings

The organization-level setting is the global on/off switch for auditing. If this is disabled, no auditing occurs regardless of entity or attribute settings.

```csharp
using Fake4Dataverse.Abstractions.Audit;

[Fact]
public void Should_RespectOrganizationLevelSetting()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    
    // Organization-level auditing is DISABLED by default
    Assert.False(auditRepository.IsAuditEnabled);
    
    // Enable organization-level auditing
    auditRepository.IsAuditEnabled = true;
    
    // Now auditing can occur (if entity/attribute levels also enabled)
    service.Create(new Entity("account") { ["name"] = "Test" });
    
    var audits = auditRepository.GetAllAuditRecords();
    Assert.NotEmpty(audits);
}
```

### Entity-Level Settings

Entity-level settings control which tables are audited. Even if organization-level auditing is enabled, specific entities must also have auditing enabled through their metadata.

**Reference:** [EntityMetadata.IsAuditEnabled](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata.isauditenabled) - The IsAuditEnabled property controls whether auditing is enabled for a specific entity. This is a BooleanManagedProperty that can be set to true or false.

```csharp
using Microsoft.Xrm.Sdk.Metadata;

[Fact]
public void Should_RespectEntityLevelSettings()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    
    // Enable organization-level auditing
    auditRepository.IsAuditEnabled = true;
    
    // Configure entity metadata - ENABLE auditing for account
    var accountMetadata = new EntityMetadata
    {
        LogicalName = "account",
        IsAuditEnabled = new BooleanManagedProperty(true) // Auditing ENABLED
    };
    
    // Configure entity metadata - DISABLE auditing for contact  
    var contactMetadata = new EntityMetadata
    {
        LogicalName = "contact",
        IsAuditEnabled = new BooleanManagedProperty(false) // Auditing DISABLED
    };
    
    context.InitializeMetadata(new[] { accountMetadata, contactMetadata });
    
    // Act - Create both entities
    var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
    var contactId = service.Create(new Entity("contact") { ["firstname"] = "John" });
    
    // Assert - Only account is audited
    var audits = auditRepository.GetAllAuditRecords().ToList();
    Assert.Single(audits);
    Assert.Equal("account", audits[0].GetAttributeValue<EntityReference>("objectid").LogicalName);
}
```

### Attribute-Level Settings

Attribute-level settings provide granular control over which fields are tracked in update operations. Only attributes with auditing enabled will have their changes recorded.

**Reference:** [AttributeMetadata.IsAuditEnabled](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isauditenabled) - The IsAuditEnabled property controls whether changes to a specific attribute are tracked in audit records. This is a BooleanManagedProperty that determines if old and new values are captured during updates.

```csharp
[Fact]
public void Should_OnlyAuditEnabledAttributes()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    
    // Enable organization-level auditing
    auditRepository.IsAuditEnabled = true;
    
    // Configure entity with selective attribute auditing
    var entityMetadata = new EntityMetadata
    {
        LogicalName = "account",
        IsAuditEnabled = new BooleanManagedProperty(true)
    };
    
    // Name attribute - AUDITED
    var nameAttribute = new StringAttributeMetadata
    {
        LogicalName = "name",
        IsAuditEnabled = new BooleanManagedProperty(true) // Changes tracked
    };
    
    // Description attribute - NOT AUDITED
    var descriptionAttribute = new StringAttributeMetadata
    {
        LogicalName = "description",
        IsAuditEnabled = new BooleanManagedProperty(false) // Changes NOT tracked
    };
    
    entityMetadata.SetAttributeCollection(new[] { nameAttribute, descriptionAttribute });
    context.InitializeMetadata(entityMetadata);
    
    // Create account
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Original Name",
        ["description"] = "Original Description"
    });
    
    auditRepository.ClearAuditData(); // Clear creation audit
    
    // Update both attributes
    service.Update(new Entity("account", accountId)
    {
        ["name"] = "Updated Name",
        ["description"] = "Updated Description"
    });
    
    // Retrieve audit details
    var audits = auditRepository.GetAllAuditRecords().ToList();
    var auditDetail = (Microsoft.Crm.Sdk.Messages.AttributeAuditDetail)
        auditRepository.GetAuditDetails(audits[0].Id);
    
    // Assert - Only "name" change is tracked
    Assert.True(auditDetail.NewValue.Contains("name"));
    Assert.Equal("Original Name", auditDetail.OldValue.GetAttributeValue<string>("name"));
    Assert.Equal("Updated Name", auditDetail.NewValue.GetAttributeValue<string>("name"));
    
    // "description" is NOT in audit (not tracked)
    Assert.False(auditDetail.NewValue.Contains("description"));
}
```

### Complete Three-Level Example

This example demonstrates all three audit levels working together:

```csharp
using Fake4Dataverse.Abstractions.Audit;
using Microsoft.Xrm.Sdk.Metadata;

[Fact]
public void Should_RequireAllThreeLevelsEnabled()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var auditRepository = context.GetProperty<IAuditRepository>();
    
    // Level 1: Organization - Enable globally
    auditRepository.IsAuditEnabled = true;
    
    // Level 2: Entity - Enable for account table
    var entityMetadata = new EntityMetadata
    {
        LogicalName = "account",
        IsAuditEnabled = new BooleanManagedProperty(true)
    };
    
    // Level 3: Attributes - Enable specific fields
    var nameAttribute = new StringAttributeMetadata
    {
        LogicalName = "name",
        IsAuditEnabled = new BooleanManagedProperty(true) // Tracked
    };
    
    var revenueAttribute = new MoneyAttributeMetadata
    {
        LogicalName = "revenue",
        IsAuditEnabled = new BooleanManagedProperty(true) // Tracked
    };
    
    var descriptionAttribute = new StringAttributeMetadata
    {
        LogicalName = "description",
        IsAuditEnabled = new BooleanManagedProperty(false) // NOT tracked
    };
    
    entityMetadata.SetAttributeCollection(new[] 
    { 
        nameAttribute, 
        revenueAttribute, 
        descriptionAttribute 
    });
    context.InitializeMetadata(entityMetadata);
    
    // Test: Create and update operations
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Contoso",
        ["revenue"] = new Money(100000m),
        ["description"] = "A company"
    });
    
    service.Update(new Entity("account", accountId)
    {
        ["name"] = "Contoso Ltd",
        ["revenue"] = new Money(150000m),
        ["description"] = "A bigger company"
    });
    
    // Verify: 2 audit records (Create + Update)
    var audits = auditRepository.GetAllAuditRecords().ToList();
    Assert.Equal(2, audits.Count);
    
    // Verify: Update audit only tracks name and revenue (not description)
    var updateAudit = audits.First(a => a.GetAttributeValue<int>("action") == AuditAction.Update);
    var detail = (Microsoft.Crm.Sdk.Messages.AttributeAuditDetail)
        auditRepository.GetAuditDetails(updateAudit.Id);
    
    Assert.True(detail.NewValue.Contains("name"));
    Assert.True(detail.NewValue.Contains("revenue"));
    Assert.False(detail.NewValue.Contains("description")); // Not tracked
}
```

**Key Points:**

1. **All three levels must be enabled** for auditing to occur
2. **Organization-level** = global on/off switch
3. **Entity-level** = per-table control (which tables to audit)
4. **Attribute-level** = per-field control (which fields to track)
5. **Dynamic entities** (no metadata) have all attributes audited when entity is audited
6. **Create operations** audit the entire entity when entity-level is enabled
7. **Update operations** only audit attributes with IsAuditEnabled = true

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

### 5. Configure Metadata for Realistic Testing

When testing with entity metadata, configure audit settings to match your production environment:

```csharp
// Match production audit configuration in tests
var entityMetadata = new EntityMetadata
{
    LogicalName = "account",
    IsAuditEnabled = new BooleanManagedProperty(true) // Same as production
};

var nameAttribute = new StringAttributeMetadata
{
    LogicalName = "name",
    IsAuditEnabled = new BooleanManagedProperty(true) // Audited in production
};

var internalNotesAttribute = new StringAttributeMetadata
{
    LogicalName = "internalnotes",
    IsAuditEnabled = new BooleanManagedProperty(false) // Not audited in production
};

entityMetadata.SetAttributeCollection(new[] { nameAttribute, internalNotesAttribute });
context.InitializeMetadata(entityMetadata);
```

This ensures your tests accurately reflect production behavior regarding which data is audited.

## See Also

- [Dataverse Auditing Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Official Microsoft documentation
- [Configure Auditing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/configure) - Audit configuration details
- [Security and Permissions](./security-permissions.md) - Related security testing
- [Testing Plugins](./testing-plugins.md) - Test plugins with auditing
- [Message Executors](../messages/audit.md) - Audit message reference

---

**Questions?** Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
