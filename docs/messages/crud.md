# CRUD Messages

Documentation for Create, Retrieve, Update, Delete, and Upsert message executors in Fake4Dataverse.

## Overview

CRUD operations are the foundation of any Dataverse application. Fake4Dataverse fully supports these operations through the standard SDK messages.

## Supported Messages

| Message | Request Type | Response Type | Description |
|---------|-------------|---------------|-------------|
| Create | `CreateRequest` | `CreateResponse` | Create a new entity record |
| Retrieve | `RetrieveRequest` | `RetrieveResponse` | Retrieve a single entity record |
| Update | `UpdateRequest` | `UpdateResponse` | Update an existing entity record |
| Delete | `DeleteRequest` | `DeleteResponse` | Delete an entity record |
| Upsert | `UpsertRequest` | `UpsertResponse` | Create or update a record |

## Create Message

Creates a new entity record.

### Using IOrganizationService

```csharp
var service = context.GetOrganizationService();

var account = new Entity("account")
{
    ["name"] = "Contoso Ltd",
    ["revenue"] = new Money(1000000)
};

Guid accountId = service.Create(account);
```

### Using CreateRequest

```csharp
var request = new CreateRequest
{
    Target = new Entity("account")
    {
        ["name"] = "Contoso Ltd",
        ["revenue"] = new Money(1000000)
    }
};

var response = (CreateResponse)service.Execute(request);
Guid accountId = response.id;
```

### Complete Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

[Fact]
public void Should_Create_Account_With_CreateRequest()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new CreateRequest
    {
        Target = new Entity("account")
        {
            ["name"] = "Test Account",
            ["revenue"] = new Money(500000),
            ["numberofemployees"] = 100
        }
    };
    
    // Act
    var response = (CreateResponse)service.Execute(request);
    
    // Assert
    Assert.NotEqual(Guid.Empty, response.id);
    
    var retrieved = service.Retrieve("account", response.id, 
        new ColumnSet("name", "revenue", "numberofemployees"));
    
    Assert.Equal("Test Account", retrieved["name"]);
    Assert.Equal(500000m, ((Money)retrieved["revenue"]).Value);
    Assert.Equal(100, retrieved["numberofemployees"]);
}
```

## Retrieve Message

Retrieves a single entity record by ID.

### Using IOrganizationService

```csharp
var entity = service.Retrieve(
    "account",
    accountId,
    new ColumnSet("name", "revenue"));
```

### Using RetrieveRequest

```csharp
var request = new RetrieveRequest
{
    Target = new EntityReference("account", accountId),
    ColumnSet = new ColumnSet("name", "revenue")
};

var response = (RetrieveResponse)service.Execute(request);
Entity entity = response.Entity;
```

### Complete Example

```csharp
[Fact]
public void Should_Retrieve_Account_With_RetrieveRequest()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Contoso",
        ["revenue"] = new Money(1000000)
    });
    
    var request = new RetrieveRequest
    {
        Target = new EntityReference("account", accountId),
        ColumnSet = new ColumnSet("name", "revenue")
    };
    
    // Act
    var response = (RetrieveResponse)service.Execute(request);
    
    // Assert
    Assert.Equal(accountId, response.Entity.Id);
    Assert.Equal("Contoso", response.Entity["name"]);
    Assert.Equal(1000000m, ((Money)response.Entity["revenue"]).Value);
}
```

## Update Message

Updates an existing entity record.

### Using IOrganizationService

```csharp
var accountUpdate = new Entity("account")
{
    Id = accountId,
    ["name"] = "Updated Name",
    ["revenue"] = new Money(2000000)
};

service.Update(accountUpdate);
```

### Using UpdateRequest

```csharp
var request = new UpdateRequest
{
    Target = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Updated Name"
    }
};

var response = (UpdateResponse)service.Execute(request);
```

### Complete Example

```csharp
[Fact]
public void Should_Update_Account_With_UpdateRequest()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Original Name",
        ["revenue"] = new Money(1000000)
    });
    
    var request = new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = accountId,
            ["name"] = "Updated Name",
            ["revenue"] = new Money(2000000)
        }
    };
    
    // Act
    var response = (UpdateResponse)service.Execute(request);
    
    // Assert
    var updated = service.Retrieve("account", accountId, 
        new ColumnSet("name", "revenue"));
    
    Assert.Equal("Updated Name", updated["name"]);
    Assert.Equal(2000000m, ((Money)updated["revenue"]).Value);
}
```

## Delete Message

Deletes an entity record.

### Using IOrganizationService

```csharp
service.Delete("account", accountId);
```

### Using DeleteRequest

```csharp
var request = new DeleteRequest
{
    Target = new EntityReference("account", accountId)
};

var response = (DeleteResponse)service.Execute(request);
```

### Complete Example

```csharp
[Fact]
public void Should_Delete_Account_With_DeleteRequest()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Account to Delete"
    });
    
    var request = new DeleteRequest
    {
        Target = new EntityReference("account", accountId)
    };
    
    // Act
    var response = (DeleteResponse)service.Execute(request);
    
    // Assert
    var accounts = context.CreateQuery("account").ToList();
    Assert.Empty(accounts);
}
```

## Upsert Message

Creates a record if it doesn't exist, or updates it if it does.

### Using UpsertRequest

```csharp
var request = new UpsertRequest
{
    Target = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Account Name"
    }
};

