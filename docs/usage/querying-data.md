# Querying Data

This guide covers how to query data in Fake4Dataverse using LINQ, QueryExpression, and FetchXML.

## Table of Contents
- [LINQ Queries](#linq-queries)
- [QueryExpression](#queryexpression)
- [FetchXML Queries](#fetchxml-queries)
- [Early-Bound vs Late-Bound](#early-bound-vs-late-bound)
- [Query Performance](#query-performance)
- [Best Practices](#best-practices)

## LINQ Queries

LINQ provides a type-safe way to query entities.

### Basic LINQ Query

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;
using System.Linq;

[Fact]
public void Should_Query_Accounts_With_LINQ()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new[]
    {
        new Entity("account") 
        { 
            Id = Guid.NewGuid(),
            ["name"] = "Contoso"
        },
        new Entity("account") 
        { 
            Id = Guid.NewGuid(),
            ["name"] = "Fabrikam"
        }
    });
    
    // Act - Query with LINQ
    var accounts = context.CreateQuery("account")
        .Where(a => ((string)a["name"]).StartsWith("Con"))
        .ToList();
    
    // Assert
    Assert.Single(accounts);
    Assert.Equal("Contoso", accounts[0]["name"]);
}
```

### Filtering by Attributes

```csharp
[Fact]
public void Should_Filter_By_Revenue()
{
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "High Revenue",
            ["revenue"] = new Money(2000000)
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Low Revenue",
            ["revenue"] = new Money(500000)
        }
    });
    
    var highRevenueAccounts = context.CreateQuery("account")
        .Where(a => ((Money)a["revenue"]).Value > 1000000)
        .ToList();
    
    Assert.Single(highRevenueAccounts);
    Assert.Equal("High Revenue", highRevenueAccounts[0]["name"]);
}
```

### Filtering by OptionSet Values

```csharp
[Fact]
public void Should_Filter_By_OptionSet()
{
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Active Account",
            ["statecode"] = new OptionSetValue(0) // Active
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Inactive Account",
            ["statecode"] = new OptionSetValue(1) // Inactive
        }
    });
    
    var activeAccounts = context.CreateQuery("account")
        .Where(a => ((OptionSetValue)a["statecode"]).Value == 0)
        .ToList();
    
    Assert.Single(activeAccounts);
    Assert.Equal("Active Account", activeAccounts[0]["name"]);
}
```

### Filtering by Entity Reference

```csharp
[Fact]
public void Should_Filter_By_Lookup()
{
    var context = XrmFakedContextFactory.New();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Parent Account" },
        new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = "John",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        },
        new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = "Jane",
            ["parentcustomerid"] = new EntityReference("account", Guid.NewGuid())
        }
    });
    
    var contacts = context.CreateQuery("contact")
        .Where(c => ((EntityReference)c["parentcustomerid"]).Id == accountId)
        .ToList();
    
    Assert.Single(contacts);
    Assert.Equal("John", contacts[0]["firstname"]);
}
```

### Ordering Results

```csharp
[Fact]
public void Should_Order_Results()
{
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Zebra" },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Apple" },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Microsoft" }
    });
    
    // Order by name ascending
    var accounts = context.CreateQuery("account")
        .OrderBy(a => (string)a["name"])
        .ToList();
    
    Assert.Equal("Apple", accounts[0]["name"]);
    Assert.Equal("Microsoft", accounts[1]["name"]);
    Assert.Equal("Zebra", accounts[2]["name"]);
    
    // Order by name descending
    var accountsDesc = context.CreateQuery("account")
        .OrderByDescending(a => (string)a["name"])
        .ToList();
    
    Assert.Equal("Zebra", accountsDesc[0]["name"]);
}
```

### Pagination

```csharp
[Fact]
public void Should_Support_Pagination()
{
    var context = XrmFakedContextFactory.New();
    
    // Create 10 accounts
    var accounts = Enumerable.Range(1, 10)
        .Select(i => new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = $"Account {i}"
        })
        .ToArray();
    
    context.Initialize(accounts);
    
    // Get first 5
    var page1 = context.CreateQuery("account")
        .OrderBy(a => (string)a["name"])
        .Take(5)
        .ToList();
    
    Assert.Equal(5, page1.Count);
    
    // Get next 5
    var page2 = context.CreateQuery("account")
        .OrderBy(a => (string)a["name"])
        .Skip(5)
        .Take(5)
        .ToList();
    
    Assert.Equal(5, page2.Count);
}
```

## QueryExpression

QueryExpression provides a programmatic way to build queries.

### Basic QueryExpression

```csharp
using Microsoft.Xrm.Sdk.Query;

