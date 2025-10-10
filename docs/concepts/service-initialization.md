# Service Initialization

> **📝 Note**: This documentation is currently under development. For now, see related documentation below.

## Related Documentation

- [XrmFakedContext](./xrm-faked-context.md) - Learn about getting organization services from the context
- [Basic Concepts](../getting-started/basic-concepts.md) - Understanding IOrganizationService
- [Testing Plugins](../usage/testing-plugins.md) - Service usage in plugin tests

## Quick Example

```csharp
using Fake4Dataverse.Middleware;

// Create context and get service
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Use the service
var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
```

## Coming Soon

This guide will cover:
- Creating organization services
- Setting caller properties
- Multiple service instances
- Service configuration patterns
- Advanced service scenarios

For now, refer to [XrmFakedContext documentation](./xrm-faked-context.md#service-initialization) for service initialization patterns.
