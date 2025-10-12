# Alternate Keys

Fake4Dataverse supports alternate keys for testing scenarios where records are identified by attributes other than their primary GUID. This feature allows you to test code that uses alternate keys for record references, updates, and retrievals.

## Overview

**Reference:** https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity

Alternate keys in Dataverse allow you to uniquely identify records using one or more attributes instead of the primary GUID. This is particularly useful for:

- Integration scenarios where external systems use natural keys
- Improving performance by avoiding additional lookups
- Creating more readable and maintainable code

## How Alternate Keys Work

Alternate keys are defined in the `EntityKeyMetadata` class:

- **KeyAttributes**: Array of attribute names that form the key
- Multiple alternate keys can be defined per entity
- An alternate key can consist of one or more attributes
- All attributes in a key must have values for the key to be valid

## Configuration

### Setting Up Alternate Keys

To use alternate keys in your tests, you must first configure the metadata:

```csharp
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

public class AlternateKeyTests
{
    private readonly IXrmFakedContext _context;
    private readonly IOrganizationService _service;

    public AlternateKeyTests()
    {
        _context = XrmFakedContextFactory.New();
        _service = _context.GetOrganizationService();
    }

    private void SetupAlternateKey()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
        // EntityKeyMetadata defines alternate keys with KeyAttributes property containing
        // an array of attribute names that uniquely identify records
        
        var accountMetadata = new EntityMetadata();
        accountMetadata.LogicalName = "account";
        
        var alternateKeyMetadata = new EntityKeyMetadata();
        alternateKeyMetadata.KeyAttributes = new string[] { "accountnumber" };
        
        // Use reflection to set the internal _keys field
        accountMetadata.SetFieldValue("_keys", new EntityKeyMetadata[]
        {
            alternateKeyMetadata
        });
        
        _context.InitializeMetadata(accountMetadata);
    }
}
```

### Multiple Attribute Keys

An alternate key can consist of multiple attributes:

```csharp
private void SetupMultiAttributeKey()
{
    var contactMetadata = new EntityMetadata();
    contactMetadata.LogicalName = "contact";
    
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
    // An alternate key can consist of multiple attributes. All attributes must match
    // for the key to uniquely identify a record.
    var alternateKeyMetadata = new EntityKeyMetadata();
    alternateKeyMetadata.KeyAttributes = new string[] 
    { 
        "firstname", 
        "lastname", 
        "emailaddress1" 
    };
    
    contactMetadata.SetFieldValue("_keys", new EntityKeyMetadata[]
    {
        alternateKeyMetadata
    });
    
    _context.InitializeMetadata(contactMetadata);
}
```

### Multiple Alternate Keys

An entity can have multiple alternate keys:

```csharp
private void SetupMultipleAlternateKeys()
{
    var accountMetadata = new EntityMetadata();
    accountMetadata.LogicalName = "account";
    
    // First alternate key: account number
    var key1 = new EntityKeyMetadata();
    key1.KeyAttributes = new string[] { "accountnumber" };
    
    // Second alternate key: email address
    var key2 = new EntityKeyMetadata();
    key2.KeyAttributes = new string[] { "emailaddress1" };
    
    // Third alternate key: name + website combination
    var key3 = new EntityKeyMetadata();
    key3.KeyAttributes = new string[] { "name", "websiteurl" };
    
    accountMetadata.SetFieldValue("_keys", new EntityKeyMetadata[]
    {
        key1, key2, key3
    });
    
    _context.InitializeMetadata(accountMetadata);
}
```

## Basic Usage

### Creating Records

Records created normally can be referenced later by alternate keys:

```csharp
[Fact]
public void Should_Create_Record_With_Alternate_Key_Attribute()
{
    // Arrange
    SetupAlternateKey();
    
    var account = new Entity("account")
    {
        ["name"] = "Contoso Ltd",
        ["accountnumber"] = "ACC-001"  // Alternate key value
    };
    
    // Act
    var id = _service.Create(account);
    
    // Assert
    Assert.NotEqual(Guid.Empty, id);
    
    // Can retrieve by GUID
    var retrieved = _service.Retrieve("account", id, new ColumnSet(true));
    Assert.Equal("ACC-001", retrieved["accountnumber"]);
}
```

### Retrieving by Alternate Key

Use the alternate key constructor to retrieve records:

```csharp
[Fact]
public void Should_Retrieve_Record_By_Alternate_Key()
{
    // Arrange
    SetupAlternateKey();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso Ltd",
        ["accountnumber"] = "ACC-001"
    };
    _context.Initialize(new[] { account });
    
    // Act - Retrieve using alternate key
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-alternate-key-reference-record
    // Use the Entity constructor with keyName and keyValue to reference by alternate key
    var retrieved = _service.Retrieve(
        "account",
        "accountnumber",        // Key attribute name
        "ACC-001",             // Key value
        new ColumnSet(true)
    );
    
    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal(account.Id, retrieved.Id);
    Assert.Equal("Contoso Ltd", retrieved["name"]);
}
```

### Updating by Alternate Key