[Fact]
public void Should_Query_With_QueryExpression()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Contoso" },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Fabrikam" }
    });
    
    var query = new QueryExpression("account")
    {
        ColumnSet = new ColumnSet("name")
    };
    
    var results = service.RetrieveMultiple(query);
    
    Assert.Equal(2, results.Entities.Count);
}
```

### QueryExpression with Filters

```csharp
[Fact]
public void Should_Filter_With_QueryExpression()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Contoso",
            ["revenue"] = new Money(2000000)
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Fabrikam",
            ["revenue"] = new Money(500000)
        }
    });
    
    var query = new QueryExpression("account")
    {
        ColumnSet = new ColumnSet("name", "revenue"),
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("revenue", ConditionOperator.GreaterThan, 1000000)
            }
        }
    };
    
    var results = service.RetrieveMultiple(query);
    
    Assert.Single(results.Entities);
    Assert.Equal("Contoso", results.Entities[0]["name"]);
}
```

### QueryExpression with Multiple Conditions

```csharp
[Fact]
public void Should_Support_Multiple_Conditions()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Contoso",
            ["revenue"] = new Money(2000000),
            ["numberofemployees"] = 500
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Fabrikam",
            ["revenue"] = new Money(1500000),
            ["numberofemployees"] = 100
        }
    });
    
    var query = new QueryExpression("account")
    {
        ColumnSet = new ColumnSet(true),
        Criteria = new FilterExpression
        {
            FilterOperator = LogicalOperator.And,
            Conditions =
            {
                new ConditionExpression("revenue", ConditionOperator.GreaterThan, 1000000),
                new ConditionExpression("numberofemployees", ConditionOperator.GreaterThan, 200)
            }
        }
    };
    
    var results = service.RetrieveMultiple(query);
    
    Assert.Single(results.Entities);
    Assert.Equal("Contoso", results.Entities[0]["name"]);
}
```

### QueryExpression with Linked Entities (Joins)

```csharp
[Fact]
public void Should_Support_Linked_Entities()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = accountId,
            ["name"] = "Contoso"
        },
        new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = "John",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        }
    });
    
    var query = new QueryExpression("account")
    {
        ColumnSet = new ColumnSet("name"),
        LinkEntities =
        {
            new LinkEntity
            {
                LinkFromEntityName = "account",
                LinkToEntityName = "contact",
                LinkFromAttributeName = "accountid",
                LinkToAttributeName = "parentcustomerid",
                Columns = new ColumnSet("firstname"),
                EntityAlias = "contact"
            }
        }
    };
    
    var results = service.RetrieveMultiple(query);
    
    Assert.Single(results.Entities);
    var account = results.Entities[0];
    Assert.Equal("Contoso", account["name"]);
    Assert.Equal("John", account.GetAttributeValue<AliasedValue>("contact.firstname").Value);
}
```

## FetchXML Queries

FetchXML is an XML-based query language for Dataverse.

### Basic FetchXML

```csharp
using Microsoft.Xrm.Sdk.Query;

