# Specialized Messages

> **📝 Note**: This documentation is currently under development.

## Overview

This section covers other specialized messages supported by Fake4Dataverse that don't fit into other categories.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| WhoAmI | `WhoAmIRequest` | Get current user information |
| RetrieveVersion | `RetrieveVersionRequest` | Get organization version |
| ExecuteFetch | `ExecuteFetchRequest` | Execute FetchXML query |
| FetchXmlToQueryExpression | `FetchXmlToQueryExpressionRequest` | Convert FetchXML to QueryExpression |
| RetrieveExchangeRate | `RetrieveExchangeRateRequest` | Get exchange rate |
| SendEmail | `SendEmailRequest` | Send email |
| PublishXml | `PublishXmlRequest` | Publish metadata changes |
| BulkDelete | `BulkDeleteRequest` | Bulk delete operation |

## Quick Examples

### WhoAmI

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Execute_WhoAmI()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var userId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    var request = new WhoAmIRequest();
    var response = (WhoAmIResponse)service.Execute(request);
    
    Assert.Equal(userId, response.UserId);
}
```

### ExecuteFetch

```csharp
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

[Fact]
public void Should_Execute_FetchXML()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
            </entity>
        </fetch>";
    
    var request = new ExecuteFetchRequest { FetchXml = fetchXml };
    var response = (ExecuteFetchResponse)service.Execute(request);
}
```

## Coming Soon

Detailed documentation for each specialized message with examples.

## See Also

- [Message Executors Overview](./README.md)
- [Querying Data](../usage/querying-data.md) - For FetchXML examples
