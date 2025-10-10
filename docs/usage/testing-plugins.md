# Testing Plugins

Plugin testing is one of the primary use cases for Fake4Dataverse. This guide shows you how to test your Dataverse/Dynamics 365 plugins effectively.

## Table of Contents
- [Quick Start](#quick-start)
- [Plugin Execution Context](#plugin-execution-context)
- [Testing Different Plugin Stages](#testing-different-plugin-stages)
- [Testing Plugin Messages](#testing-plugin-messages)
- [Testing with Related Data](#testing-with-related-data)
- [Testing Plugin Images](#testing-plugin-images)
- [Testing Plugin Steps](#testing-plugin-steps)
- [Async Plugins](#async-plugins)
- [Plugin Pipeline Simulator](#plugin-pipeline-simulator) **NEW**
- [Best Practices](#best-practices)

## Quick Start

### Basic Plugin Test

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Plugins;
using Microsoft.Xrm.Sdk;
using Xunit;

public class MyPluginTests
{
    [Fact]
    public void Should_SetDefaultValue_When_AccountCreated()
    {
        // Arrange
        var context = XrmFakedContextFactory.New();
        var service = context.GetOrganizationService();
        
        var account = new Entity("account")
        {
            ["name"] = "Test Account"
        };
        
        // Act - Execute plugin
        context.ExecutePluginWith<MyPlugin>(
            pluginContext => 
            {
                pluginContext.MessageName = "Create";
                pluginContext.Stage = 20; // Pre-operation
            },
            account
        );
        
        // Assert - Verify plugin behavior
        Assert.NotNull(account["accountnumber"]);
        Assert.Equal("AUTO-001", account["accountnumber"]);
    }
}
```

### Sample Plugin

Here's a simple plugin to test:

```csharp
using Microsoft.Xrm.Sdk;
using System;

public class MyPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Get plugin execution context
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        // Get organization service
        var serviceFactory = (IOrganizationServiceFactory)serviceProvider
            .GetService(typeof(IOrganizationServiceFactory));
        var service = serviceFactory.CreateOrganizationService(context.UserId);
        
        // Get target entity
        if (context.InputParameters.Contains("Target") &&
            context.InputParameters["Target"] is Entity target)
        {
            if (target.LogicalName == "account" && 
                context.MessageName == "Create")
            {
                // Set default account number
                target["accountnumber"] = "AUTO-001";
            }
        }
    }
}
```

## Plugin Execution Context

The plugin execution context provides information about the operation that triggered the plugin.

### Context Properties

```csharp
[Fact]
public void Should_HaveCorrectContext_When_PluginExecutes()
{
    var context = XrmFakedContextFactory.New();
    
    var accountId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    context.ExecutePluginWith<TestContextPlugin>(
        pluginContext =>
        {
            // Message being processed
            pluginContext.MessageName = "Create";
            
            // Pipeline stage (10=PreValidation, 20=PreOperation, 40=PostOperation)
            pluginContext.Stage = 20;
            
            // Event mode (0=Synchronous, 1=Asynchronous)
            pluginContext.Mode = 0;
            
            // Organization name
            pluginContext.OrganizationName = "TestOrg";
            
            // Depth (prevent infinite loops)
            pluginContext.Depth = 1;
            
            // Correlation ID
            pluginContext.CorrelationId = Guid.NewGuid();
            
            // User ID
            pluginContext.UserId = userId;
            pluginContext.InitiatingUserId = userId;
        },
        new Entity("account") { Id = accountId }
    );
}
```

### Accessing Context in Plugin

```csharp
public class TestContextPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        // Access context properties
        string message = context.MessageName;
        int stage = context.Stage;
        Guid userId = context.UserId;
        Guid initiatingUserId = context.InitiatingUserId;
        int depth = context.Depth;
        
        // Access input parameters
        if (context.InputParameters.Contains("Target"))
        {
            var target = (Entity)context.InputParameters["Target"];
            // Process target
        }
    }
}
```

## Testing Different Plugin Stages

Plugins can execute at different stages in the pipeline.

### Pre-Validation (Stage 10)

Runs before database transaction, outside main transaction.

```csharp
[Fact]
public void Should_ValidateData_In_PreValidation()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "" // Invalid - empty name
    };
    
    // Test validation plugin
    var ex = Assert.Throws<InvalidPluginExecutionException>(() =>
        context.ExecutePluginWith<ValidationPlugin>(
            pluginContext =>
            {
                pluginContext.MessageName = "Create";
                pluginContext.Stage = 10; // PreValidation
            },
            account
        )
    );
    
    Assert.Contains("Name is required", ex.Message);
}
```

### Pre-Operation (Stage 20)

Runs before database transaction, inside main transaction.

```csharp
[Fact]
public void Should_ModifyData_In_PreOperation()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "test account" // lowercase
    };
    
    context.ExecutePluginWith<CapitalizationPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 20; // PreOperation
        },
        account
    );
    
    // Plugin should capitalize the name
    Assert.Equal("Test Account", account["name"]);
}
```

### Post-Operation (Stage 40)

Runs after database transaction.

```csharp
[Fact]
public void Should_CreateRelatedRecords_In_PostOperation()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var account = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    };
    
    context.Initialize(account);
    
    context.ExecutePluginWith<CreateContactPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 40; // PostOperation
        },
        account
    );
    
    // Plugin should create a related contact
    var contacts = context.CreateQuery("contact")
        .Where(c => ((EntityReference)c["parentcustomerid"]).Id == accountId)
        .ToList();
    
    Assert.Single(contacts);
}
```

## Testing Plugin Messages

Different messages provide different context.

### Create Message

```csharp
[Fact]
public void Should_HandleCreate_Message()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "New Account"
    };
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 20;
            // Target is available in InputParameters
        },
        account
    );
}
```

### Update Message

```csharp
[Fact]
public void Should_HandleUpdate_Message()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    // Initialize with existing account
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Old Name",
        ["revenue"] = new Money(1000)
    });
    
    // Update entity
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["name"] = "New Name"
    };
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 20;
        },
        accountUpdate
    );
}
```

### Delete Message

```csharp
[Fact]
public void Should_HandleDelete_Message()
{
    var context = XrmFakedContextFactory.New();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Account to Delete"
    });
    
    var entityRef = new EntityReference("account", accountId);
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Delete";
            pluginContext.Stage = 20;
        },
        entityRef
    );
}
```

## Testing with Related Data

### Testing Lookups

```csharp
[Fact]
public void Should_ProcessRelatedAccount_When_ContactCreated()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    // Initialize with parent account
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Parent Account",
        ["revenue"] = new Money(1000000)
    });
    
    var contact = new Entity("contact")
    {
        ["firstname"] = "John",
        ["lastname"] = "Doe",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };
    
    context.ExecutePluginWith<ContactPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 20;
        },
        contact
    );
    
    // Plugin can access related account via service
}
```

## Testing Plugin Images

Pre-images and post-images provide snapshots of entity data.

### Pre-Image (Before Operation)

```csharp
[Fact]
public void Should_AccessPreImage_In_Update()
{
    var context = XrmFakedContextFactory.New();
    
    var accountId = Guid.NewGuid();
    
    // Initialize with original data
    var originalAccount = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Original Name",
        ["revenue"] = new Money(1000)
    };
    
    context.Initialize(originalAccount);
    
    // Update
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Updated Name"
    };
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 20;
            
            // Add pre-image
            pluginContext.PreEntityImages.Add("PreImage", originalAccount);
        },
        accountUpdate
    );
}
```

### Using Images in Plugin

```csharp
public class AuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        if (context.MessageName == "Update")
        {
            var target = (Entity)context.InputParameters["Target"];
            
            // Access pre-image (before update)
            if (context.PreEntityImages.Contains("PreImage"))
            {
                var preImage = context.PreEntityImages["PreImage"];
                
                // Compare old vs new values
                if (target.Contains("name"))
                {
                    string oldName = preImage.GetAttributeValue<string>("name");
                    string newName = target.GetAttributeValue<string>("name");
                    
                    // Log the change
                }
            }
        }
    }
}
```

## Testing Plugin Steps

Plugins can be registered on different steps.

### Multiple Steps

```csharp
[Fact]
public void Should_Execute_PreAndPost_Steps()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "Test Account"
    };
    
    // Execute pre-operation step
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 20;
        },
        account
    );
    
    // Simulate actual create
    context.Initialize(account);
    
    // Execute post-operation step
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 40;
        },
        account
    );
}
```

## Async Plugins

Asynchronous plugins in Fake4Dataverse run synchronously (since there's no real async infrastructure).

```csharp
[Fact]
public void Should_ExecuteAsync_Plugin_Synchronously()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "Test Account"
    };
    
    context.ExecutePluginWith<MyAsyncPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 40;
            pluginContext.Mode = 1; // Asynchronous mode
        },
        account
    );
    
    // Plugin executes immediately in tests
    // (not queued like in real Dataverse)
}
```

## Plugin Pipeline Simulator

**New in v4.x (2025-10-10)**: Fake4Dataverse now includes comprehensive plugin pipeline simulation with support for multiple plugins per message/entity/stage combination.

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework

### Multiple Plugins Per Message

You can now register multiple plugins for the same message, entity, and stage. Plugins execute in order based on their `ExecutionOrder` (rank) property.

```csharp
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;

