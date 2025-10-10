# Extension Methods

> **📝 Note**: This documentation is currently under development.

## Overview

Fake4Dataverse provides extension methods to simplify common testing operations.

## Context Extensions

### ExecutePluginWith<T>()

Executes a plugin with the specified context.

```csharp
using Fake4Dataverse.Plugins;

context.ExecutePluginWith<MyPlugin>(
    pluginContext =>
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20;
    },
    targetEntity
);
```

See [Testing Plugins](../usage/testing-plugins.md) for detailed examples.

## Entity Extensions

### GetAttributeValue<T>()

Gets a typed attribute value from an entity.

```csharp
string name = entity.GetAttributeValue<string>("name");
Money revenue = entity.GetAttributeValue<Money>("revenue");
```

### ToEntity<T>()

Converts a late-bound Entity to an early-bound entity.

```csharp
Account account = entity.ToEntity<Account>();
```

## Coming Soon

Complete documentation of all extension methods with:
- Method signatures
- Parameters
- Return types
- Examples
- Best practices

## See Also

- [Testing Plugins](../usage/testing-plugins.md) - Plugin execution extensions
- [XrmFakedContext](../concepts/xrm-faked-context.md) - Context methods
