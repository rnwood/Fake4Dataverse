# Calculated Fields Support

## Overview

Fake4Dataverse simulates calculated field evaluation using the NCalc expression engine. Supported features:
- Arithmetic operations
- String manipulation
- Date/time functions
- Logical operators
- Field references within the same entity or related entities

## Microsoft Documentation

Official references:
- [Define Calculated Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields) - Main documentation for calculated columns
- [Types of Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields) - Field types

## Evaluation Behavior

Fake4Dataverse evaluates calculated fields:
- **On entity retrieve** - Calculated in real-time when the entity is retrieved
- **On entity update** - Re-calculated when dependent field values change

## Usage

### Basic Arithmetic Formula

The simplest calculated field performs arithmetic operations on other fields:

```csharp
using Fake4Dataverse.CalculatedFields;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

[Fact]
public void Should_Calculate_Total_Price()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var evaluator = context.CalculatedFieldEvaluator;

    // Define calculated field: totalprice = quantity * unitprice
    var definition = new CalculatedFieldDefinition
    {
        EntityLogicalName = "product",
        AttributeLogicalName = "totalprice",
        Formula = "[quantity] * [unitprice]",
        ResultType = typeof(decimal),
        Dependencies = { "quantity", "unitprice" }
    };
    evaluator.RegisterCalculatedField(definition);

    var product = new Entity("product")
    {
        Id = Guid.NewGuid(),
        ["quantity"] = 10,
        ["unitprice"] = 25.50m
    };

    // Act - Evaluate calculated fields
    evaluator.EvaluateCalculatedFields(product);

    // Assert
    Assert.Equal(255.00m, product.GetAttributeValue<decimal>("totalprice"));
}
```

### String Concatenation with CONCAT

Use the CONCAT function to combine text values:

```csharp
// Define calculated field: fullname = CONCAT(firstname, ' ', lastname)
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "contact",
    AttributeLogicalName = "fullname",
    Formula = "CONCAT([firstname], ' ', [lastname])",
    ResultType = typeof(string)
};
evaluator.RegisterCalculatedField(definition);

var contact = new Entity("contact")
{
    Id = Guid.NewGuid(),
    ["firstname"] = "John",
    ["lastname"] = "Doe"
};

evaluator.EvaluateCalculatedFields(contact);

// fullname = "John Doe"
Assert.Equal("John Doe", contact.GetAttributeValue<string>("fullname"));
```

### Conditional Logic with IF

Use IF statements for conditional calculations:

```csharp
// Define calculated field: grade = IF(score >= 60, 'Pass', 'Fail')
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "student",
    AttributeLogicalName = "grade",
    Formula = "IF([score] >= 60, 'Pass', 'Fail')",
    ResultType = typeof(string)
};
evaluator.RegisterCalculatedField(definition);

var passingStudent = new Entity("student")
{
    Id = Guid.NewGuid(),
    ["score"] = 75
};

evaluator.EvaluateCalculatedFields(passingStudent);

// grade = "Pass"
Assert.Equal("Pass", passingStudent.GetAttributeValue<string>("grade"));
```

### Date Difference Calculations

Calculate differences between dates using DIFFINDAYS and related functions:

```csharp
// Define calculated field: duration = DIFFINDAYS(startdate, enddate)
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "task",
    AttributeLogicalName = "duration",
    Formula = "DIFFINDAYS([startdate], [enddate])",
    ResultType = typeof(int)
};
evaluator.RegisterCalculatedField(definition);

var task = new Entity("task")
{
    Id = Guid.NewGuid(),
    ["startdate"] = new DateTime(2025, 1, 1),
    ["enddate"] = new DateTime(2025, 1, 11)
};

evaluator.EvaluateCalculatedFields(task);

// duration = 10 days
Assert.Equal(10, task.GetAttributeValue<int>("duration"));
```

### Adding Time to Dates

Use ADDDAYS, ADDMONTHS, ADDYEARS, and similar functions:

