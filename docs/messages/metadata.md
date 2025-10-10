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

## Coming Soon

Detailed documentation for each metadata message with examples.

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Metadata Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/metadata-services)
