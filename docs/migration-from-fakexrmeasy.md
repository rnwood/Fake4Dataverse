# Migration Guide: FakeXrmEasy → Fake4Dataverse

This guide helps you migrate existing tests from [FakeXrmEasy](https://github.com/jordimontana82/fake-xrm-easy) to Fake4Dataverse.

## Why Migrate?

| Feature | FakeXrmEasy | Fake4Dataverse |
|---|---|---|
| Target frameworks | .NET Framework / .NET 6 | .NET Framework 4.6.2 + .NET 10 |
| License | Commercial (v2+) | MIT |
| Thread safety | Limited | Full (`ReaderWriterLockSlim` + `ConcurrentDictionary`) |
| Pipeline hooks | Basic | Pre-validation / Pre-op / Post-op |
| Security simulation | No | Roles, privileges, sharing, BU scoping |
| Metadata validation | Limited | Full entity/attribute metadata with validation |
| Calculated fields | No | Lambda-based formulas + rollup |
| FetchXml aggregates | Partial | Full (Count/Sum/Avg/Min/Max/GroupBy) |
| Snapshots | No | `TakeSnapshot()` / `RestoreSnapshot()` / `Scope()` |
| Time control | No | `IClock` / `FakeClock` / `AdvanceTime()` |

## Concept Mapping

### Service Initialization

**FakeXrmEasy:**
```csharp
var context = new XrmFakedContext();
var service = context.GetOrganizationService();
```

**Fake4Dataverse:**
```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
// Environment owns shared state (metadata, security, pipeline, store).
// Service is a lightweight session for a specific caller.
```

### Optional Companion Packages (Moq / FakeItEasy / Assertions)

If your existing tests assert interactions with a mocking framework, you can keep that style
and still run behavior through the in-memory Fake4Dataverse engine.

Install one adapter package as needed:

```bash
// Moq adapter
dotnet add package Fake4Dataverse.Moq

// FakeItEasy adapter
dotnet add package Fake4Dataverse.FakeItEasy

// AwesomeAssertions adapter
dotnet add package Fake4Dataverse.AwesomeAssertions

// Shouldly adapter
dotnet add package Fake4Dataverse.Shouldly
```

**Moq bridge:**

```csharp
using Fake4Dataverse.Moq;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var mock = service.AsMock();

// Pass mock.Object to production code
```

**FakeItEasy bridge:**

```csharp
using Fake4Dataverse.FakeItEasy;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var fake = service.AsFake();

// Pass fake to production code
```

**AwesomeAssertions operation-log assertions:**

```csharp
using AwesomeAssertions;
using Fake4Dataverse.AwesomeAssertions;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

service.Should().HaveCreated("account", id);
```

**Shouldly operation-log assertions:**

```csharp
using Fake4Dataverse.Shouldly;

var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

service.ShouldHaveCreated("account", id);
```

### Initializing Entity Data

**FakeXrmEasy:**
```csharp
var context = new XrmFakedContext();
context.Initialize(new List<Entity>
{
    new Entity("account") { Id = id1, ["name"] = "Contoso" },
    new Entity("account") { Id = id2, ["name"] = "Fabrikam" }
});
```

**Fake4Dataverse:**
```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
env.Seed(
    new Entity("account") { Id = id1, ["name"] = "Contoso" },
    new Entity("account") { Id = id2, ["name"] = "Fabrikam" }
);
```

### File-Based Seeding Helpers

If you use JSON/CSV files for test data setup, file-based seed helpers are included in
the core package (no separate DataProviders package required):

```csharp
using Fake4Dataverse.DataProviders;

var env = new FakeDataverseEnvironment();
env.SeedFromJsonFile("seed-data.json");
env.SeedFromCsvFile("seed-data.csv");
var service = env.CreateOrganizationService();
```

### Plugin Tests

Need a runnable end-to-end example? See:

- `samples/Fake4Dataverse.Samples.Plugin/AccountPrimaryContactPlugin.cs`
- `samples/Fake4Dataverse.Samples.Plugin.Tests/AccountPrimaryContactPluginTests.cs`

**FakeXrmEasy:**
```csharp
var context = new XrmFakedContext();
context.ExecutePluginWith<MyPlugin>(pluginContext);
```

**Fake4Dataverse:**

Register a real `IPlugin` instance directly — no adapters needed. Fake4Dataverse builds
the same `IServiceProvider` that Dataverse passes to plugins at runtime:

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();

// Register the real plugin class
env.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account",
    new MyPlugin());

// Trigger the operation as normal
service.Create(new Entity("account") { ["name"] = "Contoso" });

