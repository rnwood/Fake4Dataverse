# Quick Start Guide

Get up and running with Fake4Dataverse in 5 minutes! This guide shows you how to write your first test.

## Your First Test in 5 Minutes

### 1. Install Fake4Dataverse

```bash
dotnet add package Fake4Dataverse.9
```

### 2. Write a Simple CRUD Test

Create a test file `AccountTests.cs`:

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using System;
using System.Linq;

namespace MyProject.Tests
{
    public class AccountTests
    {
        [Fact]
        public void Should_Create_And_Retrieve_Account()
        {
            // Arrange - Set up the fake Dataverse context
            var context = XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            
            // Act - Create an account
            var account = new Entity("account")
            {
                ["name"] = "Contoso Ltd",
                ["revenue"] = new Money(1000000)
            };
            var accountId = service.Create(account);
            
            // Assert - Verify the account was created
            var retrieved = service.Retrieve("account", accountId, 
                new ColumnSet("name", "revenue"));
            
            Assert.Equal("Contoso Ltd", retrieved["name"]);
            Assert.Equal(1000000m, ((Money)retrieved["revenue"]).Value);
        }
    }
}
```

### 3. Run Your Test

```bash
dotnet test
```

That's it! You've written and run your first Fake4Dataverse test. 🎉

## Common Testing Patterns

### Pattern 1: Initialize with Test Data

Pre-populate the context with test data before running your tests:

```csharp
[Fact]
public void Should_Update_Account_Owner()
{
    // Arrange - Set up context with existing data
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var oldOwnerId = Guid.NewGuid();
    var newOwnerId = Guid.NewGuid();
    
    // Initialize with test data
    context.Initialize(new[]
    {
        new Entity("account")
        {
            Id = accountId,
            ["name"] = "Test Account",
            ["ownerid"] = new EntityReference("systemuser", oldOwnerId)
        },
        new Entity("systemuser") { Id = oldOwnerId },
        new Entity("systemuser") { Id = newOwnerId }
    });
    
    // Act - Update the owner
    var account = new Entity("account")
    {
        Id = accountId,
        ["ownerid"] = new EntityReference("systemuser", newOwnerId)
    };
    service.Update(account);
    
    // Assert - Verify owner changed
    var updated = service.Retrieve("account", accountId, 
        new ColumnSet("ownerid"));
    Assert.Equal(newOwnerId, ((EntityReference)updated["ownerid"]).Id);
}
```

### Pattern 2: Query Test Data with LINQ

Use LINQ to query your test data:

```csharp
[Fact]
public void Should_Find_Active_Accounts()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
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
    
    // Act - Query using LINQ
    var activeAccounts = (from a in context.CreateQuery("account")
                         where ((OptionSetValue)a["statecode"]).Value == 0
                         select a).ToList();
    
    // Assert
    Assert.Single(activeAccounts);
    Assert.Equal("Active Account", activeAccounts[0]["name"]);
}
```

### Pattern 3: Test Plugin Execution

Test your plugin code:

```csharp
using Fake4Dataverse.Plugins;

[Fact]
public void Should_Execute_Plugin_On_Create()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        ["name"] = "Test Account"
    };
    
    // Act - Execute plugin
    context.ExecutePluginWith<MyAccountPlugin>(
        pluginContext => 
        {
            pluginContext.MessageName = "Create";
            pluginContext.Stage = 20; // Pre-operation
        },
        account
    );
    
    // Assert - Check plugin effect
    // (e.g., plugin sets a default value)
    Assert.NotNull(account["accountnumber"]);
}
```

## Testing Different Operations

### Create Operation

```csharp
var entity = new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
};
var id = service.Create(entity);
```

### Retrieve Operation

```csharp
var entity = service.Retrieve("contact", contactId, 
    new ColumnSet("firstname", "lastname"));
```

### Update Operation

```csharp
var entity = new Entity("contact")
{
    Id = contactId,
    ["lastname"] = "Smith"
};
service.Update(entity);
```

### Delete Operation

```csharp
service.Delete("contact", contactId);
```

### Associate/Disassociate

```csharp
// Associate
service.Associate(
    "contact", 
    contactId,
    new Relationship("contact_account_customer"),
    new EntityReferenceCollection { 
        new EntityReference("account", accountId) 
    }
);

