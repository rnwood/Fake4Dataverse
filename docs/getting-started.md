# Getting Started with Fake4Dataverse

Fake4Dataverse provides an in-memory fake `IOrganizationService` for unit testing Dataverse / Dynamics 365 applications without a live connection.

## Installation

```
dotnet add package Fake4Dataverse
```

Early-bound metadata registration is included in the core package (no separate EarlyBound package required).
File-based data provider helpers are included in the core package (no separate DataProviders package required).

Optional integrations:

```
dotnet add package Fake4Dataverse.FakeItEasy
dotnet add package Fake4Dataverse.Moq
dotnet add package Fake4Dataverse.FluentAssertions
dotnet add package Fake4Dataverse.AwesomeAssertions
dotnet add package Fake4Dataverse.Shouldly
dotnet add package Fake4Dataverse.Spkl
```

## Quick Start

### Repository Samples

For runnable examples, the samples are split by type and tested in separate test projects:

- `samples/Fake4Dataverse.Samples.AccountService/AccountService.cs` — service-layer code that consumes `IOrganizationService`.
- `samples/Fake4Dataverse.Samples.AccountService.Tests/AccountServiceTests.cs` — unit tests for CRUD, scope rollback, and query joins.
- `samples/Fake4Dataverse.Samples.Plugin/AccountPrimaryContactPlugin.cs` — `IPlugin` sample that creates a related contact record.
- `samples/Fake4Dataverse.Samples.Plugin.Tests/AccountPrimaryContactPluginTests.cs` — tests that register and verify plugin pipeline behavior.

### Basic CRUD

```csharp
using Fake4Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// Create a fake environment and session — no connection needed
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();

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
        var env = new FakeDataverseEnvironment();
        var service = env.CreateOrganizationService();

        // Act — your production code uses IOrganizationService
        var myService = new AccountService(service);
        var id = myService.CreateAccount("Contoso", 1000000m);

        // Assert
        var account = service.Retrieve("account", id, new ColumnSet(true));
        Assert.Equal("Contoso", account["name"]);
    }
}
```

### Early-Bound Metadata Registration (Built-In)

```csharp
using Fake4Dataverse.EarlyBound;

var env = new FakeDataverseEnvironment(FakeOrganizationServiceOptions.Strict);
env.RegisterEarlyBoundEntities(typeof(Account).Assembly);
var service = env.CreateOrganizationService();
```

### Fluent Assertions

```csharp
using Fake4Dataverse.FluentAssertions;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

// Chain assertions on the per-session operation log
service.Should()
    .HaveCreated("account", id)
    .HaveUpdated("account", id)
    .NotHaveDeleted("account", id);
```

### AwesomeAssertions Adapter

```csharp
using AwesomeAssertions;
using Fake4Dataverse.AwesomeAssertions;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

service.Should()
    .HaveCreated("account", id)
    .HaveUpdated("account", id);
```

### Shouldly Adapter

```csharp
using Fake4Dataverse.Shouldly;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

service.ShouldHaveCreated("account", id)
    .ShouldNotHaveDeleted("account", id);
```

### FakeItEasy Adapter

```csharp
using Fake4Dataverse.FakeItEasy;
using FakeItEasy;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var fake = service.AsFake();

fake.Create(new Entity("account") { ["name"] = "Contoso" });

A.CallTo(() => fake.Create(A<Entity>.Ignored))
    .MustHaveHappenedOnceExactly();
```

## Configuration

### Options Presets

```csharp
// Strict mode — validates metadata, enforces security
var strict = new FakeDataverseEnvironment(FakeOrganizationServiceOptions.Strict);
var strictService = strict.CreateOrganizationService();

// Lenient mode — minimal validation, all auto-behaviors off
var lenient = new FakeDataverseEnvironment(FakeOrganizationServiceOptions.Lenient);
var lenientService = lenient.CreateOrganizationService();

// Default — auto-timestamps, auto-owner, pipeline on
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
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
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
env.Options.ValidateWithMetadata = true;

// Register entity metadata
env.MetadataStore.AddEntity("account", "accountid", "name");
env.MetadataStore.AddAttribute("account", "name",
    attributeType: "String", maxLength: 100, requiredLevel: "ApplicationRequired");

// This will throw because 'name' is required
service.Create(new Entity("account") { ["revenue"] = new Money(100m) });
```

