# Testing Plugins

Plugin testing is one of the primary use cases for Fake4Dataverse. This guide shows you how to test your Dataverse/Dynamics 365 plugins effectively.

## Table of Contents
- [Quick Start](#quick-start)
- [Plugin Execution Context](#plugin-execution-context)
- [Testing Different Plugin Stages](#testing-different-plugin-stages)
- [Testing Plugin Messages](#testing-plugin-messages)
- [Testing with Related Data](#testing-with-related-data)
- [Testing Plugin Images](#testing-plugin-images)
  - [Automatic Image Registration (Recommended)](#automatic-image-registration-recommended)
  - [SPKL Image Attribute Auto-Discovery](#spkl-image-attribute-auto-discovery)
  - [Filtered Attributes in Images](#filtered-attributes-in-images)
  - [Multiple Named Images](#multiple-named-images)
  - [Manual Image Creation (Legacy)](#manual-image-creation-legacy)
  - [Message-Specific Image Availability](#message-specific-image-availability)
- [Testing Plugin Steps](#testing-plugin-steps)
- [Async Plugins](#async-plugins)
- [Plugin Pipeline Simulator](#plugin-pipeline-simulator)
- [Plugin Auto-Discovery from Assemblies](#plugin-auto-discovery-from-assemblies)
- [Custom Action and Custom API Plugin Support](#custom-action-and-custom-api-plugin-support)
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

Fake4Dataverse provides comprehensive pre/post image support with automatic image creation and SPKL attribute auto-discovery.

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities

Pre-images and post-images provide snapshots of entity data at different points in the pipeline:
- **Pre-images**: Entity state before the core operation (available for Update, Delete)
- **Post-images**: Entity state after the core operation (available for Create, Update)

### Automatic Image Registration (Recommended)

**New in v4.x**: Register images with plugin steps for automatic creation during pipeline execution.

```csharp
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;

[Fact]
public void Should_AutomaticallyCreateImages_WhenRegistered()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Initialize with original data
    var accountId = Guid.NewGuid();
    var originalAccount = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Original Name",
        ["revenue"] = new Money(100000)
    };
    context.Initialize(originalAccount);
    
    // Register plugin with pre and post images
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Update",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(AuditPlugin),
        PreImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "PreImage",
                EntityAlias = "PreImage",
                ImageType = ProcessingStepImageType.PreImage,
                Attributes = new HashSet<string> { "name", "revenue" } // Filtered attributes
            }
        },
        PostImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "PostImage",
                EntityAlias = "PostImage",
                ImageType = ProcessingStepImageType.PostImage,
                Attributes = new HashSet<string>() // Empty = all attributes
            }
        }
    });
    
    var service = context.GetOrganizationService();
    
    // Act - Update account (images automatically created)
    var accountUpdate = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Updated Name"
    };
    service.Update(accountUpdate);
    
    // Plugin receives images automatically populated
}
```

### SPKL Image Attribute Auto-Discovery

Use SPKL `CrmPluginRegistrationImage` attributes for automatic image registration.

```csharp
using SparkleXrm.Tasks;

[CrmPluginRegistration("Update", "account", StageEnum.PreOperation, 
    ExecutionModeEnum.Synchronous, "", "AuditPlugin", 1, IsolationModeEnum.Sandbox)]
[CrmPluginRegistrationImage(
    ImageTypeEnum.PreImage,           // PreImage, PostImage, or Both
    "PreImage",                       // Image name (key in EntityImageCollection)
    "name,revenue")]                  // Filtered attributes (comma-separated)
[CrmPluginRegistrationImage(
    ImageTypeEnum.PostImage,
    "PostImage",
    "")]                              // Empty = all attributes
public class AuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        // Images are automatically populated based on attributes
        var preImage = context.PreEntityImages["PreImage"];
        var postImage = context.PostEntityImages["PostImage"];
        
        // Pre-image contains only 'name' and 'revenue' (filtered)
        string oldName = preImage.GetAttributeValue<string>("name");
        Money oldRevenue = preImage.GetAttributeValue<Money>("revenue");
        
        // Post-image contains all attributes
        string newName = postImage.GetAttributeValue<string>("name");
    }
}
```

**Auto-discover and register:**
```csharp
[Fact]
public void Should_AutoDiscoverPlugins_WithImageAttributes()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Discover and register plugins with images
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(
        new[] { typeof(AuditPlugin).Assembly });
    
    // Images are automatically created during operations
}
```

### Filtered Attributes in Images

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#filter-attributes

Filtering attributes improves performance by including only necessary data in images:

```csharp
[Fact]
public void Should_CreateFilteredImages()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    var accountId = Guid.NewGuid();
    var originalAccount = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Original Name",
        ["revenue"] = new Money(100000),
        ["telephone1"] = "555-0100",
        ["emailaddress1"] = "test@example.com"
    };
    context.Initialize(originalAccount);
    
    // Register plugin with filtered pre-image (only name and revenue)
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Update",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(FilteredImagePlugin),
        PreImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "FilteredPreImage",
                ImageType = ProcessingStepImageType.PreImage,
                Attributes = new HashSet<string> { "name", "revenue" }
            }
        }
    });
    
    var service = context.GetOrganizationService();
    service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated" });
    
    // Plugin receives pre-image with ONLY 'name' and 'revenue'
    // telephone1 and emailaddress1 are NOT included
}
```

### Multiple Named Images

Register multiple images with different attribute filters:

```csharp
[Fact]
public void Should_CreateMultipleImages()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Contoso",
        ["revenue"] = new Money(1000000),
        ["telephone1"] = "555-0100"
    });
    
    // Register plugin with multiple pre-images
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Update",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(MultiImagePlugin),
        PreImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "NameImage",
                ImageType = ProcessingStepImageType.PreImage,
                Attributes = new HashSet<string> { "name" }
            },
            new PluginStepImageRegistration
            {
                Name = "FinancialImage",
                ImageType = ProcessingStepImageType.PreImage,
                Attributes = new HashSet<string> { "revenue" }
            }
        }
    });
}
```

### Manual Image Creation (Legacy)

For backward compatibility, you can still manually add images to the plugin context:

```csharp
[Fact]
public void Should_AccessPreImage_In_Update_Manual()
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
            
            // Manually add pre-image
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
            
            // Access post-image (after update)
            if (context.PostEntityImages.Contains("PostImage"))
            {
                var postImage = context.PostEntityImages["PostImage"];
                
                // Access updated entity state with all attributes
                string updatedName = postImage.GetAttributeValue<string>("name");
            }
        }
    }
}
```

### Message-Specific Image Availability

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images

Images are only available for specific message types:

| Message | Pre-Image | Post-Image |
|---------|-----------|------------|
| Create  | ❌ No     | ✅ Yes     |
| Update  | ✅ Yes    | ✅ Yes     |
| Delete  | ✅ Yes    | ❌ No      |

```csharp
[Fact]
public void Should_CreatePostImage_ForCreateMessage()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Post-image IS available for Create
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        PluginType = typeof(CreatePlugin),
        PostImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "PostImage",
                ImageType = ProcessingStepImageType.PostImage
            }
        }
    });
    
    // Pre-image is NOT created for Create message (entity doesn't exist yet)
}

[Fact]
public void Should_CreatePreImage_ForDeleteMessage()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Pre-image IS available for Delete
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Delete",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(DeletePlugin),
        PreImages = new List<PluginStepImageRegistration>
        {
            new PluginStepImageRegistration
            {
                Name = "PreImage",
                ImageType = ProcessingStepImageType.PreImage
            }
        }
    });
    
    // Post-image is NOT created for Delete message (entity no longer exists)
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

Fake4Dataverse includes comprehensive async plugin support with a simulated system job queue that mirrors Dataverse's asyncoperation entity.

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service

In Dataverse, asynchronous plugins (Mode = 1) are queued as asyncoperation records and execute after the main transaction completes. Fake4Dataverse simulates this behavior by:
- Queueing async plugins instead of executing them immediately
- Providing APIs to monitor, execute, and wait for async jobs
- Tracking async operation status (Ready, InProgress, Succeeded, Failed)
- Capturing plugin execution errors

### Basic Async Plugin Testing

Register an async plugin and control when it executes:

```csharp
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;

[Fact]
public void Should_QueueAsyncPlugin_ForLaterExecution()
{
    // Arrange - Register an async plugin
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous, // Async mode
        PluginType = typeof(MyAsyncPlugin)
    });
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test Account"
    };
    
    // Act - Execute pipeline stage (plugin is queued, not executed)
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        account);
    
    // Assert - Plugin is queued
    Assert.Equal(1, simulator.AsyncJobQueue.PendingCount);
    Assert.Equal(0, simulator.AsyncJobQueue.CompletedCount);
    
    // Execute queued async operations
    simulator.AsyncJobQueue.ExecuteAll();
    
    // Assert - Plugin has now executed
    Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
    Assert.Equal(1, simulator.AsyncJobQueue.CompletedCount);
}
```

### Monitoring Async Operations

The async job queue provides full visibility into queued and completed operations:

```csharp
[Fact]
public void Should_MonitorAsyncOperations()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register async plugin
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(MyAsyncPlugin)
    });
    
    // Queue operation
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
    
    // Get pending operations
    var pendingOps = simulator.AsyncJobQueue.GetPending();
    Assert.Single(pendingOps);
    
    var asyncOp = pendingOps[0];
    
    // Check initial state
    Assert.Equal(AsyncOperationState.Ready, asyncOp.StateCode);
    Assert.Equal(AsyncOperationStatus.WaitingForResources, asyncOp.StatusCode);
    Assert.Equal(AsyncOperationType.ExecutePlugin, asyncOp.OperationType);
    Assert.Equal("Create", asyncOp.MessageName);
    Assert.Equal("account", asyncOp.PrimaryEntityName);
    
    // Execute and check final state
    simulator.AsyncJobQueue.Execute(asyncOp.AsyncOperationId);
    
    Assert.Equal(AsyncOperationState.Completed, asyncOp.StateCode);
    Assert.Equal(AsyncOperationStatus.Succeeded, asyncOp.StatusCode);
    Assert.True(asyncOp.IsSuccessful);
    Assert.NotNull(asyncOp.StartedOn);
    Assert.NotNull(asyncOp.CompletedOn);
}
```

### Waiting for Async Operations

Use `WaitForAll()` or `WaitFor()` to wait for async operations to complete:

```csharp
[Fact]
public void Should_WaitForAsyncOperations_ToComplete()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register async plugin
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(MyAsyncPlugin)
    });
    
    // Queue multiple operations
    for (int i = 0; i < 3; i++)
    {
        simulator.ExecutePipelineStage(
            "Create",
            "account",
            ProcessingStepStage.Postoperation,
            new Entity("account") { Id = Guid.NewGuid(), ["name"] = $"Test {i}" });
    }
    
    // Wait for all to complete (with 30 second timeout)
    bool completed = simulator.AsyncJobQueue.WaitForAll(timeoutMilliseconds: 30000);
    
    Assert.True(completed);
    Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
    Assert.Equal(3, simulator.AsyncJobQueue.CompletedCount);
}
```

### Async/Await Support

The async job queue also supports async/await patterns:

```csharp
[Fact]
public async Task Should_ExecuteAsyncOperations_WithAwait()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register and queue async plugin
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(MyAsyncPlugin)
    });
    
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
    
    // Execute asynchronously
    int executedCount = await simulator.AsyncJobQueue.ExecuteAllAsync();
    
    Assert.Equal(1, executedCount);
    
    // Or wait asynchronously
    bool completed = await simulator.AsyncJobQueue.WaitForAllAsync(timeoutMilliseconds: 30000);
    Assert.True(completed);
}
```

### Handling Failed Async Operations

Failed async operations are captured with full error details:

```csharp
[Fact]
public void Should_CaptureAsyncPluginErrors()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Register a plugin that throws an exception
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(FailingPlugin)
    });
    
    // Queue and execute
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
    
    simulator.AsyncJobQueue.ExecuteAll();
    
    // Get failed operations
    var failedOps = simulator.AsyncJobQueue.GetFailed();
    Assert.Single(failedOps);
    
    var failedOp = failedOps[0];
    Assert.Equal(AsyncOperationState.Completed, failedOp.StateCode);
    Assert.Equal(AsyncOperationStatus.Failed, failedOp.StatusCode);
    Assert.NotNull(failedOp.ErrorMessage);
    Assert.NotNull(failedOp.Exception);
}
```

### Auto-Execute Mode

For simpler tests, enable auto-execute mode to run async plugins immediately:

```csharp
[Fact]
public void Should_AutoExecuteAsyncPlugins_WhenEnabled()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Enable auto-execute for async plugins
    simulator.AsyncJobQueue.AutoExecute = true;
    
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(MyAsyncPlugin)
    });
    
    // Plugin executes immediately when queued
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
    
    // Already completed
    Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
    Assert.Equal(1, simulator.AsyncJobQueue.CompletedCount);
}
```

### Legacy ExecutePluginWith for Async Plugins

The legacy `ExecutePluginWith` method still works but executes async plugins synchronously:

```csharp
[Fact]
public void Should_ExecuteAsync_Plugin_Synchronously_WithLegacyMethod()
{
    var context = XrmFakedContextFactory.New();
    
    var account = new Entity("account")
    {
        ["name"] = "Test Account"
    };
    
    // Using legacy method - executes synchronously even though Mode = 1
    context.ExecutePluginWith<MyAsyncPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 40;
            pluginContext.Mode = 1; // Asynchronous mode
        },
        account
    );
    
    // Plugin executes immediately in tests (not queued)
}
```

### AsyncOperation Entity Structure

Async operations can be converted to entities (simulating the asyncoperation entity in Dataverse):

```csharp
[Fact]
public void Should_ConvertAsyncOperation_ToEntity()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Queue async plugin
    simulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Postoperation,
        Mode = ProcessingStepMode.Asynchronous,
        PluginType = typeof(MyAsyncPlugin)
    });
    
    simulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Postoperation,
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
    
    var asyncOp = simulator.AsyncJobQueue.GetPending().First();
    
    // Convert to entity (mirrors Dataverse asyncoperation entity)
    Entity asyncOpEntity = asyncOp.ToEntity();
    
    Assert.Equal("asyncoperation", asyncOpEntity.LogicalName);
    Assert.Equal(asyncOp.AsyncOperationId, asyncOpEntity.Id);
    Assert.Equal((int)AsyncOperationType.ExecutePlugin, 
        asyncOpEntity.GetAttributeValue<OptionSetValue>("operationtype").Value);
    Assert.Equal((int)AsyncOperationState.Ready, 
        asyncOpEntity.GetAttributeValue<OptionSetValue>("statecode").Value);
}
```

### Cleanup

Clear completed operations to clean up test state:

```csharp
[Fact]
public void Should_CleanupCompletedOperations()
{
    var context = XrmFakedContextFactory.New();
    var simulator = context.PluginPipelineSimulator;
    
    // Queue and execute operations
    // ... (registration and execution code)
    
    simulator.AsyncJobQueue.ExecuteAll();
    
    // Clear completed operations
    int clearedCount = simulator.AsyncJobQueue.ClearCompleted();
    
    // Or clear all operations
    simulator.AsyncJobQueue.Clear();
}
```



## Plugin Pipeline Simulator

Fake4Dataverse includes comprehensive plugin pipeline simulation with support for multiple plugins per message/entity/stage combination.

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework

### Pipeline Features

The plugin pipeline implementation provides:

1. **Explicit Registration**: Plugins must be explicitly registered using `PluginPipelineSimulator.RegisterPluginStep()`. There is no automatic plugin discovery from assemblies (though you can use `DiscoverAndRegisterPlugins()` for SPKL-style auto-discovery).

2. **Manual Pipeline Control**: You have full control over when pipeline stages execute. By default, plugins do NOT execute during CRUD operations.

3. **Opt-In Auto-Execution**: Enable `context.UsePipelineSimulation = true` to automatically execute registered plugins during Create/Update/Delete operations.

4. **Direct Configuration**: Plugin configuration (secure/unsecure) is passed directly in the `PluginStepRegistration` object, not through external configuration files.

5. **Async Plugin Queuing**: Async plugins are queued in a simulated system job queue for on-demand execution. See [Async Plugins](#async-plugins) section for details.

### Auto-Registration Mode (Recommended)

Enable automatic plugin execution during CRUD operations by setting `UsePipelineSimulation = true`:

```csharp
[Fact]
public void Should_AutoExecutePlugins_During_CrudOperations()
{
    // Arrange - Enable auto-execution
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true; // Enable auto-execution
    
    // Register plugins once
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(AccountNumberPlugin)
    });
    
    var service = context.GetOrganizationService();
    
    // Act - Plugins automatically execute
    var account = new Entity("account") { ["name"] = "Test Account" };
    var id = service.Create(account); // AccountNumberPlugin executes automatically
    
    // Assert - Plugin effects are visible
    var created = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
    Assert.NotNull(created["accountnumber"]);
}
```

**When to use auto-registration:**
- Testing plugins as part of normal CRUD operations
- Integration testing where plugins should behave like production
- Testing plugin interactions and execution order
- Most realistic plugin testing scenarios

### Manual Pipeline Execution

For fine-grained control, manually execute specific pipeline stages:

```csharp
[Fact]
public void Should_ManuallyExecutePipelineStage()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    // Note: UsePipelineSimulation remains false (default)
    
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "Create",
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(ValidationPlugin)
    });
    
    var account = new Entity("account") { ["name"] = "Test" };
    
    // Act - Manually trigger specific pipeline stage
    context.PluginPipelineSimulator.ExecutePipelineStage(
        "Create",
        "account",
        ProcessingStepStage.Preoperation,
        account);
    
    // Plugin executed only when explicitly called
}
```

**When to use manual execution:**
- Unit testing individual plugin behavior in isolation
- Testing specific pipeline stages without full CRUD simulation
- When you need precise control over execution timing
- Testing plugin context properties

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

### Pipeline Configuration

The plugin pipeline provides flexible configuration options:

1. **Explicit Registration**: Register plugins explicitly via `PluginPipelineSimulator.RegisterPluginStep()` 
2. **Direct Control**: Full control over when pipeline stages execute
3. **Configuration**: Plugin configuration passed directly in the registration
4. **Auto-Execution**: Enable `UsePipelineSimulation = true` for automatic plugin execution during CRUD operations

## Plugin Auto-Discovery from Assemblies

Fake4Dataverse can automatically discover and register plugins from assemblies with support for SPKL and XrmTools.Meta attributes.

### Auto-Discovery with SPKL Attributes

Fake4Dataverse can automatically scan assemblies and register plugins decorated with SPKL `CrmPluginRegistrationAttribute` and `CrmPluginRegistrationImage` attributes (without requiring a reference to the SPKL package):

```csharp
[Fact]
public void Should_AutoDiscover_PluginsWithSPKLAttributes()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Act - Discover and register all plugins from assembly
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(
        new[] { typeof(MyPlugin).Assembly });
    
    Console.WriteLine($"Discovered and registered {count} plugin steps");
    
    // Plugins automatically execute during CRUD operations
    var service = context.GetOrganizationService();
    var account = new Entity("account") { ["name"] = "Test" };
    service.Create(account); // Registered plugins execute automatically
}
```

**SPKL Attribute Example:**
```csharp
using SparkleXrm.Tasks;

