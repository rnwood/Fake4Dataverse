# Rollup Fields Support

## Overview

Rollup fields (also known as rollup columns) automatically aggregate data from related child records using functions such as SUM, COUNT, MIN, MAX, and AVG. Fake4Dataverse simulates Dataverse rollup field evaluation, allowing you to test business logic that depends on aggregated values from related entities.

**Implemented:** 2025-10-11 (Issue #7)

## Microsoft Documentation

Official references:
- [Define Rollup Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields) - Main documentation for rollup columns
- [Types of Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields) - Field types in Dataverse

## What are Rollup Fields?

Rollup fields allow you to:
- Automatically aggregate values from related child records
- Use aggregate functions: SUM, COUNT, MIN, MAX, AVG
- Apply filters to restrict which records are included
- Calculate values across one-to-many relationships
- Support hierarchical rollups (entire entity hierarchy)

### When Rollup Fields are Evaluated

According to Microsoft documentation, rollup fields are evaluated:
- **Asynchronously** - By scheduled system jobs in the background
- **On-demand** - Using the CalculateRollupField message
- **After related record changes** - When child records are created, updated, or deleted

In Fake4Dataverse, rollup fields can be evaluated:
- **Manually** - Using `EvaluateRollupFields(entity)` or `TriggerRollupCalculation(entityLogicalName, recordId)`
- **Automatically** - When related records are created, updated, or deleted (automatic refresh)

## Usage

### Basic COUNT Operation

Count the number of related records:

```csharp
using Fake4Dataverse.RollupFields;
using Microsoft.Xrm.Sdk;
using Xunit;

[Fact]
public void Should_Count_Related_Contacts()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    // Define rollup field: count of related contacts
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "contactcount",
        RelatedEntityLogicalName = "contact",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    };

    var contact1 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "John",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    var contact2 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "Jane",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, contact1, contact2 });

    // Act - Evaluate rollup fields
    evaluator.EvaluateRollupFields(account);

    // Assert
    Assert.Equal(2, context.Data["account"][accountId].GetAttributeValue<int>("contactcount"));
}
```

### SUM Operation with Decimal Values

Sum numeric values across related records:

```csharp
[Fact]
public void Should_Sum_Opportunity_Revenue()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    // Define rollup field: sum of estimated value from opportunities
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    };

    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 50000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opp1, opp2 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(150000m, updatedAccount.GetAttributeValue<decimal>("totalrevenue"));
}
```

### SUM with Money (Currency) Fields

Aggregate Currency (Money) field values:

```csharp
[Fact]
public void Should_Sum_Annual_Income()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalincome",
        RelatedEntityLogicalName = "contact",
        AggregateAttributeLogicalName = "annualincome",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(Money)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account")
    {
        Id = accountId,
        ["name"] = "Test Account"
    };

    var contact1 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["annualincome"] = new Money(75000m),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    var contact2 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["annualincome"] = new Money(85000m),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, contact1, contact2 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    var totalIncome = updatedAccount.GetAttributeValue<Money>("totalincome");
    Assert.NotNull(totalIncome);
    Assert.Equal(160000m, totalIncome.Value);
}
```

### AVG (Average) Operation

Calculate the average value across related records:

```csharp
[Fact]
public void Should_Calculate_Average_Deal_Size()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "avgdealsize",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Avg,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 50000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp3 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 75000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opp1, opp2, opp3 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(75000m, updatedAccount.GetAttributeValue<decimal>("avgdealsize"));
}
```

### MIN (Minimum) Operation

Find the minimum value across related records:

```csharp
[Fact]
public void Should_Find_Earliest_Close_Date()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "earliestclosedate",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedclosedate",
        AggregateFunction = RollupAggregateFunction.Min,
        ResultType = typeof(DateTime)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedclosedate"] = new DateTime(2025, 3, 15),
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedclosedate"] = new DateTime(2025, 1, 10),
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opp1, opp2 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(new DateTime(2025, 1, 10), updatedAccount.GetAttributeValue<DateTime>("earliestclosedate"));
}
```

### MAX (Maximum) Operation

Find the maximum value across related records:

```csharp
[Fact]
public void Should_Find_Largest_Deal()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "largestdeal",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Max,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 250000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opp1, opp2 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(250000m, updatedAccount.GetAttributeValue<decimal>("largestdeal"));
}
```

## Filtering

### State Filters

Filter records by state (active/inactive):

```csharp
[Fact]
public void Should_Count_Only_Active_Records()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "activecontactcount",
        RelatedEntityLogicalName = "contact",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int),
        StateFilter = RollupStateFilter.Active  // Only active records
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var activeContact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["statecode"] = new OptionSetValue(0),  // Active
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    var inactiveContact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["statecode"] = new OptionSetValue(1),  // Inactive
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, activeContact, inactiveContact });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert - Only 1 active contact counted
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(1, updatedAccount.GetAttributeValue<int>("activecontactcount"));
}
```

**Available State Filters:**
- `RollupStateFilter.Active` - Only active records (statecode = 0) [Default]
- `RollupStateFilter.Inactive` - Only inactive records (statecode = 1)
- `RollupStateFilter.All` - All records regardless of state

### Custom Filters

Apply custom logic to filter records:

```csharp
[Fact]
public void Should_Apply_Custom_Filter()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    // Define rollup field with custom filter: only opportunities > 100k
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "largedealcount",
        RelatedEntityLogicalName = "opportunity",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int),
        Filter = entity =>
        {
            var value = entity.GetAttributeValue<decimal>("estimatedvalue");
            return value > 100000m;
        }
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var smallDeal = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 50000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var largeDeal1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 150000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var largeDeal2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 200000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, smallDeal, largeDeal1, largeDeal2 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert - Only 2 deals > 100k counted
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(2, updatedAccount.GetAttributeValue<int>("largedealcount"));
}
```

### Combining Filters

Combine state filters with custom filters:

```csharp
[Fact]
public void Should_Combine_State_And_Custom_Filter()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "activehighincome",
        RelatedEntityLogicalName = "contact",
        AggregateAttributeLogicalName = "annualincome",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(Money),
        StateFilter = RollupStateFilter.Active,  // Only active records
        Filter = entity =>  // And income > 50k
        {
            var income = entity.GetAttributeValue<Money>("annualincome");
            return income != null && income.Value > 50000m;
        }
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    // Active, high income - INCLUDED
    var contact1 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["statecode"] = new OptionSetValue(0),
        ["annualincome"] = new Money(75000m),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    // Active, low income - EXCLUDED
    var contact2 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["statecode"] = new OptionSetValue(0),
        ["annualincome"] = new Money(40000m),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    // Inactive, high income - EXCLUDED
    var contact3 = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["statecode"] = new OptionSetValue(1),
        ["annualincome"] = new Money(100000m),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, contact1, contact2, contact3 });

    // Act
    evaluator.EvaluateRollupFields(account);

    // Assert - Only contact1 (active + high income) included
    var updatedAccount = context.Data["account"][accountId];
    var total = updatedAccount.GetAttributeValue<Money>("activehighincome");
    Assert.Equal(75000m, total.Value);
}
```

## Automatic Rollup Refresh

Rollup fields are automatically recalculated when related child records are created, updated, or deleted. This matches Dataverse behavior where rollup columns are refreshed when related data changes.

### Auto-Refresh on Create

When a new related record is created, the parent entity's rollup fields are automatically updated:

```csharp
[Fact]
public void Rollup_Auto_Refreshes_On_Create()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var evaluator = context.RollupFieldEvaluator;

    // Register rollup field: count of contacts
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "contactcount",
        RelatedEntityLogicalName = "contact",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };
    context.Initialize(new[] { account });

    // Initial calculation
    evaluator.EvaluateRollupFields(account);
    Assert.Equal(0, context.Data["account"][accountId].GetAttributeValue<int>("contactcount"));

    // Act - Create a related contact
    service.Create(new Entity("contact")
    {
        ["firstname"] = "John",
        ["parentcustomerid"] = new EntityReference("account", accountId)
    });

    // Assert - Rollup automatically refreshed
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(1, updatedAccount.GetAttributeValue<int>("contactcount"));
}
```

### Auto-Refresh on Update

When a related record is updated, the parent entity's rollup fields are automatically recalculated:

```csharp
[Fact]
public void Rollup_Auto_Refreshes_On_Update()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };
    var oppId = Guid.NewGuid();
    var opportunity = new Entity("opportunity")
    {
        Id = oppId,
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opportunity });
    evaluator.EvaluateRollupFields(account);

    // Act - Update the opportunity value
    service.Update(new Entity("opportunity")
    {
        Id = oppId,
        ["estimatedvalue"] = 150000m
    });

    // Assert - Rollup automatically refreshed
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(150000m, updatedAccount.GetAttributeValue<decimal>("totalrevenue"));
}
```

### Auto-Refresh on Delete

When a related record is deleted, the parent entity's rollup fields are automatically updated:

```csharp
[Fact]
public void Rollup_Auto_Refreshes_On_Delete()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "contactcount",
        RelatedEntityLogicalName = "contact",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };
    var contactId = Guid.NewGuid();
    var contact = new Entity("contact")
    {
        Id = contactId,
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, contact });
    evaluator.EvaluateRollupFields(account);
    Assert.Equal(1, context.Data["account"][accountId].GetAttributeValue<int>("contactcount"));

    // Act - Delete the contact
    service.Delete("contact", contactId);

    // Assert - Rollup automatically refreshed
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(0, updatedAccount.GetAttributeValue<int>("contactcount"));
}
```

### Auto-Refresh When Lookup Changes

When a related record's lookup field is updated to point to a different parent, both the old and new parent entities' rollup fields are automatically refreshed:

```csharp
[Fact]
public void Rollup_Auto_Refreshes_When_Lookup_Changes()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);

    var account1Id = Guid.NewGuid();
    var account1 = new Entity("account") { Id = account1Id };
    var account2Id = Guid.NewGuid();
    var account2 = new Entity("account") { Id = account2Id };
    var oppId = Guid.NewGuid();
    var opportunity = new Entity("opportunity")
    {
        Id = oppId,
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", account1Id)
    };

    context.Initialize(new[] { account1, account2, opportunity });
    evaluator.EvaluateRollupFields(account1);
    evaluator.EvaluateRollupFields(account2);

    // Act - Move opportunity to account2
    service.Update(new Entity("opportunity")
    {
        Id = oppId,
        ["parentaccountid"] = new EntityReference("account", account2Id)
    });

    // Assert - Account2's rollup automatically refreshed
    var updatedAccount2 = context.Data["account"][account2Id];
    Assert.Equal(100000m, updatedAccount2.GetAttributeValue<decimal>("totalrevenue"));
}
```

## On-Demand Calculation

Trigger rollup calculation for a specific record:

```csharp
[Fact]
public void Should_Trigger_Rollup_For_Specific_Record()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "contactcount",
        RelatedEntityLogicalName = "contact",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int)
    };
    evaluator.RegisterRollupField(definition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var contact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, contact });

    // Act - Trigger calculation for specific account
    evaluator.TriggerRollupCalculation("account", accountId);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(1, updatedAccount.GetAttributeValue<int>("contactcount"));
}
```

## Multiple Rollup Fields

Define multiple rollup fields on the same entity:

```csharp
[Fact]
public void Should_Handle_Multiple_Rollup_Fields()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;

    // Define multiple rollup fields
    var countDefinition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "opportunitycount",
        RelatedEntityLogicalName = "opportunity",
        AggregateFunction = RollupAggregateFunction.Count,
        ResultType = typeof(int)
    };

    var sumDefinition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };

    evaluator.RegisterRollupField(countDefinition);
    evaluator.RegisterRollupField(sumDefinition);

    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };

    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 50000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };

    context.Initialize(new[] { account, opp1, opp2 });

    // Act - Single call evaluates all rollup fields
    evaluator.EvaluateRollupFields(account);

    // Assert
    var updatedAccount = context.Data["account"][accountId];
    Assert.Equal(2, updatedAccount.GetAttributeValue<int>("opportunitycount"));
    Assert.Equal(150000m, updatedAccount.GetAttributeValue<decimal>("totalrevenue"));
}
```

## Testing Patterns

### Test Rollup Field in Plugin

```csharp
[Fact]
public void Plugin_Should_Use_Rollup_Field()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var evaluator = context.RollupFieldEvaluator;
    
    // Register rollup field
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);
    
    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };
    
    var opp = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };
    
    context.Initialize(new[] { account, opp });
    
    // Evaluate rollup before plugin execution
    evaluator.EvaluateRollupFields(account);
    
    // Act - Execute plugin that reads rollup field
    context.ExecutePluginWith<MyAccountPlugin>(
        pluginContext => {
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 40;
        },
        refObject: account
    );
    
    // Plugin can now access totalrevenue rollup field value
}
```

### Test Aggregate Query vs Rollup Field

Compare FetchXML aggregate results with rollup field values:

```csharp
[Fact]
public void Rollup_Should_Match_Aggregate_Query()
{
    // Arrange
    var context = (XrmFakedContext)XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    var evaluator = context.RollupFieldEvaluator;
    
    var definition = new RollupFieldDefinition
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "totalrevenue",
        RelatedEntityLogicalName = "opportunity",
        AggregateAttributeLogicalName = "estimatedvalue",
        AggregateFunction = RollupAggregateFunction.Sum,
        ResultType = typeof(decimal)
    };
    evaluator.RegisterRollupField(definition);
    
    var accountId = Guid.NewGuid();
    var account = new Entity("account") { Id = accountId };
    
    var opp1 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 100000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };
    
    var opp2 = new Entity("opportunity")
    {
        Id = Guid.NewGuid(),
        ["estimatedvalue"] = 50000m,
        ["parentaccountid"] = new EntityReference("account", accountId)
    };
    
    context.Initialize(new[] { account, opp1, opp2 });
    
    // Evaluate rollup
    evaluator.EvaluateRollupFields(account);
    var rollupTotal = context.Data["account"][accountId].GetAttributeValue<decimal>("totalrevenue");
    
    // Execute aggregate FetchXML
    var fetchXml = $@"
        <fetch aggregate='true'>
            <entity name='opportunity'>
                <attribute name='estimatedvalue' alias='total' aggregate='sum' />
                <filter>
                    <condition attribute='parentaccountid' operator='eq' value='{accountId}' />
                </filter>
            </entity>
        </fetch>";
    
    var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
    var fetchTotal = ((AliasedValue)results.Entities[0]["total"]).Value as decimal?;
    
    // Assert - Both should produce same result
    Assert.Equal(rollupTotal, fetchTotal);
}
```

## Key Differences from Other Testing Frameworks

**Note**: Rollup field support varies across testing frameworks. The implementation in Fake4Dataverse is based on Microsoft's official Dataverse documentation and provides the following approach:

### Comparison Table

| Feature | Fake4Dataverse v4 | Notes |
|---------|-------------------|-------|
| **Registration** | Code-based via `RegisterRollupField()` | Explicit registration with strongly-typed definitions |
| **Evaluation** | Manual via `EvaluateRollupFields()` or automatic on related record changes | Flexible control over when rollup fields are calculated |
| **Auto-refresh** | ✅ **Automatic when related records change** | Matches Dataverse behavior for Create/Update/Delete operations |
| **Hierarchical rollups** | Planned (placeholder exists) | Future enhancement |
| **Custom filters** | Lambda expression predicates | Type-safe C# predicates for flexible filtering |
| **State filters** | Dedicated `StateFilter` property | Built-in support for Active/Inactive/All |

### Setup Approach

**Fake4Dataverse v4:**
```csharp
// Code-based approach with explicit registration
var evaluator = context.RollupFieldEvaluator;
evaluator.RegisterRollupField(new RollupFieldDefinition
{
    EntityLogicalName = "account",
    AttributeLogicalName = "totalrevenue",
    RelatedEntityLogicalName = "opportunity",
    AggregateAttributeLogicalName = "estimatedvalue",
    AggregateFunction = RollupAggregateFunction.Sum,
    Filter = entity => /* lambda expression */
});

// Initial evaluation required
evaluator.EvaluateRollupFields(account);

// After initial evaluation, automatic refresh handles updates
```

## Supported Data Types

### Aggregate Function Compatibility

| Function | Supported Types |
|----------|----------------|
| **COUNT** | All entity types (counts records, not field values) |
| **SUM** | Integer, Decimal, Money (Currency), Double |
| **AVG** | Integer, Decimal, Money (Currency), Double |
| **MIN** | Integer, Decimal, Money (Currency), Double, DateTime |
| **MAX** | Integer, Decimal, Money (Currency), Double, DateTime |

## Limitations and Known Issues

1. **Manual Initial Evaluation**: Rollup fields must be evaluated at least once manually after registration using `EvaluateRollupFields()`. After that, automatic refresh handles updates.

2. **Hierarchical Rollups**: Currently not fully implemented. The `IsHierarchical` property exists but the logic to traverse hierarchies is placeholder code.

3. **Relationship Detection**: Uses simple lookup field matching. Complex relationships may not be detected correctly.

4. **No CalculateRollupField Message**: The actual Dataverse `CalculateRollupFieldRequest` message executor is not implemented. Use `TriggerRollupCalculation()` instead.

## Related Features

- [CRUD Operations](./crud-operations.md) - Basic entity operations
- [Querying Data](./querying-data.md) - FetchXML aggregate queries
- [Calculated Fields](./calculated-fields.md) - Field-level formulas
- [Testing Plugins](./testing-plugins.md) - Plugin testing with rollup fields

## References

- [Microsoft: Define Rollup Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields)
- [Microsoft: Types of Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields)
- [Microsoft: CalculateRollupFieldRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.calculaterollupfieldrequest)
