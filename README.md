# Fake4Dataverse

An in-memory fake `IOrganizationService` for unit testing Dataverse / Dynamics 365 applications — no live connection required.

[![Build & Test](https://github.com/nicknow/Fake4Dataverse/actions/workflows/build.yml/badge.svg)](https://github.com/nicknow/Fake4Dataverse/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Fake4Dataverse.svg)](https://www.nuget.org/packages/Fake4Dataverse)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Features

| Category | Capabilities |
|----------|-------------|
| **CRUD** | Create, Retrieve, Update, Delete with auto-set fields (timestamps, owner, state, version number) |
| **QueryExpression** | Filtering (40+ condition operators), ordering, column projection, TopCount, paging with cookies |
| **FetchXml** | Full FetchXml parsing and evaluation, including aggregation (Count, Sum, Avg, Min, Max, GroupBy) |
| **LinkEntity** | Inner/outer joins, nested joins, link criteria, aliased attributes |
| **Pipeline** | Pre-validation, pre-operation, and post-operation hooks (plugin-like) |
| **Metadata** | Entity/attribute metadata store, validation on Create/Update, auto-discovery |
| **Security** | Security roles, privilege enforcement, record sharing (Grant/Modify/Revoke access) |
| **Execute handlers** | WhoAmI, SetState, Assign, Upsert, ExecuteMultiple, ExecuteTransaction, and more |
| **Calculated fields** | Calculated and rollup field definitions evaluated on Retrieve |
| **Currency** | Exchange rates and auto-computed base currency amounts |
| **Activity parties** | ActivityParty entity support for from/to/cc/bcc fields |
| **Binary attributes** | Image and file column storage |
| **Seeding** | Bulk insert, JSON seeding, `EntityBuilder` fluent API |
| **Snapshots** | `TakeSnapshot()` / `RestoreSnapshot()` / `Scope()` for test isolation |
| **Time control** | `FakeClock` for deterministic date/time testing |
| **Operation log** | Records all service calls for post-hoc assertions |
| **Configuration** | `FakeOrganizationServiceOptions` with Strict/Lenient presets |
| **Multi-target** | .NET Framework 4.6.2 and .NET 10 |

## Installation

```
dotnet add package Fake4Dataverse
```

## Quick Start

```csharp
using Fake4Dataverse;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// Create the fake service
var service = new FakeOrganizationService();

// Use it like a real IOrganizationService
var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

var retrieved = service.Retrieve("account", id, new ColumnSet("name"));
// retrieved["name"] == "Contoso"

service.Update(new Entity("account") { Id = id, ["name"] = "Contoso Ltd." });

service.Delete("account", id);
```

## Configuration Options

Control automatic behaviors with `FakeOrganizationServiceOptions`:

```csharp
// Use presets
var strict = new FakeOrganizationService(FakeOrganizationServiceOptions.Strict);
var lenient = new FakeOrganizationService(FakeOrganizationServiceOptions.Lenient);

// Or configure individually
var options = new FakeOrganizationServiceOptions
{
    AutoSetTimestamps = true,
    AutoSetOwner = true,
    AutoSetVersionNumber = true,
    AutoSetStateCode = true,
    ValidateWithMetadata = false,
    EnforceSecurityRoles = false,
    EnablePipeline = true,
    EnableOperationLog = true,
};
var service = new FakeOrganizationService(options);
```

| Preset | Description |
|--------|-------------|
| **Default** (`new()`) | All auto-set behaviors on, validation/security off — matches current behavior |
| **Strict** | Enables metadata validation and security role enforcement |
| **Lenient** | Disables all auto-set behaviors, pipeline, and operation log — full manual control |

## QueryExpression

```csharp
var service = new FakeOrganizationService();
service.Create(new Entity("contact") { ["firstname"] = "John", ["lastname"] = "Doe" });
service.Create(new Entity("contact") { ["firstname"] = "Jane", ["lastname"] = "Doe" });

var query = new QueryExpression("contact") { ColumnSet = new ColumnSet(true) };
query.Criteria.AddCondition("lastname", ConditionOperator.Equal, "Doe");
query.AddOrder("firstname", OrderType.Ascending);

var results = service.RetrieveMultiple(query);
// results.Entities.Count == 2
```

### LinkEntity (Joins)

```csharp
var query = new QueryExpression("contact") { ColumnSet = new ColumnSet("fullname") };
var link = query.AddLink("account", "parentcustomerid", "accountid", JoinOperator.Inner);
link.Columns = new ColumnSet("name");
link.EntityAlias = "acct";

var results = service.RetrieveMultiple(query);
// Access linked attributes: entity.GetAttributeValue<AliasedValue>("acct.name")
```

### Paging

```csharp
var query = new QueryExpression("account") { ColumnSet = new ColumnSet(true) };
query.PageInfo = new PagingInfo { PageNumber = 1, Count = 50 };

var page1 = service.RetrieveMultiple(query);
// page1.MoreRecords indicates if there are more pages
```

## FetchXml

```csharp
var fetchXml = @"
<fetch top='10'>
  <entity name='account'>
    <attribute name='name' />
    <filter>
      <condition attribute='name' operator='like' value='Contoso%' />
    </filter>
    <order attribute='name' />
  </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

### FetchXml Aggregation

```csharp
var fetchXml = @"
<fetch aggregate='true'>
  <entity name='opportunity'>
    <attribute name='estimatedvalue' alias='total' aggregate='sum' />
    <attribute name='statecode' alias='state' groupby='true' />
  </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

## Pipeline (Plugin-Like Hooks)

```csharp
using Fake4Dataverse.Pipeline;

var service = new FakeOrganizationService();

// Register a pre-operation step
service.Pipeline.RegisterStep("Create", PipelineStage.PreOperation, ctx =>
{
    var target = (Entity)ctx.InputParameters["Target"];
    target["description"] = "Auto-populated by pipeline";
});

// Register a post-operation step
service.Pipeline.RegisterStep("Update", PipelineStage.PostOperation, "account", ctx =>
{
    // Fires only for account updates
    var target = (Entity)ctx.InputParameters["Target"];
    // Post-operation logic here
});
```

## Execute Handlers

Built-in handlers include `WhoAmI`, `SetState`, `Assign`, `Upsert`, `ExecuteMultiple`, `ExecuteTransaction`, `RetrieveEntity`, `RetrieveAllEntities`, `RetrieveAttribute`, `GrantAccess`, `ModifyAccess`, `RevokeAccess`, and `RetrievePrincipalAccess`.

### Custom Execute Handlers

```csharp
public class MyCustomActionHandler : IOrganizationRequestHandler
{
    public bool CanHandle(OrganizationRequest request)
        => request.RequestName == "my_CustomAction";

    public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
    {
        var response = new OrganizationResponse();
        response.Results["OutputParam"] = "Hello!";
        return response;
    }
}

var service = new FakeOrganizationService();
service.HandlerRegistry.Register(new MyCustomActionHandler());
```

## Metadata & Validation

```csharp
var service = new FakeOrganizationService();
service.ValidateWithMetadata = true;

service.MetadataStore.AddEntity("account")
    .AddStringAttribute("name", maxLength: 100, requiredLevel: AttributeRequiredLevel.ApplicationRequired)
    .AddMoneyAttribute("revenue")
    .AddOptionSetAttribute("industrycode", 1, 2, 3);
```

## Security

```csharp
using Fake4Dataverse.Security;

var service = new FakeOrganizationService();
service.Security.EnforceSecurityRoles = true;

var role = new SecurityRole("Sales Rep")
    .AddPrivilege("account", PrivilegeType.Create, PrivilegeDepth.Organization)
    .AddPrivilege("account", PrivilegeType.Read, PrivilegeDepth.Organization);

service.Security.AssignRole(service.CallerId, role);

// Now only account Create/Read are permitted; other operations will throw
```

## Seeding Test Data

```csharp
// Bulk seed (bypasses pipeline, auto-fields, security)
service.Seed(
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Contoso" },
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Fabrikam" }
);

// Seed from JSON
service.SeedFromJson(@"[
    {""logicalName"":""account"",""id"":""11111111-1111-1111-1111-111111111111"",""attributes"":{""name"":""Acme""}}
]");

// EntityBuilder
var entity = new EntityBuilder("contact")
    .WithId(Guid.NewGuid())
    .WithName("John Doe")
    .WithAttribute("emailaddress1", "john@contoso.com")
    .WithOwner(service.CallerId)
    .Build();
```

## Snapshots & Test Isolation

```csharp
// Scoped auto-rollback
using (service.Scope())
{
    service.Create(new Entity("account") { ["name"] = "Temporary" });
    // modifications are automatically reverted when scope is disposed
}

// Manual snapshot
var snapshot = service.TakeSnapshot();
service.Create(new Entity("account") { ["name"] = "Will be reverted" });
service.RestoreSnapshot(snapshot);
```

## Time Manipulation

```csharp
var service = new FakeOrganizationService();
service.Clock = new FakeClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

var id = service.Create(new Entity("account") { ["name"] = "Test" });
var created = service.Retrieve("account", id, new ColumnSet("createdon"));
// created["createdon"] == 2026-01-01T00:00:00Z

service.AdvanceTime(TimeSpan.FromDays(30));
// Now service.Clock.UtcNow == 2026-01-31T00:00:00Z
```

## Operation Log

```csharp
var service = new FakeOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Test" });
service.Update(new Entity("account") { Id = id, ["name"] = "Updated" });

// Assert operations
Assert.True(service.OperationLog.HasCreated("account", id));
Assert.True(service.OperationLog.HasUpdated("account", id));

// Query operation records
var creates = service.OperationLog.GetOperations("Create", "account");
```

## Alternate Keys

```csharp
service.MetadataStore.AddEntity("account")
    .AddAlternateKey("ak_accountnumber", "accountnumber");

var entity = new Entity("account") { ["accountnumber"] = "ACC-001", ["name"] = "Contoso" };
service.Create(entity);

// Retrieve by alternate key
var keys = new KeyAttributeCollection { { "accountnumber", "ACC-001" } };
var retrieved = service.Retrieve("account", keys, new ColumnSet(true));
```

## Performance

Benchmarks run with [BenchmarkDotNet](https://benchmarkdotnet.org/) comparing Fake4Dataverse (Lenient preset) against **FakeXrmEasy 1.x** (net462) and **FakeXrmEasy 3.x** (net10).

> **FakeXrmEasy 1.x** can only be compared on .NET Framework 4.6.2.
>
> **FakeXrmEasy 3.x** (`FakeXrmEasy.Core.v9`) is benchmarked with the `RPL_1_5` license and the minimal middleware setup (`.AddCrud().UseCrud()`).

Run the benchmarks yourself:

```powershell
# .NET Framework 4.6.2 — Fake4Dataverse vs FakeXrmEasy 1.x
dotnet run --configuration Release --project benchmarks/Fake4Dataverse.Benchmarks -f net462

# .NET 10 — Fake4Dataverse vs FakeXrmEasy 3.x
dotnet run --configuration Release --project benchmarks/Fake4Dataverse.Benchmarks -f net10.0
```

### CRUD operations

**Environment:** Windows 11 · Intel Core Ultra 7 258V · BenchmarkDotNet v0.14.0 · ShortRun (3 warmup + 3 iterations)

| Benchmark | F4D (net462) | FXE v1 (net462) | Speedup | F4D (net10) | FXE v3 (net10) | Speedup |
|-----------|-------------:|----------------:|--------:|------------:|---------------:|--------:|
| Create | 2.54 µs | 10.43 µs | **4.1×** | 2.62 µs | 14.2 µs | **5.4×** |
| Create + Update + Delete | 1.28 µs | 16.95 µs | **13.3×** | 744 ns | 29.8 µs | **40×** |
| Retrieve (by ID) | 213 ns | 9.68 µs | **45×** | 136 ns | 13.3 µs | **98×** |
| Update | 353 ns | 5.48 µs | **15.5×** | 168 ns | 7.75 µs | **46×** |

### QueryExpression / RetrieveMultiple

**Environment:** same as above · Paged query (page 1, 50 records per page)

| Scenario | Rows | F4D (net462) | FXE v1 (net462) | Speedup | F4D (net10) | FXE v3 (net10) | Speedup |
|----------|-----:|-------------:|----------------:|--------:|------------:|---------------:|--------:|
| All columns | 100 | 59.9 µs | 1.69 ms | **28×** | 40.3 µs | 2.10 ms | **52×** |
| Filter (statecode=0) | 100 | 79.2 µs | 2.73 ms | **34×** | 54.6 µs | 3.17 ms | **58×** |
| Order by name | 100 | 141 µs | 4.54 ms | **32×** | 66.5 µs | 2.17 ms | **33×** |
| Filter + order | 100 | 134 µs | 5.66 ms | **42×** | 62.5 µs | 3.27 ms | **52×** |
| All columns | 1 000 | 781 µs | 15.32 ms | **19.6×** | 353 µs | 7.83 ms | **22×** |
| Filter (statecode=0) | 1 000 | 1.22 ms | 13.83 ms | **11.3×** | 518 µs | 9.26 ms | **18×** |
| Order by name | 1 000 | 2.38 ms | 19.04 ms | **8.0×** | 1.11 ms | 10.1 ms | **9.1×** |
| Filter + order | 1 000 | 1.56 ms | 16.19 ms | **10.4×** | 813 µs | 9.60 ms | **11.8×** |
| All columns | 10 000 | 25.3 ms | 221.6 ms | **8.8×** | 12.3 ms | 117 ms | **9.5×** |
| Filter (statecode=0) | 10 000 | 26.3 ms | 163.0 ms | **6.2×** | 13.4 ms | 91.6 ms | **6.8×** |
| Order by name | 10 000 | 42.3 ms | 235.1 ms | **5.6×** | 16.0 ms | 116 ms | **7.2×** |
| Filter + order | 10 000 | 33.6 ms | 200.6 ms | **6.0×** | 16.3 ms | 97.1 ms | **6.0×** |

## License

[MIT](LICENSE)
