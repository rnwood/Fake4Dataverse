# Message Executors Overview

Fake4Dataverse supports a wide range of Dataverse/Dynamics 365 messages through **message executors**. This guide provides an overview of supported messages and how to use them.

## What are Message Executors?

In Dataverse, operations beyond basic CRUD are performed using the `Execute` method with specialized request/response classes. Fake4Dataverse implements these operations through **message executors**.

### Example: Using a Message

```csharp
using Microsoft.Crm.Sdk.Messages;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Execute a WhoAmI request
var request = new WhoAmIRequest();
var response = (WhoAmIResponse)service.Execute(request);

Guid userId = response.UserId;
```

## Supported Message Categories

Fake4Dataverse currently supports **46+ message executors** across these categories:

### 📝 CRUD Messages
Basic create, read, update, delete operations.
- [Learn more](./crud.md)

### 🔗 Association Messages
Managing relationships between entities.
- [Learn more](./associations.md)

### 🔒 Security Messages
Managing access rights and sharing.
- [Learn more](./security.md)

### 📝 Audit Messages
Tracking and retrieving change history.
- [Learn more](./audit.md)

### 💼 Business Process Messages
Business-specific operations like closing cases, winning opportunities.
- [Learn more](./business-process.md)

### 📋 Queue Messages
Queue management operations.
- [Learn more](./queues.md)

### 👥 Team Messages
Team membership management.
- [Learn more](./teams.md)

### 📊 Metadata Messages
Retrieving entity and attribute metadata.
- [Learn more](./metadata.md)

### 🔧 Specialized Messages
Other supported messages.
- [Learn more](./specialized.md)

## Complete List of Supported Messages

### CRUD & Core Operations
| Message | Request Type | Description |
|---------|-------------|-------------|
| Create | `CreateRequest` | Create a new entity record |
| Retrieve | `RetrieveRequest` | Retrieve a single entity record |
| Update | `UpdateRequest` | Update an existing entity record |
| Delete | `DeleteRequest` | Delete an entity record |
| Upsert | `UpsertRequest` | Create or update a record |
| RetrieveMultiple | `RetrieveMultipleRequest` | Query multiple records |

### Association Operations
| Message | Request Type | Description |
|---------|-------------|-------------|
| Associate | `AssociateRequest` | Create relationships between records |
| Disassociate | `DisassociateRequest` | Remove relationships between records |

### Security & Access Control
| Message | Request Type | Description |
|---------|-------------|-------------|
| GrantAccess | `GrantAccessRequest` | Grant access to a record |
| ModifyAccess | `ModifyAccessRequest` | Modify access to a record |
| RevokeAccess | `RevokeAccessRequest` | Revoke access to a record |
| RetrievePrincipalAccess | `RetrievePrincipalAccessRequest` | Get access rights for a principal |
| RetrieveSharedPrincipalsAndAccess | `RetrieveSharedPrincipalsAndAccessRequest` | Get shared access info |

### Audit Operations
| Message | Request Type | Description |
|---------|-------------|-------------|
| RetrieveAuditDetails | `RetrieveAuditDetailsRequest` | Retrieve audit details for specific audit record |
| RetrieveRecordChangeHistory | `RetrieveRecordChangeHistoryRequest` | Get complete change history for a record |
| RetrieveAttributeChangeHistory | `RetrieveAttributeChangeHistoryRequest` | Get change history for specific attribute |

### Business Process Messages
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

### Queue Operations
| Message | Request Type | Description |
|---------|-------------|-------------|
| AddToQueue | `AddToQueueRequest` | Add item to queue |
| RemoveFromQueue | `RemoveFromQueueRequest` | Remove item from queue |
| PickFromQueue | `PickFromQueueRequest` | Pick item from queue |

### Team & Membership
| Message | Request Type | Description |
|---------|-------------|-------------|
| AddMembersTeam | `AddMembersTeamRequest` | Add members to team |
| RemoveMembersTeam | `RemoveMembersTeamRequest` | Remove members from team |
| AddUserToRecordTeam | `AddUserToRecordTeamRequest` | Add user to record team |
| RemoveUserFromRecordTeam | `RemoveUserFromRecordTeamRequest` | Remove user from record team |
| AddMemberList | `AddMemberListRequest` | Add member to marketing list |
| AddListMembersList | `AddListMembersListRequest` | Add members to marketing list |

### Metadata & Schema
| Message | Request Type | Description |
|---------|-------------|-------------|
| RetrieveEntity | `RetrieveEntityRequest` | Retrieve entity metadata |
| RetrieveAttribute | `RetrieveAttributeRequest` | Retrieve attribute metadata |
| RetrieveOptionSet | `RetrieveOptionSetRequest` | Retrieve option set metadata |
| RetrieveRelationship | `RetrieveRelationshipRequest` | Retrieve relationship metadata |
| InsertOptionValue | `InsertOptionValueRequest` | Insert option value |
| InsertStatusValue | `InsertStatusValueRequest` | Insert status value |

