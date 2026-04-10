# Request Handlers Reference

Fake4Dataverse includes **100+ built-in request handlers** covering CRUD, metadata,
security, file operations, and more. Most standard Dataverse SDK requests work
out of the box via `service.Execute(request)`.

## How Execute Works

When you call `service.Execute(request)`, the
`OrganizationRequestHandlerRegistry` walks through registered handlers in
reverse order (last registered wins) and dispatches to the first handler whose
`CanHandle` method returns `true`.

Each handler implements `IOrganizationRequestHandler`:

```csharp
public interface IOrganizationRequestHandler
{
    bool CanHandle(OrganizationRequest request);
    OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service);
}
```

You can register additional handlers at any time:

```csharp
// Full handler class
env.HandlerRegistry.Register(myHandler);

// Simple lambda shorthand for custom APIs
env.RegisterCustomApi("my_Action", (req, svc) => new OrganizationResponse());
```

Because later registrations take priority, a custom handler automatically
overrides any built-in handler for the same request type.

---

## Built-In Handlers

### CRUD

| Request Type | Description |
|---|---|
| `CreateRequest` | Create an entity record |
| `RetrieveRequest` | Retrieve a single record by ID |
| `RetrieveMultipleRequest` | Query records (QueryExpression, FetchXml) |
| `UpdateRequest` | Update an entity record |
| `DeleteRequest` | Delete an entity record |
| `UpsertRequest` | Create or update by ID / alternate key |
| `CreateMultipleRequest` | Bulk create records |
| `UpdateMultipleRequest` | Bulk update records |
| `UpsertMultipleRequest` | Bulk upsert records |

### Batch Operations

| Request Type | Description |
|---|---|
| `ExecuteMultipleRequest` | Execute a batch with `ContinueOnError` / `ReturnResponses` |
| `ExecuteTransactionRequest` | Execute a batch as a transaction (rolls back on error) |
| `BulkDeleteRequest` | Async bulk delete by query |

### Lifecycle / State

| Request Type | Description |
|---|---|
| `SetStateRequest` | Set record state and status |
| `AssignRequest` | Change record owner |
| `MergeRequest` | Merge two records |
| `CloseIncidentRequest` | Close a case / incident |
| `CloseQuoteRequest` | Close a quote |
| `ReviseQuoteRequest` | Revise a closed quote |
| `QualifyLeadRequest` | Qualify a lead into account/contact/opportunity |
| `WinOpportunityRequest` | Win an opportunity |
| `LoseOpportunityRequest` | Lose an opportunity |

### Access Control (Sharing)

| Request Type | Description |
|---|---|
| `GrantAccessRequest` | Share a record with a user or team |
| `ModifyAccessRequest` | Modify sharing rights on a record |
| `RevokeAccessRequest` | Remove sharing from a record |
| `RetrievePrincipalAccessRequest` | Query effective access rights for a principal |
| `CreateAsyncJobToRevokeInheritedAccessRequest` | Async inherited-access revocation |

### Metadata — Entity / Attribute

| Request Type | Description |
|---|---|
| `CreateEntityRequest` | Create an entity definition |
| `UpdateEntityRequest` | Update an entity definition |
| `DeleteEntityRequest` | Delete an entity definition |
| `CreateAttributeRequest` | Create an attribute metadata |
| `UpdateAttributeRequest` | Update an attribute metadata |
| `DeleteAttributeRequest` | Delete an attribute metadata |
| `RetrieveEntityRequest` | Retrieve entity metadata |
| `RetrieveAllEntitiesRequest` | List all entity metadata |
| `RetrieveAttributeRequest` | Retrieve attribute metadata |
| `RetrieveMetadataChangesRequest` | Incremental metadata query |

### Option Set Operations

| Request Type | Description |
|---|---|
| `CreateOptionSetRequest` | Create a global option set |
| `UpdateOptionSetRequest` | Update an option set |
| `DeleteOptionSetRequest` | Delete an option set |
| `RetrieveOptionSetRequest` | Retrieve an option set |
| `RetrieveAllOptionSetsRequest` | List all option sets |
| `InsertOptionValueRequest` | Add an option to an option set |
| `UpdateOptionValueRequest` | Update an option value label |
| `DeleteOptionValueRequest` | Remove an option from an option set |
| `InsertStatusValueRequest` | Add a status code value |
| `UpdateStateValueRequest` | Update a state code value |
| `OrderOptionRequest` | Reorder options in an option set |

### Relationship & Key Operations

| Request Type | Description |
|---|---|
| `CreateOneToManyRequest` | Create a 1:N relationship |
| `CreateManyToManyRequest` | Create an N:N relationship |
| `UpdateRelationshipRequest` | Update a relationship definition |
| `DeleteRelationshipRequest` | Delete a relationship |
| `RetrieveRelationshipRequest` | Retrieve a relationship definition |
| `CreateCustomerRelationshipsRequest` | Create customer-type relationships |
| `CreateEntityKeyRequest` | Create an alternate key |
| `DeleteEntityKeyRequest` | Delete an alternate key |
| `RetrieveEntityKeyRequest` | Retrieve an alternate key |
| `ReactivateEntityKeyRequest` | Reactivate a failed key |

### Validation & Compatibility

