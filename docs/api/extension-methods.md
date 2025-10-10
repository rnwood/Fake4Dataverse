# Extension Methods

Fake4Dataverse provides extension methods to simplify common testing operations. These methods make your tests more readable and reduce boilerplate code.

## Table of Contents

- [Overview](#overview)
- [Context Extensions](#context-extensions)
- [Entity Extensions](#entity-extensions)
- [Plugin Testing Extensions](#plugin-testing-extensions)
- [Query Extensions](#query-extensions)
- [Complete Examples](#complete-examples)
- [See Also](#see-also)

## Overview

Extension methods in Fake4Dataverse extend:
- `IXrmFakedContext` - Context operations
- `Entity` - Entity manipulation
- `IOrganizationService` - Service operations
- Plugin execution - Simplified plugin testing

## Context Extensions

### ExecutePluginWith<T>()

Executes a plugin with a configured execution context.

**Namespace:** `Fake4Dataverse.Plugins`

```csharp
using Fake4Dataverse.Plugins;

[Fact]
public void Should_Execute_Plugin()
{
    var context = XrmFakedContextFactory.New();
    var account = new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" };
    context.Initialize(account);
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 20; // Pre-operation
        },
        account
    );
}
```

**Parameters:**
- `configureContext` (Action<XrmFakedPluginExecutionContext>) - Configure the plugin execution context
- `targetEntity` (Entity) - The entity being operated on

### ExecutePluginWithTarget<T>()

Executes a plugin with a target entity and optional input parameters.

```csharp
context.ExecutePluginWithTarget<MyPlugin>(
    targetEntity,
    pluginContext =>
    {
        pluginContext.MessageName = "Create";
        pluginContext.Stage = 40; // Post-operation
        pluginContext.InputParameters["Target"] = targetEntity;
    }
);
```

## Entity Extensions

### GetAttributeValue<T>()

Gets a strongly-typed attribute value from an entity.

**Built into SDK:** `Microsoft.Xrm.Sdk`

```csharp
var entity = new Entity("account");
entity["name"] = "Contoso";
entity["revenue"] = new Money(100000);
entity["createdon"] = DateTime.Now;

// Get typed values
string name = entity.GetAttributeValue<string>("name");
Money revenue = entity.GetAttributeValue<Money>("revenue");
DateTime createdOn = entity.GetAttributeValue<DateTime>("createdon");
```

**Returns:** The attribute value cast to type `T`, or `default(T)` if attribute doesn't exist.

### ToEntity<T>()

Converts a late-bound `Entity` to an early-bound entity.

**Built into SDK:** `Microsoft.Xrm.Sdk`

```csharp
// Enable proxy types first
context.EnableProxyTypes(typeof(Account).Assembly);

var lateEntity = new Entity("account");
lateEntity["name"] = "Test Account";

// Convert to early-bound
Account account = lateEntity.ToEntity<Account>();
Assert.Equal("Test Account", account.Name);
```

### ToEntityReference()

Creates an `EntityReference` from an entity.

**Built into SDK:** `Microsoft.Xrm.Sdk`

```csharp
var entity = new Entity("account") { Id = Guid.NewGuid() };
EntityReference reference = entity.ToEntityReference();

Assert.Equal("account", reference.LogicalName);
Assert.Equal(entity.Id, reference.Id);
```

### Contains()

Checks if an entity contains an attribute.

**Built into SDK:** `Microsoft.Xrm.Sdk`

```csharp
var entity = new Entity("account");
entity["name"] = "Test";

Assert.True(entity.Contains("name"));
Assert.False(entity.Contains("revenue"));
```

## Plugin Testing Extensions

### SetInputParameter()

Helper for setting input parameters on plugin context.

```csharp
public static class PluginContextExtensions
{
    public static void SetInputParameter<T>(
        this IPluginExecutionContext context, 
        string key, 
        T value)
    {
        context.InputParameters[key] = value;
    }
    
    public static T GetInputParameter<T>(
        this IPluginExecutionContext context, 
        string key)
    {
        return (T)context.InputParameters[key];
    }
}

// Usage
[Fact]
public void Should_Use_Input_Parameters()
{
    var context = XrmFakedContextFactory.New();
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Update";
            pluginContext.SetInputParameter("Target", targetEntity);
            pluginContext.SetInputParameter("CustomParam", "CustomValue");
        },
        targetEntity
    );
}
```

### GetPreImage() / GetPostImage()

Helper methods for accessing entity images.

```csharp
public static class PluginImageExtensions
{
    public static Entity GetPreImage(
        this IPluginExecutionContext context, 
        string imageName = "PreImage")
    {
        return context.PreEntityImages.Contains(imageName)
            ? context.PreEntityImages[imageName]
            : null;
    }
    
    public static Entity GetPostImage(
        this IPluginExecutionContext context, 
        string imageName = "PostImage")
    {
        return context.PostEntityImages.Contains(imageName)
            ? context.PostEntityImages[imageName]
            : null;
    }
}

// Usage
[Fact]
public void Should_Access_Entity_Images()
{
    var context = XrmFakedContextFactory.New();
    var preImage = new Entity("account") { ["name"] = "Old Name" };
    var target = new Entity("account") { ["name"] = "New Name" };
    
    context.ExecutePluginWith<MyPlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Update";
            pluginContext.PreEntityImages["PreImage"] = preImage;
        },
        target
    );
}
```

## Query Extensions

### FirstOrDefault() with Predicate

LINQ extension for querying entities.

```csharp
var account = context.CreateQuery("account")
    .FirstOrDefault(a => a.GetAttributeValue<string>("name") == "Test");
```

### Any() / Count()

Check existence or count records.

```csharp
// Check if any accounts exist
bool hasAccounts = context.CreateQuery("account").Any();

// Count active accounts
int activeCount = context.CreateQuery("account")
    .Count(a => a.GetAttributeValue<OptionSetValue>("statecode").Value == 0);
```

### Select() for Projections

Project specific attributes.

```csharp
var accountNames = context.CreateQuery("account")
    .Select(a => a.GetAttributeValue<string>("name"))
    .ToList();
```

## Complete Examples

### Plugin Testing with Extensions

```csharp
using Fake4Dataverse.Plugins;

public class AccountPluginTests
{
    [Fact]
    public void Should_Update_Contact_When_Account_Created()
    {
        // Arrange
        var context = XrmFakedContextFactory.New();
        var service = context.GetOrganizationService();
        
        var contactId = Guid.NewGuid();
        context.Initialize(new Entity("contact")
        {
            Id = contactId,
            ["firstname"] = "John",
            ["lastname"] = "Doe"
        });
        
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account",
            ["primarycontactid"] = new EntityReference("contact", contactId)
        };
        
        // Act
        context.ExecutePluginWith<AccountPlugin>(
            pluginContext =>
            {
                pluginContext.MessageName = "Create";
                pluginContext.Stage = 40;
                pluginContext.InputParameters["Target"] = account;
            },
            account
        );
        
        // Assert
        var contact = service.Retrieve("contact", contactId, new ColumnSet("fullname"));
        Assert.Contains("Test Account", contact.GetAttributeValue<string>("fullname"));
    }
}
```

### Entity Manipulation

```csharp
[Fact]
public void Should_Work_With_Entity_Extensions()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create entity with typed values
    var account = new Entity("account");
    account["name"] = "Contoso";
    account["revenue"] = new Money(100000);
    account["numberofemployees"] = 50;
    
    var accountId = service.Create(account);
    
    // Retrieve and use extensions
    var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));
    
    // GetAttributeValue
    string name = retrieved.GetAttributeValue<string>("name");
    Money revenue = retrieved.GetAttributeValue<Money>("revenue");
    int employees = retrieved.GetAttributeValue<int>("numberofemployees");
    
    // Contains
    Assert.True(retrieved.Contains("name"));
    Assert.False(retrieved.Contains("description"));
    
    // ToEntityReference
    EntityReference reference = retrieved.ToEntityReference();
    Assert.Equal("account", reference.LogicalName);
    Assert.Equal(accountId, reference.Id);
}
```

### Query Extensions

```csharp
[Fact]
public void Should_Use_Query_Extensions()
{
    var context = XrmFakedContextFactory.New();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "A Corp", ["revenue"] = new Money(100000) },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "B Corp", ["revenue"] = new Money(200000) },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "C Corp", ["revenue"] = new Money(50000) }
    });
    
    // Any
    bool hasHighRevenue = context.CreateQuery("account")
        .Any(a => a.GetAttributeValue<Money>("revenue").Value > 150000);
    Assert.True(hasHighRevenue);
    
    // Count
    int lowRevenueCount = context.CreateQuery("account")
        .Count(a => a.GetAttributeValue<Money>("revenue").Value < 100000);
    Assert.Equal(1, lowRevenueCount);
    
    // Select
    var names = context.CreateQuery("account")
        .Select(a => a.GetAttributeValue<string>("name"))
        .OrderBy(n => n)
        .ToList();
    Assert.Equal(3, names.Count);
    Assert.Equal("A Corp", names[0]);
    
    // FirstOrDefault
    var account = context.CreateQuery("account")
        .FirstOrDefault(a => a.GetAttributeValue<string>("name") == "B Corp");
    Assert.NotNull(account);
    Assert.Equal(200000, account.GetAttributeValue<Money>("revenue").Value);
}
```

### Custom Extension Methods

You can create your own extension methods:

```csharp
public static class CustomEntityExtensions
{
    public static bool IsActive(this Entity entity)
    {
        if (!entity.Contains("statecode"))
            return false;
            
        return entity.GetAttributeValue<OptionSetValue>("statecode").Value == 0;
    }
    
    public static void SetActive(this Entity entity, bool active)
    {
        entity["statecode"] = new OptionSetValue(active ? 0 : 1);
        entity["statuscode"] = new OptionSetValue(active ? 1 : 2);
    }
}

// Usage
[Fact]
public void Should_Use_Custom_Extensions()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test"
    };
    
    account.SetActive(true);
    Assert.True(account.IsActive());
    
    account.SetActive(false);
    Assert.False(account.IsActive());
}
```

## See Also

- [Testing Plugins](../usage/testing-plugins.md) - Plugin execution patterns
- [XrmFakedContext](../concepts/xrm-faked-context.md) - Context operations
- [IXrmFakedContext Reference](./ixrm-faked-context.md) - Context interface
- [Microsoft SDK Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk)
