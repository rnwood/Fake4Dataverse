# Metadata Messages

> **📝 Note**: This documentation is currently under development.

## Overview

Metadata messages retrieve information about entity and attribute definitions in Dataverse.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| RetrieveEntity | `RetrieveEntityRequest` | Retrieve entity metadata |
| RetrieveAttribute | `RetrieveAttributeRequest` | Retrieve attribute metadata |
| RetrieveOptionSet | `RetrieveOptionSetRequest` | Retrieve option set metadata |
| RetrieveRelationship | `RetrieveRelationshipRequest` | Retrieve relationship metadata |
| InsertOptionValue | `InsertOptionValueRequest` | Insert option value (for testing) |
| InsertStatusValue | `InsertStatusValueRequest` | Insert status value (for testing) |

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

[Fact]
public void Should_Retrieve_Entity_Metadata()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveEntityRequest
    {
        LogicalName = "account",
        EntityFilters = EntityFilters.Attributes
    };
    
    var response = (RetrieveEntityResponse)service.Execute(request);
    // Note: Metadata support in Fake4Dataverse is limited
}
```

## Detailed Examples

### RetrieveAttribute

Retrieve attribute (field) metadata.

**Reference:** [RetrieveAttributeRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.retrieveattributerequest) - Retrieves metadata for a specific attribute (field) of an entity, including its type, display name, requirements, and other properties.

```csharp
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

[Fact]
public void Should_Retrieve_Attribute_Metadata()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveAttributeRequest
    {
        EntityLogicalName = "account",
        LogicalName = "name",
        RetrieveAsIfPublished = true
    };
    
    var response = (RetrieveAttributeResponse)service.Execute(request);
    
    // Note: Metadata support in Fake4Dataverse is limited
    Assert.NotNull(response);
}
```

### RetrieveOptionSet

Retrieve option set (picklist) metadata.

**Reference:** [RetrieveOptionSetRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.retrieveoptionsetrequest) - Retrieves the definition of an option set (picklist), including all its options with their values, labels, and colors.

```csharp
[Fact]
public void Should_Retrieve_OptionSet_Metadata()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveOptionSetRequest
    {
        Name = "account_industrycode",
        RetrieveAsIfPublished = true
    };
    
    var response = (RetrieveOptionSetResponse)service.Execute(request);
    
    // Note: Metadata support in Fake4Dataverse is limited
    Assert.NotNull(response);
}
```

### RetrieveRelationship

Retrieve relationship metadata.

**Reference:** [RetrieveRelationshipRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.retrieverelationshiprequest) - Retrieves metadata for an entity relationship, including relationship type (1:N, N:1, N:N) and participating entities.

```csharp
[Fact]
public void Should_Retrieve_Relationship_Metadata()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveRelationshipRequest
    {
        Name = "account_primary_contact",
        RetrieveAsIfPublished = true
    };
    
    var response = (RetrieveRelationshipResponse)service.Execute(request);
    
    // Note: Metadata support in Fake4Dataverse is limited
    Assert.NotNull(response);
}
```

### InsertOptionValue

Insert a new option into an option set (for testing).

**Reference:** [InsertOptionValueRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.insertoptionvaluerequest) - Adds a new option to a picklist or status attribute, specifying the option value, label, and optional description.

```csharp
using Microsoft.Xrm.Sdk.Metadata;

[Fact]
public void Should_Insert_Option_Value()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new InsertOptionValueRequest
    {
        EntityLogicalName = "account",
        AttributeLogicalName = "industrycode",
        Value = 999,
        Label = new Label(new LocalizedLabel("Custom Industry", 1033), new LocalizedLabel[] { })
    };
    
    var response = (InsertOptionValueResponse)service.Execute(request);
    
    Assert.NotNull(response);
    Assert.True(response.NewOptionValue > 0);
}
```

### InsertStatusValue

Insert a new status value (for testing state transitions).

**Reference:** [InsertStatusValueRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.insertstatusvaluerequest) - Adds a new status option to a status attribute, associating it with a specific state value for entity lifecycle management.

```csharp
[Fact]
public void Should_Insert_Status_Value()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new InsertStatusValueRequest
    {
        EntityLogicalName = "incident",
        AttributeLogicalName = "statuscode",
        StateCode = 0, // Active
        Value = 99,
        Label = new Label(new LocalizedLabel("Custom Status", 1033), new LocalizedLabel[] { })
    };
    
    var response = (InsertStatusValueResponse)service.Execute(request);
    
    Assert.NotNull(response);
    Assert.True(response.NewOptionValue > 0);
}
```

## Working with Limited Metadata Support

Fake4Dataverse has limited metadata support. For most testing scenarios, you won't need full metadata operations. Here are strategies for working within these limitations:

### Testing Entity Definitions

```csharp
[Fact]
public void Should_Work_With_Entity_Without_Full_Metadata()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // You don't need full metadata to test entity operations
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test Account",
        ["revenue"] = new Money(100000),
        ["industrycode"] = new OptionSetValue(1) // Banking
    };
    
    context.Initialize(account);
    
    // Test operations work without metadata
    var retrieved = service.Retrieve("account", account.Id, new ColumnSet(true));
    Assert.Equal("Test Account", retrieved["name"]);
}
```

### Mock Metadata Responses

For tests that specifically need metadata:

```csharp
[Fact]
public void Should_Handle_Metadata_Dependent_Logic()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // If your code retrieves metadata, you may need to mock the response
    // or refactor to inject metadata dependencies
    
    // Example: Testing code that doesn't directly depend on metadata
    var account = new Entity("account") { ["name"] = "Test" };
    var accountId = service.Create(account);
    
    Assert.NotEqual(Guid.Empty, accountId);
}
```

## Testing Tips

### Metadata-Light Testing

Focus on business logic rather than metadata operations:

```csharp
[Theory]
[InlineData("account", "name")]
[InlineData("contact", "fullname")]
[InlineData("opportunity", "name")]
public void Should_Retrieve_Entity_Primary_Name_Attribute(string entityName, string primaryName)
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var entity = new Entity(entityName)
    {
        Id = Guid.NewGuid(),
        [primaryName] = "Test Entity"
    };
    
    context.Initialize(entity);
    
    var retrieved = service.Retrieve(entityName, entity.Id, new ColumnSet(primaryName));
    Assert.Equal("Test Entity", retrieved[primaryName]);
}
```

### Option Set Testing

Test option set logic without full metadata:

```csharp
[Fact]
public void Should_Work_With_OptionSet_Values()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test",
        ["statecode"] = new OptionSetValue(0), // Active
        ["statuscode"] = new OptionSetValue(1), // Active
        ["industrycode"] = new OptionSetValue(1) // Banking
    };
    
    context.Initialize(account);
    
    var retrieved = service.Retrieve("account", account.Id, new ColumnSet(true));
    Assert.Equal(1, retrieved.GetAttributeValue<OptionSetValue>("industrycode").Value);
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Metadata Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/metadata-services)
