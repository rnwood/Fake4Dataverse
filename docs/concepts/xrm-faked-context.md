# XrmFakedContext

The `XrmFakedContext` is the core component of Fake4Dataverse. It simulates a Microsoft Dataverse environment in memory, allowing you to test code without connecting to a real CRM instance.

## Table of Contents
- [What is XrmFakedContext?](#what-is-xrmfakedcontext)
- [Creating a Context](#creating-a-context)
- [Context Properties](#context-properties)
- [Context Methods](#context-methods)
- [Service Initialization](#service-initialization)
- [Data Management](#data-management)
- [Context Configuration](#context-configuration)
- [Advanced Usage](#advanced-usage)

## What is XrmFakedContext?

`XrmFakedContext` is an in-memory implementation of a Dataverse environment. Think of it as a lightweight, testable version of Dataverse that runs entirely in your test process.

### Key Characteristics

- **In-Memory**: All data stored in RAM, no database required
- **Fast**: No network calls or I/O operations
- **Isolated**: Each test gets its own independent context
- **Disposable**: Automatically cleaned up after tests

### Architecture

```
XrmFakedContext
    ├── Data Store (Dictionary<string, List<Entity>>)
    ├── Middleware Pipeline
    ├── Organization Service(s)
    ├── Caller Properties
    └── Configuration Options
```

## Creating a Context

### Using Factory (Recommended)

The simplest way to create a context:

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions;

// Create with default configuration
IXrmFakedContext context = XrmFakedContextFactory.New();
```

This creates a context with:
- Default middleware pipeline (CRUD + Message Executors)
- Empty data store
- Default caller properties

### Using Middleware Builder

For custom configuration:

```csharp
using Fake4Dataverse.Middleware;

var context = MiddlewareBuilder
    .New()
    // Add middleware components
    .AddCrud()
    .AddFakeMessageExecutors()
    // Configure pipeline
    .UseCrud()
    .UseMessages()
    .Build();
```

### Interface vs Concrete Type

Always use the `IXrmFakedContext` interface:

```csharp
// ✅ Good - use interface
IXrmFakedContext context = XrmFakedContextFactory.New();

// ❌ Avoid - concrete type
XrmFakedContext context = new XrmFakedContext();
```

**Why?**
- Better for testing and mocking
- Easier to migrate to future versions
- Follows dependency inversion principle
- More maintainable code

## Context Properties

### CallerProperties

Defines who is making the requests:

```csharp
var context = XrmFakedContextFactory.New();

// Set the calling user
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

// Set the business unit
var businessUnitId = Guid.NewGuid();
context.CallerProperties.BusinessUnitId = businessUnitId;
```

Usage in tests:

```csharp
[Fact]
public void Should_Use_Caller_Id_In_Plugin()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var userId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // When WhoAmI is executed
    var request = new WhoAmIRequest();
    var response = (WhoAmIResponse)service.Execute(request);
    
    // Returns the caller ID
    Assert.Equal(userId, response.UserId);
}
```

### Data Store

Access the underlying data (advanced):

```csharp
var context = XrmFakedContextFactory.New();

// Access as XrmFakedContext to get Data property
var concreteContext = context as XrmFakedContext;
var data = concreteContext.Data;

// Check if entity type exists
bool hasAccounts = data.ContainsKey("account");

// Get all accounts
if (hasAccounts)
{
    var accounts = data["account"];
}
```

**Note**: Directly accessing Data is rarely needed. Use `Initialize()` and query methods instead.

## Context Methods

### Initialize

Pre-populate the context with test data:

```csharp
// Single entity
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso"
};
context.Initialize(account);

// Multiple entities
context.Initialize(new[]
{
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Contoso" },
    new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "John" }
});

// Collection
IEnumerable<Entity> entities = GetTestData();
context.Initialize(entities);
```

### GetOrganizationService

Get a service for executing requests:

```csharp
var service = context.GetOrganizationService();

// Use the service
var id = service.Create(entity);
var retrieved = service.Retrieve("account", id, new ColumnSet(true));
```

### CreateQuery

Query entities using LINQ:

```csharp
// Late-bound (dynamic)
var accounts = context.CreateQuery("account")
    .Where(a => ((string)a["name"]).StartsWith("Con"))
    .ToList();

// Early-bound (strongly-typed)
context.EnableProxyTypes(typeof(Account).Assembly);
var typedAccounts = context.CreateQuery<Account>()
    .Where(a => a.Name.StartsWith("Con"))
    .ToList();
```

### EnableProxyTypes

Enable early-bound entity classes:

```csharp
// Enable for specific assembly
context.EnableProxyTypes(typeof(Account).Assembly);

// Now you can use strongly-typed entities
var account = new Account
{
    Name = "Contoso",
    Revenue = new Money(1000000)
};

context.Initialize(account);

var accounts = context.CreateQuery<Account>()
    .Where(a => a.Revenue.Value > 500000)
    .ToList();
```

### GetProperty / SetProperty

Store custom properties on the context:

```csharp
// Set a property
context.SetProperty("TestMode", true);
context.SetProperty("MaxRetries", 3);

// Get a property
bool testMode = context.GetProperty<bool>("TestMode");
int maxRetries = context.GetProperty<int>("MaxRetries");

// Check if property exists
if (context.GetProperty<string>("SomeKey") != null)
{
    // Property exists
}
```

Use cases:
- Store test configuration
- Share state between test setup and assertions
- Pass data to custom middleware

## Service Initialization

### Basic Service

```csharp
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Use service
var id = service.Create(new Entity("account") { ["name"] = "Test" });
```

### Multiple Services

Each service can have a different caller:

```csharp
var context = XrmFakedContextFactory.New();

// Service as User 1
var user1Id = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", user1Id);
var service1 = context.GetOrganizationService();

// Service as User 2
var user2Id = Guid.NewGuid();
var service2 = context.GetOrganizationService();
// Note: CallerProperties affects all services from the same context
```

## Data Management

### Initializing Data

#### Empty Context
```csharp
// Start with empty context
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Add data via service
var id = service.Create(new Entity("account") { ["name"] = "Test" });
```

#### Pre-populated Context
```csharp
// Start with test data
var context = XrmFakedContextFactory.New();

var testData = new[]
{
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2" },
    new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "John" }
};

context.Initialize(testData);
```

### Querying Data

#### LINQ
```csharp
var accounts = context.CreateQuery("account")
    .Where(a => ((string)a["name"]).Contains("Contoso"))
    .ToList();
```

#### Service.RetrieveMultiple
```csharp
var service = context.GetOrganizationService();
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name")
};
var results = service.RetrieveMultiple(query);
```

### Modifying Data

```csharp
var service = context.GetOrganizationService();

// Create
var id = service.Create(entity);

// Update
service.Update(updatedEntity);

// Delete
service.Delete("account", id);
```

## Context Configuration

### Custom Middleware Pipeline

```csharp
var context = MiddlewareBuilder
    .New()
    // Only include what you need
    .AddCrud()
    .UseCrud()
    .Build();
```

### With Message Executors

```csharp
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();
```

### Custom Middleware

```csharp
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .Use(next => (ctx, request) =>
    {
        // Custom logic before
        Console.WriteLine($"Executing {request.RequestName}");
        
        // Call next middleware
        var response = next(ctx, request);
        
        // Custom logic after
        Console.WriteLine($"Completed {request.RequestName}");
        
        return response;
    })
    .UseCrud()
    .Build();
```

## Advanced Usage

### Context Per Test

Best practice - create a fresh context for each test:

```csharp
public class AccountTests
{
    [Fact]
    public void Test1()
    {
        var context = XrmFakedContextFactory.New();
        // Test using context
    }
    
    [Fact]
    public void Test2()
    {
        var context = XrmFakedContextFactory.New();
        // Independent context
    }
}
```

### Shared Setup via Constructor

```csharp
public class AccountTests
{
    private readonly IXrmFakedContext _context;
    private readonly IOrganizationService _service;
    
    public AccountTests()
    {
        _context = XrmFakedContextFactory.New();
        _service = _context.GetOrganizationService();
        
        // Shared initialization
        InitializeTestData();
    }
    
    private void InitializeTestData()
    {
        _context.Initialize(new[]
        {
            new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test Account" }
        });
    }
    
    [Fact]
    public void Test1()
    {
        // Use _context and _service
    }
}
```

**⚠️ Warning**: Tests may not be isolated with shared setup. Consider if this is appropriate for your tests.

### Disposing Context

The context is disposable:

```csharp
[Fact]
public void Should_Dispose_Context()
{
    using (var context = XrmFakedContextFactory.New())
    {
        // Use context
    } // Automatically disposed
}
```

However, in most test scenarios, explicit disposal is not necessary as the garbage collector will clean up.

### Context Extension Methods

Create reusable extension methods:

```csharp
public static class FakeContextExtensions
{
    public static void InitializeWithAccountAndContacts(
        this IXrmFakedContext context,
        out Guid accountId,
        int contactCount = 3)
    {
        accountId = Guid.NewGuid();
        
        var entities = new List<Entity>
        {
            new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            }
        };
        
        for (int i = 0; i < contactCount; i++)
        {
            entities.Add(new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact {i}",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            });
        }
        
        context.Initialize(entities);
    }
}

// Usage
[Fact]
public void Test_WithExtension()
{
    var context = XrmFakedContextFactory.New();
    
    Guid accountId;
    context.InitializeWithAccountAndContacts(out accountId, contactCount: 5);
    
    var contacts = context.CreateQuery("contact").ToList();
    Assert.Equal(5, contacts.Count);
}
```

## Common Patterns

### Pattern: Test Fixture

```csharp
public class AccountTestFixture : IDisposable
{
    public IXrmFakedContext Context { get; }
    public IOrganizationService Service { get; }
    public Guid StandardAccountId { get; }
    
    public AccountTestFixture()
    {
        Context = XrmFakedContextFactory.New();
        Service = Context.GetOrganizationService();
        
        StandardAccountId = Guid.NewGuid();
        Context.Initialize(new Entity("account")
        {
            Id = StandardAccountId,
            ["name"] = "Standard Test Account"
        });
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}

// Usage with xUnit
public class AccountTests : IClassFixture<AccountTestFixture>
{
    private readonly AccountTestFixture _fixture;
    
    public AccountTests(AccountTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void Should_Use_Shared_Context()
    {
        var account = _fixture.Service.Retrieve(
            "account", 
            _fixture.StandardAccountId,
            new ColumnSet(true));
        
        Assert.NotNull(account);
    }
}
```

## Troubleshooting

### "Entity not found" Errors

**Problem**: Trying to retrieve an entity that doesn't exist.

**Solution**: Initialize the entity first:
```csharp
context.Initialize(myEntity);
```

### Service Returns Wrong Results

**Problem**: Query returns unexpected data.

**Solution**: Check what's in the context:
```csharp
var allAccounts = context.CreateQuery("account").ToList();
// Debug: what's actually in there?
```

### Type Casting Errors

**Problem**: Cannot cast attribute value.

**Solution**: Check attribute type:
```csharp
// ❌ Wrong type
var value = (string)entity["revenue"]; // revenue is Money!

// ✅ Correct type
var value = ((Money)entity["revenue"]).Value;
```

## Next Steps

- [Middleware Architecture](./middleware.md) - Understand the request pipeline
- [Data Management](./data-management.md) - Advanced data management
- [Service Initialization](./service-initialization.md) - Service patterns
- [Testing Plugins](../usage/testing-plugins.md) - Use context to test plugins

## See Also

- [Basic Concepts](../getting-started/basic-concepts.md) - Framework fundamentals
- [Quick Start](../getting-started/quickstart.md) - First test examples
- [API Reference](../api/ixrm-faked-context.md) - Complete API documentation