[Fact]
public void Should_Query_With_FetchXML()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Contoso" },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Fabrikam" }
    });
    
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
    
    Assert.Equal(2, results.Entities.Count);
}
```

### FetchXML with Filters

```csharp
[Fact]
public void Should_Filter_With_FetchXML()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Contoso",
            ["revenue"] = new Money(2000000)
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Fabrikam",
            ["revenue"] = new Money(500000)
        }
    });
    
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
                <attribute name='revenue' />
                <filter>
                    <condition attribute='revenue' operator='gt' value='1000000' />
                </filter>
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
    
    Assert.Single(results.Entities);
    Assert.Equal("Contoso", results.Entities[0]["name"]);
}
```

### FetchXML with Aggregation

```csharp
[Fact]
public void Should_Support_Aggregation()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Account 1",
            ["revenue"] = new Money(1000000)
        },
        new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Account 2",
            ["revenue"] = new Money(2000000)
        }
    });
    
    var fetchXml = @"
        <fetch aggregate='true'>
            <entity name='account'>
                <attribute name='revenue' aggregate='sum' alias='total_revenue' />
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
    
    Assert.Single(results.Entities);
    var totalRevenue = (Money)((AliasedValue)results.Entities[0]["total_revenue"]).Value;
    Assert.Equal(3000000m, totalRevenue.Value);
}
```

### FetchXML with Linked Entities

```csharp
[Fact]
public void Should_Support_FetchXML_Joins()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = accountId,
            ["name"] = "Contoso"
        },
        new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = "John",
            ["parentcustomerid"] = new EntityReference("account", accountId)
        }
    });
    
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
                <link-entity name='contact' from='parentcustomerid' to='accountid' alias='contact'>
                    <attribute name='firstname' />
                </link-entity>
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
    
    Assert.Single(results.Entities);
    var account = results.Entities[0];
    Assert.Equal("Contoso", account["name"]);
    Assert.Equal("John", account.GetAttributeValue<AliasedValue>("contact.firstname").Value);
}
```

## Early-Bound vs Late-Bound

### Late-Bound Queries (Dynamic)

```csharp
[Fact]
public void Should_Query_Late_Bound()
{
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso"
    });
    
    // Late-bound - using string entity name
    var accounts = context.CreateQuery("account")
        .Where(a => ((string)a["name"]) == "Contoso")
        .ToList();
    
    Assert.Single(accounts);
}
```

### Early-Bound Queries (Strongly-Typed)

```csharp
[Fact]
public void Should_Query_Early_Bound()
{
    var context = XrmFakedContextFactory.New();
    
    // Enable early-bound entities
    context.EnableProxyTypes(typeof(Account).Assembly);
    
    var account = new Account
    {
        Id = Guid.NewGuid(),
        Name = "Contoso"
    };
    
    context.Initialize(account);
    
    // Early-bound - using strongly-typed class
    var accounts = context.CreateQuery<Account>()
        .Where(a => a.Name == "Contoso")
        .ToList();
    
    Assert.Single(accounts);
    Assert.Equal("Contoso", accounts[0].Name);
}
```

## Query Performance

### Efficient Queries

```csharp
// ✅ Good - specific columns
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet("name", "revenue")
};

// ✅ Good - filter early
var accounts = context.CreateQuery("account")
    .Where(a => ((string)a["name"]).StartsWith("Con"))
    .Take(10)
    .ToList();
```

### Inefficient Queries

```csharp
// ❌ Avoid - retrieves all columns
var query = new QueryExpression("account")
{
    ColumnSet = new ColumnSet(true)
};

// ❌ Avoid - filters in memory after retrieving all
var allAccounts = context.CreateQuery("account").ToList();
var filtered = allAccounts.Where(a => a.GetAttributeValue<string>("name").StartsWith("Con"));
```

## Best Practices

### ✅ Do

1. **Use specific column sets**
   ```csharp
   new ColumnSet("name", "revenue") // ✅ Good
   ```

2. **Filter in the query, not in memory**
   ```csharp
   context.CreateQuery("account")
       .Where(a => condition) // ✅ Good - filtered in query
       .ToList();
   ```

3. **Use early-bound entities when possible**
   ```csharp
   context.CreateQuery<Account>()
       .Where(a => a.Name == "Contoso") // ✅ Type-safe
   ```

### ❌ Don't

1. **Don't retrieve all columns unnecessarily**
   ```csharp
   new ColumnSet(true) // ❌ Avoid in production
   ```

2. **Don't filter after retrieval**
   ```csharp
   var all = context.CreateQuery("account").ToList();
   var filtered = all.Where(x => condition); // ❌ Inefficient
   ```

## Next Steps

- [CRUD Operations](./crud-operations.md) - Create, update, delete data
- [Batch Operations](./batch-operations.md) - ExecuteMultiple for bulk queries
- [Testing Plugins](./testing-plugins.md) - Test plugins that query data
- [Data Management](../concepts/data-management.md) - Managing test data

## See Also

- [QueryExpression Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/queryexpression/overview)
- [FetchXML Reference](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/fetchxml/overview)
