# Data Management

> **📝 Note**: This documentation is currently under development. For now, see related documentation below.

## Related Documentation

- [XrmFakedContext](./xrm-faked-context.md) - Context initialization and data methods
- [CRUD Operations](../usage/crud-operations.md) - Working with entity data
- [Querying Data](../usage/querying-data.md) - Querying test data

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

var context = XrmFakedContextFactory.New();

// Initialize with test data
context.Initialize(new[]
{
    new Entity("account") 
    { 
        Id = Guid.NewGuid(), 
        ["name"] = "Test Account" 
    },
    new Entity("contact") 
    { 
        Id = Guid.NewGuid(), 
        ["firstname"] = "John" 
    }
});

// Query the data
var accounts = context.CreateQuery("account").ToList();
```

## Coming Soon

This guide will cover:
- Initializing test data efficiently
- Managing relationships in test data
- Data builders and factories
- Complex data scenarios
- Best practices for test data management

For now, refer to:
- [XrmFakedContext documentation](./xrm-faked-context.md#data-management) for initialization
- [CRUD Operations guide](../usage/crud-operations.md) for working with data
