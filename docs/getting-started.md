# Getting Started with Fake4Dataverse

Fake4Dataverse provides an in-memory fake `IOrganizationService` for unit testing Dataverse / Dynamics 365 applications without a live connection.

## Installation

```
dotnet add package Fake4Dataverse
```

## Quick Start

### Basic CRUD

```csharp
using Fake4Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// Create a fake service — no connection needed
var service = new FakeOrganizationService();

// Create
var id = service.Create(new Entity("account")
{
    ["name"] = "Contoso Ltd",
    ["revenue"] = new Money(1000000m)
});

// Retrieve
var account = service.Retrieve("account", id, new ColumnSet(true));
// account["name"] == "Contoso Ltd"

// Update
service.Update(new Entity("account", id)
{
    ["name"] = "Contoso Corp"
});

// Delete
service.Delete("account", id);
```

### Writing a Unit Test

```csharp
using Fake4Dataverse;
using Microsoft.Xrm.Sdk;
using Xunit;

public class AccountServiceTests
{
    [Fact]
    public void CreateAccount_SetsNameAndRevenue()
    {
        // Arrange
        var service = new FakeOrganizationService();

        // Act — your production code uses IOrganizationService
        var myService = new AccountService(service);
        var id = myService.CreateAccount("Contoso", 1000000m);

        // Assert
        var account = service.Retrieve("account", id, new ColumnSet(true));
        Assert.Equal("Contoso", account["name"]);
    }
}
```

### Fluent Assertions

```csharp
var service = new FakeOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

// Chain assertions on the operation log
service.Should()
    .HaveCreated("account", id)
    .HaveUpdated("account", id)
    .NotHaveDeleted("account", id);
```

## Configuration

### Options Presets

```csharp
// Strict mode — validates metadata, enforces security
var strict = new FakeOrganizationService(FakeOrganizationServiceOptions.Strict);

// Lenient mode — minimal validation, all auto-behaviors off
var lenient = new FakeOrganizationService(FakeOrganizationServiceOptions.Lenient);

// Default — auto-timestamps, auto-owner, pipeline on
var service = new FakeOrganizationService();
```

### Key Options

| Option | Default | Description |
|---|---|---|
| `AutoSetTimestamps` | `true` | Auto-set `createdon`/`modifiedon` |
| `AutoSetOwner` | `true` | Auto-set `ownerid`, `createdby`, `modifiedby` |
| `AutoSetVersionNumber` | `true` | Increment `versionnumber` on update |
| `AutoSetStateCode` | `true` | Set `statecode`/`statuscode` on create |
| `ValidateWithMetadata` | `false` | Require metadata registration for validation |
| `EnforceSecurityRoles` | `false` | Enforce security role checks |
| `EnablePipeline` | `true` | Enable plugin-like pipeline hooks |
| `EnableOperationLog` | `true` | Record all operations for assertions |

### Loading Options from JSON or Environment Variables

```csharp
// From JSON string
var options = FakeOrganizationServiceOptions.FromJson("""
{
    "AutoSetTimestamps": true,
    "ValidateWithMetadata": true
}
""");

// From a config file
var options = FakeOrganizationServiceOptions.FromJsonFile("test-config.json");

// From environment variables (prefix: FAKE4DATAVERSE_)
// e.g. FAKE4DATAVERSE_AUTOSETTIMESTAMPS=false
var options = FakeOrganizationServiceOptions.FromEnvironment();
```

## QueryExpression

```csharp
var query = new QueryExpression("contact")
{
    ColumnSet = new ColumnSet("firstname", "lastname", "emailaddress1")
};
query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
query.AddOrder("lastname", OrderType.Ascending);
query.PageInfo = new PagingInfo { Count = 50, PageNumber = 1 };

var results = service.RetrieveMultiple(query);
```

## FetchXml

```csharp
var fetch = @"
<fetch top='10'>
  <entity name='account'>
    <attribute name='name' />
    <attribute name='revenue' />
    <filter>
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
    <order attribute='name' />
  </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetch));
```

## Metadata & Validation

