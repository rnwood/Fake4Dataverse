# Service Initialization

The `IOrganizationService` is the primary interface for interacting with Dataverse in both production and test scenarios. This guide covers patterns for creating and configuring services in Fake4Dataverse.

## Table of Contents

- [Overview](#overview)
- [Getting Organization Service](#getting-organization-service)
- [Setting Caller Properties](#setting-caller-properties)
- [Multiple Service Instances](#multiple-service-instances)
- [Service Configuration Patterns](#service-configuration-patterns)
- [Advanced Scenarios](#advanced-scenarios)
- [See Also](#see-also)

## Overview

In Fake4Dataverse, the organization service is obtained from the `IXrmFakedContext` and provides the same interface (`IOrganizationService`) as the real Dataverse service.

**Reference:** [IOrganizationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice) - The core interface for executing operations against Dataverse, providing methods like Create, Retrieve, Update, Delete, Associate, Disassociate, and Execute.

## Getting Organization Service

### Basic Service Creation

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Use the service
var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
```

### Service Lifecycle

The service instance is tied to the context and shares its data store:

```csharp
[Fact]
public void Services_Share_Context_Data()
{
    var context = XrmFakedContextFactory.New();
    var service1 = context.GetOrganizationService();
    var service2 = context.GetOrganizationService();
    
    // Create with service1
    var accountId = service1.Create(new Entity("account") { ["name"] = "Test" });
    
    // Retrieve with service2 - same data store
    var account = service2.Retrieve("account", accountId, new ColumnSet(true));
    Assert.Equal("Test", account["name"]);
}
```

## Setting Caller Properties

The caller properties determine which user is executing operations. This is essential for testing security and user-specific logic.

**Reference:** [CallerID Property](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/impersonate-a-user) - In Dataverse, the CallerID represents the user executing operations and affects security, ownership, and audit behavior.

### Setting the Calling User

```csharp
[Fact]
public void Should_Execute_As_Specific_User()
{
    var context = XrmFakedContextFactory.New();
    
    var userId = Guid.NewGuid();
    var user = new Entity("systemuser")
    {
        Id = userId,
        ["fullname"] = "Test User"
    };
    
    context.Initialize(user);
    
    // Set the calling user
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    var service = context.GetOrganizationService();
    
    // Operations will execute as this user
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
}
```

### Setting Business Unit

```csharp
[Fact]
public void Should_Set_Business_Unit()
{
    var context = XrmFakedContextFactory.New();
    
    var businessUnitId = Guid.NewGuid();
    var businessUnit = new Entity("businessunit")
    {
        Id = businessUnitId,
        ["name"] = "Sales"
    };
    
    context.Initialize(businessUnit);
    context.CallerProperties.BusinessUnitId = businessUnitId;
    
    var service = context.GetOrganizationService();
}
```

## Multiple Service Instances

You can create multiple service instances representing different users:

### Testing Multi-User Scenarios

```csharp
[Fact]
public void Should_Support_Multiple_Users()
{
    var context = XrmFakedContextFactory.New();
    
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    var user1 = new Entity("systemuser") { Id = user1Id, ["fullname"] = "User 1" };
    var user2 = new Entity("systemuser") { Id = user2Id, ["fullname"] = "User 2" };
    
    context.Initialize(new[] { user1, user2 });
    
    // Service for User 1
    context.CallerProperties.CallerId = new EntityReference("systemuser", user1Id);
    var service1 = context.GetOrganizationService();
    
    // Create account as User 1
    var accountId = service1.Create(new Entity("account") { ["name"] = "User 1 Account" });
    
    // Service for User 2
    context.CallerProperties.CallerId = new EntityReference("systemuser", user2Id);
    var service2 = context.GetOrganizationService();
    
    // User 2 can also retrieve the account
    var account = service2.Retrieve("account", accountId, new ColumnSet(true));
    Assert.NotNull(account);
}
```

## Service Configuration Patterns

### Test Base Class Pattern

```csharp
public abstract class TestBase
{
    protected IXrmFakedContext Context { get; private set; }
    protected IOrganizationService Service { get; private set; }
    
    protected TestBase()
    {
        Context = XrmFakedContextFactory.New();
        Service = Context.GetOrganizationService();
        SetupTestData();
    }
    
    protected virtual void SetupTestData()
    {
        // Override in derived classes for specific test data
    }
}

public class AccountTests : TestBase
{
    protected override void SetupTestData()
    {
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account"
        };
        Context.Initialize(account);
    }
    
    [Fact]
    public void Should_Update_Account()
    {
        // Service is already available from base class
        var accounts = Context.CreateQuery("account").ToList();
        Assert.Single(accounts);
    }
}
```

### Factory Pattern

```csharp
public static class ServiceFactory
{
    public static IOrganizationService CreateService(
        IXrmFakedContext context, 
        string userFullName = "Test User")
    {
        var userId = Guid.NewGuid();
        var user = new Entity("systemuser")
        {
            Id = userId,
            ["fullname"] = userFullName
        };
        
        context.Initialize(user);
        context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
        
        return context.GetOrganizationService();
    }
}

// Usage
[Fact]
public void Test_With_Factory()
{
    var context = XrmFakedContextFactory.New();
    var service = ServiceFactory.CreateService(context, "Sales Manager");
}
```

## Advanced Scenarios

### Testing Impersonation

```csharp
[Fact]
public void Should_Support_Impersonation()
{
    var context = XrmFakedContextFactory.New();
    
    var adminId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    var admin = new Entity("systemuser") { Id = adminId, ["fullname"] = "Admin" };
    var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Regular User" };
    
    context.Initialize(new[] { admin, user });
    
    // Set admin as caller
    context.CallerProperties.CallerId = new EntityReference("systemuser", adminId);
    var adminService = context.GetOrganizationService();
    
    // Create account as admin
    var accountId = adminService.Create(new Entity("account") { ["name"] = "Admin Account" });
    
    // Switch to regular user
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    var userService = context.GetOrganizationService();
    
    // Regular user can access the account
    var account = userService.Retrieve("account", accountId, new ColumnSet(true));
    Assert.NotNull(account);
}
```

### Service with Custom Configuration

```csharp
public class ConfiguredServiceBuilder
{
    private readonly IXrmFakedContext _context;
    private Guid? _userId;
    private Guid? _businessUnitId;
    
    public ConfiguredServiceBuilder(IXrmFakedContext context)
    {
        _context = context;
    }
    
    public ConfiguredServiceBuilder WithUser(Guid userId)
    {
        _userId = userId;
        return this;
    }
    
    public ConfiguredServiceBuilder WithBusinessUnit(Guid businessUnitId)
    {
        _businessUnitId = businessUnitId;
        return this;
    }
    
    public IOrganizationService Build()
    {
        if (_userId.HasValue)
        {
            _context.CallerProperties.CallerId = new EntityReference("systemuser", _userId.Value);
        }
        
        if (_businessUnitId.HasValue)
        {
            _context.CallerProperties.BusinessUnitId = _businessUnitId.Value;
        }
        
        return _context.GetOrganizationService();
    }
}

// Usage
[Fact]
public void Test_With_Configured_Service()
{
    var context = XrmFakedContextFactory.New();
    var userId = Guid.NewGuid();
    
    var service = new ConfiguredServiceBuilder(context)
        .WithUser(userId)
        .Build();
}
```

## See Also

- [XrmFakedContext](./xrm-faked-context.md) - Context methods and properties
- [Basic Concepts](../getting-started/basic-concepts.md) - Understanding IOrganizationService
- [Testing Plugins](../usage/testing-plugins.md) - Using services in plugin tests
- [Security and Permissions](../usage/security-permissions.md) - Testing with different users