```csharp
// Define calculated field: duedate = ADDDAYS(createdon, 7)
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "task",
    AttributeLogicalName = "duedate",
    Formula = "ADDDAYS([createdon], 7)",
    ResultType = typeof(DateTime)
};
evaluator.RegisterCalculatedField(definition);

var task = new Entity("task")
{
    Id = Guid.NewGuid(),
    ["createdon"] = new DateTime(2025, 1, 1)
};

evaluator.EvaluateCalculatedFields(task);

// duedate = January 8, 2025
Assert.Equal(new DateTime(2025, 1, 8), task.GetAttributeValue<DateTime>("duedate"));
```

### Logical Operators

Combine boolean conditions with AND, OR, and NOT:

```csharp
// Define calculated field: isqualified = isactive AND hasrevenue
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "account",
    AttributeLogicalName = "isqualified",
    Formula = "[isactive] AND [hasrevenue]",
    ResultType = typeof(bool)
};
evaluator.RegisterCalculatedField(definition);

var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["isactive"] = true,
    ["hasrevenue"] = true
};

evaluator.EvaluateCalculatedFields(account);

// isqualified = true
Assert.True(account.GetAttributeValue<bool>("isqualified"));
```

### Automatic Evaluation on Retrieve

Calculated fields are automatically evaluated when entities are retrieved:

```csharp
var context = XrmFakedContextFactory.New();
var evaluator = context.CalculatedFieldEvaluator;
var service = context.GetOrganizationService();

// Register calculated field
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "product",
    AttributeLogicalName = "totalprice",
    Formula = "[quantity] * [unitprice]",
    ResultType = typeof(decimal)
};
evaluator.RegisterCalculatedField(definition);

// Initialize entity
var product = new Entity("product")
{
    Id = Guid.NewGuid(),
    ["quantity"] = 5,
    ["unitprice"] = 10.00m
};
context.Initialize(new[] { product });

// Retrieve entity - calculated field is automatically evaluated
var retrieved = service.Retrieve("product", product.Id, new ColumnSet(true));

// totalprice is automatically calculated
Assert.Equal(50.00m, retrieved.GetAttributeValue<decimal>("totalprice"));
```

## Supported Functions

### String Functions

