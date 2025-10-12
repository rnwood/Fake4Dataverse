# Duplicate Detection

Fake4Dataverse supports duplicate detection for testing scenarios involving the `RetrieveDuplicatesRequest` message. This allows you to test how your code handles duplicate records based on configurable duplicate detection rules.

## Overview

**Reference:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data

Duplicate detection in Dataverse helps prevent duplicate records by defining rules that compare attributes across records. Fake4Dataverse simulates this behavior by evaluating `duplicaterule` and `duplicaterulecondition` entities that you configure in your test context.

**Implementation Date:** January 2025  
**GitHub Issue:** See PR for alternate keys and duplicate detection

## How Duplicate Detection Works

Duplicate detection evaluates:

1. **Duplicate Rules** (`duplicaterule` entity) - Defines which entity types to compare
   - `baseentityname`: Entity type being checked for duplicates
   - `matchingentityname`: Entity type to search for matches
   - `statecode`: Must be 0 (Active) for rule to be evaluated
   - `statuscode`: Must be 2 (Published) for rule to be evaluated

2. **Duplicate Rule Conditions** (`duplicaterulecondition` entity) - Defines comparison criteria
   - `duplicateruleid`: Reference to the parent duplicate rule
   - `baseattributename`: Attribute name in the base entity
   - `matchingattributename`: Attribute name in the matching entity
   - `operatorcode`: Comparison operator (0 = ExactMatch)

**Reference:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities

All conditions in a rule must match for a record to be considered a duplicate (AND logic).

## Basic Usage

### Setting Up a Duplicate Detection Rule

```csharp
using Fake4Dataverse.Abstractions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

public class DuplicateDetectionTests
{
    private readonly IXrmFakedContext _context;
    private readonly IOrganizationService _service;

    public DuplicateDetectionTests()
    {
        _context = XrmFakedContextFactory.New();
        _service = _context.GetOrganizationService();
    }

    [Fact]
    public void Should_Detect_Duplicate_Accounts_By_AccountNumber()
    {
        // Arrange - Create duplicate detection rule
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        // A duplicaterule with statecode=0 (Active) and statuscode=2 (Published) is evaluated
        var duplicateRule = new Entity("duplicaterule")
        {
            Id = Guid.NewGuid(),
            ["baseentityname"] = "account",
            ["matchingentityname"] = "account",
            ["statecode"] = new OptionSetValue(0),   // Active
            ["statuscode"] = new OptionSetValue(2)    // Published
        };

        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        // duplicaterulecondition defines the comparison: operatorcode=0 means ExactMatch
        var condition = new Entity("duplicaterulecondition")
        {
            Id = Guid.NewGuid(),
            ["duplicateruleid"] = duplicateRule.ToEntityReference(),
            ["baseattributename"] = "accountnumber",
            ["matchingattributename"] = "accountnumber",
            ["operatorcode"] = new OptionSetValue(0)  // ExactMatch
        };

        // Create test accounts
        var account1 = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Contoso Ltd",
            ["accountnumber"] = "ACC-001"
        };

        var account2 = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Contoso Corporation",
            ["accountnumber"] = "ACC-001"  // Duplicate!
        };

        _context.Initialize(new[] { account1, account2, duplicateRule, condition });

        // Act - Check for duplicates
        // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
        var request = new RetrieveDuplicatesRequest
        {
            BusinessEntity = account1,
            MatchingEntityName = "account"
        };

        var response = (RetrieveDuplicatesResponse)_service.Execute(request);

        // Assert
        Assert.Single(response.DuplicateCollection.Entities);
        Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
    }
}
```

## Advanced Scenarios

### Multiple Conditions (AND Logic)

When a duplicate rule has multiple conditions, ALL conditions must match:

```csharp
[Fact]
public void Should_Match_Only_When_All_Conditions_Match()
{
    // Arrange
    var duplicateRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "account",
        ["matchingentityname"] = "account",
        ["statecode"] = new OptionSetValue(0),
        ["statuscode"] = new OptionSetValue(2)
    };

    // Both conditions must match
    var condition1 = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = duplicateRule.ToEntityReference(),
        ["baseattributename"] = "accountnumber",
        ["matchingattributename"] = "accountnumber",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var condition2 = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = duplicateRule.ToEntityReference(),
        ["baseattributename"] = "websiteurl",
        ["matchingattributename"] = "websiteurl",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var account1 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001",
        ["websiteurl"] = "www.contoso.com"
    };

    var account2 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001",
        ["websiteurl"] = "www.contoso.com"  // Matches both
    };

    var account3 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001",
        ["websiteurl"] = "www.different.com"  // Matches only first condition
    };

    _context.Initialize(new[] { 
        account1, account2, account3, 
        duplicateRule, condition1, condition2 
    });

    // Act
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = account1,
        MatchingEntityName = "account"
    };

    var response = (RetrieveDuplicatesResponse)_service.Execute(request);

    // Assert - Only account2 matches (account3 doesn't match both conditions)
    Assert.Single(response.DuplicateCollection.Entities);
    Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
}
```

