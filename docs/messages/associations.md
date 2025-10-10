# Association Messages

Documentation for Associate and Disassociate message executors in Fake4Dataverse.

## Overview

Association messages manage many-to-many and one-to-many relationships between entity records in Dataverse. These operations are essential for linking related records.

## Supported Messages

| Message | Request Type | Response Type | Description |
|---------|-------------|---------------|-------------|
| Associate | `AssociateRequest` | `AssociateResponse` | Create relationships between records |
| Disassociate | `DisassociateRequest` | `DisassociateResponse` | Remove relationships between records |

## Associate Message

Creates relationships between entity records.

### Using IOrganizationService

```csharp
// Associate contacts with an account
service.Associate(
    "account",                              // Entity name
    accountId,                              // Entity ID
    new Relationship("contact_customer_accounts"),  // Relationship name
    new EntityReferenceCollection           // Related entities
    {
        new EntityReference("contact", contact1Id),
        new EntityReference("contact", contact2Id)
    }
);
```

### Using AssociateRequest

```csharp
var request = new AssociateRequest
{
    Target = new EntityReference("account", accountId),
    Relationship = new Relationship("contact_customer_accounts"),
    RelatedEntities = new EntityReferenceCollection
    {
        new EntityReference("contact", contact1Id),
        new EntityReference("contact", contact2Id)
    }
};

var response = (AssociateResponse)service.Execute(request);
```

### Complete Example - Many-to-Many

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;
using System.Linq;

[Fact]
public void Should_Associate_Contacts_With_Account()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contact1Id = Guid.NewGuid();
    var contact2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Contoso" },
        new Entity("contact") { Id = contact1Id, ["firstname"] = "John" },
        new Entity("contact") { Id = contact2Id, ["firstname"] = "Jane" }
    });
    
    var request = new AssociateRequest
    {
        Target = new EntityReference("account", accountId),
        Relationship = new Relationship("contact_customer_accounts"),
        RelatedEntities = new EntityReferenceCollection
        {
            new EntityReference("contact", contact1Id),
            new EntityReference("contact", contact2Id)
        }
    };
    
    // Act
    var response = (AssociateResponse)service.Execute(request);
    
    // Assert
    // In Fake4Dataverse, you can verify the association was created
    // (specific verification depends on how your context tracks relationships)
}
```

### One-to-Many Association

For one-to-many relationships, you typically use Update instead of Associate:

```csharp
[Fact]
public void Should_Set_Parent_Account_On_Contact()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Contoso" },
        new Entity("contact") { Id = contactId, ["firstname"] = "John" }
    });
    
    // Set parent account (one-to-many)
    var contactUpdate = new Entity("contact")
    {
        Id = contactId,
        ["parentcustomerid"] = new EntityReference("account", accountId)
    };
    
    service.Update(contactUpdate);
    
    // Verify
    var contact = service.Retrieve("contact", contactId, 
        new ColumnSet("parentcustomerid"));
    
    Assert.Equal(accountId, 
        ((EntityReference)contact["parentcustomerid"]).Id);
}
```

## Disassociate Message

Removes relationships between entity records.

### Using IOrganizationService

```csharp
// Disassociate contacts from account
service.Disassociate(
    "account",
    accountId,
    new Relationship("contact_customer_accounts"),
    new EntityReferenceCollection
    {
        new EntityReference("contact", contact1Id)
    }
);
```

### Using DisassociateRequest

```csharp
var request = new DisassociateRequest
{
    Target = new EntityReference("account", accountId),
    Relationship = new Relationship("contact_customer_accounts"),
    RelatedEntities = new EntityReferenceCollection
    {
        new EntityReference("contact", contact1Id)
    }
};

var response = (DisassociateResponse)service.Execute(request);
```

### Complete Example

```csharp
[Fact]
public void Should_Disassociate_Contact_From_Account()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contact1Id = Guid.NewGuid();
    var contact2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Contoso" },
        new Entity("contact") { Id = contact1Id, ["firstname"] = "John" },
        new Entity("contact") { Id = contact2Id, ["firstname"] = "Jane" }
    });
    
    // First, associate both contacts
    service.Associate(
        "account",
        accountId,
        new Relationship("contact_customer_accounts"),
        new EntityReferenceCollection
        {
            new EntityReference("contact", contact1Id),
            new EntityReference("contact", contact2Id)
        }
    );
    
    // Now disassociate one contact
    var request = new DisassociateRequest
    {
        Target = new EntityReference("account", accountId),
        Relationship = new Relationship("contact_customer_accounts"),
        RelatedEntities = new EntityReferenceCollection
        {
            new EntityReference("contact", contact1Id)
        }
    };
    
    // Act
    var response = (DisassociateResponse)service.Execute(request);
    
    // Assert
    // Verify contact1 is no longer associated
    // (specific verification depends on context implementation)
}
```

## Common Relationship Scenarios

### Marketing List Members

```csharp
[Fact]
public void Should_Add_Contacts_To_Marketing_List()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var listId = Guid.NewGuid();
    var contact1Id = Guid.NewGuid();
    var contact2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("list") { Id = listId, ["listname"] = "Newsletter" },
        new Entity("contact") { Id = contact1Id, ["firstname"] = "John" },
        new Entity("contact") { Id = contact2Id, ["firstname"] = "Jane" }
    });
    
    // Associate contacts with marketing list
    service.Associate(
        "list",
        listId,
        new Relationship("listcontact_association"),
        new EntityReferenceCollection
        {
            new EntityReference("contact", contact1Id),
            new EntityReference("contact", contact2Id)
        }
    );
}
```

### Team Members

```csharp
[Fact]
public void Should_Add_Users_To_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var teamId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("team") { Id = teamId, ["name"] = "Sales Team" },
        new Entity("systemuser") { Id = user1Id, ["fullname"] = "John Doe" },
        new Entity("systemuser") { Id = user2Id, ["fullname"] = "Jane Smith" }
    });
    
    // Associate users with team
    service.Associate(
        "team",
        teamId,
        new Relationship("teammembership_association"),
        new EntityReferenceCollection
        {
            new EntityReference("systemuser", user1Id),
            new EntityReference("systemuser", user2Id)
        }
    );
}
```

### Opportunity Products

```csharp
[Fact]
public void Should_Add_Products_To_Opportunity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var opportunityId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("opportunity") 
        { 
            Id = opportunityId, 
            ["name"] = "Big Deal" 
        },
        new Entity("product") 
        { 
            Id = productId, 
            ["name"] = "Premium Widget" 
        }
    });
    
    // For opportunity products, you typically create opportunityproduct records
    // rather than using Associate
    var oppProduct = new Entity("opportunityproduct")
    {
        ["opportunityid"] = new EntityReference("opportunity", opportunityId),
        ["productid"] = new EntityReference("product", productId),
        ["quantity"] = 10m,
        ["priceperunit"] = new Money(100)
    };
    
    service.Create(oppProduct);
}
```

## Testing Patterns

### Testing Plugin on Associate

```csharp
using Fake4Dataverse.Plugins;

