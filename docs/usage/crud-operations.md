# CRUD Operations

This guide covers Create, Read, Update, Delete (CRUD) operations in Fake4Dataverse.

## Table of Contents
- [Create Operations](#create-operations)
- [Retrieve Operations](#retrieve-operations)
- [Update Operations](#update-operations)
- [Delete Operations](#delete-operations)
- [Upsert Operations](#upsert-operations)
- [Best Practices](#best-practices)

## Create Operations

### Basic Create

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

[Fact]
public void Should_Create_Account()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        ["name"] = "Contoso Ltd",
        ["revenue"] = new Money(1000000),
        ["numberofemployees"] = 250
    };
    
    // Act
    var accountId = service.Create(account);
    
    // Assert
    Assert.NotEqual(Guid.Empty, accountId);
    Assert.Equal(accountId, account.Id); // ID is set on the entity
}
```

### Create with Relationships

```csharp
[Fact]
public void Should_Create_Contact_With_Parent_Account()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create parent account first
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Parent Account"
    });
    
    // Create contact with parent reference
    var contact = new Entity("contact")
    {
        ["firstname"] = "John",
        ["lastname"] = "Doe",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };
    
    var contactId = service.Create(contact);
    
    // Verify
    var retrieved = service.Retrieve("contact", contactId, 
        new ColumnSet("parentcustomerid"));
    var parent = (EntityReference)retrieved["parentcustomerid"];
    Assert.Equal(accountId, parent.Id);
}
```

### Create with OptionSets

```csharp
[Fact]
public void Should_Create_Account_With_OptionSet_Values()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        ["name"] = "Test Account",
        ["accountcategorycode"] = new OptionSetValue(1), // Preferred Customer
        ["customertypecode"] = new OptionSetValue(3)     // Competitor
    };
    
    var accountId = service.Create(account);
    
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet("accountcategorycode", "customertypecode"));
    
    Assert.Equal(1, ((OptionSetValue)retrieved["accountcategorycode"]).Value);
    Assert.Equal(3, ((OptionSetValue)retrieved["customertypecode"]).Value);
}
```

## Retrieve Operations

### Basic Retrieve

```csharp
[Fact]
public void Should_Retrieve_Account_By_Id()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account",
        ["revenue"] = new Money(500000)
    });
    
    // Retrieve with specific columns
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet("name", "revenue"));
    
    Assert.Equal("Test Account", retrieved["name"]);
    Assert.Equal(500000m, ((Money)retrieved["revenue"]).Value);
}
```

### Retrieve All Columns

```csharp
[Fact]
public void Should_Retrieve_All_Columns()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account",
        ["revenue"] = new Money(500000),
        ["numberofemployees"] = 100
    });
    
    // Retrieve all columns
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet(true));
    
    Assert.True(retrieved.Contains("name"));
    Assert.True(retrieved.Contains("revenue"));
    Assert.True(retrieved.Contains("numberofemployees"));
}
```

### Retrieve with EntityReference

```csharp
[Fact]
public void Should_Retrieve_Using_EntityReference()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    });
    
    var entityRef = new EntityReference("account", accountId);
    
    var retrieved = service.Retrieve(
        entityRef.LogicalName, 
        entityRef.Id,
        new ColumnSet("name"));
    
    Assert.Equal("Test Account", retrieved["name"]);
}
```

## Update Operations

### Basic Update

```csharp
[Fact]
public void Should_Update_Account_Name()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    // Initialize with original data
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Old Name",
        ["revenue"] = new Money(1000000)
    });
    
    // Update only the name
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["name"] = "New Name"
    };
    
    service.Update(accountUpdate);
    
    // Verify
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet("name", "revenue"));
    
    Assert.Equal("New Name", retrieved["name"]);
    Assert.Equal(1000000m, ((Money)retrieved["revenue"]).Value); // Unchanged
}
```

### Update Multiple Attributes

```csharp
[Fact]
public void Should_Update_Multiple_Attributes()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Old Name",
        ["revenue"] = new Money(1000000),
        ["numberofemployees"] = 100
    });
    
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["name"] = "New Name",
        ["revenue"] = new Money(2000000),
        ["numberofemployees"] = 200
    };
    
    service.Update(accountUpdate);
    
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet(true));
    
    Assert.Equal("New Name", retrieved["name"]);
    Assert.Equal(2000000m, ((Money)retrieved["revenue"]).Value);
    Assert.Equal(200, retrieved["numberofemployees"]);
}
```

### Update Lookup Fields

```csharp
[Fact]
public void Should_Update_Owner()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var oldOwnerId = Guid.NewGuid();
    var newOwnerId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = accountId,
            ["ownerid"] = new EntityReference("systemuser", oldOwnerId)
        },
        new Entity("systemuser") { Id = oldOwnerId },
        new Entity("systemuser") { Id = newOwnerId }
    });
    
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["ownerid"] = new EntityReference("systemuser", newOwnerId)
    };
    
    service.Update(accountUpdate);
    
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet("ownerid"));
    
    Assert.Equal(newOwnerId, ((EntityReference)retrieved["ownerid"]).Id);
}
```

## Delete Operations

### Basic Delete

```csharp
[Fact]
public void Should_Delete_Account()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Account to Delete"
    });
    
    // Delete the account
    service.Delete("account", accountId);
    
    // Verify deletion
    var accounts = context.CreateQuery("account").ToList();
    Assert.Empty(accounts);
}
```

### Delete with Verification

```csharp
[Fact]
public void Should_Throw_When_Deleting_Nonexistent_Entity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var nonExistentId = Guid.NewGuid();
    
    // Attempting to delete non-existent entity should throw
    Assert.Throws<Exception>(() => 
        service.Delete("account", nonExistentId));
}
```

## Upsert Operations

Upsert creates an entity if it doesn't exist, or updates it if it does.

### Basic Upsert - Insert

```csharp
using Microsoft.Xrm.Sdk.Messages;