Verified from [Microsoft's calculated fields documentation](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax):

| Function | Description | Example |
|----------|-------------|---------|
| **CONCAT** | Combines multiple text values | `CONCAT([firstname], ' ', [lastname])` |
| **TRIMLEFT** | Removes leading whitespace | `TRIMLEFT([name])` |
| **TRIMRIGHT** | Removes trailing whitespace | `TRIMRIGHT([name])` |
| **UPPER** | Converts to uppercase | `UPPER([name])` |
| **LOWER** | Converts to lowercase | `LOWER([email])` |
| **LEN** | Returns string length | `LEN([description])` |
| **LEFT** | Returns leftmost characters | `LEFT([code], 3)` |
| **RIGHT** | Returns rightmost characters | `RIGHT([code], 3)` |
| **MID** | Returns substring | `MID([text], 2, 5)` |
| **REPLACE** | Replaces substring | `REPLACE([text], 'old', 'new')` |
| **TRIM** | Removes leading/trailing spaces | `TRIM([name])` |

### Date/Time Functions

Verified from [Microsoft's calculated fields documentation](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax):

| Function | Description | Example |
|----------|-------------|---------|
| **DIFFINDAYS** | Difference in days | `DIFFINDAYS([startdate], [enddate])` |
| **DIFFINHOURS** | Difference in hours | `DIFFINHOURS([starttime], [endtime])` |
| **DIFFINMINUTES** | Difference in minutes | `DIFFINMINUTES([start], [end])` |
| **DIFFINMONTHS** | Difference in months | `DIFFINMONTHS([startdate], [enddate])` |
| **DIFFINWEEKS** | Difference in weeks | `DIFFINWEEKS([startdate], [enddate])` |
| **DIFFINYEARS** | Difference in years | `DIFFINYEARS([birthdate], [today])` |
| **ADDHOURS** | Add hours to date | `ADDHOURS([datetime], 2)` |
| **ADDDAYS** | Add days to date | `ADDDAYS([date], 7)` |
| **ADDWEEKS** | Add weeks to date | `ADDWEEKS([date], 2)` |
| **ADDMONTHS** | Add months to date | `ADDMONTHS([date], 3)` |
| **ADDYEARS** | Add years to date | `ADDYEARS([date], 1)` |
| **SUBTRACTHOURS** | Subtract hours from date | `SUBTRACTHOURS([datetime], 2)` |
| **SUBTRACTDAYS** | Subtract days from date | `SUBTRACTDAYS([date], 7)` |
| **SUBTRACTWEEKS** | Subtract weeks from date | `SUBTRACTWEEKS([date], 2)` |
| **SUBTRACTMONTHS** | Subtract months from date | `SUBTRACTMONTHS([date], 3)` |
| **SUBTRACTYEARS** | Subtract years from date | `SUBTRACTYEARS([date], 1)` |
| **NOW** | Current date/time | `NOW()` |
| **TODAY** | Current date | `TODAY()` |

### Math Functions

| Function | Description | Example |
|----------|-------------|---------|
| **ROUND** | Round to decimals | `ROUND([value], 2)` |
| **ABS** | Absolute value | `ABS([difference])` |
| **FLOOR** | Round down | `FLOOR([value])` |
| **CEILING** | Round up | `CEILING([value])` |

### Logical Functions

| Function | Description | Example |
|----------|-------------|---------|
| **IF** | Conditional logic | `IF([condition], 'true', 'false')` |
| **ISNULL** | Check if null | `ISNULL([value])` |

### Logical Operators

Verified from [Microsoft's calculated fields documentation](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields):

| Operator | Description | Example |
|----------|-------------|---------|
| **AND** | Logical AND | `[condition1] AND [condition2]` |
| **OR** | Logical OR | `[condition1] OR [condition2]` |
| **NOT** | Logical NOT | `NOT [condition]` |

### Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| **>** | Greater than | `[value] > 100` |
| **<** | Less than | `[value] < 50` |
| **>=** | Greater or equal | `[score] >= 60` |
| **<=** | Less or equal | `[score] <= 100` |
| **==** | Equal to | `[status] == 1` |
| **!=** | Not equal to | `[status] != 0` |

## Formula Syntax

### Field References

Use square brackets to reference fields:

```csharp
// Single field
[fieldname]

// Example
[quantity]
[unitprice]
[createdon]
```

### Related Entity Fields

Reference fields from related entities using dot notation (converted to underscore internally):

```csharp
// Related field
[relatedentity.fieldname]

// Example
[account.name]
[parentaccount.revenue]
```

### String Literals

Enclose string literals in single quotes:

```csharp
// String literal
'text value'

// Example
CONCAT([firstname], ' ', [lastname])
IF([status] == 'Active', 1, 0)
```

### Complex Formulas

Combine multiple operations and functions:

```csharp
// Nested functions
IF([score] >= 60, CONCAT('Pass: ', [score]), CONCAT('Fail: ', [score]))

// Multiple conditions
IF([isactive] AND [hasrevenue], [quantity] * [unitprice], 0)

// Date calculations
DIFFINDAYS([startdate], ADDMONTHS([startdate], 3))
```

## Advanced Scenarios

### Dependent Calculated Fields

Calculated fields can depend on other calculated fields:

```csharp
// First calculated field: subtotal = quantity * unitprice
var subtotalDef = new CalculatedFieldDefinition
{
    EntityLogicalName = "invoice",
    AttributeLogicalName = "subtotal",
    Formula = "[quantity] * [unitprice]",
    ResultType = typeof(decimal),
    Dependencies = { "quantity", "unitprice" }
};
evaluator.RegisterCalculatedField(subtotalDef);

// Second calculated field depends on first: total = subtotal * (1 + taxrate)
var totalDef = new CalculatedFieldDefinition
{
    EntityLogicalName = "invoice",
    AttributeLogicalName = "total",
    Formula = "[subtotal] * (1 + [taxrate])",
    ResultType = typeof(decimal),
    Dependencies = { "subtotal", "taxrate" }
};
evaluator.RegisterCalculatedField(totalDef);

var invoice = new Entity("invoice")
{
    Id = Guid.NewGuid(),
    ["quantity"] = 10,
    ["unitprice"] = 25.00m,
    ["taxrate"] = 0.08m
};

evaluator.EvaluateCalculatedFields(invoice);

// subtotal = 250.00, total = 270.00
Assert.Equal(250.00m, invoice.GetAttributeValue<decimal>("subtotal"));
Assert.Equal(270.00m, invoice.GetAttributeValue<decimal>("total"));
```

### Null Handling

Use ISNULL function to handle null values:

```csharp
// If cost is null, use 0, otherwise use cost
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "product",
    AttributeLogicalName = "safecost",
    Formula = "IF(ISNULL([cost]), 0, [cost])",
    ResultType = typeof(decimal)
};
```

### Type Conversions

Calculated field evaluator automatically converts result types:

```csharp
// Result is converted to Money type
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "invoice",
    AttributeLogicalName = "totalamount",
    Formula = "[quantity] * [unitprice]",
    ResultType = typeof(Money) // Automatically converts decimal to Money
};
```

## Error Handling

### Circular Dependencies

The evaluator detects and prevents circular dependencies:

```csharp
// Field A depends on Field B
var fieldA = new CalculatedFieldDefinition
{
    EntityLogicalName = "entity",
    AttributeLogicalName = "fielda",
    Formula = "[fieldb] * 2",
    ResultType = typeof(int),
    Dependencies = { "fieldb" }
};

// Field B depends on Field A (circular!)
var fieldB = new CalculatedFieldDefinition
{
    EntityLogicalName = "entity",
    AttributeLogicalName = "fieldb",
    Formula = "[fielda] / 2",
    ResultType = typeof(int),
    Dependencies = { "fielda" }
};

evaluator.RegisterCalculatedField(fieldA);
evaluator.RegisterCalculatedField(fieldB);

var entity = new Entity("entity") { Id = Guid.NewGuid() };

// This will throw CircularDependencyException
Assert.Throws<CircularDependencyException>(() => 
    evaluator.EvaluateCalculatedFields(entity));
```

### Invalid Formulas

Invalid formulas throw InvalidOperationException:

```csharp
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "entity",
    AttributeLogicalName = "field",
    Formula = "[nonexistent] * 2", // Field doesn't exist
    ResultType = typeof(int)
};

// Will throw exception during evaluation
```

## Testing Patterns

### Test Calculated Field in Plugin

```csharp
[Fact]
public void Plugin_Should_Use_Calculated_Field()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var evaluator = context.CalculatedFieldEvaluator;
    
    // Register calculated field
    var definition = new CalculatedFieldDefinition
    {
        EntityLogicalName = "opportunity",
        AttributeLogicalName = "weightedrevenue",
        Formula = "[estimatedvalue] * [probability]",
        ResultType = typeof(decimal)
    };
    evaluator.RegisterCalculatedField(definition);
    
    var opportunity = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["probability"] = 0.75m
    };
    context.Initialize(new[] { opportunity });
    
    // Act - Execute plugin that reads calculated field
    context.ExecutePluginWith<MyOpportunityPlugin>(
        pluginContext => {
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 40;
        },
        opportunity
    );
    
    // Assert - Plugin used calculated weighted revenue
    Assert.Equal(75000m, opportunity.GetAttributeValue<decimal>("weightedrevenue"));
}
```

## Limitations

Current limitations in Fake4Dataverse calculated fields:

1. **Related Entity Lookups**: Related entity fields like `[account.name]` are preprocessed but require manual data setup
2. **Metadata Integration**: Calculated field definitions are not automatically loaded from EntityMetadata
3. **Rollup Fields**: Not yet supported (different feature - see Issue #7)
4. **Business Rules**: Not integrated with business rules engine (see Issue #8)

## Best Practices

1. **Define Dependencies**: Always list dependent fields in the `Dependencies` property
2. **Use Descriptive Names**: Name calculated fields clearly (e.g., `totalprice`, `weightedrevenue`)
3. **Test Edge Cases**: Test with null values, zero values, and extreme values
4. **Document Formulas**: Add comments explaining complex formula logic
5. **Validate Types**: Ensure `ResultType` matches the formula result type

## Related Features

- [CRUD Operations](./crud-operations.md) - Basic entity operations
- [Querying Data](./querying-data.md) - Retrieving entities with calculated fields
- [Testing Plugins](./testing-plugins.md) - Plugin testing with calculated fields

## References

- [Microsoft: Define Calculated Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields)
- [Microsoft: Types of Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields)
- [NCalc Expression Evaluator](https://github.com/ncalc/ncalc)
