# Merge Request Operations

## Overview

The Merge Request feature allows you to merge two entity records in Dataverse, combining their data and updating all references to point to the surviving record. This is a common operation when dealing with duplicate records.

**Implemented:** 2025-10-10 (Issue #1)

## Microsoft Documentation

Official reference: [MergeRequest Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest)

## Usage

### Basic Merge Operation

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Fake4Dataverse.Middleware;

// Create test context
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Create two account records
var targetAccount = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Corp"
};

var subordinateAccount = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Inc"
};

context.Initialize(new[] { targetAccount, subordinateAccount });

// Create merge request
var mergeRequest = new MergeRequest
{
    Target = targetAccount.ToEntityReference(),
    SubordinateId = subordinateAccount.Id,
    UpdateContent = new Entity("account")
    {
        ["name"] = "Contoso Corporation" // Updated name for target
    },
    PerformParentingChecks = false
};

// Execute merge
var response = (MergeResponse)service.Execute(mergeRequest);

// After merge:
// - Target account exists with updated name
// - Subordinate account is deactivated (statecode = 1, statuscode = 2)
// - All references to subordinate now point to target
```

### Merge with Parenting Checks

The `PerformParentingChecks` property validates that both records have the same parent before merging:

```csharp
var parentAccount = new Entity("account") { Id = Guid.NewGuid() };

var target = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["parentaccountid"] = parentAccount.ToEntityReference()
};

var subordinate = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["parentaccountid"] = parentAccount.ToEntityReference()
};

context.Initialize(new[] { parentAccount, target, subordinate });

var mergeRequest = new MergeRequest
{
    Target = target.ToEntityReference(),
    SubordinateId = subordinate.Id,
    UpdateContent = new Entity("account"),
    PerformParentingChecks = true // Will validate parents match
};

service.Execute(mergeRequest);
```

## How It Works

When a merge request is executed, the framework:

1. **Validates** that both target and subordinate entities exist
2. **Prevents self-merge** - cannot merge an entity with itself
3. **Validates parenting** if `PerformParentingChecks` is true
4. **Applies updates** from `UpdateContent` to the target entity
5. **Updates references** - all EntityReference attributes pointing to the subordinate are updated to point to the target
6. **Deactivates subordinate** - sets statecode = 1 (Inactive) and statuscode = 2 (Inactive)

## Reference Updates

The merge operation automatically updates all references across the entire context:

```csharp
// Set up entities with references
var targetAccount = new Entity("account") { Id = Guid.NewGuid() };
var subordinateAccount = new Entity("account") { Id = Guid.NewGuid() };

var contact = new Entity("contact")
{
    Id = Guid.NewGuid(),
    ["parentcustomerid"] = subordinateAccount.ToEntityReference() // Points to subordinate
};

context.Initialize(new[] { targetAccount, subordinateAccount, contact });

// Merge accounts
var mergeRequest = new MergeRequest
{
    Target = targetAccount.ToEntityReference(),
    SubordinateId = subordinateAccount.Id,
    UpdateContent = new Entity("account")
};

service.Execute(mergeRequest);

// Contact now points to target account
var updatedContact = service.Retrieve("contact", contact.Id, new ColumnSet(true));
Assert.Equal(targetAccount.Id, updatedContact.GetAttributeValue<EntityReference>("parentcustomerid").Id);
```

## Error Scenarios

### Missing Entity
```csharp
var mergeRequest = new MergeRequest
{
    Target = new EntityReference("account", Guid.NewGuid()), // Doesn't exist
    SubordinateId = Guid.NewGuid(),
    UpdateContent = new Entity("account")
};

// Throws: Target entity account with id {guid} not found
service.Execute(mergeRequest);
```

### Self-Merge Attempt
```csharp
var accountId = Guid.NewGuid();
var mergeRequest = new MergeRequest
{
    Target = new EntityReference("account", accountId),
    SubordinateId = accountId, // Same as target
    UpdateContent = new Entity("account")
};

// Throws: Cannot merge an entity with itself
service.Execute(mergeRequest);
```

### Parenting Check Failure
```csharp
var parent1 = new Entity("account") { Id = Guid.NewGuid() };
var parent2 = new Entity("account") { Id = Guid.NewGuid() };

var target = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["parentaccountid"] = parent1.ToEntityReference()
};

var subordinate = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["parentaccountid"] = parent2.ToEntityReference() // Different parent
};

context.Initialize(new[] { parent1, parent2, target, subordinate });

var mergeRequest = new MergeRequest
{
    Target = target.ToEntityReference(),
    SubordinateId = subordinate.Id,
    UpdateContent = new Entity("account"),
    PerformParentingChecks = true
};

// Throws: Cannot merge records with different parent records when PerformParentingChecks is enabled
service.Execute(mergeRequest);
```

## Best Practices

1. **Always provide UpdateContent**: Even if empty, this parameter is required by the API
2. **Use PerformParentingChecks carefully**: Enable it when hierarchical consistency matters
3. **Test reference updates**: Verify that related records are updated correctly after merge
4. **Handle deactivation**: Remember the subordinate record is deactivated, not deleted

## Common Use Cases

### Duplicate Management
```csharp
// Merge duplicate accounts, keeping the older one
var olderAccount = accounts.OrderBy(a => a.GetAttributeValue<DateTime>("createdon")).First();
var duplicateAccount = accounts.OrderBy(a => a.GetAttributeValue<DateTime>("createdon")).Last();

var mergeRequest = new MergeRequest
{
    Target = olderAccount.ToEntityReference(),
    SubordinateId = duplicateAccount.Id,
    UpdateContent = new Entity("account")
    {
        // Preserve important fields from duplicate
        ["description"] = duplicateAccount.GetAttributeValue<string>("description")
    }
};

service.Execute(mergeRequest);
```

### Account Hierarchy Cleanup
```csharp
// Merge child account into parent
var mergeRequest = new MergeRequest
{
    Target = parentAccount.ToEntityReference(),
    SubordinateId = childAccount.Id,
    UpdateContent = new Entity("account"),
    PerformParentingChecks = false // They may have different parents
};

service.Execute(mergeRequest);
```

## Related Documentation

- [Microsoft MergeRequest Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest)
- [Duplicate Detection](duplicate-detection.md) (Coming soon)
- [Entity Operations](../Fake4DataverseCore/README.md#entity-operations)

## Implementation Details

- **File**: `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/MergeRequestExecutor.cs`
- **Tests**: `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/MergeRequestTests/`
- **Feature Parity**: Matches FakeXrmEasy v2+ behavior
