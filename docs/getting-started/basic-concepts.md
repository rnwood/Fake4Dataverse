# Basic Concepts

Understanding the core concepts of Fake4Dataverse will help you write better tests and use the framework effectively.

## Table of Contents
- [What is Fake4Dataverse?](#what-is-fake4dataverse)
- [The XrmFakedContext](#the-xrmfakedcontext)
- [IOrganizationService](#iorganizationservice)
- [In-Memory Data Store](#in-memory-data-store)
- [Middleware Architecture](#middleware-architecture)
- [Test Lifecycle](#test-lifecycle)

## What is Fake4Dataverse?

Fake4Dataverse is a **testing double** (or "fake") for Microsoft Dataverse/Dynamics 365. It provides an in-memory implementation of the Dataverse platform that you can use in unit tests.

### Inspired By

The framework follows patterns from popular testing libraries:

- **Moq** - Provides mock objects for testing
- **NSubstitute** - Substitute implementations
- **Entity Framework InMemory** - In-memory database for testing

Like these libraries, Fake4Dataverse lets you:
- ✅ Test without external dependencies
- ✅ Run tests fast (no network calls)
- ✅ Control test data precisely
- ✅ Verify behavior in isolation

## The XrmFakedContext

The `XrmFakedContext` is the core of the framework. It simulates a Dataverse environment in memory.

### Creating a Context

```csharp
using Fake4Dataverse.Middleware;

// Simple creation
var context = XrmFakedContextFactory.New();
```

### What Does It Do?

The context:
1. **Stores entities** in memory
2. **Processes requests** through a middleware pipeline
3. **Simulates Dataverse behavior** (CRUD, queries, plugins, etc.)
4. **Provides services** like IOrganizationService

Think of it as a miniature Dataverse running in your test process.

### Context Interface: IXrmFakedContext

Always use the interface `IXrmFakedContext`, not the concrete type:

```csharp
using Fake4Dataverse.Abstractions;

// ✅ Good - use interface
IXrmFakedContext context = XrmFakedContextFactory.New();

// ❌ Avoid - concrete type couples to implementation
XrmFakedContext context = new XrmFakedContext();
```

**Why?** Using the interface:
- Makes tests more maintainable
- Follows dependency inversion principle
- Enables easier migration to future versions

## IOrganizationService

The `IOrganizationService` is how your code interacts with Dataverse. Fake4Dataverse provides a fake implementation.

### Getting the Service

```csharp
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();
```

### Service Operations

The service supports standard Dataverse operations:

```csharp
// Create
var id = service.Create(entity);

// Retrieve
var entity = service.Retrieve("account", id, new ColumnSet(true));

// Update
service.Update(entity);

// Delete
service.Delete("account", id);

// Execute (for special messages)
var response = service.Execute(request);

// RetrieveMultiple (queries)
var results = service.RetrieveMultiple(query);

// Associate/Disassociate (relationships)
service.Associate(entityName, id, relationship, relatedEntities);
service.Disassociate(entityName, id, relationship, relatedEntities);
```

## In-Memory Data Store

Fake4Dataverse stores all entities in memory using a dictionary-based structure.

### How It Works

```
XrmFakedContext
    └── Data (Dictionary)
        ├── "account" → List<Entity>
        ├── "contact" → List<Entity>
        └── "opportunity" → List<Entity>
```

### Initializing Data

You can pre-populate the data store:

```csharp
var context = XrmFakedContextFactory.New();

// Initialize with a single entity
var account = new Entity("account") 
{ 
    Id = Guid.NewGuid(),
    ["name"] = "Contoso"
};
context.Initialize(account);

// Initialize with multiple entities
context.Initialize(new[]
{
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Contoso" },
    new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "John" }
});
```

### Querying Data

Query using LINQ:

```csharp
var accounts = context.CreateQuery("account")
    .Where(a => ((string)a["name"]).StartsWith("Cont"))
    .ToList();
```

Or with strongly-typed entities:

```csharp
context.EnableProxyTypes(typeof(Account).Assembly);

var accounts = context.CreateQuery<Account>()
    .Where(a => a.Name.StartsWith("Cont"))
    .ToList();
```

## Middleware Architecture

Fake4Dataverse uses a **middleware pipeline** inspired by ASP.NET Core.

### What is Middleware?

Middleware are components that handle requests in a pipeline:

```
Request → Middleware 1 → Middleware 2 → Middleware 3 → Response
```

Each middleware can:
- Process the request
- Call the next middleware
- Modify the response
- Short-circuit the pipeline

### Default Pipeline

When you use `XrmFakedContextFactory.New()`, you get this pipeline:

1. **CRUD Middleware** - Handles Create, Retrieve, Update, Delete
2. **Message Executors** - Handles special messages (Associate, WhoAmI, etc.)
3. **Query Middleware** - Handles RetrieveMultiple (LINQ, FetchXML)

### Custom Pipeline

You can build a custom pipeline:

```csharp
using Fake4Dataverse.Middleware;

var context = MiddlewareBuilder
    .New()
    // Add middleware components
    .AddCrud()
    .AddFakeMessageExecutors()
    // Configure pipeline order
    .UseCrud()
    .UseMessages()
    .Build();
```

### Why Middleware?

Benefits:
- **Extensible** - Add your own middleware
- **Configurable** - Choose what to include
- **Testable** - Test middleware in isolation
- **Composable** - Build complex behavior from simple parts

Learn more: [Middleware Architecture](../concepts/middleware.md)

## Test Lifecycle

Understanding the test lifecycle helps you write better tests.

### Typical Test Structure

```csharp
[Fact]
public void Should_DoSomething_When_Condition()
{
    // === ARRANGE ===
    // 1. Create context
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // 2. Initialize test data
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" }
    });
    
    // 3. Set up test conditions
    var inputData = new Entity("contact")
    {
        ["firstname"] = "John"
    };
    
    // === ACT ===
    // 4. Execute the operation under test
    var contactId = service.Create(inputData);
    
    // === ASSERT ===
    // 5. Verify expectations
    Assert.NotEqual(Guid.Empty, contactId);
    var created = service.Retrieve("contact", contactId, new ColumnSet(true));
    Assert.Equal("John", created["firstname"]);
}
```

### Test Isolation

Each test should:
- ✅ Create its own context
- ✅ Initialize its own data
- ✅ Be independent of other tests
- ✅ Clean up automatically (via context disposal)

```csharp
public class MyTests
{
    // ❌ Don't share context between tests
    private static IXrmFakedContext _sharedContext;
    
    [Fact]
    public void Test1()
    {
        // ✅ Create fresh context per test
        var context = XrmFakedContextFactory.New();
        // ... test code
    }
    
    [Fact]
    public void Test2()
    {
        // ✅ Create fresh context per test
        var context = XrmFakedContextFactory.New();
        // ... test code
    }
}
```

### Dispose Pattern (Optional)

If you want explicit cleanup:

```csharp
[Fact]
public void Should_DoSomething()
{
    using (var context = XrmFakedContextFactory.New())
    {
        var service = context.GetOrganizationService();
        // ... test code
    } // Context disposed here
}
```

## Key Differences from Real Dataverse

While Fake4Dataverse simulates Dataverse closely, there are differences:

| Aspect | Real Dataverse | Fake4Dataverse |
|--------|---------------|----------------|
| **Storage** | SQL Database | In-memory dictionary |
| **Network** | HTTP/SOAP calls | Direct method calls |
| **Performance** | Slower (network + DB) | Very fast (in-memory) |
| **Persistence** | Data persists | Data cleared after test |
| **Transactions** | Real DB transactions | Simulated |
| **Async Plugins** | Run asynchronously | Run synchronously |
| **Validation** | Full platform validation | Subset of validation |

### What's Simulated?

✅ **Fully Simulated**:
- CRUD operations
- Relationships (Associate/Disassociate)
- Query expressions (LINQ, QueryExpression)
- FetchXML queries
- Message executors (WhoAmI, Assign, etc.)
- Plugin execution context
- Security roles (basic)

⚠️ **Partially Simulated**:
- Transactions (no rollback on error)
- Async plugins (run synchronously)
- Metadata (limited)
- Calculated/Rollup fields

✅ **Now Supported**:
- Cloud Flows - Power Automate flow simulation (with JSON import!)
- Custom APIs - Modern Dataverse Custom APIs

❌ **Not Simulated**:
- Business rules
- Classic Workflows (deprecated WF engine)
- JavaScript web resources
- Canvas apps

## Common Patterns

### Pattern 1: Test Setup with Constructor

```csharp
public class AccountPluginTests
{
    private readonly IXrmFakedContext _context;
    private readonly IOrganizationService _service;
    
    public AccountPluginTests()
    {
        _context = XrmFakedContextFactory.New();
        _service = _context.GetOrganizationService();
    }
    
    [Fact]
    public void Test1()
    {
        // Use _context and _service
    }
}
```

### Pattern 2: Test Data Builders

```csharp
public static class TestDataBuilder
{
    public static Entity CreateTestAccount(string name = "Test Account")
    {
        return new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = name,
            ["revenue"] = new Money(1000000)
        };
    }
    
    public static Entity CreateTestContact(string firstName, string lastName)
    {
        return new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = firstName,
            ["lastname"] = lastName
        };
    }
}

// Usage
[Fact]
public void Should_DoSomething()
{
    var context = XrmFakedContextFactory.New();
    var account = TestDataBuilder.CreateTestAccount("Contoso");
    context.Initialize(account);
    // ...
}
```

### Pattern 3: Extension Methods for Common Operations

```csharp
public static class FakeContextExtensions
{
    public static void InitializeWithRelatedData(
        this IXrmFakedContext context,
        Entity parent,
        params Entity[] children)
    {
        var entities = new List<Entity> { parent };
        entities.AddRange(children);
        context.Initialize(entities);
    }
}

// Usage
[Fact]
public void Should_DoSomething()
{
    var context = XrmFakedContextFactory.New();
    var account = new Entity("account") { Id = Guid.NewGuid() };
    var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
    var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
    
    context.InitializeWithRelatedData(account, contact1, contact2);
    // ...
}
```

## Next Steps

Now that you understand the basics:

- **[XrmFakedContext Deep Dive](../concepts/xrm-faked-context.md)** - Learn more about the context
- **[Middleware Architecture](../concepts/middleware.md)** - Understand the request pipeline
- **[Data Management](../concepts/data-management.md)** - Master test data setup
- **[Testing Plugins](../usage/testing-plugins.md)** - Test your plugins
- **[Querying Data](../usage/querying-data.md)** - Master queries

## Summary

Key takeaways:
- 🎯 **XrmFakedContext** simulates Dataverse in memory
- 🎯 **IOrganizationService** provides the same interface as real Dataverse
- 🎯 **Middleware pipeline** processes requests flexibly
- 🎯 **Test isolation** - one context per test
- 🎯 **Fast execution** - no network or database calls
