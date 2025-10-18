# Frequently Asked Questions (FAQ)

Common questions and answers about Fake4Dataverse.

## General Questions

### What is Fake4Dataverse?

Fake4Dataverse is a testing framework for Microsoft Dataverse (formerly Dynamics 365 / Dynamics CRM). It allows you to write unit tests for plugins, custom workflow activities, and other code without needing a live CRM instance.

### Is it free?

Yes! Fake4Dataverse is completely free and open-source under the MIT License.

### How is it different from FakeXrmEasy?

Fake4Dataverse is a fork of the last MIT-licensed version of FakeXrmEasy (v2.0.1). FakeXrmEasy later moved to a commercial licensing model. Fake4Dataverse continues the open-source development of the framework.

### Can I use it in production?

Fake4Dataverse is designed for **testing only**. It simulates Dataverse in-memory and should never be used in production code. Use it in your test projects to test plugins, workflows, and other business logic.

### Which Dataverse versions are supported?

Fake4Dataverse supports:
- Dynamics 365 / Dataverse (v9 and later)

> **Note**: This fork only supports Dynamics 365 v9+. For earlier versions (2011-2016, v8.x), please use the original FakeXrmEasy packages.

## Installation & Setup

### Which package should I install?

For Dynamics 365 / Dataverse v9+:
```bash
dotnet add package Fake4Dataverse.9
```

> **Note**: This fork only supports v9+. See the [Installation Guide](./installation.md) for more details.

### Do I need other packages?

For plugin testing, also install:
```bash
dotnet add package Fake4Dataverse.Plugins
```

### How do I enable early-bound entities?

```csharp
var context = XrmFakedContextFactory.New();
context.EnableProxyTypes(typeof(Account).Assembly);
```

### Can I use it with NUnit or MSTest?

Yes! While examples use xUnit, Fake4Dataverse works with any .NET test framework.

## Usage Questions

### How do I create a test?

```csharp
[Fact]
public void My_First_Test()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Act
    var id = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Assert
    Assert.NotEqual(Guid.Empty, id);
}
```

See the [Quick Start Guide](./getting-started/quickstart.md) for more.

### How do I initialize test data?

```csharp
context.Initialize(new[]
{
    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
    new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "John" }
});
```

### How do I test a plugin?

```csharp
context.ExecutePluginWith<MyPlugin>(
    pluginContext =>
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 20; // Pre-operation
    },
    targetEntity
);
```

See [Testing Plugins](./usage/testing-plugins.md) for complete guide.

### How do I query data?

Using LINQ:
```csharp
var accounts = context.CreateQuery("account")
    .Where(a => ((string)a["name"]).StartsWith("Con"))
    .ToList();
```

See [Querying Data](./usage/querying-data.md) for more examples.

### How do I test security/permissions?

```csharp
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
```

See [Security & Permissions](./usage/security-permissions.md).

## Features & Limitations

### What features are supported?

✅ Supported:
- CRUD operations
- LINQ queries
- FetchXML queries
- Plugin execution
- Message executors (43+)
- Relationships (Associate/Disassociate)
- ExecuteMultiple
- ExecuteTransaction
- Basic security

