# Hierarchical Query Operators

## Overview

Hierarchical query operators allow you to query parent-child relationships in entity hierarchies. This is essential for working with organizational structures, account hierarchies, and other tree-like data structures in Dataverse.

**Implemented:** 2025-10-10 (Issue #2)

## Microsoft Documentation

Official reference: [ConditionOperator Enum](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator)

## Supported Operators

| Operator | Description |
|----------|-------------|
| `Above` | Returns all records above the specified record in the hierarchy |
| `AboveOrEqual` | Returns the specified record and all records above it |
| `Under` | Returns all records under the specified record in the hierarchy |
| `UnderOrEqual` | Returns the specified record and all records under it |
| `ChildOf` | Returns direct children of the specified record |

## Usage

### Setup: Account Hierarchy

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Create a hierarchy:
//   Contoso Corp (root)
//   ├── North America Division
//   │   ├── US Office
//   │   └── Canada Office
//   └── Europe Division
//       └── UK Office

var contosoId = Guid.NewGuid();
var northAmericaId = Guid.NewGuid();
var europeId = Guid.NewGuid();
var usOfficeId = Guid.NewGuid();
var canadaOfficeId = Guid.NewGuid();
var ukOfficeId = Guid.NewGuid();

var contoso = new Entity("account")
{
    Id = contosoId,
    ["name"] = "Contoso Corp",
    ["parentaccountid"] = null
};

var northAmerica = new Entity("account")
{
    Id = northAmericaId,
    ["name"] = "North America Division",
    ["parentaccountid"] = new EntityReference("account", contosoId)
};

var europe = new Entity("account")
{
    Id = europeId,
    ["name"] = "Europe Division",
    ["parentaccountid"] = new EntityReference("account", contosoId)
};

var usOffice = new Entity("account")
{
    Id = usOfficeId,
    ["name"] = "US Office",
    ["parentaccountid"] = new EntityReference("account", northAmericaId)
};

var canadaOffice = new Entity("account")
{
    Id = canadaOfficeId,
    ["name"] = "Canada Office",
    ["parentaccountid"] = new EntityReference("account", northAmericaId)
};

var ukOffice = new Entity("account")
{
    Id = ukOfficeId,
    ["name"] = "UK Office",
    ["parentaccountid"] = new EntityReference("account", europeId)
};

context.Initialize(new[] { contoso, northAmerica, europe, usOffice, canadaOffice, ukOffice });
```

### Above Operator

Returns all records **above** the specified record in the hierarchy (ancestors):

```csharp
// Find all accounts above US Office
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.Above, usOfficeId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: North America Division, Contoso Corp
```

### AboveOrEqual Operator

Returns the specified record **and** all records above it:

```csharp
// Find US Office and all parent accounts
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.AboveOrEqual, usOfficeId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: US Office, North America Division, Contoso Corp
```

### Under Operator

Returns all records **under** the specified record in the hierarchy (descendants):

```csharp
// Find all accounts under North America Division
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.Under, northAmericaId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: US Office, Canada Office
```

### UnderOrEqual Operator

Returns the specified record **and** all records under it:

```csharp
// Find Contoso Corp and all subsidiary accounts
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.UnderOrEqual, contosoId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: Contoso Corp, North America Division, Europe Division, US Office, Canada Office, UK Office
```

### ChildOf Operator

Returns **direct children only** (not all descendants):

```csharp
// Find direct children of Contoso Corp
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.ChildOf, contosoId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: North America Division, Europe Division (direct children only)
```

## FetchXML Support

Hierarchical operators are also supported in FetchXML queries:

### Above in FetchXML
```csharp
var fetchXml = $@"
<fetch>
    <entity name='account'>
        <attribute name='name' />
        <filter>
            <condition attribute='accountid' operator='above' value='{usOfficeId}' />
        </filter>
    </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

### Under in FetchXML
```csharp
var fetchXml = $@"
<fetch>
    <entity name='account'>
        <attribute name='name' />
        <filter>
            <condition attribute='accountid' operator='under' value='{contosoId}' />
        </filter>
    </entity>
</fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

## Advanced Scenarios

### Combining with Other Conditions

```csharp
// Find all active accounts under North America Division
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name", "statecode"),
    Criteria = new FilterExpression
    {
        FilterOperator = LogicalOperator.And,
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.UnderOrEqual, northAmericaId),
            new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Active
        }
    }
};