```csharp
var service = new FakeOrganizationService();
service.ValidateWithMetadata = true;

// Register entity metadata
service.MetadataStore.AddEntity("account", "accountid", "name");
service.MetadataStore.AddAttribute("account", "name",
    attributeType: "String", maxLength: 100, requiredLevel: "ApplicationRequired");

// This will throw because 'name' is required
service.Create(new Entity("account") { ["revenue"] = new Money(100m) });
```

## Pipeline Hooks

Pipeline step callbacks receive the SDK's `IPluginExecutionContext`, so the same code works
in both unit tests and real Dataverse plugins.

```csharp
var service = new FakeOrganizationService();

// Lambda-based pre-operation hook
service.Pipeline.RegisterPreOperation("Create", "account", context =>
{
    var entity = (Entity)context.InputParameters["Target"];
    entity["custom_processed"] = true;
});

// Lambda-based post-operation hook
service.Pipeline.RegisterPostOperation("Create", "account", context =>
{
    var newId = (Guid)context.OutputParameters["id"]; // available in PostOperation
});

// All three stages are available
service.Pipeline.RegisterPreValidation("Create", context =>
{
    // Runs first; throw InvalidPluginExecutionException to cancel the operation
});
```

### Testing a Real IPlugin

Register an actual `IPlugin` instance — the fake will construct the same `IServiceProvider`
that Dataverse passes to plugins at runtime:

```csharp
var service = new FakeOrganizationService();
var plugin = new MyAccountPlugin(); // implements IPlugin

service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account", plugin);

// Act
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

// Assert — verify side effects created by the plugin
var contacts = service.RetrieveMultiple(new QueryExpression("contact") { ColumnSet = new ColumnSet(true) });
Assert.Single(contacts.Entities);
```

The `IServiceProvider` passed to `IPlugin.Execute` resolves:

| Type | What you get |
|---|---|
| `IPluginExecutionContext` | Full execution context (message, entity, params, images, …) |
| `IOrganizationServiceFactory` | Factory backed by `FakeOrganizationService` |
| `ITracingService` | In-memory tracing — read traces from `service.Pipeline.Traces` |

### Capturing Plugin Traces

```csharp
var service = new FakeOrganizationService();
service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, new MyPlugin());

service.Create(new Entity("account") { ["name"] = "Contoso" });

// Inspect what the plugin traced
Assert.Contains("Processing account", service.Pipeline.Traces[0]);

service.Pipeline.ClearTraces(); // reset between tests if needed
```

### Explicit RegisterStep API

```csharp
// Lambda overloads
service.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, context => { ... });
service.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, "account", context => { ... });

// IPlugin overloads
service.Pipeline.RegisterStep("Delete", PipelineStage.PostOperation, new MyPlugin());
service.Pipeline.RegisterStep("Delete", PipelineStage.PostOperation, "account", new MyPlugin());

// All RegisterStep calls return a PipelineStepRegistration — dispose to unregister
using var reg = service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx => { ... });
```

## Seeding Test Data

```csharp
// Bulk seed without triggering the pipeline
service.Seed(
    new Entity("account") { ["name"] = "Contoso" },
    new Entity("account") { ["name"] = "Fabrikam" },
    new Entity("account") { ["name"] = "Northwind" }
);

// Seed from CSV
service.SeedFromCsv(@"logicalname,name,revenue
account,Contoso,1000000
account,Fabrikam,2000000");

// Builder pattern
var account = new EntityBuilder("account")
    .WithName("Contoso")
    .WithState(0)
    .Build();
```

## Snapshots & Scoping

```csharp
// Snapshot / Restore
service.TakeSnapshot();
service.Create(new Entity("account") { ["name"] = "Temp" });
service.RestoreSnapshot(); // all changes rolled back

// Scoped auto-rollback
using (service.Scope())
{
    service.Create(new Entity("account") { ["name"] = "Temp" });
    // auto-rolled back when scope is disposed
}
```

## Time Manipulation

```csharp
var service = new FakeOrganizationService();
service.Clock = new FakeClock(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
// createdon == 2024-01-01

service.AdvanceTime(TimeSpan.FromDays(30));
// Now == 2024-01-31
```

## Next Steps

- [Cookbook — Common Testing Patterns](cookbook.md)
- [Migration Guide from FakeXrmEasy](migration-from-fakexrmeasy.md)
- [API Reference](../api/index.md)
