# Custom Message Executors

> **📝 Note**: This documentation is currently under development.

## Overview

You can extend Fake4Dataverse by creating custom message executors for messages that aren't yet implemented or for your own custom messages.

## IFakeMessageExecutor Interface

Custom message executors must implement `IFakeMessageExecutor`:

```csharp
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;

public interface IFakeMessageExecutor
{
    bool CanExecute(OrganizationRequest request);
    OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx);
    Type GetResponsibleRequestType();
}
```

## Creating a Custom Executor

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
        
        return new MyCustomResponse
        {
            ["Result"] = "Success"
        };
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(MyCustomRequest);
    }
}
```

## Registering Custom Executors

```csharp
using Fake4Dataverse.Middleware;

var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors() // Adds default executors
    // Add your custom executor
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

## Best Practices

1. **Follow naming conventions** - Name executors like `[Message]Executor`
2. **Document behavior** - Include references to Microsoft docs
3. **Handle errors** - Throw appropriate exceptions
4. **Test thoroughly** - Write tests for your custom executor

## Coming Soon

Complete guide covering:
- Detailed executor implementation patterns
- Testing custom executors
- Contributing executors back to the project
- Advanced scenarios

## See Also

- [Middleware Architecture](../concepts/middleware.md) - Understanding the pipeline
- [Message Executors Overview](../messages/README.md) - Built-in executors

## Reference

Review existing executors in the [source code](https://github.com/rnwood/Fake4Dataverse/tree/main/Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors) for examples.