[CrmPluginRegistration(
    "Create",                              // Message
    "account",                             // Entity
    StageEnum.PreOperation,                // Stage
    ExecutionModeEnum.Synchronous,         // Mode
    "",                                    // FilteringAttributes (empty = all)
    "AccountCreatePlugin",                 // Name
    1,                                     // ExecutionOrder/Rank
    IsolationModeEnum.Sandbox)]
public class AccountCreatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Plugin logic
    }
}
```

**SPKL with Image Attributes Example:**
```csharp
using SparkleXrm.Tasks;

[CrmPluginRegistration(
    "Update",
    "account",
    StageEnum.PreOperation,
    ExecutionModeEnum.Synchronous,
    "name,revenue",                        // FilteringAttributes
    "AccountAuditPlugin",
    1,
    IsolationModeEnum.Sandbox)]
[CrmPluginRegistrationImage(
    ImageTypeEnum.PreImage,                // PreImage, PostImage, or Both
    "PreImage",                            // Image name
    "name,revenue,modifiedon")]            // Filtered attributes (comma-separated)
[CrmPluginRegistrationImage(
    ImageTypeEnum.PostImage,
    "PostImage",
    "")]                                   // Empty = all attributes
public class AccountAuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        // Images are automatically populated
        var preImage = context.PreEntityImages["PreImage"];
        var postImage = context.PostEntityImages["PostImage"];
    }
}
```

**How it works:**
- Scans assemblies for classes implementing `IPlugin`
- Uses reflection to read `CrmPluginRegistrationAttribute` properties (duck typing - no package reference required)
- Discovers `CrmPluginRegistrationImage` attributes and links them to parent plugin steps
- Automatically creates `PluginStepRegistration` objects with pre-populated `PreImages` and `PostImages` collections
- Registers all discovered steps in the pipeline simulator

### Auto-Discovery with XrmTools.Meta Attributes

Fake4Dataverse also supports XrmTools.Meta `StepAttribute` and `ImageAttribute` (without requiring a reference to the package):

```csharp
[Fact]
public void Should_AutoDiscover_PluginsWithXrmToolsMetaAttributes()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Act - Discover and register all plugins from assembly
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(
        new[] { typeof(MyPlugin).Assembly });
    
    Console.WriteLine($"Discovered and registered {count} plugin steps");
    
    // Plugins automatically execute during CRUD operations
    var service = context.GetOrganizationService();
    var account = new Entity("account") { ["name"] = "Test" };
    service.Create(account); // Registered plugins execute automatically
}
```

**XrmTools.Meta StepAttribute Example:**
```csharp
using XrmTools.Meta.Attributes;
using XrmTools.Meta.Model;