var response = (UpsertResponse)service.Execute(request);
bool wasCreated = response.RecordCreated;
```

### Upsert - Insert Scenario

```csharp
[Fact]
public void Should_Insert_When_Record_Does_Not_Exist()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var request = new UpsertRequest
    {
        Target = new Entity("account")
        {
            Id = accountId,
            ["name"] = "New Account"
        }
    };
    
    var response = (UpsertResponse)service.Execute(request);
    
    Assert.True(response.RecordCreated);
    
    var retrieved = service.Retrieve("account", accountId, 
        new ColumnSet("name"));
    Assert.Equal("New Account", retrieved["name"]);
}
```

### Upsert - Update Scenario

```csharp
[Fact]
public void Should_Update_When_Record_Exists()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    // Initialize with existing record
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Existing Account"
    });
    
    var request = new UpsertRequest
    {
        Target = new Entity("account")
        {
            Id = accountId,
            ["name"] = "Updated Account"
        }
    };
    
    var response = (UpsertResponse)service.Execute(request);
    
    Assert.False(response.RecordCreated);
    
    var retrieved = service.Retrieve("account", accountId, 
        new ColumnSet("name"));
    Assert.Equal("Updated Account", retrieved["name"]);
}
```

## Testing Patterns

### Testing Create with Relationships

```csharp
[Fact]
public void Should_Create_Contact_With_Parent_Account()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Parent Account"
    });
    
    var request = new CreateRequest
    {
        Target = new Entity("contact")
        {
            ["firstname"] = "John",
            ["lastname"] = "Doe",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        }
    };
    
    var response = (CreateResponse)service.Execute(request);
    
    var contact = service.Retrieve("contact", response.id, 
        new ColumnSet("parentcustomerid"));
    
    Assert.Equal(accountId, 
        ((EntityReference)contact["parentcustomerid"]).Id);
}
```

### Testing Update with Pre-Image

```csharp
[Fact]
public void Should_Track_Changes_During_Update()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    // Original state
    var originalRevenue = new Money(1000000);
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Original Name",
        ["revenue"] = originalRevenue
    });
    
    // Get pre-image (before update)
    var preImage = service.Retrieve("account", accountId, 
        new ColumnSet("name", "revenue"));
    
    // Update
    var request = new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = accountId,
            ["revenue"] = new Money(2000000)
        }
    };
    service.Execute(request);
    
    // Get post-image (after update)
    var postImage = service.Retrieve("account", accountId, 
        new ColumnSet("name", "revenue"));
    
    // Verify change
    Assert.Equal(1000000m, ((Money)preImage["revenue"]).Value);
    Assert.Equal(2000000m, ((Money)postImage["revenue"]).Value);
}
```

### Testing Delete with Cascade

```csharp
[Fact]
public void Should_Delete_Record()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Account" },
        new Entity("contact")
        {
            Id = contactId,
            ["firstname"] = "John",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        }
    });
    
    // Delete account
    var request = new DeleteRequest
    {
        Target = new EntityReference("account", accountId)
    };
    service.Execute(request);
    
    // Verify account deleted
    var accounts = context.CreateQuery("account").ToList();
    Assert.Empty(accounts);
    
    // Note: Cascade delete is not automatically simulated in Fake4Dataverse
    // Contact still exists unless you explicitly delete it
}
```

## Error Scenarios

### Entity Not Found on Retrieve

```csharp
[Fact]
public void Should_Throw_When_Entity_Not_Found()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveRequest
    {
        Target = new EntityReference("account", Guid.NewGuid()),
        ColumnSet = new ColumnSet(true)
    };
    
    Assert.Throws<Exception>(() => service.Execute(request));
}
```

### Entity Not Found on Update

```csharp
[Fact]
public void Should_Throw_When_Updating_Nonexistent_Entity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Updated"
        }
    };
    
    Assert.Throws<Exception>(() => service.Execute(request));
}
```

## Best Practices

### ✅ Do

1. **Use specific column sets in Retrieve**
   ```csharp
   new ColumnSet("name", "revenue") // ✅ Good
   ```

2. **Check if record exists before operations**
   ```csharp
   var exists = context.CreateQuery("account")
       .Any(a => a.Id == accountId);
   ```

3. **Initialize test data before tests**
   ```csharp
   context.Initialize(testData);
   ```

### ❌ Don't

1. **Don't retrieve all columns unnecessarily**
   ```csharp
   new ColumnSet(true) // ❌ Avoid in production code
   ```

2. **Don't forget to set entity ID for updates**
   ```csharp
   new Entity("account") { ["name"] = "Test" } // ❌ Missing ID
   ```

## See Also

- [CRUD Operations Guide](../usage/crud-operations.md) - Complete CRUD patterns
- [Message Executors Overview](./README.md) - All supported messages
- [Querying Data](../usage/querying-data.md) - RetrieveMultiple and queries

## Reference

- [Create (SDK)](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-create)
- [Retrieve (SDK)](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-retrieve)
- [Update (SDK)](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-update-delete)
- [Upsert (SDK)](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-update-delete#use-upsert)