### Cross-Entity Duplicate Detection

Detect duplicates across different entity types:

```csharp
[Fact]
public void Should_Detect_Duplicates_Across_Entity_Types()
{
    // Arrange - Check if contact email matches account email
    var duplicateRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "contact",
        ["matchingentityname"] = "account",  // Different entity!
        ["statecode"] = new OptionSetValue(0),
        ["statuscode"] = new OptionSetValue(2)
    };

    var condition = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = duplicateRule.ToEntityReference(),
        ["baseattributename"] = "emailaddress1",
        ["matchingattributename"] = "emailaddress1",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var contact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "John",
        ["emailaddress1"] = "john@contoso.com"
    };

    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso Ltd",
        ["emailaddress1"] = "john@contoso.com"  // Matches!
    };

    _context.Initialize(new[] { contact, account, duplicateRule, condition });

    // Act
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = contact,
        MatchingEntityName = "account"  // Check against accounts
    };

    var response = (RetrieveDuplicatesResponse)_service.Execute(request);

    // Assert
    Assert.Single(response.DuplicateCollection.Entities);
    Assert.Equal(account.Id, response.DuplicateCollection.Entities[0].Id);
}
```

### Inactive and Unpublished Rules

Only active and published rules are evaluated:

```csharp
[Fact]
public void Should_Ignore_Inactive_Or_Unpublished_Rules()
{
    // Arrange - Create inactive rule
    var inactiveRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "account",
        ["matchingentityname"] = "account",
        ["statecode"] = new OptionSetValue(1),  // Inactive
        ["statuscode"] = new OptionSetValue(2)
    };

    // Create unpublished rule
    var unpublishedRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "account",
        ["matchingentityname"] = "account",
        ["statecode"] = new OptionSetValue(0),  // Active
        ["statuscode"] = new OptionSetValue(0)  // Draft (not published)
    };

    var condition1 = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = inactiveRule.ToEntityReference(),
        ["baseattributename"] = "accountnumber",
        ["matchingattributename"] = "accountnumber",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var condition2 = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = unpublishedRule.ToEntityReference(),
        ["baseattributename"] = "accountnumber",
        ["matchingattributename"] = "accountnumber",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var account1 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };

    var account2 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };

    _context.Initialize(new[] { 
        account1, account2, 
        inactiveRule, unpublishedRule, 
        condition1, condition2 
    });

    // Act
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = account1,
        MatchingEntityName = "account"
    };

    var response = (RetrieveDuplicatesResponse)_service.Execute(request);

    // Assert - No duplicates found (rules not active/published)
    Assert.Empty(response.DuplicateCollection.Entities);
}
```

## Comparison Operators

**Reference:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities

The `operatorcode` attribute in `duplicaterulecondition` determines how attributes are compared:

| Code | Name | Description |
|------|------|-------------|
| 0 | ExactMatch | Values must be exactly the same (case-insensitive for strings) |
| 1 | SameFirstCharacters | Beginning characters must match |
| 2 | SameLastCharacters | Ending characters must match |
| 3 | SameDate | Date values must match (ignoring time) |
| 4 | SameDateAndTime | Date and time must match exactly |
| 5 | SameNotBlank | Both values must be non-blank and match |

**Current Support:** Fake4Dataverse currently supports `ExactMatch` (0) for all attribute types. Other operators default to exact match behavior.

## Important Behaviors

### Case-Insensitive Comparison

String comparisons are case-insensitive:

```csharp
var account1 = new Entity("account")
{
    ["accountnumber"] = "ACC-001"
};

var account2 = new Entity("account")
{
    ["accountnumber"] = "acc-001"  // Different case
};

// These will match as duplicates
```

### Null Values Don't Match

Records with null values in comparison attributes are never considered duplicates:

```csharp
var account1 = new Entity("account")
{
    ["accountnumber"] = null
};

var account2 = new Entity("account")
{
    ["accountnumber"] = null
};

// These will NOT match as duplicates
```

### Source Record Exclusion