| Request Type | Description |
|---|---|
| `CanBeReferencedRequest` | Check if entity can be on the referenced side |
| `CanBeReferencingRequest` | Check if entity can be on the referencing side |
| `CanManyToManyRequest` | Check N:N relationship compatibility |
| `GetValidManyToManyRequest` | List entities valid for N:N |
| `GetValidReferencedEntitiesRequest` | List valid referenced entities |
| `GetValidReferencingEntitiesRequest` | List valid referencing entities |
| `IsValidStateTransitionRequest` | Check state/status transition validity |
| `RetrieveAllManagedPropertiesRequest` | List all managed properties |
| `RetrieveManagedPropertyRequest` | Retrieve a managed property |

### File Operations

| Request Type | Description |
|---|---|
| `InitializeFileBlocksUploadRequest` | Start a chunked file upload session |
| `UploadBlockRequest` | Upload a file block/chunk |
| `CommitFileBlocksUploadRequest` | Finalize a chunked upload |
| `DownloadBlockRequest` | Download a file block |
| `DeleteFileRequest` | Delete a file attachment |

### Queue Operations

| Request Type | Description |
|---|---|
| `AddToQueueRequest` | Add a record to a queue |
| `RemoveFromQueueRequest` | Remove a record from a queue |

### Teams & Marketing Lists

| Request Type | Description |
|---|---|
| `AddMembersTeamRequest` | Add members to a team |
| `RemoveMembersTeamRequest` | Remove members from a team |
| `AddListMembersListRequest` | Add members to a marketing list |
| `RemoveMemberListRequest` | Remove a member from a marketing list |

### Email & Templates

| Request Type | Description |
|---|---|
| `SendEmailRequest` | Send an email record |
| `SendEmailFromTemplateRequest` | Send email using a template |
| `SendFaxRequest` | Send a fax activity |
| `SendTemplateRequest` | Send a template-based message |
| `InstantiateTemplateRequest` | Instantiate an email template |
| `ExportPdfDocumentRequest` | Export a record to PDF |

### Data Encryption

| Request Type | Description |
|---|---|
| `IsDataEncryptionActiveRequest` | Check if data encryption is active |
| `SetDataEncryptionKeyRequest` | Set the data encryption key |
| `RetrieveDataEncryptionKeyRequest` | Retrieve the data encryption key |

### Miscellaneous

| Request Type | Description |
|---|---|
| `WhoAmIRequest` | Returns `CallerId`, `OrganizationId`, `BusinessUnitId` |
| `RetrieveCurrentOrganizationRequest` | Get current organization info |
| `RetrieveVersionRequest` | Get the Dataverse version string |
| `RetrieveTimestampRequest` | Get the current timestamp token |
| `RetrieveEntityChangesRequest` | Change-tracking delta query |
| `InitializeFromRequest` | Clone / template an entity record |
| `FetchXmlToQueryExpressionRequest` | Convert FetchXml string to `QueryExpression` |
| `QueryExpressionToFetchXmlRequest` | Convert a supported `QueryExpression` shape to FetchXml |
| `CalculateRollupFieldRequest` | Calculate a rollup field value |
| `PublishXmlRequest` | Publish customizations |
| `ConvertDateAndTimeBehaviorRequest` | Convert datetime behavior |
| `ExecuteAsyncRequest` | Submit a request for async execution |

### Query Conversion Notes

- `FetchXmlToQueryExpressionRequest` is registered by default and converts
    supported **non-aggregate** FetchXml into a `QueryExpression`.
- FetchXml parsing for this request supports the documented condition operators
    plus `link-type` values `inner`, `outer`, `exists`, `in`, `any`,
    `not-any`, `not-all`, and `natural`.
- `QueryExpressionToFetchXmlRequest` is also registered by default. It currently
    serializes column projection / `all-attributes`, ordering, `TopCount`,
    `Distinct`, nested filters, and nested link-entities for the join/operator
    set implemented by `QueryExpressionToFetchXmlRequestHandler`.
- Unsupported condition operators or join operators throw
    `NotSupportedException` rather than falling back to a lossy FetchXml form.
- Paging and `NoLock` are not emitted by `QueryExpressionToFetchXmlRequest`.

### Fallback / Extensibility

| Request Type | Description |
|---|---|
| `CustomApiRequestHandler` | Dispatches to handlers registered via `RegisterCustomApi` |
| `GenericCreateRequestHandler` | Fallback for create-style request variants |

---

## Registering Custom Handlers

### Simple Lambda (Custom API)

Use `RegisterCustomApi` when you need a quick handler matched by request name:

```csharp
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();

env.RegisterCustomApi("myorg_ApproveOrder", (request, svc) =>
{
    var orderId = (Guid)request["Target"];
    var order = svc.Retrieve("salesorder", orderId, new ColumnSet("statecode"));
    // ... approval logic ...
    return new OrganizationResponse();
});

// Invoke it
var response = service.Execute(new OrganizationRequest("myorg_ApproveOrder")
{
    ["Target"] = orderId
});
```

Matching is case-insensitive on the request name.

### Full Handler Class

For reusable or complex handlers, implement `IOrganizationRequestHandler`:

```csharp
public class ApproveOrderRequestHandler : IOrganizationRequestHandler
{
    public bool CanHandle(OrganizationRequest request)
        => request.RequestName == "myorg_ApproveOrder";

    public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
    {
        var orderId = (Guid)request["Target"];
        var order = service.Retrieve("salesorder", orderId, new ColumnSet("statecode"));
        // ... approval logic ...
        return new OrganizationResponse();
    }
}

// Register it
var env = new FakeDataverseEnvironment();
var service = env.CreateOrganizationService();
env.HandlerRegistry.Register(new ApproveOrderRequestHandler());
```

Later registrations take priority — register a custom handler to override any
built-in behavior for testing purposes.
