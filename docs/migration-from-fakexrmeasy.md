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
var service = new FakeOrganizationService();
// Everything is on the service directly — no separate context object.
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
var service = new FakeOrganizationService();
service.Seed(
    new Entity("account") { Id = id1, ["name"] = "Contoso" },
    new Entity("account") { Id = id2, ["name"] = "Fabrikam" }
);
```

### Plugin Tests

**FakeXrmEasy:**
```csharp
var context = new XrmFakedContext();
context.ExecutePluginWith<MyPlugin>(pluginContext);
```

**Fake4Dataverse:**

Register a real `IPlugin` instance directly — no adapters needed. Fake4Dataverse builds
the same `IServiceProvider` that Dataverse passes to plugins at runtime:

```csharp
var service = new FakeOrganizationService();

// Register the real plugin class
service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account",
    new MyPlugin());

// Trigger the operation as normal
service.Create(new Entity("account") { ["name"] = "Contoso" });

// The plugin ran — assert its side-effects in the fake store
```

The `IServiceProvider` resolves:
- `IPluginExecutionContext` — full SDK context (message, entity, parameters, images)
- `IOrganizationServiceFactory` — backed by `FakeOrganizationService`
- `ITracingService` — traces collected in `service.Pipeline.Traces`

Lambda callbacks are also supported when you want to test logic without the plugin class:

```csharp
service.Pipeline.RegisterPostOperation("Create", "account", ctx =>
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
service.MetadataStore.AddEntity("account", "accountid", "name");
service.MetadataStore.AddAttribute("account", "name", attributeType: "String", maxLength: 100);

// Or with the EarlyBound companion:
service.RegisterEarlyBoundEntities(typeof(Account).Assembly);
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
service.RegisterCustomApi("my_CustomAction", (req, svc) =>
{
    return new OrganizationResponse();
});
```

## Search-and-Replace Cheat Sheet

| FakeXrmEasy | Fake4Dataverse |
|---|---|
| `new XrmFakedContext()` | `new FakeOrganizationService()` |
| `context.GetOrganizationService()` | `service` (direct use) |
| `context.Initialize(entities)` | `service.Seed(entities)` |
| `using FakeXrmEasy;` | `using Fake4Dataverse;` |
| `context.CallerProperties.CallerId` | `service.CallerId` |

## Step-by-Step Migration

1. **Replace NuGet package**: Remove `FakeXrmEasy.*` packages, add `Fake4Dataverse`.
2. **Update usings**: Replace `using FakeXrmEasy;` with `using Fake4Dataverse;`.
3. **Replace context creation**: Replace `new XrmFakedContext()` + `GetOrganizationService()` with `new FakeOrganizationService()`.
4. **Replace `Initialize`**: Replace `context.Initialize(entities)` with `service.Seed(entities)`.
5. **Update metadata setup**: Replace `InitializeMetadata` calls with `MetadataStore.AddEntity` / `AddAttribute`.
6. **Update assertions**: Replace any FakeXrmEasy assertion helpers with `service.Should().HaveCreated(...)` fluent assertions.
7. **Run tests**: All standard `IOrganizationService` calls should work as-is.