[Step(
    "account",                             // Entity name
    "Create",                              // Message
    "",                                    // FilteringAttributes (empty = all)
    Stages.PreOperation,                   // Stage
    ExecutionMode.Synchronous)]            // Mode
public class AccountCreatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Plugin logic
    }
}
```

**XrmTools.Meta with ImageAttribute Example:**
```csharp
using XrmTools.Meta.Attributes;
using XrmTools.Meta.Model;

[Step(
    "account",
    "Update",
    "name,revenue",                        // FilteringAttributes
    Stages.PreOperation,
    ExecutionMode.Synchronous)]
[Image(
    ImageTypes.PreImage,                   // PreImage, PostImage, or Both
    "PreImage",                            // Message property name / Image name
    "name,revenue,modifiedon")]            // Filtered attributes (comma-separated)
[Image(
    ImageTypes.PostImage,
    "PostImage",
    "")]                                   // Empty = all attributes
public class AccountAuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        
        // Images are automatically populated
        var preImage = context.PreEntityImages["PreImage"];
        var postImage = context.PostEntityImages["PostImage"];
    }
}
```

**Key differences between SPKL and XrmTools.Meta:**

| Feature | SPKL | XrmTools.Meta |
|---------|------|---------------|
| Attribute Name | `CrmPluginRegistrationAttribute` | `StepAttribute` |
| Constructor | `CrmPluginRegistration(message, entity, stage, mode, ...)` | `Step(entity, message, filteringAttributes, stage, mode)` |
| Entity Property | `EntityLogicalName` | `PrimaryEntityName` |
| Mode Property | `ExecutionMode` | `Mode` |
| Image Attribute | `CrmPluginRegistrationImageAttribute` | `ImageAttribute` |
| Image Type Property | `ImageType` | `Type` |
| Image Name | `Name` | `MessagePropertyName` or `Name` |
| Package Reference | SparkleXrm.Tasks | XrmTools.Meta |

**How it works:**
- Scans assemblies for classes implementing `IPlugin`
- Uses reflection to detect both SPKL and XrmTools.Meta attributes (duck typing - no package reference required)
- Discovers image attributes and links them to parent plugin steps
- Automatically creates `PluginStepRegistration` objects with pre-populated `PreImages` and `PostImages` collections
- Registers all discovered steps in the pipeline simulator

### Auto-Discovery with Custom Type Converter

Provide a custom function to convert plugin types to registrations:

```csharp
[Fact]
public void Should_UseCustomConverter_ForPluginDiscovery()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;
    
    // Custom converter function: Type -> IEnumerable<PluginStepRegistration>
    Func<Type, IEnumerable<PluginStepRegistration>> converter = (pluginType) =>
    {
        // Custom logic to determine registrations based on plugin type
        if (pluginType.Name.StartsWith("Account"))
        {
            return new[]
            {
                new PluginStepRegistration
                {
                    PluginType = pluginType,
                    MessageName = "Create",
                    PrimaryEntityName = "account",
                    Stage = ProcessingStepStage.Preoperation,
                    ExecutionOrder = 1
                }
            };
        }
        
        if (pluginType.Name.StartsWith("Contact"))
        {
            return new[]
            {
                new PluginStepRegistration
                {
                    PluginType = pluginType,
                    MessageName = "Update",
                    PrimaryEntityName = "contact",
                    Stage = ProcessingStepStage.Postoperation,
                    ExecutionOrder = 1
                }
            };
        }
        
        return Enumerable.Empty<PluginStepRegistration>();
    };
    
    // Discover with custom converter
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(
        new[] { typeof(MyPlugin).Assembly },
        converter);
}
```

### Auto-Discovery with Custom Attribute Converter

Use your own custom attributes for plugin registration:

```csharp
// Define custom attribute
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MyPluginRegistrationAttribute : Attribute
{
    public string Message { get; set; }
    public string Entity { get; set; }
    public int Stage { get; set; }
    
    public MyPluginRegistrationAttribute(string message, string entity, int stage)
    {
        Message = message;
        Entity = entity;
        Stage = stage;
    }
}

