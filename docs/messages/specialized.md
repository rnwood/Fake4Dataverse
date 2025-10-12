# Specialized Messages

> **📝 Note**: This documentation is currently under development.

## Overview

This section covers other specialized messages supported by Fake4Dataverse that don't fit into other categories.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| WhoAmI | `WhoAmIRequest` | Get current user information |
| RetrieveVersion | `RetrieveVersionRequest` | Get organization version |
| RetrieveDuplicates | `RetrieveDuplicatesRequest` | Detect duplicate records |
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

### RetrieveDuplicates

Detect duplicate records based on configured duplicate detection rules.

**Reference:** [RetrieveDuplicatesRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest) - Detects and retrieves duplicate records for a specified record based on duplicate detection rules (duplicaterule and duplicaterulecondition entities). Only active and published rules are evaluated.

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Detect_Duplicate_Accounts()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Set up duplicate detection rule
    var duplicateRule = new Entity("duplicaterule")
    {
        Id = Guid.NewGuid(),
        ["baseentityname"] = "account",
        ["matchingentityname"] = "account",
        ["statecode"] = new OptionSetValue(0),  // Active
        ["statuscode"] = new OptionSetValue(2)   // Published
    };
    
    var condition = new Entity("duplicaterulecondition")
    {
        Id = Guid.NewGuid(),
        ["duplicateruleid"] = duplicateRule.ToEntityReference(),
        ["baseattributename"] = "accountnumber",
        ["matchingattributename"] = "accountnumber",
        ["operatorcode"] = new OptionSetValue(0)  // ExactMatch
    };
    
    var account1 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"
    };
    
    var account2 = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["accountnumber"] = "ACC-001"  // Duplicate!
    };
    
    context.Initialize(new[] { account1, account2, duplicateRule, condition });
    
    var request = new RetrieveDuplicatesRequest
    {
        BusinessEntity = account1,
        MatchingEntityName = "account"
    };
    
    var response = (RetrieveDuplicatesResponse)service.Execute(request);
    
    Assert.Single(response.DuplicateCollection.Entities);
    Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
}
```

**See also:** [Duplicate Detection Guide](../usage/duplicate-detection.md) for comprehensive examples

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

## Detailed Examples

### RetrieveVersion

Get the version of the Dataverse organization.

**Reference:** [RetrieveVersionRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.retrieveversionrequest) - Retrieves the version number of the Dataverse server, returning a string in format "Major.Minor.Build.Revision" (e.g., "9.2.0.0").

```csharp
using Microsoft.Xrm.Sdk.Messages;

[Fact]
public void Should_Retrieve_Organization_Version()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new RetrieveVersionRequest();
    var response = (RetrieveVersionResponse)service.Execute(request);
    
    Assert.NotNull(response.Version);
    Assert.NotEmpty(response.Version);
}
```

### FetchXmlToQueryExpression

Convert FetchXML to a QueryExpression for programmatic query building.

**Reference:** [FetchXmlToQueryExpressionRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.fetchxmltoqueryexpressionrequest) - Converts a FetchXML query string into a QueryExpression object, enabling conversion between the two query formats.

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

[Fact]
public void Should_Convert_FetchXml_To_QueryExpression()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var fetchXml = @"
        <fetch>
            <entity name='account'>
                <attribute name='name' />
                <filter>
                    <condition attribute='revenue' operator='gt' value='100000' />
                </filter>
            </entity>
        </fetch>";
    
    var request = new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml };
    var response = (FetchXmlToQueryExpressionResponse)service.Execute(request);
    
    Assert.NotNull(response.Query);
    Assert.Equal("account", response.Query.EntityName);
}
```

### RetrieveExchangeRate

Retrieve currency exchange rates.

**Reference:** [RetrieveExchangeRateRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveexchangeraterequest) - Retrieves the exchange rate between a transaction currency and the organization's base currency for a given date.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Retrieve_Exchange_Rate()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var currencyId = Guid.NewGuid();
    context.Initialize(new Entity("transactioncurrency")
    {
        Id = currencyId,
        ["currencyname"] = "US Dollar",
        ["exchangerate"] = 1.0m
    });
    
    var request = new RetrieveExchangeRateRequest
    {
        TransactionCurrencyId = currencyId
    };
    
    var response = (RetrieveExchangeRateResponse)service.Execute(request);
    Assert.Equal(1.0m, response.ExchangeRate);
}
```

### SendEmail

Send an email message.

**Reference:** [SendEmailRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.sendemailrequest) - Sends an email message that is already created in Dataverse, typically used to send emails from workflows or plugins after the email entity record has been created.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Send_Email()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var emailId = Guid.NewGuid();
    context.Initialize(new Entity("email")
    {
        Id = emailId,
        ["subject"] = "Test Email",
        ["description"] = "Test message body"
    });
    
    var request = new SendEmailRequest
    {
        EmailId = emailId,
        IssueSend = true,
        TrackingToken = ""
    };
    
    var response = (SendEmailResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### PublishXml

Publish metadata changes (for testing metadata modifications).

**Reference:** [PublishXmlRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.publishxmlrequest) - Publishes customization changes including entities, attributes, forms, and other metadata modifications, making them available to the organization.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Publish_Metadata_Changes()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new PublishXmlRequest
    {
        ParameterXml = "<importexportxml><entities><entity>account</entity></entities></importexportxml>"
    };
    
    var response = (PublishXmlResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### BulkDelete

Perform bulk delete operations.

**Reference:** [BulkDeleteRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.bulkdeleterequest) - Submits a bulk delete job that runs asynchronously to delete records matching a query criteria, returning a job ID to track the operation.

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

[Fact]
public void Should_Submit_Bulk_Delete_Job()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Initialize test data
    context.Initialize(new[]
    {
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Old Account 1" },
        new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Old Account 2" }
    });
    
    var query = new QueryExpression("account")
    {
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("name", ConditionOperator.BeginsWith, "Old")
            }
        }
    };
    
    var request = new BulkDeleteRequest
    {
        JobName = "Delete Old Accounts",
        QuerySet = new[] { query },
        StartDateTime = DateTime.Now,
        RecurrencePattern = "",
        SendEmailNotification = false,
        ToRecipients = new Guid[] { },
        CCRecipients = new Guid[] { }
    };
    
    var response = (BulkDeleteResponse)service.Execute(request);
    Assert.NotNull(response);
    Assert.NotEqual(Guid.Empty, response.JobId);
}
```

## Testing Tips

### Testing Message Execution

```csharp
[Theory]
[InlineData("account")]
[InlineData("contact")]
public void Should_Execute_FetchXml_For_Entity(string entityName)
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    context.Initialize(new Entity(entityName)
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test"
    });
    
    var fetchXml = $@"
        <fetch>
            <entity name='{entityName}'>
                <attribute name='name' />
            </entity>
        </fetch>";
    
    var request = new ExecuteFetchRequest { FetchXml = fetchXml };
    var response = (ExecuteFetchResponse)service.Execute(request);
    
    Assert.NotNull(response.FetchXmlResult);
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Querying Data](../usage/querying-data.md) - For FetchXML examples
