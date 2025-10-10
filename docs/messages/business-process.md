# Business Process Messages

> **📝 Note**: This documentation is currently under development.

## Overview

Business process messages handle business-specific operations like managing opportunities, cases, and other business entities.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| Assign | `AssignRequest` | Assign record to another user/team |
| SetState | `SetStateRequest` | Change state and status of a record |
| CloseIncident | `CloseIncidentRequest` | Close a case/incident |
| CloseQuote | `CloseQuoteRequest` | Close a quote |
| WinOpportunity | `WinOpportunityRequest` | Mark opportunity as won |
| LoseOpportunity | `LoseOpportunityRequest` | Mark opportunity as lost |
| QualifyLead | `QualifyLeadRequest` | Qualify a lead |
| Merge | `MergeRequest` | Merge two entity records |
| InitializeFrom | `InitializeFromRequest` | Initialize entity from another |
| ReviseQuote | `ReviseQuoteRequest` | Create revised quote |

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Close_Incident()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var incidentId = Guid.NewGuid();
    context.Initialize(new Entity("incident")
    {
        Id = incidentId,
        ["title"] = "Test Case"
    });
    
    var request = new CloseIncidentRequest
    {
        IncidentResolution = new Entity("incidentresolution")
        {
            ["subject"] = "Resolved",
            ["incidentid"] = new EntityReference("incident", incidentId)
        },
        Status = new OptionSetValue(5) // Resolved
    };
    
    var response = (CloseIncidentResponse)service.Execute(request);
}
```

## Detailed Examples

### CloseQuote

Close a quote record.

**Reference:** [CloseQuoteRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.closequoterequest) - Closes a quote by setting its state to Closed and status to Won, Lost, Canceled, or Revised, along with creating a quote close activity record.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Close_Quote_As_Won()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var quoteId = Guid.NewGuid();
    context.Initialize(new Entity("quote")
    {
        Id = quoteId,
        ["name"] = "Q-1001"
    });
    
    var request = new CloseQuoteRequest
    {
        QuoteClose = new Entity("quoteclose")
        {
            ["subject"] = "Quote Won",
            ["quoteid"] = new EntityReference("quote", quoteId)
        },
        Status = new OptionSetValue(3) // Won
    };
    
    var response = (CloseQuoteResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### WinOpportunity

Mark an opportunity as won.

**Reference:** [WinOpportunityRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.winopportunityrequest) - Sets an opportunity to Won status, creating an opportunity close activity and updating the opportunity's actual revenue and close date.

```csharp
[Fact]
public void Should_Win_Opportunity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var oppId = Guid.NewGuid();
    context.Initialize(new Entity("opportunity")
    {
        Id = oppId,
        ["name"] = "Big Deal",
        ["estimatedvalue"] = new Money(100000)
    });
    
    var request = new WinOpportunityRequest
    {
        OpportunityClose = new Entity("opportunityclose")
        {
            ["subject"] = "Deal Won!",
            ["opportunityid"] = new EntityReference("opportunity", oppId),
            ["actualrevenue"] = new Money(95000)
        },
        Status = new OptionSetValue(3) // Won
    };
    
    var response = (WinOpportunityResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### LoseOpportunity

Mark an opportunity as lost.

**Reference:** [LoseOpportunityRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.loseopportunityrequest) - Sets an opportunity to Lost status, creating an opportunity close activity with the reason for losing and closing the opportunity.

```csharp
[Fact]
public void Should_Lose_Opportunity()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var oppId = Guid.NewGuid();
    context.Initialize(new Entity("opportunity")
    {
        Id = oppId,
        ["name"] = "Lost Deal"
    });
    
    var request = new LoseOpportunityRequest
    {
        OpportunityClose = new Entity("opportunityclose")
        {
            ["subject"] = "Lost to competitor",
            ["opportunityid"] = new EntityReference("opportunity", oppId)
        },
        Status = new OptionSetValue(4) // Lost
    };
    
    var response = (LoseOpportunityResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### QualifyLead

Convert a lead to account, contact, and opportunity.

**Reference:** [QualifyLeadRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.qualifyleadrequest) - Qualifies a lead by converting it into account, contact, and/or opportunity records based on the CreateAccount, CreateContact, and CreateOpportunity flags.

```csharp
[Fact]
public void Should_Qualify_Lead()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var leadId = Guid.NewGuid();
    context.Initialize(new Entity("lead")
    {
        Id = leadId,
        ["firstname"] = "John",
        ["lastname"] = "Doe",
        ["companyname"] = "Contoso"
    });
    
    var request = new QualifyLeadRequest
    {
        LeadId = new EntityReference("lead", leadId),
        CreateAccount = true,
        CreateContact = true,
        CreateOpportunity = true,
        Status = new OptionSetValue(3) // Qualified
    };
    
    var response = (QualifyLeadResponse)service.Execute(request);
    
    Assert.NotNull(response.CreatedEntities);
    Assert.Contains(response.CreatedEntities, e => e.LogicalName == "account");
    Assert.Contains(response.CreatedEntities, e => e.LogicalName == "contact");
    Assert.Contains(response.CreatedEntities, e => e.LogicalName == "opportunity");
}
```

### InitializeFrom

Create a new record based on an existing record.

**Reference:** [InitializeFromRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.initializefromrequest) - Creates a new entity record initialized with values from an existing record based on entity mapping rules (e.g., creating an opportunity from a lead).

```csharp
[Fact]
public void Should_Initialize_Opportunity_From_Account()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = accountId,
        ["name"] = "Contoso Corp",
        ["revenue"] = new Money(1000000)
    });
    
    var request = new InitializeFromRequest
    {
        EntityMoniker = new EntityReference("account", accountId),
        TargetEntityName = "opportunity",
        TargetFieldType = TargetFieldType.ValidForCreate
    };
    
    var response = (InitializeFromResponse)service.Execute(request);
    
    Assert.NotNull(response.Entity);
    Assert.Equal("opportunity", response.Entity.LogicalName);
}
```

### ReviseQuote

Create a revised version of a quote.

**Reference:** [ReviseQuoteRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.revisequoterequest) - Creates a new quote revision by copying the original quote and incrementing the revision number, used when a quote needs to be modified after being sent.

```csharp
[Fact]
public void Should_Revise_Quote()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var quoteId = Guid.NewGuid();
    context.Initialize(new Entity("quote")
    {
        Id = quoteId,
        ["name"] = "Q-1001",
        ["revisionnumber"] = 0
    });
    
    var request = new ReviseQuoteRequest
    {
        QuoteId = quoteId
    };
    
    var response = (ReviseQuoteResponse)service.Execute(request);
    
    Assert.NotNull(response.Entity);
    Assert.Equal("quote", response.Entity.LogicalName);
    Assert.NotEqual(quoteId, response.Entity.Id);
}
```

## Complete Business Process Examples

### Opportunity Lifecycle

```csharp
[Fact]
public void Should_Process_Complete_Opportunity_Lifecycle()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Step 1: Create lead
    var leadId = Guid.NewGuid();
    context.Initialize(new Entity("lead")
    {
        Id = leadId,
        ["firstname"] = "Jane",
        ["lastname"] = "Smith",
        ["companyname"] = "Fabrikam"
    });
    
    // Step 2: Qualify lead to opportunity
    var qualifyRequest = new QualifyLeadRequest
    {
        LeadId = new EntityReference("lead", leadId),
        CreateAccount = true,
        CreateContact = true,
        CreateOpportunity = true,
        Status = new OptionSetValue(3)
    };
    var qualifyResponse = (QualifyLeadResponse)service.Execute(qualifyRequest);
    
    var opportunityRef = qualifyResponse.CreatedEntities
        .First(e => e.LogicalName == "opportunity");
    
    // Step 3: Win opportunity
    var winRequest = new WinOpportunityRequest
    {
        OpportunityClose = new Entity("opportunityclose")
        {
            ["subject"] = "Won!",
            ["opportunityid"] = opportunityRef,
            ["actualrevenue"] = new Money(50000)
        },
        Status = new OptionSetValue(3)
    };
    service.Execute(winRequest);
    
    // Verify opportunity is closed as won
    var opp = service.Retrieve("opportunity", opportunityRef.Id, new ColumnSet("statecode"));
    Assert.Equal(1, opp.GetAttributeValue<OptionSetValue>("statecode").Value); // Won
}
```

### Quote to Order Process

```csharp
[Fact]
public void Should_Process_Quote_To_Order()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var oppId = Guid.NewGuid();
    var quoteId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("opportunity") { Id = oppId, ["name"] = "Big Deal" },
        new Entity("quote") 
        { 
            Id = quoteId, 
            ["name"] = "Q-1001",
            ["opportunityid"] = new EntityReference("opportunity", oppId)
        }
    });
    
    // Step 1: Revise quote if needed
    var reviseRequest = new ReviseQuoteRequest { QuoteId = quoteId };
    var reviseResponse = (ReviseQuoteResponse)service.Execute(reviseRequest);
    var revisedQuoteId = reviseResponse.Entity.Id;
    
    // Step 2: Close revised quote as won
    var closeRequest = new CloseQuoteRequest
    {
        QuoteClose = new Entity("quoteclose")
        {
            ["subject"] = "Quote Accepted",
            ["quoteid"] = new EntityReference("quote", revisedQuoteId)
        },
        Status = new OptionSetValue(3) // Won
    };
    service.Execute(closeRequest);
    
    Assert.NotEqual(quoteId, revisedQuoteId);
}
```

### Case Resolution Workflow

```csharp
[Fact]
public void Should_Resolve_Case_Workflow()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var incidentId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("incident") { Id = incidentId, ["title"] = "Support Case" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "Support Agent" }
    });
    
    // Assign case to agent
    var assignRequest = new AssignRequest
    {
        Assignee = new EntityReference("systemuser", userId),
        Target = new EntityReference("incident", incidentId)
    };
    service.Execute(assignRequest);
    
    // Resolve case
    var closeRequest = new CloseIncidentRequest
    {
        IncidentResolution = new Entity("incidentresolution")
        {
            ["subject"] = "Issue Resolved",
            ["incidentid"] = new EntityReference("incident", incidentId)
        },
        Status = new OptionSetValue(5) // Resolved
    };
    service.Execute(closeRequest);
    
    // Verify case is resolved
    var incident = service.Retrieve("incident", incidentId, new ColumnSet("statecode"));
    Assert.Equal(1, incident.GetAttributeValue<OptionSetValue>("statecode").Value); // Resolved
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Testing Plugins](../usage/testing-plugins.md)