[Fact]
public void Should_Execute_MultiplePlugins_InOrder()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register Plugin 1 (executes first)
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 1,  // Lower numbers execute first
        PluginType = typeof(ValidationPlugin)
    });
    
    // Register Plugin 2 (executes second)
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 2,
        PluginType = typeof(EnrichmentPlugin)
    });
    
    // Register Plugin 3 (executes third)
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 3,
        PluginType = typeof(AuditPlugin)
    });
    
    var account = new Entity("account") { ["name"] = "Test" };
    
    // Act - Execute pipeline stage
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Preoperation,
        account);
    
    // Assert - All plugins executed in order
    // Each plugin can modify the entity for the next plugin
}
```

### Filtering Attributes (Update Message)

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in#filtering-attributes

Plugins on Update messages can specify filtering attributes. The plugin only executes if one of the specified attributes was modified.

```csharp
[Fact]
public void Should_OnlyExecute_WhenFilteredAttributeModified()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Plugin only executes when 'name' or 'revenue' changes
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Update",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(MyPlugin),
        FilteringAttributes = new HashSet<string> { "name", "revenue" }
    });
    
    var account = new Entity("account") { Id = Guid.NewGuid() };
    
    // Act 1 - Update 'name' (plugin executes)
    var modifiedAttributes = new HashSet<string> { "name" };
    simulator.ExecutePipelineStage(
        "Update", "account", ProcessingStepStage.Preoperation,
        account, modifiedAttributes);
    
    // Act 2 - Update 'telephone' only (plugin does NOT execute)
    modifiedAttributes = new HashSet<string> { "telephone" };
    simulator.ExecutePipelineStage(
        "Update", "account", ProcessingStepStage.Preoperation,
        account, modifiedAttributes);
}
```

### Plugin Configuration

Plugins can receive configuration parameters (secure and unsecure).

```csharp
simulator.RegisterPluginStep(new PluginStepRegistration
{
    MessageName = "Create",
    PrimaryEntityName = "account",
    Stage = ProcessingStepStage.Preoperation,
    PluginType = typeof(ConfigurablePlugin),
    UnsecureConfiguration = "Setting1=Value1",
    SecureConfiguration = "ApiKey=SecretKey123"
});
```

The plugin constructor receives these parameters:

```csharp
public class ConfigurablePlugin : IPlugin
{
    private readonly string _unsecureConfig;
    private readonly string _secureConfig;
    
