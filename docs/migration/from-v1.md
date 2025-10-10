# Migrating from FakeXrmEasy v1.x

> **📝 Note**: This documentation is currently under development. For detailed migration information, see the [Fake4Dataverse package README](../../Fake4Dataverse/README.md#migration-guide).

## Overview

This guide helps you migrate from FakeXrmEasy v1.x to Fake4Dataverse v4.x.

## Quick Summary

Key changes:
- Package names changed to `Fake4Dataverse.*`
- Context creation uses `XrmFakedContextFactory.New()`
- Interface-based approach with `IXrmFakedContext`
- New middleware architecture

## Migration Steps

### 1. Update Package References

```xml
<!-- Remove v1.x packages -->
<!-- <PackageReference Include="FakeXrmEasy" Version="1.x.x" /> -->

<!-- Add v4.x packages -->
<PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
<PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
```

### 2. Update Context Creation

**v1.x:**
```csharp
var context = new XrmFakedContext();
```

**v4.x:**
```csharp
var context = XrmFakedContextFactory.New();
```

### 3. Update Namespace References

```csharp
// v1.x
using FakeXrmEasy;

// v4.x
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions;
```

### 4. Enable Proxy Types

**v1.x:**
```csharp
context.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account));
```

**v4.x:**
```csharp
context.EnableProxyTypes(typeof(Account).Assembly);
```

## Complete Migration Guide

For comprehensive migration instructions, see the [Fake4Dataverse README](../../Fake4Dataverse/README.md#migrating-from-fakexrmeasy-v1x-to-fake4dataverse-v4x).

## Common Issues

### Plugin Execution

**v1.x:**
```csharp
context.ExecutePluginWithTarget<MyPlugin>(target);
```

**v4.x:**
```csharp
context.ExecutePluginWith<MyPlugin>(
    pluginContext => 
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20;
    },
    target
);
```

### CallerProperties

**v1.x:**
```csharp
context.CallerId = new EntityReference("systemuser", userId);
```

**v4.x:**
```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
```

## Need Help?

- See [Fake4Dataverse Migration Guide](../../Fake4Dataverse/README.md#migration-guide)
- Check [FAQ](../getting-started/faq.md)
- Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