// Use custom attribute on plugins
[MyPluginRegistration("Create", "account", 20)]
[MyPluginRegistration("Update", "account", 20)]
public class AccountPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Plugin logic
    }
}

// Discover with custom attribute converter
[Fact]
public void Should_UseCustomAttributeConverter()
{
    var context = XrmFakedContextFactory.New();
    
    Func<Type, Attribute, PluginStepRegistration> attributeConverter = 
        (pluginType, attribute) =>
    {
        if (attribute is MyPluginRegistrationAttribute myAttr)
        {
            return new PluginStepRegistration
            {
                PluginType = pluginType,
                MessageName = myAttr.Message,
                PrimaryEntityName = myAttr.Entity,
                Stage = (ProcessingStepStage)myAttr.Stage
            };
        }
        return null;
    };
    
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPluginsWithAttributeConverter(
        new[] { typeof(AccountPlugin).Assembly },
        typeof(MyPluginRegistrationAttribute),
        attributeConverter);
}
```

### When to Use Auto-Discovery

**Use auto-discovery when:**
- You have many plugins with SPKL attributes
- You want to test the complete plugin configuration from your assembly
- You're migrating from a project that uses SPKL for deployment
- You want to ensure test configuration matches deployment configuration

**Use manual registration when:**
- You need fine-grained control over specific plugin registrations
- Testing individual plugin behavior in isolation
- You don't use SPKL or custom attributes
- You need to test edge cases or specific configurations

## Custom Action and Custom API Plugin Support

Fake4Dataverse supports registering and executing plugins for Custom Actions and Custom APIs.

Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api

### Registering Plugins for Custom Actions

Custom Actions and Custom APIs use custom message names that you define. Plugins can be registered for these messages just like standard CRUD messages:

```csharp
[Fact]
public void Should_ExecutePlugin_ForCustomAction()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;

    // Setup custom API metadata
    var customApi = new Entity("customapi")
    {
        Id = Guid.NewGuid(),
        ["uniquename"] = "new_CalculateScore", // Custom action name
        ["isenabled"] = true,
        ["boundentitylogicalname"] = "account" // Entity-bound custom action
    };
    context.Initialize(customApi);

    // Register plugin for custom action
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "new_CalculateScore", // Custom action message name
        PrimaryEntityName = "account",
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(CalculateScorePlugin)
    });

    var service = context.GetOrganizationService();

    // Act - Execute custom action
    var request = new OrganizationRequest("new_CalculateScore");
    request.Parameters["Target"] = new EntityReference("account", Guid.NewGuid());
    request.Parameters["InputValue"] = 100;
    
    var response = service.Execute(request);

    // Plugin executes automatically during custom action execution
}
```

### Global Custom Actions

For global (unbound) custom actions, use an empty string for `PrimaryEntityName`:

```csharp
[Fact]
public void Should_ExecutePlugin_ForGlobalCustomAction()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;

    // Setup global custom API metadata
    var customApi = new Entity("customapi")
    {
        Id = Guid.NewGuid(),
        ["uniquename"] = "new_ProcessBatch",
        ["isenabled"] = true
        // No boundentitylogicalname = global custom action
    };
    context.Initialize(customApi);

    // Register plugin for global custom action
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "new_ProcessBatch",
        PrimaryEntityName = string.Empty, // Global custom action
        Stage = ProcessingStepStage.Preoperation,
        PluginType = typeof(ProcessBatchPlugin)
    });

    var service = context.GetOrganizationService();

    // Execute global custom action
    var request = new OrganizationRequest("new_ProcessBatch");
    request.Parameters["BatchSize"] = 100;
    
    service.Execute(request);
}
```

### Multiple Plugins for Custom Actions

Multiple plugins can be registered for the same custom action with different execution orders:

```csharp
[Fact]
public void Should_ExecuteMultiplePlugins_ForCustomAction()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;

    // Setup custom API
    var customApi = new Entity("customapi")
    {
        Id = Guid.NewGuid(),
        ["uniquename"] = "new_ComplexOperation",
        ["isenabled"] = true
    };
    context.Initialize(customApi);

    // Register multiple plugins with execution order
    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "new_ComplexOperation",
        PrimaryEntityName = string.Empty,
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 1, // Executes first
        PluginType = typeof(ValidationPlugin)
    });

    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "new_ComplexOperation",
        PrimaryEntityName = string.Empty,
        Stage = ProcessingStepStage.Preoperation,
        ExecutionOrder = 2, // Executes second
        PluginType = typeof(TransformationPlugin)
    });

    context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
    {
        MessageName = "new_ComplexOperation",
        PrimaryEntityName = string.Empty,
        Stage = ProcessingStepStage.Postoperation,
        ExecutionOrder = 1,
        PluginType = typeof(NotificationPlugin)
    });

    var service = context.GetOrganizationService();
    var request = new OrganizationRequest("new_ComplexOperation");
    service.Execute(request);
    
    // Plugins execute in order: ValidationPlugin -> TransformationPlugin -> NotificationPlugin
}
```

### Auto-Discovery for Custom Actions

Custom action plugins can be auto-discovered using SPKL attributes:

```csharp
using SparkleXrm.Tasks;

