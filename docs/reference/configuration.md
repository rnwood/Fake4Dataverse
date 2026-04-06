# Configuration Reference

## FakeOrganizationServiceOptions

All behavior of `FakeOrganizationService` is controlled through `FakeOrganizationServiceOptions`, passed at construction time.

### Presets

| Preset | Description |
|--------|-------------|
| **Default** (`new FakeOrganizationServiceOptions()`) | All auto-set behaviors ON, metadata validation and security OFF |
| **Strict** (`FakeOrganizationServiceOptions.Strict`) | Metadata validation + security enforcement ON (all auto-set behaviors remain ON) |
| **Lenient** (`FakeOrganizationServiceOptions.Lenient`) | All features OFF — maximum speed, full manual control |

```csharp
// Default — recommended starting point
var service = new FakeOrganizationService();

// Strict — mirrors real Dataverse validation
var strict = new FakeOrganizationService(FakeOrganizationServiceOptions.Strict);

// Lenient — bare-bones, nothing automatic
var lenient = new FakeOrganizationService(FakeOrganizationServiceOptions.Lenient);
```

### All Options

| Option | Type | Default | Strict | Lenient | Description |
|--------|------|---------|--------|---------|-------------|
| `AutoSetTimestamps` | `bool` | `true` | `true` | `false` | Auto-set `createdon`/`modifiedon` on Create and Update |
| `AutoSetOwner` | `bool` | `true` | `true` | `false` | Auto-set `ownerid`, `createdby`, `modifiedby` from `CallerId` |
| `AutoSetVersionNumber` | `bool` | `true` | `true` | `false` | Auto-increment `versionnumber` on Create/Update |
| `AutoSetStateCode` | `bool` | `true` | `true` | `false` | Auto-set `statecode=0`/`statuscode=1` on Create |
| `ValidateWithMetadata` | `bool` | `false` | `true` | `false` | Requires entity metadata registration; validates on Create/Update |
| `EnforceSecurityRoles` | `bool` | `false` | `true` | `false` | Enforces security role/privilege checks |
| `EnablePipeline` | `bool` | `true` | `true` | `false` | Enables pre/post operation pipeline hooks |
| `EnableOperationLog` | `bool` | `true` | `true` | `false` | Records all operations for assertions |

### Loading Options

**Programmatic:**

```csharp
var options = new FakeOrganizationServiceOptions
{
    AutoSetTimestamps = true,
    ValidateWithMetadata = true,
    EnforceSecurityRoles = false,
};
var service = new FakeOrganizationService(options);
```

**From a JSON string (`FromJson`):**

```csharp
var json = """{"AutoSetTimestamps": false, "ValidateWithMetadata": true}""";
var options = FakeOrganizationServiceOptions.FromJson(json);
var service = new FakeOrganizationService(options);
```

Unspecified properties keep their default values. Property names are case-insensitive.

**From a JSON file (`FromJsonFile`):**

```csharp
var options = FakeOrganizationServiceOptions.FromJsonFile("fake4dataverse.json");
var service = new FakeOrganizationService(options);
```

**From environment variables (`FromEnvironment`):**

```csharp
// Reads FAKE4DATAVERSE_AUTOSETTIMESTAMPS, FAKE4DATAVERSE_VALIDATEWITHMETADATA, etc.
var options = FakeOrganizationServiceOptions.FromEnvironment();
var service = new FakeOrganizationService(options);
```

Environment variable names are `FAKE4DATAVERSE_` followed by the option name in uppercase. Only variables that are set override the defaults.

---

## Service Properties

These properties are available on `FakeOrganizationService` after construction.

| Property | Type | Description |
|----------|------|-------------|
| `Options` | `FakeOrganizationServiceOptions` | Read-only configuration options |
| `CallerId` | `Guid` | Current user ID (used for auto-set owner and security checks) |
| `InitiatingUserId` | `Guid` | Initiating user (differs from `CallerId` in impersonation scenarios) |
| `BusinessUnitId` | `Guid` | Current user's business unit |
| `OrganizationId` | `Guid` | Organization identifier |
| `OrganizationName` | `string` | Organization name (surfaced to plugins) |
| `Clock` | `IClock` | Clock implementation (default: `SystemClock`) |
| `ValidateWithMetadata` | `bool` | Shortcut to toggle metadata validation at runtime |
| `UseSystemContext` | `bool` | When `true`, all security checks are bypassed |
| `MetadataStore` | `InMemoryMetadataStore` | Entity/attribute metadata store |
| `Security` | `SecurityManager` | Security roles, privileges, and record sharing |
| `Pipeline` | `PipelineManager` | Pre/post operation pipeline hooks |
| `CalculatedFields` | `CalculatedFieldManager` | Calculated and rollup field evaluation |
| `Currency` | `CurrencyManager` | Exchange rates and base currency computation |
| `OperationLog` | `OperationLog` | Recorded operations for post-hoc assertions |
| `HandlerRegistry` | `OrganizationRequestHandlerRegistry` | Registry for custom request handlers |

```csharp
var service = new FakeOrganizationService();
service.CallerId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
service.Clock = new FakeClock(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
```

---

## Auto-Set Behaviors Detail

### AutoSetTimestamps

On **Create**: sets `createdon` and `modifiedon` to `Clock.UtcNow`.
On **Update**: sets `modifiedon` to `Clock.UtcNow`.

Use `FakeClock` to control the time deterministically in tests:

```csharp
var clock = new FakeClock(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
var service = new FakeOrganizationService { Clock = clock };

var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
var entity = service.Retrieve("account", id, new ColumnSet(true));

Assert.Equal(clock.UtcNow, entity.GetAttributeValue<DateTime>("createdon"));
```

### AutoSetOwner

On **Create**: sets `ownerid`, `createdby`, and `modifiedby` to `CallerId`.
On **Update**: sets `modifiedby` to `CallerId`.

```csharp
service.CallerId = userId;
var id = service.Create(new Entity("account") { ["name"] = "Test" });
var entity = service.Retrieve("account", id, new ColumnSet(true));

Assert.Equal(userId, entity.GetAttributeValue<EntityReference>("ownerid").Id);
Assert.Equal(userId, entity.GetAttributeValue<EntityReference>("createdby").Id);
```

### AutoSetVersionNumber

On **Create**: sets `versionnumber` to `1`.
On **Update**: increments `versionnumber` by `1`.

The version number is a global counter across all entities in the store.

### AutoSetStateCode

On **Create**: sets `statecode` to `0` (Active) and `statuscode` to `1` (Active).

These values match the default Dataverse behavior for most entities. Use `SetStateRequest` or `UpdateRequest` to change state after creation.

---

## See Also

- [Getting Started](../guides/getting-started.md)
- [Metadata Validation](../guides/metadata-validation.md)
- [Security & Access Control](../guides/security-access-control.md)
- [Pipeline & Plugin Testing](../guides/pipeline-plugin-testing.md)
