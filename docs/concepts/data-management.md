# Data Management

Managing test data effectively is crucial for writing maintainable and reliable tests. This guide covers patterns and best practices for initializing and managing test data in Fake4Dataverse.

## Table of Contents

- [Overview](#overview)
- [Initializing Test Data](#initializing-test-data)
- [Managing Relationships](#managing-relationships)
- [Data Builders and Factories](#data-builders-and-factories)
- [Complex Data Scenarios](#complex-data-scenarios)
- [Best Practices](#best-practices)
- [See Also](#see-also)

## Overview

Fake4Dataverse provides an in-memory data store that simulates Dataverse. You can initialize this store with test data using the `Initialize()` method on the context.

## Initializing Test Data

### Single Entity

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

var context = XrmFakedContextFactory.New();

var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Corp"
};

context.Initialize(account);
```

### Multiple Entities

```csharp
var account1 = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Corp"
};

var account2 = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Fabrikam Inc"
};

context.Initialize(new[] { account1, account2 });
```

### Entity Collection

```csharp
var collection = new EntityCollection();
collection.Entities.Add(new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test 1" });
collection.Entities.Add(new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test 2" });

context.Initialize(collection);
```

## Managing Relationships

### One-to-Many (Lookup) Relationships

Use `EntityReference` to establish lookup relationships:

```csharp
var accountId = Guid.NewGuid();
var contactId = Guid.NewGuid();

var account = new Entity("account")
{
    Id = accountId,
    ["name"] = "Contoso Corp"
};

var contact = new Entity("contact")
{
    Id = contactId,
    ["firstname"] = "John",
    ["lastname"] = "Doe",
    ["parentcustomerid"] = new EntityReference("account", accountId)
};

context.Initialize(new[] { account, contact });
```

### Many-to-Many (N:N) Relationships

For N:N relationships, use the intersect entity or `Associate` message:

```csharp
using Microsoft.Crm.Sdk.Messages;

var userId = Guid.NewGuid();
var roleId = Guid.NewGuid();

var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Test User" };
var role = new Entity("role") { Id = roleId, ["name"] = "Sales Manager" };

context.Initialize(new[] { user, role });

var service = context.GetOrganizationService();
service.Associate(
    "systemuser",
    userId,
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", roleId) }
);
```

## Data Builders and Factories

For maintainable tests, consider using builder or factory patterns:

### Simple Builder Pattern

```csharp
public class AccountBuilder
{
    private readonly Entity _account;

    public AccountBuilder()
    {
        _account = new Entity("account") { Id = Guid.NewGuid() };
    }

    public AccountBuilder WithName(string name)
    {
        _account["name"] = name;
        return this;
    }

    public AccountBuilder WithRevenue(decimal revenue)
    {
        _account["revenue"] = new Money(revenue);
        return this;
    }

    public AccountBuilder WithPrimaryContact(EntityReference contact)
    {
        _account["primarycontactid"] = contact;
        return this;
    }

    public Entity Build() => _account;
}

// Usage
[Fact]
public void Test_With_Builder()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new AccountBuilder()
        .WithName("Contoso")
        .WithRevenue(1000000)
        .Build();
    
    context.Initialize(account);
}
```

### Factory Pattern

```csharp
public static class TestDataFactory
{
    public static Entity CreateAccount(string name = "Test Account")
    {
        return new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = name,
            ["createdon"] = DateTime.UtcNow
        };
    }

    public static Entity CreateContact(string firstName, string lastName, EntityReference parentAccount = null)
    {
        var contact = new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = firstName,
            ["lastname"] = lastName
        };

        if (parentAccount != null)
        {
            contact["parentcustomerid"] = parentAccount;
        }

        return contact;
    }
}

// Usage
[Fact]
public void Test_With_Factory()
{
    var context = XrmFakedContextFactory.New();
    
    var account = TestDataFactory.CreateAccount("Contoso");
    var contact = TestDataFactory.CreateContact("John", "Doe", account.ToEntityReference());
    
    context.Initialize(new[] { account, contact });
}
```

## Complex Data Scenarios

### Hierarchical Data

```csharp
// Create account hierarchy
var parentId = Guid.NewGuid();
var childId = Guid.NewGuid();
var grandchildId = Guid.NewGuid();

var parent = new Entity("account")
{
    Id = parentId,
    ["name"] = "Parent Corp"
};

var child = new Entity("account")
{
    Id = childId,
    ["name"] = "Child Division",
    ["parentaccountid"] = new EntityReference("account", parentId)
};

var grandchild = new Entity("account")
{
    Id = grandchildId,
    ["name"] = "Grandchild Office",
    ["parentaccountid"] = new EntityReference("account", childId)
};

context.Initialize(new[] { parent, child, grandchild });
```

### Multiple Related Entities

```csharp
public static class ScenarioData
{
    public static void CreateSalesScenario(IXrmFakedContext context)
    {
        var accountId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        
        var account = new Entity("account")
        {
            Id = accountId,
            ["name"] = "Adventure Works"
        };
        
        var contact = new Entity("contact")
        {
            Id = contactId,
            ["firstname"] = "John",
            ["lastname"] = "Doe",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        };
        
        var opportunity = new Entity("opportunity")
        {
            Id = opportunityId,
            ["name"] = "Big Deal",
            ["customerid"] = new EntityReference("account", accountId),
            ["estimatedvalue"] = new Money(100000),
            ["statecode"] = new OptionSetValue(0) // Open
        };
        
        context.Initialize(new[] { account, contact, opportunity });
    }
}
```

## Best Practices

### ✅ Do

- **Use meaningful IDs**: Generate GUIDs once and reuse them for related entities
- **Initialize all required fields**: Set all fields your test logic depends on
- **Use builders for complex entities**: Simplify test setup with builder patterns
- **Create scenario helpers**: Reuse common data setups across tests
- **Keep test data minimal**: Only create data needed for the specific test

```csharp
// ✅ Good - Clear and minimal
[Fact]
public void Should_Calculate_Total_Revenue()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["revenue"] = new Money(50000)
    };
    
    context.Initialize(account);
    // Test logic...
}
```

### ❌ Don't

- **Don't create unnecessary data**: Avoid initializing entities not used in the test
- **Don't use hard-coded GUIDs**: They make tests hard to read and maintain
- **Don't repeat complex setup**: Extract to helper methods
- **Don't ignore relationships**: Set up lookups properly

```csharp
// ❌ Bad - Unnecessary complexity
[Fact]
public void Should_Calculate_Total_Revenue()
{
    var context = XrmFakedContextFactory.New();
    
    // Creating entities not used in test
    var user = new Entity("systemuser") { Id = Guid.NewGuid() };
    var team = new Entity("team") { Id = Guid.NewGuid() };
    var businessunit = new Entity("businessunit") { Id = Guid.NewGuid() };
    
    var account = new Entity("account")
    {
        Id = new Guid("12345678-1234-1234-1234-123456789012"), // Hard-coded GUID
        ["revenue"] = new Money(50000)
    };
    
    context.Initialize(new[] { user, team, businessunit, account });
    // Test logic...
}
```

### Initialize Before Each Test

Use your test framework's setup methods:

```csharp
public class AccountTests : IDisposable
{
    private readonly IXrmFakedContext _context;
    
    public AccountTests()
    {
        _context = XrmFakedContextFactory.New();
        InitializeTestData();
    }
    
    private void InitializeTestData()
    {
        // Common test data for all tests in this class
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account"
        };
        
        _context.Initialize(account);
    }
    
    [Fact]
    public void Test1()
    {
        // Test using initialized data
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

## See Also

- [XrmFakedContext](./xrm-faked-context.md) - Context initialization methods
- [CRUD Operations](../usage/crud-operations.md) - Creating and modifying entities
- [Querying Data](../usage/querying-data.md) - Retrieving test data
- [Testing Plugins](../usage/testing-plugins.md) - Using test data in plugin tests