## Pipeline Hooks

Pipeline step callbacks receive the SDK's `IPluginExecutionContext`, so the same code works
in both unit tests and real Dataverse plugins.

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();

// Lambda-based pre-operation hook
env.Pipeline.RegisterPreOperation("Create", "account", context =>
{
    var entity = (Entity)context.InputParameters["Target"];
    entity["custom_processed"] = true;
});

// Lambda-based post-operation hook
env.Pipeline.RegisterPostOperation("Create", "account", context =>
{
    var newId = (Guid)context.OutputParameters["id"]; // available in PostOperation
});

// All three stages are available
env.Pipeline.RegisterPreValidation("Create", context =>
{
    // Runs first; throw InvalidPluginExecutionException to cancel the operation
});
```

### Testing a Real IPlugin

Register an actual `IPlugin` instance — the fake will construct the same `IServiceProvider`
that Dataverse passes to plugins at runtime:

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var plugin = new MyAccountPlugin(); // implements IPlugin

env.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account", plugin);

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
| `ITracingService` | In-memory tracing — read traces from `env.Pipeline.Traces` |

### Auto-Register Plugins from SPKL Attributes

```csharp
using Fake4Dataverse;
using Fake4Dataverse.Spkl;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();

// Scans an assembly for [CrmPluginRegistration(...)] on IPlugin types
// and registers supported plugin-step forms.
using var reg = env.RegisterSpklPluginsFromAssembly(typeof(MyPlugin).Assembly);
```

### Capturing Plugin Traces

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
env.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, new MyPlugin());

service.Create(new Entity("account") { ["name"] = "Contoso" });

// Inspect what the plugin traced
Assert.Contains("Processing account", env.Pipeline.Traces[0]);

env.Pipeline.ClearTraces(); // reset between tests if needed
```

### Explicit RegisterStep API

```csharp
// Lambda overloads
env.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, context => { ... });
env.Pipeline.RegisterStep("Update", PipelineStage.PreOperation, "account", context => { ... });

// IPlugin overloads
env.Pipeline.RegisterStep("Delete", PipelineStage.PostOperation, new MyPlugin());
env.Pipeline.RegisterStep("Delete", PipelineStage.PostOperation, "account", new MyPlugin());

// All RegisterStep calls return a PipelineStepRegistration — dispose to unregister
using var reg = env.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx => { ... });
```

## Seeding Test Data

```csharp
using Fake4Dataverse.DataProviders;

// Bulk seed without triggering the pipeline
env.Seed(
    new Entity("account") { ["name"] = "Contoso" },
    new Entity("account") { ["name"] = "Fabrikam" },
    new Entity("account") { ["name"] = "Northwind" }
);

// Seed from CSV
env.SeedFromCsv(@"logicalname,name,revenue
account,Contoso,1000000
account,Fabrikam,2000000");

// Seed from files
env.SeedFromJsonFile("seed-data.json");
env.SeedFromCsvFile("seed-data.csv");

// Builder pattern
var account = new EntityBuilder("account")
    .WithName("Contoso")
    .WithState(0)
    .Build();
```

## Snapshots & Scoping

```csharp
// Snapshot / Restore
env.TakeSnapshot();
service.Create(new Entity("account") { ["name"] = "Temp" });
env.RestoreSnapshot(); // all changes rolled back

// Scoped auto-rollback
using (env.Scope())
{
    service.Create(new Entity("account") { ["name"] = "Temp" });
    // auto-rolled back when scope is disposed
}
```

## Time Manipulation

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
env.Clock = new FakeClock(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
// createdon == 2024-01-01

env.AdvanceTime(TimeSpan.FromDays(30));
// Now == 2024-01-31
```

## Next Steps

- [Cookbook — Common Testing Patterns](cookbook.md)
- [Migration Guide from FakeXrmEasy](migration-from-fakexrmeasy.md)
- [API Reference](../api/index.md)