    public ConfigurablePlugin(string unsecureConfig, string secureConfig)
    {
        _unsecureConfig = unsecureConfig;
        _secureConfig = secureConfig;
    }
    
    public void Execute(IServiceProvider serviceProvider)
    {
        // Use configuration
    }
}
```

### Complete Pipeline Simulation

Execute plugins through all pipeline stages:

```csharp
[Fact]
public void Should_Execute_CompletePluginPipeline()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register plugins for different stages
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Prevalidation,
        ExecutionOrder = 1,
        PluginType = typeof(PreValidationPlugin)
    });
    
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 1,
        PluginType = typeof(PreOperationPlugin)
    });
    
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        ExecutionOrder = 1,
        PluginType = typeof(PostOperationPlugin)
    });
    
    var account = new Entity("account") { ["name"] = "Test" };
    
    // Act - Execute each pipeline stage
    simulator.ExecutePipelineStage("Create", "account", 
        ProcessingStepStage.Prevalidation, account);
    
    // Simulate main operation (Create)
    account.Id = Guid.NewGuid();
    context.Initialize(account);
    
    simulator.ExecutePipelineStage("Create", "account", 
        ProcessingStepStage.Preoperation, account);
    
    simulator.ExecutePipelineStage("Create", "account", 
        ProcessingStepStage.Postoperation, account);
}
```

### Depth Tracking

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-recursive-loops

The pipeline simulator tracks execution depth to prevent infinite loops. The default maximum depth is 8 (matching Dataverse).

```csharp
[Fact]
public void Should_PreventInfiniteLoops()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Set max depth for testing
    simulator.MaxDepth = 3;
    
    var account = new Entity("account") { ["name"] = "Test" };
    
    // This will throw if depth exceeds 3
    var exception = Assert.Throws<InvalidPluginExecutionException>(() =>
    {
        simulator.ExecutePipelineStage(
            "Create", "account", ProcessingStepStage.Preoperation,
            account, currentDepth: 4);
    });
    
    Assert.Contains("Maximum plugin execution depth", exception.Message);
}
```

### Unregistering Plugins

```csharp
[Fact]
public void Should_UnregisterPlugin()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    var registration = new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(MyPlugin)
    };
    
    simulator.RegisterPluginStep(registration);
    
    // Remove the registration
    simulator.UnregisterPluginStep(registration);
    
    // Or clear all registrations
    simulator.ClearAllPluginSteps();
}
```

### Migration Notes

**From FakeXrmEasy v2.x**: The plugin pipeline simulation in Fake4Dataverse may have different setup compared to commercial FakeXrmEasy v2+:

1. **Explicit Registration**: Plugins must be explicitly registered via `PluginPipelineSimulator.RegisterPluginStep()` 
2. **Direct Control**: You have full control over when pipeline stages execute
3. **Configuration**: Plugin configuration is passed directly in the registration

## Best Practices

### ✅ Do

1. **Test plugin logic, not the platform**
   ```csharp
   // ✅ Good - tests your business logic
   [Fact]
   public void Should_SetCreditLimit_Based_OnRevenue()
   {
       // Test the credit limit calculation
   }
   ```

2. **Initialize with necessary data**
   ```csharp
   // ✅ Good - provides all data plugin needs
   context.Initialize(new[] { account, owner, priceList });
   ```

3. **Test error handling**
   ```csharp
   // ✅ Good - tests validation
   [Fact]
   public void Should_Throw_When_NameIsMissing()
   {
       var ex = Assert.Throws<InvalidPluginExecutionException>(...);
   }
   ```

4. **Use descriptive test names**
   ```csharp
   // ✅ Good
   [Fact]
   public void Should_CreateDefaultContact_When_AccountIsCreated()
   ```

### ❌ Don't

1. **Don't test Dataverse platform behavior**
   ```csharp
   // ❌ Bad - tests Dataverse, not your plugin
   [Fact]
   public void Should_CreateRecord_When_ServiceCreateIsCalled()
   ```

2. **Don't rely on real GUIDs**
   ```csharp
   // ❌ Bad - brittle test
   var accountId = new Guid("12345678-1234-1234-1234-123456789012");
   
   // ✅ Good - generate IDs
   var accountId = Guid.NewGuid();
   ```

3. **Don't test platform validation**
   ```csharp
   // ❌ Bad - platform concern
   [Fact]
   public void Should_Reject_DuplicateAccountNumber()
   ```

### Pattern: Plugin Test Base Class

```csharp
public abstract class PluginTestBase
{
    protected IXrmFakedContext Context { get; }
    protected IOrganizationService Service { get; }
    
