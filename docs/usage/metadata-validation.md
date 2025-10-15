# Metadata Validation

Fake4Dataverse v4.0.0+ enforces attribute metadata validation using `IsValidForCreate`, `IsValidForUpdate`, and `IsValidForRead` properties, matching Microsoft Dataverse behavior.

## Overview

**Implemented:** October 2025 (Issue #84)

Microsoft Dataverse validates attribute operations based on metadata properties. Fake4Dataverse now replicates this behavior to provide more accurate testing and catch potential runtime issues before deployment.

## Key Metadata Properties

### IsValidForCreate

Determines whether an attribute can be set during Create operations.

**Reference:** [Microsoft Docs - AttributeMetadata.IsValidForCreate](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate)

- **Default for most attributes:** `true`
- **System attributes:** `false` (e.g., `statecode`, `statuscode`)
- **Validation:** Enabled by default when using `service.Create()`

### IsValidForUpdate

Determines whether an attribute can be modified during Update operations.

**Reference:** [Microsoft Docs - AttributeMetadata.IsValidForUpdate](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforupdate)

- **Default for most attributes:** `true`
- **Read-only attributes:** `false` (e.g., primary ID attributes)
- **Validation:** Enabled by default when using `service.Update()`

### IsValidForRead

Determines whether an attribute can be retrieved.

**Reference:** [Microsoft Docs - AttributeMetadata.IsValidForRead](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforread)

- **Default for all attributes:** `true`
- **Validation:** Currently not enforced in Fake4Dataverse

## Default Validation Behavior

### When Validation is Enabled (Default)

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

var context = XrmFakedContextFactory.New();
context.InitializeMetadata(typeof(Account).Assembly);
var service = context.GetOrganizationService();

// ❌ FAILS - statecode is not valid for Create
var account = new Entity("account")
{
    ["name"] = "Test Account",
    ["statecode"] = new OptionSetValue(1)
};
service.Create(account); // Throws FaultException

// ✅ WORKS - statecode can be updated
account.Id = Guid.NewGuid();
context.Initialize(account); // Initialize with any state for test setup
account["statecode"] = new OptionSetValue(1);
service.Update(account); // Success
```

### When Validation is Disabled

```csharp
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Middleware;

// Disable validation for backward compatibility
var context = XrmFakedContextFactory.New(new IntegrityOptions
{
    ValidateAttributeTypes = false
});
var service = context.GetOrganizationService();

// ✅ WORKS - validation disabled
var account = new Entity("account")
{
    ["name"] = "Test Account",
    ["statecode"] = new OptionSetValue(1)
};
service.Create(account); // Success (validation bypassed)
```

## Attribute Validation Rules

### System State Attributes

| Attribute | IsValidForCreate | IsValidForUpdate | IsValidForRead |
|-----------|------------------|------------------|----------------|
| `statecode` | ❌ false | ✅ true | ✅ true |
| `statuscode` | ❌ false | ✅ true | ✅ true |

**Rationale:** State and status are managed through state transitions, not direct setting during create.

### Primary ID Attributes

| Attribute | IsValidForCreate | IsValidForUpdate | IsValidForRead |
|-----------|------------------|------------------|----------------|
| `accountid` | ✅ true | ❌ false | ✅ true |
| `contactid` | ✅ true | ❌ false | ✅ true |

**Rationale:** IDs can be specified during create but cannot be changed after creation.

### Regular Attributes

| Attribute | IsValidForCreate | IsValidForUpdate | IsValidForRead |
|-----------|------------------|------------------|----------------|
| `name` | ✅ true | ✅ true | ✅ true |
| `telephone1` | ✅ true | ✅ true | ✅ true |
| `revenue` | ✅ true | ✅ true | ✅ true |

**Rationale:** Standard business attributes are fully accessible.

## Usage Examples

### Example 1: Create with Valid Attributes

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

public class MetadataValidationTests
{
    [Fact]
    public void Create_Should_Allow_Regular_Attributes()
    {
        // Arrange
        var context = XrmFakedContextFactory.New();
        context.InitializeMetadata(typeof(Account).Assembly);
        var service = context.GetOrganizationService();
        
        // Act - Create with valid attributes
        var account = new Entity("account")
        {
            ["name"] = "Contoso",
            ["telephone1"] = "555-1234",
            ["revenue"] = new Money(1000000m),
            ["numberofemployees"] = 100
        };
        var id = service.Create(account);
        
        // Assert
        Assert.NotEqual(Guid.Empty, id);
        var retrieved = service.Retrieve("account", id, new ColumnSet(true));
        Assert.Equal("Contoso", retrieved["name"]);
    }
}
```

### Example 2: Handle StateCode Validation

```csharp
[Fact]
public void Create_Should_Reject_StateCode_When_Validation_Enabled()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeMetadata(typeof(Account).Assembly);
    var service = context.GetOrganizationService();
    
    // Act & Assert - Attempt to create with statecode
    var account = new Entity("account")
    {
        ["name"] = "Test",
        ["statecode"] = new OptionSetValue(1) // Invalid for Create
    };
    
    var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
        service.Create(account));
    
    Assert.Contains("statecode", ex.Message);
    Assert.Contains("not valid for Create", ex.Message);
}
```

### Example 3: Update StateCode

```csharp
[Fact]
public void Update_Should_Allow_StateCode_Change()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeMetadata(typeof(Account).Assembly);
    var service = context.GetOrganizationService();
    
    // Create account (without statecode)
    var account = new Entity("account") { ["name"] = "Test" };
    var id = service.Create(account);
    
    // Act - Update statecode
    var updateAccount = new Entity("account")
    {
        Id = id,
        ["statecode"] = new OptionSetValue(1) // Valid for Update
    };
    service.Update(updateAccount);
    
    // Assert
    var retrieved = service.Retrieve("account", id, new ColumnSet("statecode"));
    Assert.Equal(1, ((OptionSetValue)retrieved["statecode"]).Value);
}
```

### Example 4: Test Data Initialization

```csharp
[Fact]
public void Initialize_Should_Skip_Validation_For_Test_Data()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeMetadata(typeof(Account).Assembly);
    
    // Act - Initialize test data with any state
    // Note: Initialize() bypasses validation for flexible test setup
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test Account",
        ["statecode"] = new OptionSetValue(1) // OK in Initialize
    };
    context.Initialize(account);
    
    // Assert - Data is available with specified state
    var service = context.GetOrganizationService();
    var retrieved = service.Retrieve("account", account.Id, new ColumnSet(true));
    Assert.Equal(1, ((OptionSetValue)retrieved["statecode"]).Value);
}
```

## Key Differences from FakeXrmEasy v2

**Important:** Fake4Dataverse v4.0.0+ implements attribute metadata validation that was not present in FakeXrmEasy v2.0.1 (the fork basis).

| Feature | FakeXrmEasy v2.0.1 | Fake4Dataverse v4.0.0+ |
|---------|-------------------|------------------------|
| **IsValidForCreate validation** | ❌ Not enforced | ✅ Enforced by default |
| **IsValidForUpdate validation** | ❌ Not enforced | ✅ Enforced by default |
| **statecode during Create** | ⚠️ Hardcoded check only | ✅ Metadata-based validation |
| **Metadata requirement** | Optional | Required when validation enabled |
| **Validation toggle** | N/A | `IntegrityOptions.ValidateAttributeTypes` |

### Migration Impact

If you're migrating from FakeXrmEasy v2 and have tests that:

1. **Set statecode/statuscode during Create**: Update tests to use Update or disable validation
2. **Don't initialize metadata**: Add `context.InitializeMetadata()` calls
3. **Need flexible test setup**: Use `context.Initialize()` for test data

**Example migration:**

```csharp
// FakeXrmEasy v2 - Worked without validation
var account = new Entity("account")
{
    ["name"] = "Test",
    ["statecode"] = new OptionSetValue(1)
};
service.Create(account); // Succeeded

// Fake4Dataverse v4 - Option 1: Disable validation
var context = XrmFakedContextFactory.New(new IntegrityOptions
{
    ValidateAttributeTypes = false
});

// Fake4Dataverse v4 - Option 2: Use Initialize for test setup
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Test",
    ["statecode"] = new OptionSetValue(1)
};
context.Initialize(account); // Bypasses validation
```

## Configuration Options

### Enable/Disable Validation

```csharp
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;

// Option 1: Default (validation enabled)
var context = XrmFakedContextFactory.New();

// Option 2: Explicitly enable validation
var context = XrmFakedContextFactory.New(new IntegrityOptions
{
    ValidateAttributeTypes = true
});

// Option 3: Disable validation for backward compatibility
var context = XrmFakedContextFactory.New(new IntegrityOptions
{
    ValidateAttributeTypes = false
});
```

### Metadata Initialization

Validation requires entity metadata to be loaded:

```csharp
// Option 1: From early-bound assembly
context.InitializeMetadata(typeof(Account).Assembly);

// Option 2: From CDM JSON files
await context.InitializeMetadataFromCdmAsync("path/to/cdm/Account.cdm.json");

// Option 3: Manual metadata setup
var entityMetadata = new EntityMetadata
{
    LogicalName = "account"
};
var nameAttribute = new StringAttributeMetadata
{
    LogicalName = "name"
};
nameAttribute.SetSealedPropertyValue("IsValidForCreate", true);
nameAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
nameAttribute.SetSealedPropertyValue("IsValidForRead", true);

entityMetadata.SetAttributeCollection(new[] { nameAttribute });
context.InitializeMetadata(entityMetadata);
```

## Error Messages

### IsValidForCreate Violation

```
FaultException<OrganizationServiceFault>:
The attribute 'statecode' on entity 'account' is not valid for Create operations.
```

### IsValidForUpdate Violation

```
FaultException<OrganizationServiceFault>:
The attribute 'accountid' on entity 'account' is not valid for Update operations.
```

### Missing Metadata

```
FaultException<OrganizationServiceFault>:
Could not find entity 'account' in metadata. Entity metadata must be initialized before validation can occur.
```

## Best Practices

### 1. Initialize Metadata Early

```csharp
[Collection("Database")]
public class PluginTests
{
    private readonly IXrmFakedContext _context;
    
    public PluginTests()
    {
        _context = XrmFakedContextFactory.New();
        _context.InitializeMetadata(typeof(Account).Assembly);
    }
}
```

### 2. Use Initialize() for Test Data

```csharp
// ✅ Good - Use Initialize for flexible test setup
var testAccount = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Test",
    ["statecode"] = new OptionSetValue(1)
};
context.Initialize(testAccount);

// ❌ Bad - Don't use Create for test data with restricted attributes
service.Create(testAccount); // Will fail validation
```

### 3. Test Both Valid and Invalid Scenarios

```csharp
[Fact]
public void Should_Enforce_Metadata_Validation()
{
    // Test that validation catches errors
    var invalidAccount = new Entity("account")
    {
        ["statecode"] = new OptionSetValue(1)
    };
    Assert.Throws<FaultException>(() => service.Create(invalidAccount));
    
    // Test that valid operations succeed
    var validAccount = new Entity("account")
    {
        ["name"] = "Test"
    };
    var id = service.Create(validAccount); // Should succeed
    Assert.NotEqual(Guid.Empty, id);
}
```

### 4. Document Validation Requirements

```csharp
/// <summary>
/// Tests account creation with metadata validation enabled.
/// Requires: Account entity metadata initialized.
/// Validates: IsValidForCreate constraints are enforced.
/// </summary>
[Fact]
public void Create_Account_Should_Validate_Attributes()
{
    // Test implementation
}
```

## See Also

- [CRUD Operations](./crud-operations.md) - Basic Create, Read, Update, Delete operations
- [Testing Plugins](./testing-plugins.md) - Plugin testing with metadata validation
- [Migration from v3](../migration/from-v3.md) - Migration guide covering validation differences
- [CDM Import](../cdm-import.md) - Loading entity metadata from CDM files
- [Microsoft Docs - AttributeMetadata](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata) - Official metadata reference