// Plugin with SPKL attribute for custom action
[CrmPluginRegistration(
    "new_CalculateScore",                  // Custom action message name
    "account",                             // Entity (or empty for global)
    StageEnum.PreOperation,
    ExecutionModeEnum.Synchronous,
    "",
    "CalculateScorePlugin",
    1,
    IsolationModeEnum.Sandbox)]
public class CalculateScorePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        
        // Custom action logic
        var inputValue = context.InputParameters.Contains("InputValue") 
            ? (int)context.InputParameters["InputValue"] 
            : 0;
        
        // Calculate and set output
        context.OutputParameters["CalculatedScore"] = inputValue * 1.5;
    }
}

// Test with auto-discovery
[Fact]
public void Should_AutoDiscoverCustomActionPlugins()
{
    var context = XrmFakedContextFactory.New();
    context.UsePipelineSimulation = true;

    // Setup custom API metadata
    var customApi = new Entity("customapi")
    {
        Id = Guid.NewGuid(),
        ["uniquename"] = "new_CalculateScore",
        ["isenabled"] = true,
        ["boundentitylogicalname"] = "account"
    };
    context.Initialize(customApi);

    // Auto-discover and register plugins
    var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(
        new[] { typeof(CalculateScorePlugin).Assembly });

    var service = context.GetOrganizationService();
    var request = new OrganizationRequest("new_CalculateScore");
    request.Parameters["Target"] = new EntityReference("account", Guid.NewGuid());
    request.Parameters["InputValue"] = 100;
    
    service.Execute(request);
    // Plugin executes automatically
}
```

### Custom Action Pipeline Stages

Custom actions support all three pipeline stages:

```csharp
// PreValidation - Outside transaction, validate inputs
context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
{
    MessageName = "new_CustomAction",
    PrimaryEntityName = string.Empty,
    Stage = ProcessingStepStage.Prevalidation,
    PluginType = typeof(ValidationPlugin)
});

// PreOperation - Inside transaction, before main operation
context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
{
    MessageName = "new_CustomAction",
    PrimaryEntityName = string.Empty,
    Stage = ProcessingStepStage.Preoperation,
    PluginType = typeof(PreProcessPlugin)
});

// PostOperation - Inside transaction (sync) or queued (async), after main operation
context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
{
    MessageName = "new_CustomAction",
    PrimaryEntityName = string.Empty,
    Stage = ProcessingStepStage.Postoperation,
    PluginType = typeof(PostProcessPlugin)
});
```

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