// The plugin ran — assert its side-effects in the fake store
```

The `IServiceProvider` resolves:
- `IPluginExecutionContext` — full SDK context (message, entity, parameters, images)
- `IOrganizationServiceFactory` — backed by `FakeOrganizationService`
- `ITracingService` — traces collected in `env.Pipeline.Traces`

Lambda callbacks are also supported when you want to test logic without the plugin class:

```csharp
env.Pipeline.RegisterPostOperation("Create", "account", ctx =>
{
    var target = (Entity)ctx.InputParameters["Target"];
    // inline logic here
});
```

### Querying

**FakeXrmEasy:** Uses the same `QueryExpression` / `FetchExpression` — no change needed.

**Fake4Dataverse:** Same API. All 40+ condition operators, linkentity joins, and FetchXml work identically.

### Metadata Registration

**FakeXrmEasy:**
```csharp
context.InitializeMetadata(typeof(Account).Assembly);
// or
context.InitializeMetadata(entityMetadata);
```

**Fake4Dataverse:**
```csharp
using Fake4Dataverse.EarlyBound;

env.MetadataStore.AddEntity("account", "accountid", "name");
env.MetadataStore.AddAttribute("account", "name", attributeType: "String", maxLength: 100);

// Early-bound metadata registration is built into Fake4Dataverse:
// (No separate Fake4Dataverse.EarlyBound package is required.)
env.RegisterEarlyBoundEntities(typeof(Account).Assembly);
```

### Execute Requests

**FakeXrmEasy:**
```csharp
// Some requests supported, others require custom fakes
context.GetOrganizationService().Execute(request);
```

**Fake4Dataverse:** — 25+ built-in handlers:
```csharp
service.Execute(new WhoAmIRequest());
service.Execute(new SetStateRequest { ... });
service.Execute(new AssignRequest { ... });
service.Execute(new ExecuteMultipleRequest { ... });
// ... and more
```

Register custom handlers for unsupported request types:
```csharp
env.RegisterCustomApi("my_CustomAction", (req, svc) =>
{
    return new OrganizationResponse();
});
```

## Search-and-Replace Cheat Sheet

| FakeXrmEasy | Fake4Dataverse |
|---|---|
| `new XrmFakedContext()` | `new FakeDataverseEnvironment()` + `env.CreateOrganizationService()` |
| `context.GetOrganizationService()` | `env.CreateOrganizationService()` |
| `context.Initialize(entities)` | `env.Seed(entities)` |
| file-based custom seeding helpers | `env.SeedFromJsonFile(...)` / `env.SeedFromCsvFile(...)` |
| `using FakeXrmEasy;` | `using Fake4Dataverse;` |
| `context.CallerProperties.CallerId` | `service.CallerId` |
| `context.InitializeMetadata(typeof(Account).Assembly)` | `env.RegisterEarlyBoundEntities(typeof(Account).Assembly)` |

## Step-by-Step Migration

1. **Replace NuGet package**: Remove `FakeXrmEasy.*` packages, add `Fake4Dataverse`.
2. **Optional companion package**: If you use interaction-based mocks/assertions, add `Fake4Dataverse.Moq`, `Fake4Dataverse.FakeItEasy`, `Fake4Dataverse.AwesomeAssertions`, or `Fake4Dataverse.Shouldly`.
3. **Update usings**: Replace `using FakeXrmEasy;` with `using Fake4Dataverse;`.
4. **Replace context creation**: Replace `new XrmFakedContext()` + `GetOrganizationService()` with `new FakeDataverseEnvironment()` + `env.CreateOrganizationService()`.
5. **Replace `Initialize`**: Replace `context.Initialize(entities)` with `env.Seed(entities)`.
6. **Update metadata setup**: Replace `InitializeMetadata` calls with `env.MetadataStore.AddEntity` / `AddAttribute`, or use built-in early-bound registration via `env.RegisterEarlyBoundEntities(...)`.
7. **Adopt file-based seeding helpers (optional)**: Use built-in `Fake4Dataverse.DataProviders` extension methods (`SeedFromJsonFile`, `SeedFromCsvFile`) on the environment if your tests rely on seed files.
8. **Update assertions**: Replace any FakeXrmEasy assertion helpers with operation log assertions or an assertion companion package (FluentAssertions/AwesomeAssertions/Shouldly).
9. **Migrate framework adapters**: Replace direct framework fakes with `service.AsMock()` or `service.AsFake()` where needed.
10. **Run tests**: All standard `IOrganizationService` calls should work as-is.
