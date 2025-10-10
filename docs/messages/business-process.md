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

## Coming Soon

Detailed documentation for each business process message with examples.

## See Also

- [Message Executors Overview](./README.md)
- [Testing Plugins](../usage/testing-plugins.md)