Update records using alternate keys without knowing the GUID:

```csharp
[Fact]
public void Should_Update_Record_By_Alternate_Key()
{
    // Arrange
    SetupAlternateKey();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Original Name",
        ["accountnumber"] = "ACC-001"
    };
    _context.Initialize(new[] { account });
    
    // Act - Update using alternate key
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-alternate-key-reference-record
    // Create an Entity with alternate key attributes in KeyAttributes collection
    var updateEntity = new Entity("account", "accountnumber", "ACC-001")
    {
        ["name"] = "Updated Name"
    };
    _service.Update(updateEntity);
    
    // Assert
    var retrieved = _service.Retrieve("account", account.Id, new ColumnSet(true));
    Assert.Equal("Updated Name", retrieved["name"]);
}
```

## Advanced Scenarios

### Entity References with Alternate Keys

Use alternate keys in EntityReference objects:

```csharp
[Fact]
public void Should_Use_Alternate_Key_In_Entity_Reference()
{
    // Arrange
    SetupAlternateKey();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso Ltd",
        ["accountnumber"] = "ACC-001"
    };
    
    var contact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "John",
        ["lastname"] = "Doe"
    };
    
    _context.Initialize(new[] { account, contact });
    
    // Act - Create entity reference using alternate key
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-alternate-key-reference-record
    // EntityReference supports alternate keys via KeyAttributes collection
    var accountRef = new EntityReference("account");
    accountRef.KeyAttributes.Add("accountnumber", "ACC-001");
    
    contact["parentcustomerid"] = accountRef;
    _service.Update(contact);
    
    // Assert
    var retrievedContact = _service.Retrieve("contact", contact.Id, new ColumnSet(true));
    var parentRef = retrievedContact.GetAttributeValue<EntityReference>("parentcustomerid");
    Assert.NotNull(parentRef);
    Assert.Equal(account.Id, parentRef.Id);
}
```

### Multi-Attribute Alternate Keys

Use keys composed of multiple attributes:

```csharp
[Fact]
public void Should_Work_With_Multi_Attribute_Alternate_Key()
{
    // Arrange
    SetupMultiAttributeKey();
    
    var contact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "John",
        ["lastname"] = "Doe",
        ["emailaddress1"] = "john.doe@contoso.com"
    };
    _context.Initialize(new[] { contact });
    
    // Act - Retrieve using multi-attribute key
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
    // When multiple attributes form a key, all must be specified in KeyAttributes
    var entityRef = new EntityReference("contact");
    entityRef.KeyAttributes.Add("firstname", "John");
    entityRef.KeyAttributes.Add("lastname", "Doe");
    entityRef.KeyAttributes.Add("emailaddress1", "john.doe@contoso.com");
    
    var context = _context as XrmFakedContext;
    var resolvedId = context.GetRecordUniqueId(entityRef);
    
    // Assert
    Assert.Equal(contact.Id, resolvedId);
}
```

### Upsert with Alternate Keys

Combine alternate keys with upsert operations:

```csharp
[Fact]
public void Should_Upsert_Using_Alternate_Key()
{
    // Arrange
    SetupAlternateKey();
    
    var existingAccount = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso Ltd",
        ["accountnumber"] = "ACC-001",
        ["revenue"] = new Money(1000000)
    };
    _context.Initialize(new[] { existingAccount });
    
    // Act - Upsert with alternate key
    var upsertEntity = new Entity("account", "accountnumber", "ACC-001")
    {
        ["revenue"] = new Money(2000000)  // Updated value
    };
    
    var request = new UpsertRequest
    {
        Target = upsertEntity
    };
    
    var response = (UpsertResponse)_service.Execute(request);
    
    // Assert - Record was updated, not created
    Assert.False(response.RecordCreated);
    
    var retrieved = _service.Retrieve("account", existingAccount.Id, new ColumnSet(true));
    Assert.Equal(2000000m, ((Money)retrieved["revenue"]).Value);
}
```

## Testing Patterns

### Test Alternate Key Not Found

```csharp
[Fact]
public void Should_Throw_When_Alternate_Key_Not_Found()
{
    // Arrange
    SetupAlternateKey();
    
    // Act & Assert
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-alternate-key-reference-record
    // When a record with the specified alternate key values doesn't exist,
    // an exception is thrown
    var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
    {
        _service.Retrieve(
            "account",
            "accountnumber",
            "NONEXISTENT",
            new ColumnSet(true)
        );
    });
    
    Assert.Contains("Does Not Exist", exception.Message);
}
```

### Test Invalid Alternate Key Attributes

```csharp
[Fact]
public void Should_Throw_When_Key_Attributes_Not_Defined()
{
    // Arrange - No alternate key metadata configured
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };
    _context.Initialize(new[] { account });
    
    // Act & Assert
    // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
    // If alternate key metadata is not defined for the entity,
    // attempts to use key attributes will fail
    var entityRef = new EntityReference("account");
    entityRef.KeyAttributes.Add("accountnumber", "ACC-001");
    
    var context = _context as XrmFakedContext;
    var exception = Assert.Throws<InvalidOperationException>(() =>
    {
        context.GetRecordUniqueId(entityRef);
    });
    
    Assert.Contains("do not exist", exception.Message);
}
```

