# Creating Custom Message Executors

Fake4Dataverse is extensible - you can create custom message executors for messages that aren't yet implemented or for your own custom messages and Custom APIs.

## Table of Contents

- [Overview](#overview)
- [IFakeMessageExecutor Interface](#ifakemessageexecutor-interface)
- [Creating a Custom Executor](#creating-a-custom-executor)
- [Registering Custom Executors](#registering-custom-executors)
- [Testing Custom Executors](#testing-custom-executors)
- [Best Practices](#best-practices)
- [Complete Examples](#complete-examples)
- [Contributing Back](#contributing-back)
- [See Also](#see-also)

## Overview

Custom message executors allow you to:
- Implement missing Dataverse messages
- Create executors for Custom APIs
- Add custom business logic for testing
- Extend the framework without modifying core code

**Reference:** [Custom API](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api) - Microsoft documentation on Custom APIs, which define custom messages that can be executed through the Organization Service.

## IFakeMessageExecutor Interface

All message executors must implement `IFakeMessageExecutor`:

```csharp
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;

public interface IFakeMessageExecutor
{
    /// <summary>
    /// Determines if this executor can handle the request
    /// </summary>
    bool CanExecute(OrganizationRequest request);
    
    /// <summary>
    /// Executes the request and returns a response
    /// </summary>
    OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx);
    
    /// <summary>
    /// Returns the request type this executor handles
    /// </summary>
    Type GetResponsibleRequestType();
}
```

## Creating a Custom Executor

### Basic Executor Structure

```csharp
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;

public class MyCustomMessageExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request is MyCustomRequest;
    }
    
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        var customRequest = (MyCustomRequest)request;
        var service = ctx.GetOrganizationService();
        
        // Implement your logic here
        // Access context data, perform operations, etc.
        
        return new MyCustomResponse
        {
            ["Result"] = "Success",
            ["OutputValue"] = ComputeResult(customRequest)
        };
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(MyCustomRequest);
    }
    
    private object ComputeResult(MyCustomRequest request)
    {
        // Your business logic
        return null;
    }
}
```

### Executor for Custom API

**Reference:** [Custom API Example](../usage/custom-api.md) - Complete guide to implementing Custom APIs, showing how to define custom messages with parameters and execute them in Dataverse.

```csharp
/// <summary>
/// Executor for a custom API that calculates discount
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
/// Custom APIs allow defining custom business logic with strongly-typed parameters
/// </summary>
public class CalculateDiscountExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request.RequestName == "sample_CalculateDiscount";
    }
    
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        // Get input parameters from Custom API
        var originalAmount = (decimal)request.Parameters["OriginalAmount"];
        var discountPercent = (decimal)request.Parameters["DiscountPercentage"];
        
        // Calculate
        var discountedAmount = originalAmount - (originalAmount * discountPercent / 100);
        
        // Return response with output parameters
        var response = new OrganizationResponse
        {
            ResponseName = "sample_CalculateDiscount",
            Results = new ParameterCollection
            {
                ["DiscountedAmount"] = discountedAmount
            }
        };
        
        return response;
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(OrganizationRequest);
    }
}
```

### Executor with Data Access

```csharp
/// <summary>
/// Executor that retrieves and processes entity data
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// Shows how to access the organization service within a custom executor
/// </summary>
public class GetAccountSummaryExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request.RequestName == "custom_GetAccountSummary";
    }
    
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        var accountId = (Guid)request.Parameters["AccountId"];
        var service = ctx.GetOrganizationService();
        
        // Retrieve account
        var account = service.Retrieve(
            "account", 
            accountId, 
            new ColumnSet("name", "revenue", "numberofemployees"));
        
        // Calculate summary
        var summary = new
        {
            Name = account.GetAttributeValue<string>("name"),
            Revenue = account.GetAttributeValue<Money>("revenue")?.Value ?? 0,
            Employees = account.GetAttributeValue<int>("numberofemployees"),
            RevenuePerEmployee = CalculateRevenuePerEmployee(account)
        };
        
        return new OrganizationResponse
        {
            ResponseName = "custom_GetAccountSummary",
            Results = new ParameterCollection
            {
                ["Summary"] = Newtonsoft.Json.JsonConvert.SerializeObject(summary)
            }
        };
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(OrganizationRequest);
    }
    
    private decimal CalculateRevenuePerEmployee(Entity account)
    {
        var revenue = account.GetAttributeValue<Money>("revenue")?.Value ?? 0;
        var employees = account.GetAttributeValue<int>("numberofemployees");
        
        return employees > 0 ? revenue / employees : 0;
    }
}
```

## Registering Custom Executors

### Using Middleware Builder

```csharp
using Fake4Dataverse.Middleware;

var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors() // Add default executors
    // Register custom executor
    .Use(next => (ctx, request) =>
    {
        var executor = new MyCustomMessageExecutor();
        if (executor.CanExecute(request))
        {
            return executor.Execute(request, ctx);
        }
        return next(ctx, request);
    })
    .UseCrud()
    .UseMessages()
    .Build();
```

### Multiple Custom Executors

```csharp
public static class CustomExecutorExtensions
{
    public static MiddlewareBuilder AddCustomExecutors(this MiddlewareBuilder builder)
    {
        return builder
            .Use(next => (ctx, request) =>
            {
                var executors = new IFakeMessageExecutor[]
                {
                    new CalculateDiscountExecutor(),
                    new GetAccountSummaryExecutor(),
                    new MyOtherCustomExecutor()
                };
                
                foreach (var executor in executors)
                {
                    if (executor.CanExecute(request))
                    {
                        return executor.Execute(request, ctx);
                    }
                }
                
                return next(ctx, request);
            });
    }
}

// Usage
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .AddCustomExecutors() // Add all custom executors
    .UseCrud()
    .UseMessages()
    .Build();
```

## Testing Custom Executors

### Unit Testing an Executor

```csharp
[Fact]
public void Should_Execute_Custom_Message()
{
    // Arrange
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .Use(next => (ctx, request) =>
        {
            var executor = new CalculateDiscountExecutor();
            if (executor.CanExecute(request))
            {
                return executor.Execute(request, ctx);
            }
            return next(ctx, request);
        })
        .UseCrud()
        .Build();
    
    var service = context.GetOrganizationService();
    
    // Act
    var request = new OrganizationRequest("sample_CalculateDiscount")
    {
        Parameters = new ParameterCollection
        {
            ["OriginalAmount"] = 100.0m,
            ["DiscountPercentage"] = 10.0m
        }
    };
    
    var response = service.Execute(request);
    
    // Assert
    Assert.Equal(90.0m, response.Results["DiscountedAmount"]);
}
```

### Integration Testing

```csharp
[Fact]
public void Should_Integrate_With_Other_Operations()
{
    var context = MiddlewareBuilder
        .New()
        .AddCrud()
        .AddFakeMessageExecutors()
        .AddCustomExecutors()
        .UseCrud()
        .UseMessages()
        .Build();
    
    var service = context.GetOrganizationService();
    
    // Create test data
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Test Corp",
        ["revenue"] = new Money(1000000),
        ["numberofemployees"] = 50
    });
    
    // Execute custom message
    var request = new OrganizationRequest("custom_GetAccountSummary")
    {
        Parameters = new ParameterCollection
        {
            ["AccountId"] = accountId
        }
    };
    
    var response = service.Execute(request);
    
    // Verify results
    Assert.NotNull(response.Results["Summary"]);
}
```

## Best Practices

### ✅ Do

- **Follow naming conventions**: Name executors like `[MessageName]Executor`
- **Document behavior**: Include references to Microsoft documentation
- **Validate inputs**: Check for required parameters and throw meaningful exceptions
- **Use the context**: Access data through `ctx.GetOrganizationService()`
- **Handle errors gracefully**: Throw appropriate `InvalidPluginExecutionException` or `FaultException`
- **Write tests**: Test both success and error scenarios

```csharp
public class WellDocumentedExecutor : IFakeMessageExecutor
{
    /// <summary>
    /// Executes the custom_ValidateEntity message
    /// Reference: https://learn.microsoft.com/... (if applicable)
    /// Validates entity data against business rules
    /// </summary>
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        // Validate inputs
        if (!request.Parameters.Contains("EntityId"))
        {
            throw new InvalidPluginExecutionException(
                "EntityId parameter is required");
        }
        
        var entityId = (Guid)request.Parameters["EntityId"];
        var service = ctx.GetOrganizationService();
        
        try
        {
            // Business logic
            var entity = service.Retrieve(
                "account", 
                entityId, 
                new ColumnSet(true));
            
            var isValid = ValidateEntity(entity);
            
            return new OrganizationResponse
            {
                ResponseName = "custom_ValidateEntity",
                Results = new ParameterCollection
                {
                    ["IsValid"] = isValid
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException(
                $"Error validating entity: {ex.Message}", ex);
        }
    }
    
    private bool ValidateEntity(Entity entity)
    {
        // Validation logic
        return true;
    }
    
    public bool CanExecute(OrganizationRequest request)
    {
        return request.RequestName == "custom_ValidateEntity";
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(OrganizationRequest);
    }
}
```

### ❌ Don't

- **Don't modify the context directly**: Use the organization service
- **Don't ignore errors**: Handle and wrap exceptions appropriately
- **Don't skip validation**: Always validate inputs
- **Don't hard-code values**: Use parameters from the request
- **Don't forget to test**: Custom executors need comprehensive tests

## Complete Examples

### Full Custom API Implementation

```csharp
// Custom API executor with full validation and error handling
public class AdvancedCalculationExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request.RequestName == "sample_AdvancedCalculation";
    }
    
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        try
        {
            // Validate required parameters
            ValidateParameters(request);
            
            // Extract parameters
            var entityId = (Guid)request.Parameters["EntityId"];
            var calculationType = (string)request.Parameters["CalculationType"];
            
            // Get service
            var service = ctx.GetOrganizationService();
            
            // Retrieve entity
            var entity = service.Retrieve(
                "account", 
                entityId, 
                new ColumnSet("revenue", "numberofemployees"));
            
            // Perform calculation based on type
            var result = PerformCalculation(entity, calculationType);
            
            // Return response
            return new OrganizationResponse
            {
                ResponseName = "sample_AdvancedCalculation",
                Results = new ParameterCollection
                {
                    ["Result"] = result,
                    ["CalculatedAt"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException(
                $"Error in AdvancedCalculation: {ex.Message}", ex);
        }
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(OrganizationRequest);
    }
    
    private void ValidateParameters(OrganizationRequest request)
    {
        if (!request.Parameters.Contains("EntityId"))
            throw new ArgumentException("EntityId is required");
            
        if (!request.Parameters.Contains("CalculationType"))
            throw new ArgumentException("CalculationType is required");
    }
    
    private decimal PerformCalculation(Entity entity, string type)
    {
        switch (type.ToLower())
        {
            case "revenueperemployee":
                var revenue = entity.GetAttributeValue<Money>("revenue")?.Value ?? 0;
                var employees = entity.GetAttributeValue<int>("numberofemployees");
                return employees > 0 ? revenue / employees : 0;
                
            case "grossprofitmargin":
                // Implementation for other calculation types
                return 0;
                
            default:
                throw new ArgumentException($"Unknown calculation type: {type}");
        }
    }
}
```

## Contributing Back

If you create a useful message executor for a standard Dataverse message, consider contributing it back to Fake4Dataverse!

1. **Fork the repository**
2. **Create a well-tested executor**
3. **Add documentation** with Microsoft documentation references
4. **Submit a pull request**

See the [Contributing Guide](../../README.md#contributing) for details.

## See Also

- [Middleware Architecture](../concepts/middleware.md) - Understanding the pipeline
- [Message Executors Overview](../messages/README.md) - Built-in executors
- [Custom API Usage](../usage/custom-api.md) - Using Custom APIs
- [Source Code Examples](https://github.com/rnwood/Fake4Dataverse/tree/main/Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors) - Review existing executors