    protected PluginTestBase()
    {
        Context = XrmFakedContextFactory.New();
        Service = Context.GetOrganizationService();
    }
    
    protected void ExecutePlugin<T>(
        string message,
        int stage,
        Entity target) where T : IPlugin, new()
    {
        Context.ExecutePluginWith<T>(
            ctx =>
            {
                ctx.MessageName = message;
                ctx.Stage = stage;
            },
            target
        );
    }
}

// Usage
public class MyPluginTests : PluginTestBase
{
    [Fact]
    public void Should_DoSomething()
    {
        var account = new Entity("account") { ["name"] = "Test" };
        ExecutePlugin<MyPlugin>("Create", 20, account);
        Assert.NotNull(account["accountnumber"]);
    }
}
```

## Troubleshooting

### Plugin Not Executing

**Problem**: Plugin code doesn't seem to run.

**Solution**: Ensure you've installed `Fake4Dataverse.Plugins`:
```bash
dotnet add package Fake4Dataverse.Plugins
```

### Null Reference in Plugin

**Problem**: Plugin fails with NullReferenceException.

**Solution**: Initialize all data the plugin needs:
```csharp
context.Initialize(new[] { mainEntity, relatedEntity1, relatedEntity2 });
```

### Service Provider Returns Null

**Problem**: `serviceProvider.GetService()` returns null.

**Solution**: This shouldn't happen in Fake4Dataverse. If it does, report a bug.

## Next Steps

- [CRUD Operations](./crud-operations.md) - Test CRUD operations
- [Querying Data](./querying-data.md) - Test queries in plugins
- [Security & Permissions](./security-permissions.md) - Test security
- [Batch Operations](./batch-operations.md) - Test ExecuteMultiple

## Real-World Examples

Check the test suite for complete examples:
- [Plugin Tests](https://github.com/rnwood/Fake4Dataverse/tree/main/Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline)
