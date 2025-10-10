# Testing Workflows

> **📝 Note**: This documentation is currently under development.

## Overview

Custom workflow activities can be tested using Fake4Dataverse, similar to how plugins are tested.

## Related Documentation

- [Testing Plugins](./testing-plugins.md) - Similar patterns apply to workflow activities
- [Basic Concepts](../getting-started/basic-concepts.md) - Understanding the framework

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Xunit;

[Fact]
public void Should_Execute_Custom_Workflow_Activity()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Initialize test data
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    });
    
    // Act - Execute workflow activity
    // Note: Specific workflow execution API depends on your implementation
    
    // Assert - Verify results
}
```

## Coming Soon

This guide will cover:
- Setting up workflow activity tests
- Providing input parameters
- Retrieving output parameters
- Testing workflow context
- Best practices for workflow testing

For now, refer to [Testing Plugins](./testing-plugins.md) as many patterns are similar.