// Disassociate
service.Disassociate(
    "contact",
    contactId,
    new Relationship("contact_account_customer"),
    new EntityReferenceCollection { 
        new EntityReference("account", accountId) 
    }
);
```

## Using FetchXML

Query with FetchXML instead of LINQ:

```csharp
[Fact]
public void Should_Query_With_FetchXml()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new[]
    {
        new Entity("account") 
        { 
            Id = Guid.NewGuid(),
            ["name"] = "Contoso",
            ["revenue"] = new Money(1000000)
        }
    });
    
    // Act - Query with FetchXML
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
                <attribute name='revenue' />
                <filter>
                    <condition attribute='revenue' 
                               operator='gt' 
                               value='500000' />
                </filter>
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(
        new FetchExpression(fetchXml));
    
    // Assert
    Assert.Single(results.Entities);
    Assert.Equal("Contoso", results.Entities[0]["name"]);
}
```

## Testing with Early-Bound Entities

If you use early-bound generated entity classes:

```csharp
[Fact]
public void Should_Work_With_Early_Bound_Entities()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    
    // Enable early-bound entities
    context.EnableProxyTypes(typeof(Account).Assembly);
    
    var service = context.GetOrganizationService();
    
    // Act - Use strongly-typed entities
    var account = new Account
    {
        Id = Guid.NewGuid(),
        Name = "Contoso Ltd",
        Revenue = new Money(1000000)
    };
    
    context.Initialize(account);
    
    // Query with LINQ using early-bound types
    var accounts = (from a in context.CreateQuery<Account>()
                   where a.Revenue.Value > 500000
                   select a).ToList();
    
    // Assert
    Assert.Single(accounts);
    Assert.Equal("Contoso Ltd", accounts[0].Name);
}
```

## Best Practices

### ✅ Do
- **Arrange-Act-Assert**: Structure tests clearly with these three sections
- **Initialize data**: Use `context.Initialize()` to set up test data
- **Test one thing**: Each test should verify a single behavior
- **Use meaningful names**: Name tests to describe what they verify
- **Clean tests**: Keep tests simple and readable

### ❌ Don't
- **Test multiple things**: Avoid cramming multiple assertions into one test
- **Depend on test order**: Tests should be independent
- **Use real GUIDs in asserts**: Use the GUIDs you created in your test
- **Skip assertions**: Always verify your expectations

## Next Steps

Now that you've written your first tests, explore these topics:

- **[Basic Concepts](./basic-concepts.md)** - Understand how the framework works
- **[Testing Plugins](../usage/testing-plugins.md)** - Dive deep into plugin testing
- **[Querying Data](../usage/querying-data.md)** - Master LINQ and FetchXML queries
- **[Cloud Flows](../usage/cloud-flows.md)** - Test Power Automate flows (with JSON import!) ✅ **NEW**
- **[CRUD Operations](../usage/crud-operations.md)** - Learn all CRUD patterns
- **[Batch Operations](../usage/batch-operations.md)** - Test ExecuteMultiple and transactions
- **[Custom API](../usage/custom-api.md)** - Test Custom APIs

### Advanced Features

Explore advanced testing scenarios:
- **Pipeline Simulation** - Test plugins with full pipeline behavior  
- **Security Testing** - Test security roles and permissions
- **JSON Import for Flows** - Import real Power Automate flows and test them ✅ **NEW**
- **Message Executors** - See all [supported Dataverse messages](../messages/)

### Need Help?

- Check the [FAQ](./faq.md) for common questions
- Browse the [API Reference](../api/) for detailed documentation

## Common Questions

### Q: Can I test real CRM plugins?
**A**: Yes! That's what Fake4Dataverse is designed for. See [Testing Plugins](../usage/testing-plugins.md).

### Q: Do I need a CRM instance?
**A**: No! Everything runs in-memory. That's the whole point. 🚀

### Q: Can I use this with NUnit or MSTest?
**A**: Yes! While examples use xUnit, Fake4Dataverse works with any .NET test framework.

### Q: What about asynchronous plugins?
**A**: Async plugins work, but execute synchronously in tests. See [Testing Plugins](../usage/testing-plugins.md#async-plugins).

### Q: How do I test security?
**A**: Check out [Security and Permissions](../usage/security-permissions.md).

## Troubleshooting

### Test Fails with "PullRequestException"
This means you're using a feature that's not yet implemented. Check the [Feature Comparison](../../README.md#feature-comparison) or open an issue.

### "Entity not found" Errors
Make sure you've initialized the context with the entity before querying:
```csharp
context.Initialize(myEntity);
```

### Plugin Not Executing
Ensure you've installed `Fake4Dataverse.Plugins` package and are using `ExecutePluginWith<>()`.

## Get Help

- **Documentation**: [Full documentation index](../README.md)
- **Examples**: Browse tests in the [GitHub repository](https://github.com/rnwood/Fake4Dataverse/tree/main/Fake4DataverseCore/tests)
- **Issues**: [Report bugs or request features](https://github.com/rnwood/Fake4Dataverse/issues)