### Test Null Key Values

```csharp
[Fact]
public void Should_Not_Match_With_Null_Key_Values()
{
    // Arrange
    SetupAlternateKey();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Contoso Ltd",
        ["accountnumber"] = null  // Null key value
    };
    _context.Initialize(new[] { account });
    
    // Act & Assert
    var entityRef = new EntityReference("account");
    entityRef.KeyAttributes.Add("accountnumber", null);
    
    var context = _context as XrmFakedContext;
    var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
    {
        context.GetRecordUniqueId(entityRef);
    });
}
```

## Integration with Reference Validation

When referential integrity is enabled, alternate keys work seamlessly:

```csharp
[Fact]
public void Should_Validate_References_With_Alternate_Keys()
{
    // Arrange - Enable integrity checking
    var contextWithIntegrity = MiddlewareBuilder.New()
        .AddFakeCrud()
        .AddFakeMessageExecutors()
        .UseCrud()
        .UseMessages()
        .WithReferentialIntegrity()  // Enable validation
        .Build();
    
    var serviceWithIntegrity = contextWithIntegrity.GetOrganizationService();
    
    // Set up metadata
    var accountMetadata = new EntityMetadata();
    accountMetadata.LogicalName = "account";
    var alternateKeyMetadata = new EntityKeyMetadata();
    alternateKeyMetadata.KeyAttributes = new string[] { "accountnumber" };
    accountMetadata.SetFieldValue("_keys", new EntityKeyMetadata[]
    {
        alternateKeyMetadata
    });
    contextWithIntegrity.InitializeMetadata(accountMetadata);
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };
    
    var contact = new Entity("contact")
    {
        Id = Guid.NewGuid(),
        ["firstname"] = "John"
    };
    
    contextWithIntegrity.Initialize(new[] { account, contact });
    
    // Act - Create reference using alternate key
    var accountRef = new EntityReference("account");
    accountRef.KeyAttributes.Add("accountnumber", "ACC-001");
    
    contact["parentcustomerid"] = accountRef;
    
    // Should not throw - reference is valid
    serviceWithIntegrity.Update(contact);
    
    // Assert
    var retrieved = serviceWithIntegrity.Retrieve("contact", contact.Id, new ColumnSet(true));
    Assert.NotNull(retrieved["parentcustomerid"]);
}
```

## Best Practices

1. **Choose Stable Attributes** - Use attributes that don't change frequently as keys
2. **Document Key Requirements** - Clearly document which attributes form alternate keys
3. **Test Null Scenarios** - Always test behavior with null key values
4. **Validate Before Use** - Ensure metadata is properly configured before using alternate keys
5. **Consider Performance** - In real Dataverse, alternate keys require indexes; test accordingly
6. **Use Natural Keys** - Choose attributes that are meaningful to external systems

## Important Behaviors

### Case Sensitivity

Alternate key matching is case-insensitive for string attributes:

```csharp
// These will match the same record
var ref1 = new EntityReference("account");
ref1.KeyAttributes.Add("accountnumber", "ACC-001");

var ref2 = new EntityReference("account");
ref2.KeyAttributes.Add("accountnumber", "acc-001");
```

### All Attributes Must Match

For multi-attribute keys, all attributes must match:

```csharp
// This will NOT match if only firstname and lastname match
var entityRef = new EntityReference("contact");
entityRef.KeyAttributes.Add("firstname", "John");
entityRef.KeyAttributes.Add("lastname", "Doe");
// Missing: emailaddress1
```

### Key Attributes Take Precedence

When both Id and KeyAttributes are specified, KeyAttributes are used:

```csharp
var entity = new Entity("account")
{
    Id = someGuid  // This is ignored
};
entity.KeyAttributes.Add("accountnumber", "ACC-001");  // This is used
```

## Limitations

1. **No Index Validation** - The framework doesn't validate that keys are indexed
2. **No Uniqueness Enforcement** - Duplicate key values are not prevented at create/update time
3. **No Async Key Creation** - Asynchronous key creation jobs are not simulated
4. **Performance Characteristics** - In-memory lookup performance differs from indexed database lookups

## Error Messages

Common error scenarios and their messages:

```csharp
// Key attributes not defined in metadata
// "The requested key attributes do not exist for the entity {entityName}"

// Record not found with specified key values
// "{entityName} with the specified Alternate Keys Does Not Exist"

// Invalid entity reference (Id empty, no key attributes)
// "{entityName} With Id = {Guid.Empty} Does Not Exist"
```

## See Also

- [Duplicate Detection](./duplicate-detection.md) - Detect duplicates using alternate keys
- [CRUD Operations](./crud-operations.md) - Basic entity operations
- [Reference Validation](./security-permissions.md) - Testing with referential integrity
- [Microsoft Docs: Define Alternate Keys](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity)
- [Microsoft Docs: Use Alternate Keys](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-alternate-key-reference-record)