[Fact]
public void Should_Insert_When_Entity_Does_Not_Exist()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "New Account"
    };
    
    var request = new UpsertRequest
    {
        Target = account
    };
    
    var response = (UpsertResponse)service.Execute(request);
    
    Assert.True(response.RecordCreated);
    
    var retrieved = service.Retrieve("account", account.Id,
        new ColumnSet("name"));
    Assert.Equal("New Account", retrieved["name"]);
}
```

### Basic Upsert - Update

```csharp
[Fact]
public void Should_Update_When_Entity_Exists()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Existing Account"
    });
    
    var account = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Updated Account"
    };
    
    var request = new UpsertRequest
    {
        Target = account
    };
    
    var response = (UpsertResponse)service.Execute(request);
    
    Assert.False(response.RecordCreated);
    
    var retrieved = service.Retrieve("account", accountId,
        new ColumnSet("name"));
    Assert.Equal("Updated Account", retrieved["name"]);
}
```

## Best Practices

### ✅ Do

1. **Use specific column sets**
   ```csharp
   // ✅ Good - only retrieve what you need
   var entity = service.Retrieve("account", id, 
       new ColumnSet("name", "revenue"));
   
   // ❌ Avoid in production - retrieves all columns
   var entity = service.Retrieve("account", id, 
       new ColumnSet(true));
   ```

2. **Set IDs explicitly when testing**
   ```csharp
   // ✅ Good - explicit ID for testing
   var accountId = Guid.NewGuid();
   var account = new Entity("account")
   {
       Id = accountId,
       ["name"] = "Test"
   };
   ```

3. **Check for attribute existence**
   ```csharp
   // ✅ Good - check if attribute exists
   if (entity.Contains("name"))
   {
       var name = entity.GetAttributeValue<string>("name");
   }
   
   // Or use GetAttributeValue with default
   var name = entity.GetAttributeValue<string>("name") ?? "Default Name";
   ```

4. **Initialize related entities**
   ```csharp
   // ✅ Good - initialize all related entities
   context.Initialize(new[]
   {
       account,
       primaryContact,
       owner
   });
   ```

### ❌ Don't

1. **Don't modify entities after retrieve**
   ```csharp
   // ❌ Bad - modifying retrieved entity
   var account = service.Retrieve("account", id, new ColumnSet(true));
   account["name"] = "New Name";
   service.Update(account); // May include unchanged fields
   
   // ✅ Good - create new entity for update
   var accountUpdate = new Entity("account")
   {
       Id = id,
       ["name"] = "New Name"
   };
   service.Update(accountUpdate);
   ```

2. **Don't forget to initialize test data**
   ```csharp
   // ❌ Bad - trying to update non-existent entity
   service.Update(new Entity("account") 
   { 
       Id = Guid.NewGuid(), 
       ["name"] = "Test" 
   }); // Throws!
   
   // ✅ Good - initialize first
   var account = new Entity("account") { Id = Guid.NewGuid() };
   context.Initialize(account);
   service.Update(new Entity("account") 
   { 
       Id = account.Id, 
       ["name"] = "Test" 
   });
   ```

### Pattern: Test Data Builder

```csharp
public static class TestDataBuilder
{
    public static Entity CreateAccount(
        string name = "Test Account",
        decimal? revenue = null,
        Guid? ownerId = null)
    {
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = name
        };
        
        if (revenue.HasValue)
            account["revenue"] = new Money(revenue.Value);
            
        if (ownerId.HasValue)
            account["ownerid"] = new EntityReference("systemuser", ownerId.Value);
            
        return account;
    }
}

// Usage
[Fact]
public void Test_Example()
{
    var context = XrmFakedContextFactory.New();
    var account = TestDataBuilder.CreateAccount("Contoso", 1000000);
    context.Initialize(account);
    // ...
}
```

## Error Scenarios

### Entity Not Found

```csharp
[Fact]
public void Should_Throw_When_Entity_Not_Found()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var nonExistentId = Guid.NewGuid();
    
    Assert.Throws<Exception>(() =>
        service.Retrieve("account", nonExistentId, new ColumnSet(true)));
}
```

### Invalid Entity Type

```csharp
[Fact]
public void Should_Handle_Entity_Type_Mismatch()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account") { Id = accountId });
    
    // Trying to retrieve as wrong type
    Assert.Throws<Exception>(() =>
        service.Retrieve("contact", accountId, new ColumnSet(true)));
}
```

## Next Steps

- [Querying Data](./querying-data.md) - Query multiple records with LINQ and FetchXML
- [Batch Operations](./batch-operations.md) - ExecuteMultiple and transactions
- [Testing Plugins](./testing-plugins.md) - Test plugins that perform CRUD operations
- [Message Executors](../messages/crud.md) - CRUD message reference

## See Also

- [Basic Concepts](../getting-started/basic-concepts.md) - Understanding the framework
- [Data Management](../concepts/data-management.md) - Managing test data
