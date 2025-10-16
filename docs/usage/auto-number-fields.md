# Auto Number Fields

**Implementation Date:** October 2025  
**Issue:** [Support auto number fields](https://github.com/rnwood/Fake4Dataverse/issues/...)

## Overview

Auto number fields in Dataverse automatically generate alphanumeric strings when a new record is created. Fake4Dataverse fully supports auto number field generation using the same format patterns as real Dataverse.

**Reference:** [Microsoft Docs - Auto Number Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields)

## Quick Example

```csharp
// Define entity with auto number field
var entityMetadata = new EntityMetadata { LogicalName = "new_ticket" };
entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_ticketid");

var autoNumberAttribute = new StringAttributeMetadata
{
    LogicalName = "new_ticketnumber",
    AutoNumberFormat = "TICK-{SEQNUM:5}-{RANDSTRING:3}"
};
autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);

entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
{
    autoNumberAttribute
});

context.InitializeMetadata(entityMetadata);

// Create entities - auto number is generated automatically
var ticket1 = new Entity("new_ticket");
service.Create(ticket1); // Generates: TICK-00001-A3X

var ticket2 = new Entity("new_ticket");
service.Create(ticket2); // Generates: TICK-00002-B7Y
```

## How Auto Number Fields Work

Auto number fields are string attributes with an `AutoNumberFormat` property that defines the generation pattern. When an entity is created:

1. The framework checks if the attribute has a value
2. If not set, it generates a value using the format pattern
3. The value is assigned before the entity is saved

**Key behaviors:**
- Values are only generated if the field is not already set
- User-provided values are never overridden
- Each entity/attribute combination maintains its own sequence
- Generation is thread-safe

## Format Tokens

### Sequential Number: `{SEQNUM:n}`

Generates a sequential number with `n` digits, padded with leading zeros.

**Example:** `"CASE-{SEQNUM:5}"`
```
CASE-00001
CASE-00002
CASE-00003
```

**Behavior:**
- Sequence starts at 1 and increments by 1
- Each entity/attribute combination has its own sequence
- Thread-safe for concurrent operations

### Random String: `{RANDSTRING:n}`

Generates a random alphanumeric string with `n` characters.

**Example:** `"PRD-{RANDSTRING:6}"`
```
PRD-A3X7K9
PRD-B7Y2M4
PRD-Z9K3P8
```

**Behavior:**
- Uses uppercase letters and numbers (excluding ambiguous chars: I, O, 0, 1)
- Each generation is unique (extremely high probability)
- Thread-safe

### Date/Time UTC: `{DATETIMEUTC:format}`

Includes the current UTC date/time using standard .NET DateTime format strings.

**Example:** `"ORDER-{DATETIMEUTC:yyyyMMdd}-{SEQNUM:3}"`
```
ORDER-20251016-001
ORDER-20251016-002
ORDER-20251017-003
```

**Common formats:**
- `yyyyMMdd` - 20251016
- `yyyy-MM-dd` - 2025-10-16
- `yyyyMMddHHmm` - 202510161530

### Date/Time Local: `{DATETIMELOCAL:format}`

Includes the current local date/time (in tests, this uses UTC for consistency).

**Example:** `"DOC-{DATETIMELOCAL:yyMMdd}-{SEQNUM:4}"`

## Complex Patterns

You can combine multiple tokens with static text:

```csharp
AutoNumberFormat = "VIN-{DATETIMEUTC:yyyy}-{SEQNUM:5}-{RANDSTRING:4}"
// Generates: VIN-2025-00001-A3X7
```

## Standard Entity Formats

Common Dataverse entities have predefined formats available through `AutoNumberFormatService.GetDefaultFormatForEntity()`:

| Entity | Attribute | Default Format |
|--------|-----------|----------------|
| invoice | invoicenumber | `INV-{SEQNUM:5}` |
| quote | quotenumber | `QUO-{SEQNUM:5}` |
| salesorder | ordernumber | `ORD-{SEQNUM:5}` |
| opportunity | opportunitynumber | `OPP-{SEQNUM:5}` |
| incident | ticketnumber | `CAS-{SEQNUM:5}` |
| contract | contractnumber | `CON-{SEQNUM:5}` |

## Complete Example

```csharp
using Fake4Dataverse;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

// Create context
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .UseCrud()
    .Build();

var service = context.GetOrganizationService();

// Enable validation to require metadata
var integrityOptions = context.GetProperty<IIntegrityOptions>();
integrityOptions.ValidateAttributeTypes = true;

// Define entity metadata with auto number field
var entityMetadata = new EntityMetadata { LogicalName = "new_project" };
entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_projectid");

// Define auto number attribute for project number
var projectNumberAttr = new StringAttributeMetadata
{
    LogicalName = "new_projectnumber",
    AutoNumberFormat = "PRJ-{DATETIMEUTC:yyyy}-{SEQNUM:4}"
};
projectNumberAttr.SetSealedPropertyValue("IsValidForCreate", true);
projectNumberAttr.SetSealedPropertyValue("IsValidForUpdate", false); // Read-only after create
projectNumberAttr.SetSealedPropertyValue("IsValidForRead", true);

// Define auto number attribute for reference code
var referenceCodeAttr = new StringAttributeMetadata
{
    LogicalName = "new_referencecode",
    AutoNumberFormat = "REF-{RANDSTRING:8}"
};
referenceCodeAttr.SetSealedPropertyValue("IsValidForCreate", true);
referenceCodeAttr.SetSealedPropertyValue("IsValidForUpdate", true);
referenceCodeAttr.SetSealedPropertyValue("IsValidForRead", true);

entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
{
    projectNumberAttr,
    referenceCodeAttr
});

context.InitializeMetadata(entityMetadata);

// Create project - auto numbers are generated
var project = new Entity("new_project");
project["new_name"] = "Website Redesign";

var projectId = service.Create(project);

// Retrieve and verify
var retrieved = service.Retrieve("new_project", projectId, new ColumnSet(true));
Console.WriteLine($"Project Number: {retrieved["new_projectnumber"]}"); // PRJ-2025-0001
Console.WriteLine($"Reference Code: {retrieved["new_referencecode"]}");  // REF-A3X7K9M2
```

## Advanced: Seed Values and Reset

For testing scenarios, you can control sequence values:

```csharp
using Fake4Dataverse.Services;

// Access the service (internal, for testing)
var autoNumberService = new AutoNumberFormatService();

// Set sequence to start at 1000
autoNumberService.SetSequenceSeed("new_order", "new_ordernumber", 1000);

// Next order will be 1001
var order = new Entity("new_order");
service.Create(order); // Generates: ORD-01001

// Reset sequence for testing
autoNumberService.ResetSequence("new_order", "new_ordernumber");

var order2 = new Entity("new_order");
service.Create(order2); // Generates: ORD-00001
```

## Behavior Differences from Dataverse

Auto number implementation in Fake4Dataverse closely follows Dataverse behavior with these notes:

1. **Sequence Persistence**: In Dataverse, sequences persist across sessions and are stored in the database. In Fake4Dataverse, sequences are in-memory and reset when the context is recreated.

2. **Seed Values**: In Dataverse, administrators can set seed values through the UI. In Fake4Dataverse, use `SetSequenceSeed()` for testing.

3. **Format Validation**: Dataverse validates format patterns when metadata is created. Fake4Dataverse validates at generation time.

4. **Date/Time Local**: Uses UTC in tests for consistency (Dataverse uses actual local time).

## Best Practices

1. **Use metadata validation**: Enable `ValidateAttributeTypes` to ensure metadata is loaded before using auto number fields

2. **Set IsValidForUpdate appropriately**: Most auto number fields should be read-only after creation:
   ```csharp
   autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", false);
   ```

3. **Choose appropriate formats**: Use sequential numbers for business-critical identifiers, random strings for non-sequential needs

4. **Document your patterns**: Include comments explaining format choices in your tests

5. **Test concurrency**: If your code creates entities concurrently, test that sequences remain unique

## Testing Auto Number Fields

```csharp
[Fact]
public void Test_Auto_Number_Generation()
{
    // Arrange
    var context = CreateContext();
    var service = context.GetOrganizationService();
    
    var entityMetadata = new EntityMetadata { LogicalName = "testentity" };
    entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "testentityid");
    
    var autoNumberAttr = new StringAttributeMetadata
    {
        LogicalName = "testnumber",
        AutoNumberFormat = "TEST-{SEQNUM:3}"
    };
    autoNumberAttr.SetSealedPropertyValue("IsValidForCreate", true);
    
    entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[] { autoNumberAttr });
    context.InitializeMetadata(entityMetadata);
    
    // Act
    var entity1 = new Entity("testentity");
    var id1 = service.Create(entity1);
    
    var entity2 = new Entity("testentity");
    var id2 = service.Create(entity2);
    
    // Assert
    var retrieved1 = service.Retrieve("testentity", id1, new ColumnSet("testnumber"));
    var retrieved2 = service.Retrieve("testentity", id2, new ColumnSet("testnumber"));
    
    Assert.Equal("TEST-001", retrieved1["testnumber"]);
    Assert.Equal("TEST-002", retrieved2["testnumber"]);
}
```

## See Also

- [Microsoft Docs - Auto Number Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields)
- [Testing Plugins](testing-plugins.md) - Testing with auto number fields in plugins
- [CRUD Operations](crud-operations.md) - Basic entity creation
- [Metadata](../concepts/metadata.md) - Working with entity metadata