When checking for duplicates within the same entity, the source record is excluded from results:

```csharp
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["accountnumber"] = "ACC-001"
};

var request = new RetrieveDuplicatesRequest
{
    BusinessEntity = account,
    MatchingEntityName = "account"
};

// account will not be in its own duplicate results
```

## Testing Patterns

### Test for No Duplicates

```csharp
[Fact]
public void Should_Find_No_Duplicates_When_Values_Differ()
{
    // Arrange
    var duplicateRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "account",
        ["matchingentityname"] = "account",
        ["statecode"] = new OptionSetValue(0),
        ["statuscode"] = new OptionSetValue(2)
    };

    var condition = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = duplicateRule.ToEntityReference(),
        ["baseattributename"] = "accountnumber",
        ["matchingattributename"] = "accountnumber",
        ["operatorcode"] = new OptionSetValue(0)
    };

    var account1 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };

    var account2 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-002"  // Different
    };

    _context.Initialize(new[] { account1, account2, duplicateRule, condition });

    // Act
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = account1,
        MatchingEntityName = "account"
    };

    var response = (RetrieveDuplicatesResponse)_service.Execute(request);

    // Assert
    Assert.Empty(response.DuplicateCollection.Entities);
}
```

### Test Exception Handling

```csharp
[Fact]
public void Should_Throw_When_BusinessEntity_Is_Null()
{
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = null,
        MatchingEntityName = "account"
    };

    Assert.Throws<ArgumentNullException>(() => 
        _service.Execute(request)
    );
}

[Fact]
public void Should_Throw_When_MatchingEntityName_Is_Null()
{
    var account = new Entity("account") { Id = Guid.NewGuid() };
    
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = account,
        MatchingEntityName = null
    };

    Assert.Throws<ArgumentNullException>(() => 
        _service.Execute(request)
    );
}
```

## Integration with Alternate Keys

Duplicate detection works seamlessly with alternate keys. You can use alternate keys to reference records in duplicate rules:

```csharp
// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
// Set up alternate key metadata
var accountMetadata = new EntityMetadata();
accountMetadata.LogicalName = "account";
var alternateKeyMetadata = new EntityKeyMetadata();
alternateKeyMetadata.KeyAttributes = new string[] { "accountnumber" };
accountMetadata.SetFieldValue("_keys", new[] { alternateKeyMetadata });
_context.InitializeMetadata(accountMetadata);

// Create account with alternate key
var account = new Entity("account", "accountnumber", "ACC-001")
{
    ["name"] = "Contoso Ltd"
};
_service.Create(account);

// Retrieve using alternate key
var retrieved = _service.Retrieve(
    "account", 
    "accountnumber", 
    "ACC-001", 
    new ColumnSet(true)
);

// Check for duplicates
var request = new RetrieveDuplicatesRequest
{
    BusinessEntity = retrieved,
    MatchingEntityName = "account"
};
```

## Best Practices

1. **Keep Rules Simple** - Start with single-condition rules and add complexity as needed
2. **Test Edge Cases** - Include tests for null values, case sensitivity, and missing data
3. **Document Rules** - Clearly document what each duplicate rule is checking
4. **Use Meaningful Attribute Names** - Choose attributes that genuinely indicate duplicates
5. **Test Inactive Rules** - Verify your code handles scenarios where rules are disabled
6. **Cross-Entity Carefully** - When checking across entity types, ensure attribute types match

## Error Handling

The `RetrieveDuplicatesRequest` executor validates required properties:

```csharp
// Missing BusinessEntity
var request = new RetrieveDuplicatesRequest
{
    MatchingEntityName = "account"
};
// Throws: ArgumentNullException

// Missing MatchingEntityName
var request = new RetrieveDuplicatesRequest
{
    BusinessEntity = account
};
// Throws: ArgumentNullException
```

## Limitations

1. **Operator Support** - Currently only `ExactMatch` (operatorcode=0) is fully implemented
2. **No Asynchronous Detection** - Background duplicate detection jobs are not simulated
3. **No Automatic Prevention** - Duplicates are detected but not automatically prevented during create/update
4. **No Match Codes** - The `matchcode` table used by real Dataverse is not simulated

## See Also

- [Alternate Keys Documentation](./alternate-keys.md) - Configure alternate keys for entities
- [CRUD Messages](../messages/crud.md) - Basic entity operations
- [Message Executors](../messages/README.md) - All supported messages
- [Microsoft Docs: Detect Duplicate Data](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data)
- [Microsoft Docs: Duplicate Rule Entities](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities)
