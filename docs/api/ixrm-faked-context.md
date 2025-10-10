# IXrmFakedContext Interface Reference

The `IXrmFakedContext` interface is the main entry point for Fake4Dataverse. It provides all the methods and properties needed to set up and interact with a faked Dataverse environment.

## Table of Contents

- [Overview](#overview)
- [Creating a Context](#creating-a-context)
- [Core Methods](#core-methods)
- [Properties](#properties)
- [Data Management](#data-management)
- [Querying](#querying)
- [Service Creation](#service-creation)
- [Configuration](#configuration)
- [Complete Examples](#complete-examples)
- [See Also](#see-also)

## Overview

`IXrmFakedContext` simulates a Dataverse environment in memory, providing:
- Entity data storage
- Query execution (LINQ and FetchXML)
- Organization service creation
- Plugin execution context
- Configuration management

**Reference:** [IOrganizationService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice) - The real Dataverse service interface that IXrmFakedContext wraps for testing purposes.

## Creating a Context

### Factory Method (Recommended)

```csharp
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
```

This is the preferred way to create a context in v4.x. It uses the factory pattern and returns an `IXrmFakedContext` interface.

### With Custom Configuration

```csharp
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();
```

## Core Methods

### GetOrganizationService()

Gets an instance of `IOrganizationService` for executing operations.

```csharp
var service = context.GetOrganizationService();

// Use the service
var accountId = service.Create(new Entity("account") 
{ 
    ["name"] = "Test Account" 
});
```

**Returns:** `IOrganizationService` - Service instance tied to this context

### Initialize()

Initializes the context with test data. Multiple overloads available:

#### Single Entity

```csharp
var account = new Entity("account") 
{ 
    Id = Guid.NewGuid(), 
    ["name"] = "Test" 
};

context.Initialize(account);
```

#### Multiple Entities (Array)

```csharp
var entities = new[]
{
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2" },
    new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "John" }
};

context.Initialize(entities);
```

#### Entity Collection

```csharp
var collection = new EntityCollection();
collection.Entities.Add(new Entity("account") { Id = Guid.NewGuid() });
collection.Entities.Add(new Entity("contact") { Id = Guid.NewGuid() });

context.Initialize(collection);
```

**Note:** `Initialize()` can be called multiple times to add more data.

### CreateQuery()

Creates a LINQ query for entities.

#### Late-Bound (Dynamic)

```csharp
var accounts = context.CreateQuery("account")
    .Where(a => a.GetAttributeValue<string>("name").StartsWith("Test"))
    .ToList();
```

#### Early-Bound (Strongly-Typed)

```csharp
// First, enable proxy types
context.EnableProxyTypes(typeof(Account).Assembly);

// Then query with early-bound types
var accounts = context.CreateQuery<Account>()
    .Where(a => a.Name.StartsWith("Test"))
    .ToList();
```

**Returns:** `IQueryable<Entity>` or `IQueryable<T>` - LINQ queryable collection

## Properties

### CallerProperties

Properties of the calling user executing operations.

```csharp
// Set calling user
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

// Set business unit
context.CallerProperties.BusinessUnitId = Guid.NewGuid();
```

**Available Properties:**
- `CallerId` (EntityReference) - The user executing operations
- `BusinessUnitId` (Guid) - The business unit of the caller

## Data Management

### Adding Data

```csharp
// Add single entity
context.Initialize(new Entity("account") 
{ 
    Id = Guid.NewGuid(), 
    ["name"] = "Test" 
});

// Add related entities
var accountId = Guid.NewGuid();
var contactId = Guid.NewGuid();

context.Initialize(new[]
{
    new Entity("account") 
    { 
        Id = accountId, 
        ["name"] = "Contoso" 
    },
    new Entity("contact")
    {
        Id = contactId,
        ["firstname"] = "John",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    }
});
```

### Retrieving Data

```csharp
// Via service
var service = context.GetOrganizationService();
var account = service.Retrieve("account", accountId, new ColumnSet(true));

// Via LINQ
var accounts = context.CreateQuery("account")
    .Where(a => a.GetAttributeValue<decimal>("revenue") > 100000)
    .ToList();
```

### Modifying Data

```csharp
var service = context.GetOrganizationService();

// Update
var account = new Entity("account")
{
    Id = accountId,
    ["name"] = "Updated Name"
};
service.Update(account);

// Delete
service.Delete("account", accountId);
```

## Querying

### LINQ Queries

```csharp
// Simple query
var activeAccounts = context.CreateQuery("account")
    .Where(a => a.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
    .ToList();

// Complex query with multiple conditions
var results = context.CreateQuery("opportunity")
    .Where(o => 
        o.GetAttributeValue<Money>("estimatedvalue").Value > 50000 &&
        o.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
    .OrderByDescending(o => o.GetAttributeValue<Money>("estimatedvalue").Value)
    .Take(10)
    .ToList();
```

### FetchXML Queries

```csharp
var service = context.GetOrganizationService();

var fetchXml = @"
    <fetch>
        <entity name='account'>
            <attribute name='name' />
            <attribute name='revenue' />
            <filter>
                <condition attribute='statecode' operator='eq' value='0' />
            </filter>
        </entity>
    </fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

## Service Creation

### Default Service

```csharp
var service = context.GetOrganizationService();
```

### Service with Specific User

```csharp
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
var service = context.GetOrganizationService();

// All operations with this service execute as the specified user
```

### Multiple Services

```csharp
// Service for User 1
context.CallerProperties.CallerId = new EntityReference("systemuser", user1Id);
var service1 = context.GetOrganizationService();

// Service for User 2
context.CallerProperties.CallerId = new EntityReference("systemuser", user2Id);
var service2 = context.GetOrganizationService();

// Both services share the same data store
```

## Configuration

### EnableProxyTypes()

Enables early-bound entity classes.

```csharp
// Enable for an assembly
context.EnableProxyTypes(typeof(Account).Assembly);

// Now you can use early-bound types
var accounts = context.CreateQuery<Account>()
    .Where(a => a.Revenue.Value > 100000)
    .ToList();
```

### GetProperty<T>() / SetProperty<T>()

Store and retrieve custom properties on the context.

```csharp
// Set a property
context.SetProperty("TestEnvironment", "Integration");

// Get a property
var environment = context.GetProperty<string>("TestEnvironment");
```

## Complete Examples

### Basic CRUD Test

```csharp
[Fact]
public void Should_Perform_CRUD_Operations()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Test Account",
        ["revenue"] = new Money(100000)
    });
    
    // Retrieve
    var account = service.Retrieve("account", accountId, new ColumnSet(true));
    Assert.Equal("Test Account", account["name"]);
    
    // Update
    account["name"] = "Updated Account";
    service.Update(account);
    
    // Verify update
    var updated = service.Retrieve("account", accountId, new ColumnSet("name"));
    Assert.Equal("Updated Account", updated["name"]);
    
    // Delete
    service.Delete("account", accountId);
    
    // Verify deletion
    var deleted = context.CreateQuery("account")
        .Where(a => a.Id == accountId)
        .FirstOrDefault();
    Assert.Null(deleted);
}
```

### Testing with Multiple Users

```csharp
[Fact]
public void Should_Support_Multiple_User_Contexts()
{
    var context = XrmFakedContextFactory.New();
    
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = user1Id, ["fullname"] = "User 1" },
        new Entity("systemuser") { Id = user2Id, ["fullname"] = "User 2" }
    });
    
    // User 1 creates an account
    context.CallerProperties.CallerId = new EntityReference("systemuser", user1Id);
    var service1 = context.GetOrganizationService();
    var accountId = service1.Create(new Entity("account") { ["name"] = "User 1 Account" });
    
    // User 2 can access it
    context.CallerProperties.CallerId = new EntityReference("systemuser", user2Id);
    var service2 = context.GetOrganizationService();
    var account = service2.Retrieve("account", accountId, new ColumnSet("name"));
    
    Assert.NotNull(account);
}
```

### Complex Query Example

```csharp
[Fact]
public void Should_Execute_Complex_Query()
{
    var context = XrmFakedContextFactory.New();
    
    // Initialize test data
    context.Initialize(new[]
    {
        new Entity("account") 
        { 
            Id = Guid.NewGuid(), 
            ["name"] = "High Value",
            ["revenue"] = new Money(500000),
            ["statecode"] = new OptionSetValue(0)
        },
        new Entity("account") 
        { 
            Id = Guid.NewGuid(), 
            ["name"] = "Medium Value",
            ["revenue"] = new Money(200000),
            ["statecode"] = new OptionSetValue(0)
        },
        new Entity("account") 
        { 
            Id = Guid.NewGuid(), 
            ["name"] = "Low Value",
            ["revenue"] = new Money(50000),
            ["statecode"] = new OptionSetValue(1) // Inactive
        }
    });
    
    // Query active accounts with revenue > 100K
    var highValueAccounts = context.CreateQuery("account")
        .Where(a => 
            a.GetAttributeValue<OptionSetValue>("statecode").Value == 0 &&
            a.GetAttributeValue<Money>("revenue").Value > 100000)
        .OrderByDescending(a => a.GetAttributeValue<Money>("revenue").Value)
        .ToList();
    
    Assert.Equal(2, highValueAccounts.Count);
    Assert.Equal("High Value", highValueAccounts[0]["name"]);
}
```

## See Also

- [XrmFakedContext Guide](../concepts/xrm-faked-context.md) - Comprehensive context guide
- [Basic Concepts](../getting-started/basic-concepts.md) - Framework fundamentals
- [Data Management](../concepts/data-management.md) - Managing test data
- [Service Initialization](../concepts/service-initialization.md) - Creating services