[Fact]
public void Should_Execute_Plugin_On_Associate()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId },
        new Entity("contact") { Id = contactId }
    });
    
    var associateRequest = new AssociateRequest
    {
        Target = new EntityReference("account", accountId),
        Relationship = new Relationship("contact_customer_accounts"),
        RelatedEntities = new EntityReferenceCollection
        {
            new EntityReference("contact", contactId)
        }
    };
    
    // Execute plugin on Associate message
    context.ExecutePluginWith<MyAssociatePlugin>(
        pluginContext =>
        {
            pluginContext.MessageName = "Associate";
            pluginContext.Stage = 20; // Pre-operation
            pluginContext.InputParameters["Target"] = associateRequest.Target;
            pluginContext.InputParameters["Relationship"] = associateRequest.Relationship;
            pluginContext.InputParameters["RelatedEntities"] = associateRequest.RelatedEntities;
        }
    );
}
```

### Verifying Association State

```csharp
[Fact]
public void Should_Verify_Association_Created()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Contoso" },
        new Entity("contact") { Id = contactId, ["firstname"] = "John" }
    });
    
    // Associate
    service.Associate(
        "account",
        accountId,
        new Relationship("contact_customer_accounts"),
        new EntityReferenceCollection
        {
            new EntityReference("contact", contactId)
        }
    );
    
    // In Fake4Dataverse, associations are tracked
    // Verification depends on specific implementation
    // You might query related records or check relationship data
}
```

## Error Scenarios

### Invalid Relationship Name

```csharp
[Fact]
public void Should_Handle_Invalid_Relationship()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId },
        new Entity("contact") { Id = contactId }
    });
    
    // May throw exception for invalid relationship name
    var request = new AssociateRequest
    {
        Target = new EntityReference("account", accountId),
        Relationship = new Relationship("invalid_relationship"),
        RelatedEntities = new EntityReferenceCollection
        {
            new EntityReference("contact", contactId)
        }
    };
    
    // Behavior depends on implementation
}
```

### Entity Not Found

```csharp
[Fact]
public void Should_Handle_Missing_Entity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new AssociateRequest
    {
        Target = new EntityReference("account", Guid.NewGuid()), // Doesn't exist
        Relationship = new Relationship("contact_customer_accounts"),
        RelatedEntities = new EntityReferenceCollection
        {
            new EntityReference("contact", Guid.NewGuid())
        }
    };
    
    Assert.Throws<Exception>(() => service.Execute(request));
}
```

## Best Practices

### ✅ Do

1. **Initialize all entities before associating**
   ```csharp
   context.Initialize(new[] { account, contact });
   service.Associate(...);
   ```

2. **Use correct relationship names**
   ```csharp
   // Find relationship name in customization or metadata
   new Relationship("contact_customer_accounts")
   ```

3. **Handle both directions of relationships**
   ```csharp
   // Can associate from either side
   service.Associate("account", accountId, relationship, contacts);
   // or
   service.Associate("contact", contactId, relationship, accounts);
   ```

### ❌ Don't

1. **Don't associate non-existent entities**
   ```csharp
   // ❌ Bad - entities not initialized
   service.Associate("account", Guid.NewGuid(), ...);
   ```

2. **Don't confuse one-to-many with many-to-many**
   ```csharp
   // ❌ Bad - use Update for one-to-many
   service.Associate("contact", contactId, 
       new Relationship("parentcustomerid"), ...);
   
   // ✅ Good - use Update
   contact["parentcustomerid"] = new EntityReference("account", accountId);
   service.Update(contact);
   ```

## Common Relationships

### Contact to Account (Many-to-Many)
- Relationship: `contact_customer_accounts`
- Entities: `contact`, `account`

### User to Team (Many-to-Many)
- Relationship: `teammembership_association`
- Entities: `systemuser`, `team`

### Contact to Marketing List (Many-to-Many)
- Relationship: `listcontact_association`
- Entities: `contact`, `list`

## See Also

- [CRUD Messages](./crud.md) - Create, Retrieve, Update, Delete
- [Message Executors Overview](./README.md) - All supported messages
- [Testing Plugins](../usage/testing-plugins.md) - Plugin testing patterns

## Reference

- [Associate/Disassociate Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-associate-disassociate)
- [Relationship Definitions](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity)
