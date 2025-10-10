# Testing Custom Workflow Activities

Custom workflow activities extend Dataverse workflows with custom business logic. This guide shows you how to test them using Fake4Dataverse.

## Table of Contents

- [Overview](#overview)
- [Workflow Activity Basics](#workflow-activity-basics)
- [Setting Up Tests](#setting-up-tests)
- [Input Parameters](#input-parameters)
- [Output Parameters](#output-parameters)
- [Testing Workflow Context](#testing-workflow-context)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [See Also](#see-also)

## Overview

Custom workflow activities in Dataverse inherit from `CodeActivity` and use workflow-specific services and context. Testing them requires:
1. Creating a workflow execution context
2. Setting input parameters
3. Executing the activity
4. Verifying output parameters and side effects

**Reference:** [Custom Workflow Activities](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/workflow/workflow-extensions) - Microsoft documentation on creating and using custom workflow activities in Dataverse.

## Workflow Activity Basics

A typical custom workflow activity looks like this:

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

public class CalculateDiscountActivity : CodeActivity
{
    [Input("Original Amount")]
    [RequiredArgument]
    public InArgument<decimal> OriginalAmount { get; set; }
    
    [Input("Discount Percentage")]
    public InArgument<decimal> DiscountPercentage { get; set; }
    
    [Output("Discounted Amount")]
    public OutArgument<decimal> DiscountedAmount { get; set; }
    
    protected override void Execute(CodeActivityContext context)
    {
        var workflowContext = context.GetExtension<IWorkflowContext>();
        var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
        var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
        
        var original = OriginalAmount.Get(context);
        var discount = DiscountPercentage.Get(context);
        
        var result = original - (original * discount / 100);
        
        DiscountedAmount.Set(context, result);
    }
}
```

## Setting Up Tests

### Basic Test Structure

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using Xunit;

public class CalculateDiscountActivityTests
{
    [Fact]
    public void Should_Calculate_Discount_Correctly()
    {
        // Arrange
        var context = XrmFakedContextFactory.New();
        var service = context.GetOrganizationService();
        
        // Create workflow activity instance
        var activity = new CalculateDiscountActivity();
        
        // Create workflow invoker
        var invoker = new WorkflowInvoker(activity);
        
        // Set input parameters
        invoker.Extensions.Add<ITracingService>(() => new FakeTracingService());
        invoker.Extensions.Add<IWorkflowContext>(() => new FakeWorkflowContext
        {
            UserId = Guid.NewGuid(),
            InitiatingUserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        });
        invoker.Extensions.Add<IOrganizationServiceFactory>(() => new FakeServiceFactory(service));
        
        // Act
        var inputs = new Dictionary<string, object>
        {
            { "OriginalAmount", 100.0m },
            { "DiscountPercentage", 10.0m }
        };
        
        var outputs = invoker.Invoke(inputs);
        
        // Assert
        Assert.Equal(90.0m, outputs["DiscountedAmount"]);
    }
}
```

### Helper Classes

Create helper classes for workflow testing:

```csharp
// Fake Tracing Service
public class FakeTracingService : ITracingService
{
    public List<string> Traces { get; } = new List<string>();
    
    public void Trace(string format, params object[] args)
    {
        Traces.Add(string.Format(format, args));
    }
}

// Fake Workflow Context
public class FakeWorkflowContext : IWorkflowContext
{
    public Guid UserId { get; set; }
    public Guid InitiatingUserId { get; set; }
    public Guid BusinessUnitId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public Guid PrimaryEntityId { get; set; }
    public string PrimaryEntityName { get; set; }
    public string MessageName { get; set; }
    public int Depth { get; set; }
    public int Mode { get; set; }
    public int IsolationMode { get; set; }
    public Guid CorrelationId { get; set; }
    public bool IsExecutingOffline { get; set; }
    public bool IsInTransaction { get; set; }
    public Guid RequestId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime OperationCreatedOn { get; set; }
    public ParameterCollection InputParameters { get; set; }
    public ParameterCollection OutputParameters { get; set; }
    public ParameterCollection SharedVariables { get; set; }
    public EntityReference OwningExtension { get; set; }
    public EntityImageCollection PreEntityImages { get; set; }
    public EntityImageCollection PostEntityImages { get; set; }
    public int Stage { get; set; }
    public Guid ParentContext { get; set; }
    public string StageName { get; set; }
    public Guid WorkflowCategory { get; set; }
    public int WorkflowMode { get; set; }
}

// Fake Service Factory
public class FakeServiceFactory : IOrganizationServiceFactory
{
    private readonly IOrganizationService _service;
    
    public FakeServiceFactory(IOrganizationService service)
    {
        _service = service;
    }
    
    public IOrganizationService CreateOrganizationService(Guid? userId)
    {
        return _service;
    }
}
```

## Input Parameters

### Setting Simple Input Parameters

```csharp
[Fact]
public void Should_Process_Input_Parameters()
{
    var activity = new CalculateDiscountActivity();
    var invoker = new WorkflowInvoker(activity);
    
    // Setup extensions (context, services, etc.)
    SetupExtensions(invoker);
    
    var inputs = new Dictionary<string, object>
    {
        { "OriginalAmount", 150.0m },
        { "DiscountPercentage", 20.0m }
    };
    
    var outputs = invoker.Invoke(inputs);
    
    Assert.Equal(120.0m, outputs["DiscountedAmount"]);
}
```

### Entity Reference Inputs

```csharp
public class UpdateAccountActivity : CodeActivity
{
    [Input("Account")]
    [RequiredArgument]
    public InArgument<EntityReference> Account { get; set; }
    
    [Input("New Name")]
    public InArgument<string> NewName { get; set; }
    
    protected override void Execute(CodeActivityContext context)
    {
        var workflowContext = context.GetExtension<IWorkflowContext>();
        var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
        var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
        
        var accountRef = Account.Get(context);
        var newName = NewName.Get(context);
        
        var account = new Entity("account")
        {
            Id = accountRef.Id,
            ["name"] = newName
        };
        
        service.Update(account);
    }
}

[Fact]
public void Should_Update_Account_Name()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Old Name"
    });
    
    var activity = new UpdateAccountActivity();
    var invoker = new WorkflowInvoker(activity);
    SetupExtensions(invoker, service);
    
    var inputs = new Dictionary<string, object>
    {
        { "Account", new EntityReference("account", accountId) },
        { "NewName", "New Name" }
    };
    
    invoker.Invoke(inputs);
    
    var updatedAccount = service.Retrieve("account", accountId, new ColumnSet("name"));
    Assert.Equal("New Name", updatedAccount["name"]);
}
```

## Output Parameters

### Retrieving Output Parameters

```csharp
public class GetAccountRevenueActivity : CodeActivity
{
    [Input("Account")]
    [RequiredArgument]
    public InArgument<EntityReference> Account { get; set; }
    
    [Output("Revenue")]
    public OutArgument<decimal> Revenue { get; set; }
    
    protected override void Execute(CodeActivityContext context)
    {
        var workflowContext = context.GetExtension<IWorkflowContext>();
        var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
        var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
        
        var accountRef = Account.Get(context);
        var account = service.Retrieve("account", accountRef.Id, new ColumnSet("revenue"));
        
        var revenue = account.GetAttributeValue<Money>("revenue");
        Revenue.Set(context, revenue?.Value ?? 0);
    }
}

[Fact]
public void Should_Return_Account_Revenue()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account",
        ["revenue"] = new Money(100000)
    });
    
    var activity = new GetAccountRevenueActivity();
    var invoker = new WorkflowInvoker(activity);
    SetupExtensions(invoker, service);
    
    var inputs = new Dictionary<string, object>
    {
        { "Account", new EntityReference("account", accountId) }
    };
    
    var outputs = invoker.Invoke(inputs);
    
    Assert.Equal(100000m, outputs["Revenue"]);
}
```

## Testing Workflow Context

### Using Workflow Context Properties

```csharp
public class CreateContactActivity : CodeActivity
{
    [Input("First Name")]
    public InArgument<string> FirstName { get; set; }
    
    [Input("Last Name")]
    public InArgument<string> LastName { get; set; }
    
    [Output("Contact")]
    public OutArgument<EntityReference> Contact { get; set; }
    
    protected override void Execute(CodeActivityContext context)
    {
        var workflowContext = context.GetExtension<IWorkflowContext>();
        var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
        var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
        
        var contact = new Entity("contact")
        {
            ["firstname"] = FirstName.Get(context),
            ["lastname"] = LastName.Get(context),
            // Use primary entity from workflow context
            ["parentcustomerid"] = new EntityReference(
                workflowContext.PrimaryEntityName,
                workflowContext.PrimaryEntityId)
        };
        
        var contactId = service.Create(contact);
        Contact.Set(context, new EntityReference("contact", contactId));
    }
}

[Fact]
public void Should_Link_Contact_To_Primary_Entity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    });
    
    var activity = new CreateContactActivity();
    var invoker = new WorkflowInvoker(activity);
    
    var workflowContext = new FakeWorkflowContext
    {
        UserId = Guid.NewGuid(),
        PrimaryEntityName = "account",
        PrimaryEntityId = accountId
    };
    
    invoker.Extensions.Add<ITracingService>(() => new FakeTracingService());
    invoker.Extensions.Add<IWorkflowContext>(() => workflowContext);
    invoker.Extensions.Add<IOrganizationServiceFactory>(() => new FakeServiceFactory(service));
    
    var inputs = new Dictionary<string, object>
    {
        { "FirstName", "John" },
        { "LastName", "Doe" }
    };
    
    var outputs = invoker.Invoke(inputs);
    
    var contactRef = (EntityReference)outputs["Contact"];
    var contact = service.Retrieve("contact", contactRef.Id, new ColumnSet(true));
    
    var parentRef = contact.GetAttributeValue<EntityReference>("parentcustomerid");
    Assert.Equal("account", parentRef.LogicalName);
    Assert.Equal(accountId, parentRef.Id);
}
```

## Complete Examples

### Testing Error Handling

```csharp
[Fact]
public void Should_Handle_Missing_Required_Input()
{
    var activity = new CalculateDiscountActivity();
    var invoker = new WorkflowInvoker(activity);
    SetupExtensions(invoker);
    
    var inputs = new Dictionary<string, object>
    {
        // Missing OriginalAmount (required)
        { "DiscountPercentage", 10.0m }
    };
    
    Assert.Throws<ArgumentException>(() => invoker.Invoke(inputs));
}
```

### Testing with Tracing

```csharp
[Fact]
public void Should_Write_Trace_Messages()
{
    var tracingService = new FakeTracingService();
    
    var activity = new CalculateDiscountActivity();
    var invoker = new WorkflowInvoker(activity);
    
    invoker.Extensions.Add<ITracingService>(() => tracingService);
    // Add other extensions...
    
    var inputs = new Dictionary<string, object>
    {
        { "OriginalAmount", 100.0m },
        { "DiscountPercentage", 10.0m }
    };
    
    invoker.Invoke(inputs);
    
    Assert.NotEmpty(tracingService.Traces);
}
```

## Best Practices

### ✅ Do

- **Create helper methods** for common setup
- **Test all input/output combinations**
- **Verify side effects** (created/updated records)
- **Test error scenarios**
- **Use realistic test data**

```csharp
public class WorkflowTestBase
{
    protected IXrmFakedContext Context { get; private set; }
    protected IOrganizationService Service { get; private set; }
    
    protected WorkflowTestBase()
    {
        Context = XrmFakedContextFactory.New();
        Service = Context.GetOrganizationService();
    }
    
    protected void SetupExtensions(WorkflowInvoker invoker, Guid? userId = null)
    {
        invoker.Extensions.Add<ITracingService>(() => new FakeTracingService());
        invoker.Extensions.Add<IWorkflowContext>(() => new FakeWorkflowContext
        {
            UserId = userId ?? Guid.NewGuid(),
            InitiatingUserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        });
        invoker.Extensions.Add<IOrganizationServiceFactory>(() => new FakeServiceFactory(Service));
    }
}
```

### ❌ Don't

- **Don't skip setup** - Always configure all required extensions
- **Don't ignore exceptions** - Test both success and failure paths
- **Don't test UI logic** - Focus on business logic only
- **Don't hard-code GUIDs** - Use generated GUIDs for flexibility

## See Also

- [Testing Plugins](./testing-plugins.md) - Similar testing patterns for plugins
- [Basic Concepts](../getting-started/basic-concepts.md) - Framework fundamentals
- [CRUD Operations](./crud-operations.md) - Testing entity operations
- [Microsoft Workflow Activity Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/workflow/workflow-extensions)
