# Testing Business Rules

Business rules provide a simple interface to implement and maintain fast-changing data validation and field logic in Microsoft Dataverse. This guide shows how to test business rules using Fake4Dataverse.

**Microsoft Documentation**: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule

## Overview

Business rules in Dataverse allow administrators to:
- Validate data and show error messages
- Set or clear field values automatically
- Show/hide or enable/disable fields (client-side)
- Set business requirements and recommendations
- Control field requirements

Fake4Dataverse simulates business rule execution during Create and Update operations, allowing you to test business logic without requiring a live CRM instance.

## Quick Start

Here's a simple example of testing a business rule:

```csharp
using Fake4Dataverse;
using Fake4Dataverse.BusinessRules;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

// Arrange - Create context with middleware
var context = (XrmFakedContext)XrmFakedContextFactory.New();
var executor = context.BusinessRuleExecutor;

// Define a business rule
var rule = new BusinessRuleDefinition
{
    Name = "ValidateAccountName",
    EntityLogicalName = "account",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate,
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "name",
            Operator = ConditionOperator.Null
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.ShowErrorMessage,
            FieldName = "name",
            Message = "Account name is required"
        }
    }
};

// Register the rule
executor.RegisterRule(rule);

// Act - Try to create an account without a name
var service = context.GetOrganizationService();
var account = new Entity("account") { Id = Guid.NewGuid() };

// Assert - Should throw validation error
Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
{
    service.Create(account);
});
```

## Business Rule Components

### 1. Business Rule Definition

A `BusinessRuleDefinition` contains:

```csharp
public class BusinessRuleDefinition
{
    public string Name { get; set; }                    // Rule name
    public string EntityLogicalName { get; set; }       // Target entity
    public BusinessRuleScope Scope { get; set; }        // Execution scope
    public BusinessRuleTrigger Trigger { get; set; }    // When to execute
    public bool UseAndLogic { get; set; }               // AND vs OR for conditions
    public List<BusinessRuleCondition> Conditions { get; set; }
    public List<BusinessRuleAction> Actions { get; set; }
    public List<BusinessRuleAction> ElseActions { get; set; }
}
```

### 2. Scope

Business rules can execute in different scopes:

```csharp
public enum BusinessRuleScope
{
    Entity = 0,     // Server-side (Create, Update operations)
    AllForms = 1,   // Client-side (all forms)
    Form = 2        // Client-side (specific form)
}
```

**Note**: Fake4Dataverse currently simulates Entity (server-side) scope during CRUD operations.

### 3. Triggers

Control when rules execute:

```csharp
[Flags]
public enum BusinessRuleTrigger
{
    OnCreate = 1,
    OnUpdate = 2,
    OnChange = 4,
    OnLoad = 8
}
```

Use bitwise flags to combine triggers:
```csharp
Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate
```

### 4. Conditions

Conditions determine when actions execute:

```csharp
new BusinessRuleCondition
{
    FieldName = "creditlimit",
    Operator = ConditionOperator.GreaterThan,
    Value = 100000
}
```

**Supported Operators**: All ConditionOperators from Microsoft.Xrm.Sdk.Query:
- Equal, NotEqual
- GreaterThan, GreaterEqual, LessThan, LessEqual
- Null, NotNull
- Contains, BeginsWith, EndsWith
- Like, NotLike
- And more...

### 5. Actions

Actions execute when conditions are met:

```csharp
public enum BusinessRuleActionType
{
    SetFieldValue,
    ClearFieldValue,
    SetDefaultValue,
    ShowErrorMessage,
    SetBusinessRequired,
    SetBusinessRecommendation,
    ShowHideField,
    EnableDisableField,
    LockUnlockField
}
```

## Common Scenarios

### Validating Required Fields

```csharp
var rule = new BusinessRuleDefinition
{
    Name = "RequireEmail",
    EntityLogicalName = "contact",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate,
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "emailaddress1",
            Operator = ConditionOperator.Null
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.ShowErrorMessage,
            FieldName = "emailaddress1",
            Message = "Email address is required for all contacts"
        }
    }
};

executor.RegisterRule(rule);
```

### Setting Field Values Based on Conditions

```csharp
var rule = new BusinessRuleDefinition
{
    Name = "SetPriorityCustomer",
    EntityLogicalName = "account",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate,
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "revenue",
            Operator = ConditionOperator.GreaterThan,
            Value = 1000000m
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "customertypecode",
            Value = new OptionSetValue(3) // High priority
        },
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "description",
            Value = "High-value customer - priority service"
        }
    }
};

executor.RegisterRule(rule);
```

### IF-THEN-ELSE Logic

