# Migrating from FakeXrmEasy v1.x

This guide helps you migrate from FakeXrmEasy v1.x to Fake4Dataverse v4.x. The migration is straightforward as v4 maintains compatibility with most v1 patterns while adding new features.

## Table of Contents

- [Overview](#overview)
- [Quick Summary](#quick-summary)
- [Migration Steps](#migration-steps)
- [Common Migration Patterns](#common-migration-patterns)
- [Breaking Changes](#breaking-changes)
- [New Features](#new-features)
- [Troubleshooting](#troubleshooting)
- [See Also](#see-also)

## Overview

Fake4Dataverse v4.x is based on FakeXrmEasy v2.0.1 (the last MIT-licensed version). While maintaining backward compatibility, it introduces:
- Improved middleware architecture
- Interface-based design for better testability
- Enhanced message executor system
- Better extensibility

## Quick Summary

Key changes:
- Package names changed to `Fake4Dataverse.*`
- Context creation uses `XrmFakedContextFactory.New()`
- Interface-based approach with `IXrmFakedContext`
- New middleware architecture for customization
- Some properties moved to nested objects (`CallerProperties`)

## Migration Steps

### 1. Update Package References

```xml
<!-- Remove v1.x packages -->
<!-- <PackageReference Include="FakeXrmEasy" Version="1.x.x" /> -->

<!-- Add v4.x packages -->
<PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
<PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
```

### 2. Update Namespace References

```csharp
// v1.x
using FakeXrmEasy;
using FakeXrmEasy.Extensions;

// v4.x
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Plugins;
```

### 3. Update Context Creation

**v1.x:**
```csharp
var context = new XrmFakedContext();
```

**v4.x:**
```csharp
var context = XrmFakedContextFactory.New();
```

### 4. Update Interface References

**v1.x:**
```csharp
XrmFakedContext context = new XrmFakedContext();
```

**v4.x:**
```csharp
IXrmFakedContext context = XrmFakedContextFactory.New();
```

### 5. Enable Proxy Types

**v1.x:**
```csharp
context.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account));
```

**v4.x:**
```csharp
context.EnableProxyTypes(typeof(Account).Assembly);
```

## Common Migration Patterns

### Plugin Execution

**v1.x:**
```csharp
// Simple execution
context.ExecutePluginWithTarget<MyPlugin>(target);

// With configuration
context.ExecutePluginWithTarget<MyPlugin>(
    target,
    "Create",
    20 // Stage
);
```

**v4.x:**
```csharp
using Fake4Dataverse.Plugins;

context.ExecutePluginWith<MyPlugin>(
    pluginContext => 
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20; // Pre-operation
        // Configure other properties as needed
    },
    target
);
```

### Caller Properties

**v1.x:**
```csharp
context.CallerId = new EntityReference("systemuser", userId);
```

**v4.x:**
```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
context.CallerProperties.BusinessUnitId = businessUnitId;
```

### Querying

No changes - LINQ queries work the same:

```csharp
// Both v1.x and v4.x
var accounts = context.CreateQuery("account")
    .Where(a => a.GetAttributeValue<string>("name").StartsWith("Test"))
    .ToList();
```

### Service Creation

No changes:

```csharp
// Both v1.x and v4.x
var service = context.GetOrganizationService();
```

## Breaking Changes

### 1. Constructor Change

**Breaking:**
```csharp
// v1.x - No longer works
var context = new XrmFakedContext();
```

**Fixed:**
```csharp
// v4.x
var context = XrmFakedContextFactory.New();
```

### 2. ProxyTypesAssembly Property

**Breaking:**
```csharp
// v1.x - No longer works
context.ProxyTypesAssembly = assembly;
```

**Fixed:**
```csharp
// v4.x
context.EnableProxyTypes(assembly);
```

### 3. CallerId Property

**Breaking:**
```csharp
// v1.x - No longer works
context.CallerId = userRef;
```

**Fixed:**
```csharp
// v4.x
context.CallerProperties.CallerId = userRef;
```

### 4. Plugin Execution API

**Breaking:**
```csharp
// v1.x - No longer works
context.ExecutePluginWithTarget<MyPlugin>(target, "Create", 20);
```

**Fixed:**
```csharp
// v4.x
context.ExecutePluginWith<MyPlugin>(
    pluginContext => 
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20;
    },
    target
);
```

## New Features

### Middleware Architecture

v4.x introduces a powerful middleware system:

```csharp
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    // Add custom middleware
    .Use(next => (ctx, request) =>
    {
        // Custom logic before
        var response = next(ctx, request);
        // Custom logic after
        return response;
    })
    .UseCrud()
    .UseMessages()
    .Build();
```

### Custom Message Executors

Easier to add custom executors:

```csharp
public class MyCustomExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request) 
        => request.RequestName == "custom_MyMessage";
    
    public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
    {
        // Implementation
        return new OrganizationResponse();
    }
    
    public Type GetResponsibleRequestType() 
        => typeof(OrganizationRequest);
}
```

### Better Type Safety

Interface-based design provides better compile-time checking:

```csharp
IXrmFakedContext context = XrmFakedContextFactory.New();
// IDE can provide better IntelliSense
```

## Troubleshooting

### "XrmFakedContext does not contain a definition for..."

This usually means a property or method was moved or renamed. Check the [API Reference](../api/ixrm-faked-context.md).

**Common fixes:**
- `CallerId` → `CallerProperties.CallerId`
- `ProxyTypesAssembly` → `EnableProxyTypes(assembly)`
- Constructor → Factory method

### Plugin Execution Errors

If plugin execution fails:

1. Check you're using `Fake4Dataverse.Plugins` namespace
2. Use the new `ExecutePluginWith<T>()` syntax
3. Configure the plugin context explicitly

```csharp
using Fake4Dataverse.Plugins;

context.ExecutePluginWith<MyPlugin>(
    pluginContext => 
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20;
        pluginContext.Depth = 1;
        pluginContext.UserId = userId;
        pluginContext.InitiatingUserId = userId;
        pluginContext.OrganizationId = orgId;
    },
    target
);
```

### Namespace Not Found

Make sure you have the right packages installed:

```xml
<PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
<PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
```

## Migration Checklist

Use this checklist to track your migration:

- [ ] Update NuGet packages to Fake4Dataverse
- [ ] Update all `using` statements
- [ ] Replace `new XrmFakedContext()` with `XrmFakedContextFactory.New()`
- [ ] Update `ProxyTypesAssembly` to `EnableProxyTypes()`
- [ ] Update `CallerId` to `CallerProperties.CallerId`
- [ ] Update plugin execution to use `ExecutePluginWith<T>()`
- [ ] Test all test cases
- [ ] Update any custom message executors if needed
- [ ] Review and update documentation/comments

## See Also

- [Quick Start Guide](../getting-started/quickstart.md) - Get up to speed quickly
- [API Reference](../api/ixrm-faked-context.md) - Complete API documentation
- [Testing Plugins](../usage/testing-plugins.md) - Plugin testing patterns
- [Middleware Architecture](../concepts/middleware.md) - Understanding the new architecture
- [FAQ](../getting-started/faq.md) - Common questions

## Need Help?

- Check the [FAQ](../getting-started/faq.md)
- Review [complete examples](../getting-started/quickstart.md)
- Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