See the [feature comparison](../README.md#feature-comparison) for details.

### What features are NOT supported?

❌ Not supported:
- Business rules (platform-level)
- Cloud flows
- Custom APIs (platform-level)
- JavaScript/web resources
- Canvas apps
- Real async plugin execution
- Some advanced metadata operations

### Can I test FetchXML queries?

Yes! FetchXML queries are fully supported:

```csharp
var fetchXml = @"
    <fetch>
        <entity name='account'>
            <attribute name='name' />
            <filter>
                <condition attribute='revenue' operator='gt' value='1000000' />
            </filter>
        </entity>
    </fetch>";

var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

### Can I test calculated/rollup fields?

No, calculated and rollup fields are not automatically computed. You'll need to mock their values in your test data.

### Do async plugins run asynchronously?

No, async plugins run synchronously in tests. There's no real async infrastructure, so they execute immediately.

## Troubleshooting

### I get "PullRequestException" - what does this mean?

This means you're trying to use a feature that's not yet implemented. Options:
1. Check if it's really needed for your test
2. Implement a custom message executor
3. Open an issue to request the feature
4. Contribute by submitting a PR

### Test fails with "Entity not found"

Make sure you initialize the entity before querying:

```csharp
// ✅ Initialize first
context.Initialize(myEntity);
var retrieved = service.Retrieve("account", myEntity.Id, new ColumnSet(true));
```

### Plugin doesn't execute

Install the Plugins package:
```bash
dotnet add package Fake4Dataverse.Plugins
```

### Type casting fails

Check the attribute type:
```csharp
// ❌ Wrong
var value = (string)entity["revenue"];

// ✅ Correct
var value = ((Money)entity["revenue"]).Value;
```

### Early-bound entities don't work

Enable proxy types:
```csharp
context.EnableProxyTypes(typeof(Account).Assembly);
```

### Query returns no results

Check what's actually in the context:
```csharp
var all = context.CreateQuery("account").ToList();
// Debug: what's in there?
```

## Performance

### Is Fake4Dataverse fast?

Yes! Everything runs in-memory with no network or database calls. Tests typically run in milliseconds.

### How many entities can I test with?

In-memory storage can handle thousands of entities. If performance becomes an issue, consider:
- Testing with smaller datasets
- Creating data only for specific tests
- Using fresh contexts per test

### Should I share a context between tests?

Generally no - each test should have its own context for isolation. However, for performance, you can share setup data using test fixtures.

## Best Practices

### Should I test Dataverse platform behavior?

No, test YOUR code, not the platform:

```csharp
// ❌ Bad - testing the platform
[Fact]
public void Should_Create_Record_When_ServiceCreateIsCalled()

// ✅ Good - testing your plugin
[Fact]
public void Should_SetDefaultValue_When_AccountCreated()
```

### How many assertions per test?

One logical assertion per test. Test one behavior at a time:

```csharp
// ✅ Good - tests one thing
[Fact]
public void Should_SetCreditLimit_Based_OnRevenue()
{
    // Test credit limit calculation
}

// ✅ Good - separate test for different behavior
[Fact]
public void Should_SetAccountNumber_When_Created()
{
    // Test account number generation
}
```

### Should I use early-bound or late-bound entities?

Both work. Choose based on your production code:
- If your plugin uses early-bound → use early-bound in tests
- If your plugin uses late-bound → use late-bound in tests
- Consistency with production code is most important

## Migration

### How do I migrate from FakeXrmEasy v1?

See the [Migration Guide from v1](./migration/from-v1.md).

### How do I migrate from FakeXrmEasy v3?

See the [Migration Guide from v3](./migration/from-v3.md).

Note: v3 is a commercial version released after the license change. Some v3-specific features may not be available.

## Contributing

### How can I contribute?

Ways to contribute:
- Report bugs
- Request features
- Submit pull requests
- Improve documentation
- Share examples

See the [Contributing Guide](../README.md#contributing).

### How do I add support for a new message?

See [Custom Message Executors](./api/custom-executors.md).

### Can I add my own middleware?

Yes! See [Middleware Architecture](./concepts/middleware.md).

## Additional Features

### Can I test Power Automate Cloud Flows?

Yes! - Fake4Dataverse now supports Cloud Flow simulation including:
- Registering flows programmatically
- **Importing real Power Automate flows from exported JSON** (new feature!)
- Automatic triggering on CRUD operations
- Verifying flow execution and results
- Mocking external connectors

See the [Cloud Flows Guide](../usage/cloud-flows.md) for complete documentation.

### Can I test Custom APIs?

Yes! See the [Custom API Guide](../usage/custom-api.md) for details.

### Can I test plugins?

Yes! See the [Plugin Testing Guide](../usage/testing-plugins.md) for comprehensive examples.

## Getting Help

### Where can I find examples?

- [Quick Start Guide](./getting-started/quickstart.md)
- [Usage Guides](./usage/)
- [Test suite in the repository](https://github.com/rnwood/Fake4Dataverse/tree/main/Fake4DataverseCore/tests)

### Where can I ask questions?

- Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
- Check existing documentation
- Look at test examples in the repository

### How do I report a bug?

1. Check if it's already reported
2. Create a minimal test case that reproduces the issue
3. Open an issue with:
   - Description of the problem
   - Code to reproduce
   - Expected vs actual behavior
   - Package versions

### Is there commercial support?

No, this is a community-maintained project. However, the community is responsive and welcoming to questions and contributions.

## Additional Resources

- [Main Documentation](./README.md)
- [GitHub Repository](https://github.com/rnwood/Fake4Dataverse)
- [NuGet Packages](https://www.nuget.org/packages?q=Fake4Dataverse)
- [Microsoft Dataverse Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/)
