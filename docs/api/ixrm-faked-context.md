# IXrmFakedContext Interface Reference

> **📝 Note**: This documentation is currently under development.

## Overview

`IXrmFakedContext` is the main interface for the fake Dataverse context. It provides methods for initializing data, getting services, and configuring the test environment.

## Core Methods

### GetOrganizationService()

Gets an instance of `IOrganizationService` for executing Dataverse operations.

```csharp
var service = context.GetOrganizationService();
```

### Initialize()

Initializes the context with test data.

```csharp
// Single entity
context.Initialize(entity);

// Multiple entities
context.Initialize(new[] { entity1, entity2 });

// Collection
context.Initialize(entityCollection);
```

### CreateQuery()

Creates a LINQ query for an entity type.

```csharp
// Late-bound
var accounts = context.CreateQuery("account").ToList();

// Early-bound
var accounts = context.CreateQuery<Account>().ToList();
```

### EnableProxyTypes()

Enables early-bound entity classes.

```csharp
context.EnableProxyTypes(typeof(Account).Assembly);
```

### GetProperty<T>() / SetProperty<T>()

Gets or sets custom properties on the context.

```csharp
context.SetProperty("TestMode", true);
bool testMode = context.GetProperty<bool>("TestMode");
```

## Properties

### CallerProperties

Properties of the calling user.

```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
context.CallerProperties.BusinessUnitId = businessUnitId;
```

## Complete Documentation

For detailed information and examples, see:
- [XrmFakedContext Guide](../concepts/xrm-faked-context.md) - Complete guide with examples
- [Basic Concepts](../getting-started/basic-concepts.md) - Framework fundamentals

## Coming Soon

Complete API reference with all methods, properties, and examples.