### Batch & Transaction
| Message | Request Type | Description |
|---------|-------------|-------------|
| ExecuteMultiple | `ExecuteMultipleRequest` | Execute multiple requests |
| ExecuteTransaction | `ExecuteTransactionRequest` | Execute requests in transaction |

### Utility Messages
| Message | Request Type | Description |
|---------|-------------|-------------|
| WhoAmI | `WhoAmIRequest` | Get current user info |
| RetrieveVersion | `RetrieveVersionRequest` | Get organization version |
| RetrieveDuplicates | `RetrieveDuplicatesRequest` | Detect duplicate records |
| ExecuteFetch | `ExecuteFetchRequest` | Execute FetchXML query |
| FetchXmlToQueryExpression | `FetchXmlToQueryExpressionRequest` | Convert FetchXML to QueryExpression |
| RetrieveExchangeRate | `RetrieveExchangeRateRequest` | Get exchange rate |
| SendEmail | `SendEmailRequest` | Send email |
| PublishXml | `PublishXmlRequest` | Publish metadata changes |
| BulkDelete | `BulkDeleteRequest` | Bulk delete operation |

### Custom Messages
| Message | Request Type | Description |
|---------|-------------|-------------|
| NavigateToNextEntity | Custom | Navigate to next entity (custom) |

## How to Use Messages

### Basic Pattern

1. Create the request object
2. Set request properties
3. Execute via `service.Execute()`
4. Cast response to specific type

```csharp
// 1. Create request
var request = new WhoAmIRequest();

// 2. No properties to set for WhoAmI

// 3. Execute
var response = (WhoAmIResponse)service.Execute(request);

// 4. Use response
Guid userId = response.UserId;
Guid businessUnitId = response.BusinessUnitId;
Guid organizationId = response.OrganizationId;
```

### Complex Example: Assign

```csharp
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

var accountId = Guid.NewGuid();
var oldOwnerId = Guid.NewGuid();
var newOwnerId = Guid.NewGuid();

// Initialize with data
context.Initialize(new[]
{
    new Entity("account") 
    { 
        Id = accountId,
        ["ownerid"] = new EntityReference("systemuser", oldOwnerId)
    },
    new Entity("systemuser") { Id = oldOwnerId },
    new Entity("systemuser") { Id = newOwnerId }
});

// Create assign request
var request = new AssignRequest
{
    Target = new EntityReference("account", accountId),
    Assignee = new EntityReference("systemuser", newOwnerId)
};

// Execute
var response = (AssignResponse)service.Execute(request);

// Verify
var updated = service.Retrieve("account", accountId, new ColumnSet("ownerid"));
Assert.Equal(newOwnerId, ((EntityReference)updated["ownerid"]).Id);
```

### Batch Operations Example: ExecuteMultiple

```csharp
using Microsoft.Xrm.Sdk.Messages;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Create multiple accounts in one request
var multipleRequest = new ExecuteMultipleRequest
{
    Requests = new OrganizationRequestCollection(),
    Settings = new ExecuteMultipleSettings
    {
        ContinueOnError = false,
        ReturnResponses = true
    }
};

for (int i = 0; i < 5; i++)
{
    var account = new Entity("account")
    {
        ["name"] = $"Account {i}"
    };
    multipleRequest.Requests.Add(new CreateRequest { Target = account });
}

// Execute all at once
var response = (ExecuteMultipleResponse)service.Execute(multipleRequest);

// Check results
Assert.Equal(5, response.Responses.Count);
foreach (var resp in response.Responses)
{
    Assert.IsType<CreateResponse>(resp.Response);
}
```

### Transaction Example: ExecuteTransaction

```csharp
using Microsoft.Xrm.Sdk.Messages;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Execute multiple operations as a transaction
var transactionRequest = new ExecuteTransactionRequest
{
    Requests = new OrganizationRequestCollection(),
    ReturnResponses = true
};

var account = new Entity("account") { ["name"] = "Contoso" };
var contact = new Entity("contact") { ["firstname"] = "John" };

transactionRequest.Requests.Add(new CreateRequest { Target = account });
transactionRequest.Requests.Add(new CreateRequest { Target = contact });

var response = (ExecuteTransactionResponse)service.Execute(transactionRequest);

Assert.Equal(2, response.Responses.Count);
```

## Unsupported Messages

If you try to use a message that's not implemented, you'll get a `PullRequestException`:

```csharp
// This will throw PullRequestException if not implemented
var request = new SomeUnsupportedRequest();
service.Execute(request); // Throws!
```

### Handling Unsupported Messages

You have options:

1. **Check if it's really needed** - Can you test without this message?
2. **Implement a custom executor** - See [Custom Executors](../api/custom-executors.md)
3. **Open an issue** - Request the feature on GitHub
4. **Contribute** - Implement and submit a PR