Use `ElseActions` to execute different actions when conditions aren't met:

```csharp
var rule = new BusinessRuleDefinition
{
    Name = "CategoryCustomer",
    EntityLogicalName = "account",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnCreate,
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "numberofemployees",
            Operator = ConditionOperator.GreaterThan,
            Value = 500
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "accountcategorycode",
            Value = new OptionSetValue(1) // Enterprise
        }
    },
    ElseActions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "accountcategorycode",
            Value = new OptionSetValue(2) // Small business
        }
    }
};

executor.RegisterRule(rule);
```

### Multiple Conditions (AND Logic)

By default, all conditions must be true (AND logic):

```csharp
var rule = new BusinessRuleDefinition
{
    Name = "QualifiedLead",
    EntityLogicalName = "lead",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnUpdate,
    UseAndLogic = true, // Default, can be omitted
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "budgetamount",
            Operator = ConditionOperator.GreaterThan,
            Value = 10000m
        },
        new BusinessRuleCondition
        {
            FieldName = "purchasetimeframe",
            Operator = ConditionOperator.NotNull
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "leadqualitycode",
            Value = new OptionSetValue(3) // Hot
        }
    }
};
```

### Multiple Conditions (OR Logic)

Use OR logic where any condition being true triggers the actions:

```csharp
var rule = new BusinessRuleDefinition
{
    Name = "HighRiskAccount",
    EntityLogicalName = "account",
    Scope = BusinessRuleScope.Entity,
    Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate,
    UseAndLogic = false, // OR logic
    Conditions = new List<BusinessRuleCondition>
    {
        new BusinessRuleCondition
        {
            FieldName = "creditonhold",
            Operator = ConditionOperator.Equal,
            Value = true
        },
        new BusinessRuleCondition
        {
            FieldName = "paymenttermscode",
            Operator = ConditionOperator.Equal,
            Value = new OptionSetValue(1) // Net 90
        }
    },
    Actions = new List<BusinessRuleAction>
    {
        new BusinessRuleAction
        {
            ActionType = BusinessRuleActionType.SetFieldValue,
            FieldName = "accountratingcode",
            Value = new OptionSetValue(2) // High risk
        }
    }
};
```

## Testing Business Rules

### Basic Test Pattern

Follow the Arrange-Act-Assert pattern:

```csharp
[Fact]
public void BusinessRule_Should_Execute_During_Create()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var executor = context.BusinessRuleExecutor;
    
    // Register business rule
    var rule = new BusinessRuleDefinition { /* ... */ };
    executor.RegisterRule(rule);
    
    // Act
    var service = context.GetOrganizationService();
    var entity = new Entity("account") { /* ... */ };
    var id = service.Create(entity);
    
    // Assert
    var retrieved = service.Retrieve("account", id, new ColumnSet(true));
    Assert.Equal(expectedValue, retrieved["fieldname"]);
}
```

### Testing Validation Errors

```csharp
[Fact]
public void BusinessRule_Should_Block_Save_When_Validation_Fails()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var executor = context.BusinessRuleExecutor;
    
    var rule = new BusinessRuleDefinition
    {
        Name = "ValidateRequiredField",
        EntityLogicalName = "account",
        Scope = BusinessRuleScope.Entity,
        Trigger = BusinessRuleTrigger.OnCreate,
        Conditions = new List<BusinessRuleCondition>
        {
            new BusinessRuleCondition
            {
                FieldName = "name",
                Operator = ConditionOperator.Null
            }
        },
        Actions = new List<BusinessRuleAction>
        {
            new BusinessRuleAction
            {
                ActionType = BusinessRuleActionType.ShowErrorMessage,
                FieldName = "name",
                Message = "Name is required"
            }
        }
    };
    
    executor.RegisterRule(rule);
    
    // Act & Assert
    var service = context.GetOrganizationService();
    var account = new Entity("account") { Id = Guid.NewGuid() };
    
    Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
    {
        service.Create(account);
    });
}
```

### Testing Update Operations

