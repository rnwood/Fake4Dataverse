# Fake4Dataverse — AI Agent Instructions

In-memory fake `IOrganizationService` and `IOrganizationServiceAsync2` for unit testing Dataverse / Dynamics 365 applications without a live connection.

---

## Build & Test Commands

```powershell
# Build
dotnet build --configuration Release

# Test (366 tests × 2 frameworks = ~732 total)
dotnet test --configuration Release

# Test (quick feedback, minimal output)
dotnet test --configuration Release --verbosity minimal

# Pack NuGet (tag-triggered in CI; for local validation only)
dotnet pack src/Fake4Dataverse/Fake4Dataverse.csproj --configuration Release
```

Tests run against both `net462` and `net10.0`. All tests must pass on both frameworks before merging.

---

## Project Structure

```
src/Fake4Dataverse/           # Core library
  FakeOrganizationService.cs  # Public entry point — implements IOrganizationService + IOrganizationServiceAsync2
  FakeOrganizationServiceOptions.cs  # Configuration (Strict/Lenient presets)
  InMemoryEntityStore.cs      # Thread-safe entity storage (ReaderWriterLockSlim)
  InMemoryMetadataStore.cs    # Entity/attribute metadata with validation
  QueryExpressionEvaluator.cs # 40+ ConditionOperators, LinkEntity joins, paging
  FetchXmlEvaluator.cs        # FetchXml parsing + aggregates (Count/Sum/Avg/Min/Max)
  PipelineManager.cs          # Pre-validation / pre-operation / post-operation hooks
  SecurityManager.cs          # Roles, privileges, record sharing
  OperationLog.cs             # Records all service calls for assertions
  AttributeIndex.cs           # Optional equality indexes for query performance
  CalculatedFieldManager.cs   # Calculated and rollup field evaluation
  CurrencyManager.cs          # Exchange rates and base currency computation
  Handlers/                   # One file per request type (WhoAmI, Assign, Upsert, …)
  Metadata/                   # Metadata type models
  Pipeline/                   # Pipeline infrastructure
  Security/                   # Security primitives

tests/Fake4Dataverse.Tests/   # xUnit test project, mirrors library features
```

---

## Architecture

- **Handler registry pattern**: Every `OrganizationRequest` variant is dispatched through `OrganizationRequestHandlerRegistry` to a matching `IOrganizationRequestHandler`.
- **Handler interface** — implement to add support for a new request type:
  ```csharp
  public interface IOrganizationRequestHandler
  {
      bool CanHandle(OrganizationRequest request);
      OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service);
  }
  ```
  Handlers are `internal sealed class`, placed in `Handlers/`, named `<RequestType>RequestHandler`.
- **Thread safety**: `InMemoryEntityStore` uses `ReaderWriterLockSlim` + `ConcurrentDictionary`. All write paths must hold the write lock.
- **Deep cloning**: Entities are always deep-cloned on store and retrieve to prevent aliasing bugs.

---

## Coding Conventions

- **Language**: C# with `LangVersion: latest`, `Nullable: enable`, `ImplicitUsings: disable` (explicit `using` required).
- **Warnings as errors**: `TreatWarningsAsErrors: true` — fix all warnings, not just errors.
- **Visibility**: Implementation classes are `internal sealed`; public API is minimal and intentional.
- **XML doc comments** on all public members.
- **Namespaces**: Root `Fake4Dataverse`; sub-namespaces for `Handlers`, `Metadata`, `Pipeline`, `Security`.
- **Naming**: PascalCase throughout; test methods follow `MethodName_Scenario_Expected`.

---

## Test Patterns

- Framework: **xUnit** (`[Fact]` attributes; no shared base class).
- Tests directly instantiate `FakeOrganizationService` — no mocking framework.
- Tests are organized by feature area (one file per area, e.g., `CrudTests.cs`, `FetchXmlTests.cs`).
- Use `TakeSnapshot()` / `RestoreSnapshot()` / `Scope()` for test isolation when shared state is needed.
- Use `FakeClock` for deterministic date/time scenarios.
- Use `OperationLog` assertions to verify what calls were made.

Typical test skeleton:

```csharp
[Fact]
public void Create_WithValidEntity_ReturnsNewGuid()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
    Assert.NotEqual(Guid.Empty, id);
}
```

---

## Key Configuration

`FakeOrganizationServiceOptions` controls all automatic behaviors (all `bool`):

| Option | Default | Notes |
|---|---|---|
| `AutoSetTimestamps` | `true` | Sets `createdon` / `modifiedon` |
| `AutoSetOwner` | `true` | Sets `ownerid`, `createdby`, `modifiedby` |
| `AutoSetVersionNumber` | `true` | Increments `versionnumber` |
| `AutoSetStateCode` | `true` | Sets `statecode` / `statuscode` on Create |
| `ValidateWithMetadata` | `false` | Requires metadata registration |
| `EnforceSecurityRoles` | `false` | Requires role setup |
| `EnablePipeline` | `true` | Plugin-like hooks |
| `EnableOperationLog` | `true` | Call recording |

Use `FakeOrganizationServiceOptions.Strict` (metadata + security on) or `FakeOrganizationServiceOptions.Lenient` (all features off) as presets.

---

## CI / CD

- GitHub Actions at `.github/workflows/build.yml`
- Matrix: `ubuntu-latest` × `windows-latest`
- Triggers: push to `main`, version tags (`v*`), PRs to `main`
- Steps: Restore → Build (Release) → Test → Pack & Publish (tag only)
- NuGet package: published to nuget.org on version tags

---

## Common Pitfalls

- **`TreatWarningsAsErrors: true`**: Unused variables, missing nullability annotations, and shadowed members all fail the build. Fix warnings immediately.
- **Both target frameworks**: Any API used must exist on `net462`. Avoid `net10.0`-only BCL types.
- **Deep clone discipline**: Never store or return a raw `Entity` reference from the store — always clone.
- **Lock ordering**: Acquire `InMemoryEntityStore` write lock before touching related stores to avoid deadlocks.
- **NuGet**: `NU1701` is suppressed (CrmSdk .NET Framework compatibility). Do not suppress other NuGet warnings without discussion.

---

## Adding a New Request Handler

1. Create `src/Fake4Dataverse/Handlers/<Name>RequestHandler.cs` as `internal sealed class`.
2. Implement `IOrganizationRequestHandler.CanHandle` and `Handle`.
3. Register it in `OrganizationRequestHandlerRegistry`.
4. Add tests in `tests/Fake4Dataverse.Tests/<Name>Tests.cs` (or extend an existing file if closely related).