var results = service.RetrieveMultiple(query);
```

### Multiple Hierarchical Conditions

```csharp
// Find accounts that are either under North America OR under Europe
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        FilterOperator = LogicalOperator.Or,
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.Under, northAmericaId),
            new ConditionExpression("accountid", ConditionOperator.Under, europeId)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: US Office, Canada Office, UK Office
```

### Finding Orphaned Records

```csharp
// Find accounts with no parent (root accounts)
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("parentaccountid", ConditionOperator.Null)
        }
    }
};

var results = service.RetrieveMultiple(query);
// Returns: Contoso Corp (and any other root accounts)
```

## Important Considerations

### Self-Referencing Relationships

The hierarchical operators work with self-referencing relationships where an entity has a lookup to itself:

- For `account` entity: `parentaccountid` field
- For custom entities: Define a self-referencing lookup field

### Circular References

The framework detects and prevents circular references to avoid infinite loops:

```csharp
// This would create a circular reference and should be prevented
account1["parentaccountid"] = new EntityReference("account", account2.Id);
account2["parentaccountid"] = new EntityReference("account", account1.Id);
```

### Performance

Hierarchical queries traverse relationships recursively, which can be expensive for deep hierarchies:

- Use `ChildOf` when you only need direct children
- Consider adding depth limits for very deep hierarchies
- Test with realistic data volumes

## Common Use Cases

### Organizational Reporting Structure

```csharp
// Get all employees reporting to a manager (direct and indirect)
var query = new QueryExpression("systemuser")
{
    ColumnSet = new ColumnSet("fullname", "title"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("systemuserid", ConditionOperator.Under, managerId)
        }
    }
};

var subordinates = service.RetrieveMultiple(query);
```

### Account Portfolio Management

```csharp
// Get entire account portfolio including all subsidiaries
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name", "revenue", "numberofemployees"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.UnderOrEqual, corporateAccountId)
        }
    }
};

var portfolio = service.RetrieveMultiple(query);
var totalRevenue = portfolio.Entities.Sum(e => e.GetAttributeValue<Money>("revenue")?.Value ?? 0);
```

### Territory Management

```csharp
// Find all opportunities in a territory and its sub-territories
var query = new QueryExpression("opportunity")
{
    ColumnSet = new ColumnSet("name", "estimatedvalue"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("territoryid", ConditionOperator.UnderOrEqual, territoryId)
        }
    }
};

var opportunities = service.RetrieveMultiple(query);
```

## Error Scenarios

### Invalid Entity Reference

```csharp
var query = new QueryExpression("account")
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("accountid", ConditionOperator.Above, Guid.NewGuid()) // Non-existent
        }
    }
};

// Returns empty result set (no error thrown)
var results = service.RetrieveMultiple(query);
Assert.Empty(results.Entities);
```

### Missing Hierarchy Field

```csharp
// Using hierarchical operator on entity without self-referencing relationship
var query = new QueryExpression("contact") // Contact doesn't have parentcontactid
{
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("contactid", ConditionOperator.Above, contactId)
        }
    }
};

// Behavior depends on metadata configuration
```

## Best Practices

1. **Define Clear Hierarchies**: Ensure your entity has a proper self-referencing lookup field
2. **Test Edge Cases**: Test with root nodes, leaf nodes, and orphaned records
3. **Consider Depth**: Be aware of hierarchy depth for performance
4. **Use Appropriate Operator**: Choose between `Above`/`Under` vs `AboveOrEqual`/`UnderOrEqual` based on needs
5. **Combine Wisely**: Hierarchical operators work well with other conditions

## Related Documentation

- [Microsoft ConditionOperator Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator)
- [Query Expression Guide](../Fake4DataverseCore/README.md#query-expressions)
- [FetchXML Guide](../Fake4DataverseCore/README.md#fetchxml)

## Implementation Details

- **Files**: 
  - `Fake4DataverseCore/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.Hierarchical.cs`
  - `Fake4DataverseCore/Fake4Dataverse.Core/Extensions/XmlExtensionsForFetchXml.cs`
- **Tests**: `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/HierarchicalQueryTests/`