```csharp
[Fact]
public void BusinessRule_Should_Execute_During_Update()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var executor = context.BusinessRuleExecutor;
    
    var rule = new BusinessRuleDefinition
    {
        Name = "UpdateDescription",
        EntityLogicalName = "account",
        Scope = BusinessRuleScope.Entity,
        Trigger = BusinessRuleTrigger.OnUpdate, // Only on update
        Conditions = new List<BusinessRuleCondition>(),
        Actions = new List<BusinessRuleAction>
        {
            new BusinessRuleAction
            {
                ActionType = BusinessRuleActionType.SetFieldValue,
                FieldName = "description",
                Value = "Updated by business rule"
            }
        }
    };
    
    executor.RegisterRule(rule);
    
    // Act
    var service = context.GetOrganizationService();
    
    // Create account (rule should NOT execute)
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test Account"
    };
    var id = service.Create(account);
    
    var retrieved1 = service.Retrieve("account", id, new ColumnSet(true));
    Assert.False(retrieved1.Contains("description")); // Not set by rule on create
    
    // Update account (rule SHOULD execute)
    var update = new Entity("account")
    {
        Id = id,
        ["telephone1"] = "555-1234"
    };
    service.Update(update);
    
    var retrieved2 = service.Retrieve("account", id, new ColumnSet(true));
    Assert.True(retrieved2.Contains("description"));
    Assert.Equal("Updated by business rule", retrieved2["description"]);
}
```

## Important Considerations

### Context Creation

**Always use `XrmFakedContextFactory.New()`** to create the context:

```csharp
// ✅ Correct - includes middleware for business rules
var context = (XrmFakedContext)XrmFakedContextFactory.New();

// ❌ Incorrect - no middleware, business rules won't execute
var context = new XrmFakedContext();
```

The factory sets up the middleware pipeline that enables business rule execution during CRUD operations.

### Type Casting

Cast the context to access the `BusinessRuleExecutor` property:

```csharp
var context = (XrmFakedContext)XrmFakedContextFactory.New();
var executor = context.BusinessRuleExecutor;
```

### Server-Side Execution Only

Fake4Dataverse currently simulates server-side (Entity scope) business rules. Client-side rules (form-specific behavior like show/hide fields) are tracked but not enforced in the test environment.

### Execution Order

Business rules execute before plugin pipeline stages:
1. Business rules execute
2. PreValidation plugins
3. PreOperation plugins
4. Database operation
5. PostOperation plugins

### Field Types

Business rules work with all standard Dataverse field types:
- Text, numbers, dates
- OptionSet values
- Money (use Money type in C#)
- Boolean
- Lookups (EntityReference)

## Key Differences from FakeXrmEasy v2

| Feature | FakeXrmEasy v2+ | Fake4Dataverse v4 |
|---------|----------------|-------------------|
| **Context Setup** | `new XrmRealContext()` or similar | Must use `XrmFakedContextFactory.New()` |
| **Accessing Executor** | Direct property access | Requires casting: `(XrmFakedContext)context` |
| **Rule Registration** | Built-in rule definition | Manual `BusinessRuleDefinition` objects |
| **Scope Support** | Full scope support | Entity (server-side) scope only |
| **Client-Side Rules** | Fully simulated | Tracked but not enforced |

**Migration Tip**: When migrating from FakeXrmEasy v2, update context creation to use the factory and cast when accessing `BusinessRuleExecutor`.

## Advanced Topics

### Direct Execution

You can execute business rules directly without going through CRUD operations:

```csharp
var executor = context.BusinessRuleExecutor;
var entity = new Entity("account") { /* ... */ };

var result = executor.ExecuteRules(
    entity, 
    BusinessRuleTrigger.OnCreate,
    isServerSide: true
);

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.FieldName}: {error.Message}");
    }
}
```

### Multiple Rules per Entity

Register multiple rules for the same entity:

```csharp
executor.RegisterRule(validationRule);
executor.RegisterRule(calculationRule);
executor.RegisterRule(statusRule);

// All rules execute in registration order
```

### Retrieving Registered Rules

```csharp
var accountRules = executor.GetRulesForEntity("account");
Console.WriteLine($"Total rules for account: {accountRules.Count}");
```

## Troubleshooting

### Business Rules Not Executing

**Problem**: Rules don't execute during Create/Update

**Solution**: Ensure you're using `XrmFakedContextFactory.New()`:
```csharp
var context = (XrmFakedContext)XrmFakedContextFactory.New();
```

### Cannot Access BusinessRuleExecutor

**Problem**: Compilation error accessing `BusinessRuleExecutor`

**Solution**: Cast the context:
```csharp
var context = (XrmFakedContext)XrmFakedContextFactory.New();
var executor = context.BusinessRuleExecutor;
```

### Rules Execute But Fields Not Set

**Problem**: Rules execute (no errors) but field values aren't persisted

**Solution**: This was fixed in the CRUD integration. Ensure you're using the latest version.

## See Also

- [Testing Plugins](./testing-plugins.md) - Business rules execute before plugins
- [CRUD Operations](./crud-operations.md) - How Create/Update trigger business rules
- [Microsoft Business Rules Documentation](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule)

## Implementation Date

Business rules simulation was implemented in October 2025 as part of Feature Parity Issue #8.