## Testing Message Execution

### Test Pattern

```csharp
[Fact]
public void Should_Execute_WhoAmI_Successfully()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var expectedUserId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", expectedUserId);
    
    // Act
    var request = new WhoAmIRequest();
    var response = (WhoAmIResponse)service.Execute(request);
    
    // Assert
    Assert.Equal(expectedUserId, response.UserId);
}
```

### Testing Error Conditions

```csharp
[Fact]
public void Should_Throw_When_EntityNotFound()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Act & Assert
    var request = new AssignRequest
    {
        Target = new EntityReference("account", Guid.NewGuid()), // Doesn't exist
        Assignee = new EntityReference("systemuser", Guid.NewGuid())
    };
    
    Assert.Throws<Exception>(() => service.Execute(request));
}
```


## Cloud Flow Actions

In addition to traditional message executors, Fake4Dataverse supports **Cloud Flow (Power Automate) actions** through the Dataverse connector. These actions provide an alternative, higher-level API for testing flows.

**Reference:** [Cloud Flow Simulation Documentation](../usage/cloud-flows.md)

### Supported Dataverse Connector Actions

| Action Type | Description | Status |
|------------|-------------|---------|
| `Create` | Create new records | ✅ Supported |
| `Retrieve` | Retrieve single record by ID | ✅ Supported |
| `Update` | Update existing records | ✅ Supported |
| `Delete` | Delete records | ✅ Supported |
| `ListRecords` | Query multiple records with paging | ✅ Enhanced |
| `Relate` | Associate records | ✅ Supported |
| `Unrelate` | Disassociate records | ✅ Supported |
| `ExecuteAction` | Execute custom actions/APIs | ✅ Supported |
| `UploadFile` | Upload files/images to columns | |
| `DownloadFile` | Download files/images from columns | |

### File Operations Example

```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;

var context = XrmFakedContextFactory.New();
var flowSimulator = context.CloudFlowSimulator;

// Upload a contact photo
var contactId = Guid.NewGuid();
context.Initialize(new Entity("contact") { Id = contactId, ["firstname"] = "John" });

byte[] imageBytes = File.ReadAllBytes("photo.jpg");

var flowDefinition = new CloudFlowDefinition
{
    Name = "upload_photo_flow",
    Trigger = new DataverseTrigger(),
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "UploadPhoto",
            DataverseActionType = DataverseActionType.UploadFile,
            EntityLogicalName = "contact",
            EntityId = contactId,
            ColumnName = "entityimage",
            FileContent = imageBytes,
            FileName = "photo.jpg"
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
var result = flowSimulator.SimulateTrigger("upload_photo_flow", new Dictionary<string, object>());

Assert.True(result.Succeeded);
```

### Advanced ListRecords with Paging

```csharp
// List records with paging and total count
var flowDefinition = new CloudFlowDefinition
{
    Name = "list_contacts_flow",
    Trigger = new DataverseTrigger(),
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "ListContacts",
            DataverseActionType = DataverseActionType.ListRecords,
            EntityLogicalName = "contact",
            Top = 10,              // Page size
            Skip = 0,              // Offset
            IncludeTotalCount = true,  // Include @odata.count
            OrderBy = "createdon desc"
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
var result = flowSimulator.SimulateTrigger("list_contacts_flow", new Dictionary<string, object>());

var outputs = result.ActionResults[0].Outputs;
var records = outputs["value"] as List<Dictionary<string, object>>;
var totalCount = outputs["@odata.count"];  // Total across all pages
var nextLink = outputs.ContainsKey("@odata.nextLink") ? outputs["@odata.nextLink"] : null;
```

**For more details, see:**
- [Cloud Flow Simulation Guide](../usage/cloud-flows.md) - Complete documentation
- [Expression Language Reference](../expression-language.md) - Power Automate expressions

## Message-Specific Documentation

For detailed information about specific message categories, see:

- **[CRUD Messages](./crud.md)** - Create, Retrieve, Update, Delete, Upsert
- **[Association Messages](./associations.md)** - Associate, Disassociate
- **[Security Messages](./security.md)** - Access control and sharing
- **[Business Process Messages](./business-process.md)** - Business-specific operations
- **[Queue Messages](./queues.md)** - Queue management
- **[Team Messages](./teams.md)** - Team membership
- **[Metadata Messages](./metadata.md)** - Entity and attribute metadata
- **[Specialized Messages](./specialized.md)** - Other messages

## Contributing New Message Executors

Want to add support for a new message? See [Custom Message Executors](../api/custom-executors.md).

## Next Steps

- Explore specific message categories in the [messages](.) directory
- Learn about [Custom Executors](../api/custom-executors.md)
- See [Testing Patterns](../usage/) for practical examples
