# Using CDM Instead of Early-Bound Assemblies

This document explains how to use CDM (Common Data Model) entities as an alternative to early-bound assemblies for metadata initialization in your tests.

## Background

When metadata validation is enabled (default in v4.0.0+), tests need entity metadata to function properly. There are three main approaches:

1. **Disable validation** (backward compatibility) - Inherit from `Fake4DataverseTests`
2. **Early-bound assemblies** - Use `EnableProxyTypes()` or `InitializeMetadata(assembly)`
3. **CDM files** (Recommended) - Use `InitializeMetadataFromCdmFile()` or CDM schema downloads

## Approach 1: Disable Validation (Quick Fix)

For tests that don't need validation, inherit from `Fake4DataverseTests`:

```csharp
public class MyTests : Fake4DataverseTests
{
    [Fact]
    public void My_Test()
    {
        // Use _context and _service from base class
        // Validation is disabled for backward compatibility
        var account = new Entity("account") 
        { 
            Id = Guid.NewGuid(),
            ["name"] = "Test"
        };
        _context.Initialize(new[] { account });
        
        // Test logic...
    }
}
```

## Approach 2: Early-Bound Assemblies (Traditional)

Use generated early-bound classes:

```csharp
public class MyTests
{
    [Fact]
    public void My_Test()
    {
        var context = XrmFakedContextFactory.New();
        
        // Enable proxy types to get metadata from early-bound classes
        context.EnableProxyTypes(Assembly.GetExecutingAssembly());
        
        var account = new Account 
        { 
            Id = Guid.NewGuid(),
            Name = "Test"
        };
        context.Initialize(new[] { account });
        
        // Test logic...
    }
}
```

Or initialize from assembly:

```csharp
// Initialize metadata from early-bound assembly
context.InitializeMetadata(typeof(Account).Assembly);
```

**Pros:**
- Type-safe access to entity properties
- IntelliSense support
- Compile-time checking

**Cons:**
- Requires generating early-bound classes
- Large assembly size
- Needs regeneration when schema changes
- Tight coupling to specific schema version

## Approach 3: CDM Files (Recommended Alternative)

Use CDM JSON files for metadata:

### Option 3A: Local CDM Files

```csharp
public class MyTests
{
    [Fact]
    public void My_Test()
    {
        var context = XrmFakedContextFactory.New();
        
        // Initialize from local CDM file
        context.InitializeMetadataFromCdmFile("path/to/account.cdm.json");
        
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account"
        };
        context.Initialize(new[] { account });
        
        // Test logic...
    }
}
```

### Option 3B: Standard CDM Schemas (Download from Microsoft)

```csharp
public class MyTests
{
    [Fact]
    public async Task My_Test()
    {
        var context = XrmFakedContextFactory.New();
        
        // Download standard CDM schemas from Microsoft's repository
        await context.InitializeMetadataFromStandardCdmSchemasAsync(
            new[] { "crmcommon" } // Includes Account, Contact, Lead, etc.
        );
        
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account"
        };
        context.Initialize(new[] { account });
        
        // Test logic...
    }
}
```

Available standard schemas:
- `crmcommon` - Base CRM entities (Account, Contact, Lead, etc.)
- `sales` - Sales entities (Opportunity, Quote, Order, Invoice)
- `service` - Service entities (Case/Incident, Queue, etc.)
- `portals` - Power Pages/Portal entities
- `customerinsights` - Customer Insights entities

### Option 3C: Specific Standard Entities

```csharp
// Load only specific entities
await context.InitializeMetadataFromStandardCdmEntitiesAsync(
    new[] { "account", "contact", "lead" }
);
```

### CDM JSON Format (Simple)

Create your own CDM files:

```json
{
    "jsonSchemaSemanticVersion": "1.0.0",
    "definitions": [
        {
            "$type": "LocalEntity",
            "name": "Account",
            "sourceName": "account",
            "hasAttributes": [
                {
                    "name": "accountId",
                    "dataType": "guid",
                    "sourceName": "accountid",
                    "isPrimaryKey": true
                },
                {
                    "name": "name",
                    "dataType": "string",
                    "sourceName": "name",
                    "maximumLength": 160
                }
            ]
        }
    ]
}
```

**Pros:**
- No code generation needed
- Portable JSON files
- Version control friendly
- Lightweight
- Can be shared across projects/languages
- Official Microsoft standard format

**Cons:**
- No compile-time type checking
- No IntelliSense for entity properties
- Requires manual CDM file creation (or download)

## Comparison Table

| Feature | Disable Validation | Early-Bound | CDM Files |
|---------|-------------------|-------------|-----------|
| Type Safety | ❌ No | ✅ Yes | ❌ No |
| IntelliSense | ❌ No | ✅ Yes | ❌ No |
| Code Generation | ❌ Not needed | ⚠️ Required | ❌ Not needed |
| Validation | ❌ Disabled | ✅ Enabled | ✅ Enabled |
| File Size | ✅ Small | ❌ Large | ✅ Small |
| Schema Changes | ✅ Easy | ⚠️ Regenerate | ✅ Easy |
| Portability | ✅ High | ❌ Low | ✅ Very High |
| Microsoft Standard | N/A | ⚠️ Proprietary | ✅ Yes (CDM) |

## Recommendations

1. **For quick test fixes**: Inherit from `Fake4DataverseTests` (disables validation)
2. **For new tests**: Use CDM files for better portability and maintainability
3. **For existing early-bound tests**: Keep as-is unless refactoring
4. **For production code**: Consider early-bound for type safety, CDM for flexibility

## Migration Examples

### From Early-Bound to CDM

**Before (Early-Bound):**
```csharp
context.EnableProxyTypes(Assembly.GetExecutingAssembly());

var account = new Account 
{ 
    Name = "Test",
    CreditOnHold = true
};
```

**After (CDM):**
```csharp
context.InitializeMetadataFromCdmFile("account.cdm.json");

var account = new Entity("account")
{
    ["name"] = "Test",
    ["creditonhold"] = true
};
```

### From No Metadata to CDM

**Before (No Validation):**
```csharp
public class MyTests // No base class
{
    [Fact]
    public void My_Test()
    {
        var context = XrmFakedContextFactory.New();
        // ... test code
    }
}
```

**After (With CDM):**
```csharp
public class MyTests : Fake4DataverseTests // Inherit for convenience
{
    [Fact]
    public async Task My_Test()
    {
        // Override to enable validation with CDM metadata
        var context = XrmFakedContextFactory.New();
        await context.InitializeMetadataFromStandardCdmSchemasAsync(
            new[] { "crmcommon" }
        );
        // ... test code
    }
}
```

## See Also

- [CDM Import Documentation](../docs/cdm-import.md)
- [Metadata Testing Guide](../docs/usage/testing-plugins.md)
- [Microsoft CDM Repository](https://github.com/microsoft/CDM)
