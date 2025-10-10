# Middleware Architecture

Fake4Dataverse uses a **middleware pipeline** inspired by ASP.NET Core to process Dataverse requests. This guide explains how the middleware architecture works and how to use it effectively.

## Table of Contents
- [What is Middleware?](#what-is-middleware)
- [The Request Pipeline](#the-request-pipeline)
- [Default Pipeline](#default-pipeline)
- [Custom Middleware](#custom-middleware)
- [Middleware Builder](#middleware-builder)
- [Advanced Scenarios](#advanced-scenarios)

## What is Middleware?

Middleware is software that sits between your code and the core framework, processing requests as they flow through the pipeline.

### Middleware Analogy

Think of middleware like an assembly line:

```
Your Code → [Middleware 1] → [Middleware 2] → [Middleware 3] → Response
              ↓ Logging      ↓ Validation   ↓ CRUD Handler
```

Each middleware can:
1. **Process the request** (inspect, modify)
2. **Call the next middleware** (continue pipeline)
3. **Short-circuit** (stop pipeline and return response)
4. **Process the response** (modify on the way back)

### Why Middleware?

Benefits of the middleware architecture:
- **✅ Extensible** - Add your own middleware
- **✅ Composable** - Build complex behavior from simple parts
- **✅ Testable** - Test middleware in isolation
- **✅ Configurable** - Choose what to include
- **✅ Ordered** - Control execution order

## The Request Pipeline

### How It Works

When you call `service.Execute(request)`:

1. Request enters the pipeline
2. First middleware processes it
3. Middleware calls next in chain
4. Eventually reaches handler (CRUD, message executor, etc.)
5. Response flows back through middleware
6. Response returned to caller

### Visual Representation

```
service.Execute(request)
    ↓
┌─────────────────────────┐
│  Custom Middleware 1    │  (Optional - logging, validation, etc.)
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│  CRUD Middleware        │  (Handles Create, Read, Update, Delete)
└────────────┬────────────┘
             ↓
┌─────────────────────────┐
│  Message Executors      │  (Handles special messages like WhoAmI, Assign, etc.)
└────────────┬────────────┘
             ↓
         Response
```

### Code Example

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Middleware;

// Middleware signature
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> middleware = next =>
{
    return (context, request) =>
    {
        // Before: Process request
        Console.WriteLine($"Processing {request.RequestName}");
        
        // Call next middleware
        var response = next(context, request);
        
        // After: Process response
        Console.WriteLine($"Completed {request.RequestName}");
        
        return response;
    };
};
```

## Default Pipeline

### Factory Default

When you use `XrmFakedContextFactory.New()`:

```csharp
var context = XrmFakedContextFactory.New();
```

You get this pipeline:
1. **CRUD Middleware** - Handles Create, Retrieve, Update, Delete, Associate, Disassociate
2. **Message Executor Middleware** - Handles special messages (WhoAmI, Assign, etc.)
3. **Fallback Middleware** - Throws `PullRequestException` for unsupported messages

### What's Included

```csharp
// Equivalent to:
var context = MiddlewareBuilder
    .New()
    .AddCrud()              // Add CRUD support
    .AddFakeMessageExecutors()  // Add message executors
    .UseCrud()              // Use CRUD middleware
    .UseMessages()          // Use message executor middleware
    .Build();
```

## Custom Middleware

### Creating Simple Middleware

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Middleware;

[Fact]
public void Should_Use_Custom_Middleware()
{
    // Create custom logging middleware
    Func<OrganizationRequestDelegate, OrganizationRequestDelegate> loggingMiddleware = next =>
    {
        return (context, request) =>
        {
            // Log before
            Debug.WriteLine($"[BEFORE] {request.RequestName}");
            
            // Execute request
            var response = next(context, request);
            
            // Log after
            Debug.WriteLine($"[AFTER] {request.RequestName}");
            
            return response;
        };
    };
    
    // Build context with custom middleware
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .Use(loggingMiddleware)  // Add custom middleware
        .UseCrud()
        .Build();
    
    var service = context.GetOrganizationService();
    
    // This will trigger logging
    service.Create(new Entity("account") { ["name"] = "Test" });
}
```

### Validation Middleware

```csharp
[Fact]
public void Should_Validate_Requests()
{
    // Validation middleware
    Func<OrganizationRequestDelegate, OrganizationRequestDelegate> validationMiddleware = next =>
    {
        return (context, request) =>
        {
            // Validate Create requests
            if (request is CreateRequest createRequest)
            {
                var target = createRequest.Target;
                
                // Ensure account has a name
                if (target.LogicalName == "account" && !target.Contains("name"))
                {
                    throw new InvalidPluginExecutionException("Account must have a name");
                }
            }
            
            return next(context, request);
        };
    };
    
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .Use(validationMiddleware)
        .UseCrud()
        .Build();
    
    var service = context.GetOrganizationService();
    
    // This will fail validation
    Assert.Throws<InvalidPluginExecutionException>(() =>
        service.Create(new Entity("account") { ["revenue"] = new Money(1000) })
    );
}
```

### Short-Circuiting Middleware

Middleware can stop the pipeline and return a response immediately:

```csharp
[Fact]
public void Should_Short_Circuit_Pipeline()
{
    // Cache middleware that short-circuits on WhoAmI
    var cachedUserId = Guid.NewGuid();
    
    Func<OrganizationRequestDelegate, OrganizationRequestDelegate> cacheMiddleware = next =>
    {
        return (context, request) =>
        {
            // Short-circuit WhoAmI requests
            if (request is WhoAmIRequest)
            {
                return new WhoAmIResponse
                {
                    ["UserId"] = cachedUserId,
                    ["BusinessUnitId"] = Guid.NewGuid(),
                    ["OrganizationId"] = Guid.NewGuid()
                };
            }
            
            // Continue pipeline for other requests
            return next(context, request);
        };
    };
    
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .AddFakeMessageExecutors()
        .Use(cacheMiddleware)
        .UseCrud()
        .UseMessages()
        .Build();
    
    var service = context.GetOrganizationService();
    var response = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
    
    Assert.Equal(cachedUserId, response.UserId);
}
```

### Conditional Middleware

Execute middleware only for certain conditions:

```csharp
[Fact]
public void Should_Apply_Conditional_Logic()
{
    // Only log Create and Update
    Func<OrganizationRequestDelegate, OrganizationRequestDelegate> conditionalLogger = next =>
    {
        return (context, request) =>
        {
            bool shouldLog = request is CreateRequest || request is UpdateRequest;
            
            if (shouldLog)
            {
                Debug.WriteLine($"Logging: {request.RequestName}");
            }
            
            var response = next(context, request);
            
            if (shouldLog)
            {
                Debug.WriteLine($"Logged: {request.RequestName}");
            }
            
            return response;
        };
    };
    
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .Use(conditionalLogger)
        .UseCrud()
        .Build();
}
```

## Middleware Builder

### Builder Pattern

The `MiddlewareBuilder` uses a fluent API:

```csharp
var context = MiddlewareBuilder
    .New()                      // Create new builder
    .AddCrud()                  // Add CRUD support
    .AddFakeMessageExecutors()  // Add message executors
    .Use(customMiddleware)      // Add custom middleware
    .UseCrud()                  // Configure CRUD in pipeline
    .UseMessages()              // Configure messages in pipeline
    .Build();                   // Build the context
```

### Add vs Use

- **Add**: Registers components (adds to container)
- **Use**: Configures pipeline order (adds to middleware chain)

```csharp
var context = MiddlewareBuilder
    .New()
    
    // ADD: Register components
    .AddCrud()              // Registers CRUD handlers
    .AddFakeMessageExecutors()  // Registers message executors
    
    // USE: Configure pipeline
    .UseCrud()              // CRUD middleware first
    .UseMessages()          // Message executor middleware second
    
    .Build();
```

### Middleware Order Matters

Middleware executes in the order you configure:

```csharp
// Logging happens BEFORE CRUD
var context1 = MiddlewareBuilder
    .New()
    .AddCrud()
    .Use(loggingMiddleware)  // First
    .UseCrud()               // Second
    .Build();

// CRUD happens BEFORE logging
var context2 = MiddlewareBuilder
    .New()
    .AddCrud()
    .UseCrud()               // First
    .Use(loggingMiddleware)  // Second
    .Build();
```

## Advanced Scenarios

### Multiple Custom Middleware

Chain multiple middleware together:

```csharp
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    
    // Multiple custom middleware
    .Use(loggingMiddleware)
    .Use(validationMiddleware)
    .Use(auditMiddleware)
    .Use(cacheMiddleware)
    
    .UseCrud()
    .UseMessages()
    .Build();
```

Execution order:
1. loggingMiddleware
2. validationMiddleware
3. auditMiddleware
4. cacheMiddleware
5. CRUD middleware
6. Message executor middleware

### State Sharing Between Middleware

Use context properties to share state:

```csharp
// First middleware sets a flag
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> middleware1 = next =>
{
    return (context, request) =>
    {
        context.SetProperty("ProcessingStarted", DateTime.UtcNow);
        return next(context, request);
    };
};

// Second middleware reads the flag
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> middleware2 = next =>
{
    return (context, request) =>
    {
        var response = next(context, request);
        
        var startTime = context.GetProperty<DateTime>("ProcessingStarted");
        var duration = DateTime.UtcNow - startTime;
        Debug.WriteLine($"Processing took {duration.TotalMilliseconds}ms");
        
        return response;
    };
};

var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .Use(middleware1)
    .Use(middleware2)
    .UseCrud()
    .Build();
```

### Error Handling Middleware

Catch and handle exceptions:

```csharp
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> errorHandler = next =>
{
    return (context, request) =>
    {
        try
        {
            return next(context, request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in {request.RequestName}: {ex.Message}");
            
            // Transform exception
            throw new InvalidPluginExecutionException(
                $"Request failed: {ex.Message}", 
                ex);
        }
    };
};
```

### Performance Monitoring Middleware

Track request performance:

```csharp
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> performanceMonitor = next =>
{
    return (context, request) =>
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            return next(context, request);
        }
        finally
        {
            sw.Stop();
            Debug.WriteLine($"{request.RequestName} took {sw.ElapsedMilliseconds}ms");
        }
    };
};
```

### Request/Response Transformation

Modify requests or responses:

```csharp
Func<OrganizationRequestDelegate, OrganizationRequestDelegate> transformer = next =>
{
    return (context, request) =>
    {
        // Modify request before processing
        if (request is CreateRequest createRequest)
        {
            var target = createRequest.Target;
            
            // Auto-add created timestamp
            if (!target.Contains("createdon"))
            {
                target["createdon"] = DateTime.UtcNow;
            }
        }
        
        var response = next(context, request);
        
        // Modify response after processing
        // (if needed)
        
        return response;
    };
};
```

## Testing Middleware

### Unit Testing Middleware

```csharp
[Fact]
public void Should_Test_Custom_Middleware_Independently()
{
    // Create test middleware
    var executionOrder = new List<string>();
    
    Func<OrganizationRequestDelegate, OrganizationRequestDelegate> testMiddleware = next =>
    {
        return (context, request) =>
        {
            executionOrder.Add("Before");
            var response = next(context, request);
            executionOrder.Add("After");
            return response;
        };
    };
    
    // Create minimal pipeline with middleware
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .Use(testMiddleware)
        .UseCrud()
        .Build();
    
    var service = context.GetOrganizationService();
    service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Verify middleware executed
    Assert.Equal(new[] { "Before", "After" }, executionOrder);
}
```

## Best Practices

### ✅ Do

1. **Keep middleware focused**
   ```csharp
   // ✅ Good - single responsibility
   var loggingMiddleware = CreateLoggingMiddleware();
   var validationMiddleware = CreateValidationMiddleware();
   ```

2. **Use middleware for cross-cutting concerns**
   - Logging
   - Validation
   - Error handling
   - Performance monitoring
   - Caching

3. **Consider middleware order**
   ```csharp
   // ✅ Good - validation before processing
   .Use(validationMiddleware)
   .UseCrud()
   ```

### ❌ Don't

1. **Don't put business logic in middleware**
   ```csharp
   // ❌ Bad - business logic belongs in plugins/handlers
   .Use(middleware => /* complex business logic */)
   ```

2. **Don't forget to call next**
   ```csharp
   // ❌ Bad - breaks the pipeline
   return (context, request) => {
       // Do something
       // Missing: return next(context, request);
   };
   ```

## Real-World Examples

### Audit Trail Middleware

```csharp
public static class AuditMiddleware
{
    public static Func<OrganizationRequestDelegate, OrganizationRequestDelegate> Create(
        List<string> auditLog)
    {
        return next => (context, request) =>
        {
            var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {request.RequestName}";
            
            if (request is CreateRequest createReq)
            {
                entry += $" - {createReq.Target.LogicalName}";
            }
            
            auditLog.Add(entry);
            
            return next(context, request);
        };
    }
}

// Usage
var auditLog = new List<string>();
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .Use(AuditMiddleware.Create(auditLog))
    .UseCrud()
    .Build();
```

## Next Steps

- [XrmFakedContext](./xrm-faked-context.md) - Learn about the context
- [Custom Message Executors](../api/custom-executors.md) - Create custom handlers
- [Testing Plugins](../usage/testing-plugins.md) - Test with middleware

## See Also

- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) - Inspiration for this architecture
- [Basic Concepts](../getting-started/basic-concepts.md) - Framework fundamentals
