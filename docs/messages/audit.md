# Audit Messages

This document lists all audit-related message executors supported in Fake4Dataverse.


**Reference:** [Dataverse Auditing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Microsoft documentation on audit operations and messages in Dataverse.

## Supported Messages

### RetrieveAuditDetailsRequest

**Status:** ✅ Fully Supported

**Reference:** [RetrieveAuditDetailsRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest) - Retrieves the full audit details for a specific audit record.

Retrieves detailed information about a specific audit record, including old and new attribute values for Update operations.

**Request Parameters:**
- `AuditId` (Guid): The ID of the audit record to retrieve details for

**Response:**
- `AuditDetail` (AuditDetail): The audit detail object containing change information

**Example:**
```csharp
var request = new RetrieveAuditDetailsRequest
{
    AuditId = auditId
};

var response = (RetrieveAuditDetailsResponse)service.Execute(request);
var auditDetail = response.AuditDetail;

if (auditDetail is AttributeAuditDetail attrDetail)
{
    var oldValue = attrDetail.OldValue["fieldname"];
    var newValue = attrDetail.NewValue["fieldname"];
}
```

---

### RetrieveRecordChangeHistoryRequest

**Status:** ✅ Fully Supported

**Reference:** [RetrieveRecordChangeHistoryRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieverecordchangehistoryrequest) - Retrieves the complete change history for a specific record.

Retrieves all audit records for a specific entity, showing the complete history of changes made to the record.

**Request Parameters:**
- `Target` (EntityReference): Reference to the record to retrieve history for

**Response:**
- `AuditDetailCollection` (AuditDetailCollection): Collection of all audit details for the record

**Example:**
```csharp
var request = new RetrieveRecordChangeHistoryRequest
{
    Target = new EntityReference("account", accountId)
};

var response = (RetrieveRecordChangeHistoryResponse)service.Execute(request);

foreach (var auditDetail in response.AuditDetailCollection.AuditDetails)
{
    var action = auditDetail.AuditRecord.GetAttributeValue<int>("action");
    var timestamp = auditDetail.AuditRecord.GetAttributeValue<DateTime>("createdon");
    // Process audit record
}
```

---

### RetrieveAttributeChangeHistoryRequest

**Status:** ✅ Fully Supported

**Reference:** [RetrieveAttributeChangeHistoryRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveattributechangehistoryrequest) - Retrieves the change history for a specific attribute of a record.

Retrieves audit history filtered to changes of a specific attribute, useful for tracking changes to individual fields.

**Request Parameters:**
- `Target` (EntityReference): Reference to the record
- `AttributeLogicalName` (string): Logical name of the attribute to track

**Response:**
- `AuditDetailCollection` (AuditDetailCollection): Collection of audit details for the specific attribute

**Example:**
```csharp
var request = new RetrieveAttributeChangeHistoryRequest
{
    Target = new EntityReference("account", accountId),
    AttributeLogicalName = "name"
};

var response = (RetrieveAttributeChangeHistoryResponse)service.Execute(request);

foreach (var auditDetail in response.AuditDetailCollection.AuditDetails)
{
    if (auditDetail is AttributeAuditDetail attrDetail)
    {
        var oldName = attrDetail.OldValue.GetAttributeValue<string>("name");
        var newName = attrDetail.NewValue.GetAttributeValue<string>("name");
    }
}
```

---

## Future Enhancements

### RetrieveAuditPartitionListRequest

**Status:** ⚠️ Not Yet Implemented

**Reference:** [RetrieveAuditPartitionListRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditpartitionlistrequest) - Retrieves the list of audit partitions.

In Dataverse, audit data is partitioned by date for performance. This message retrieves the list of partitions.

**Workaround:** Not typically needed for unit testing scenarios.

---

### DeleteAuditDataRequest

**Status:** ⚠️ Partial Support via ClearAuditData()

**Reference:** [DeleteAuditDataRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.deleteauditdatarequest) - Deletes audit records for a specified date range.

In Dataverse, this message deletes audit data. In Fake4Dataverse, use `IAuditRepository.ClearAuditData()` to clear all audit data for testing purposes.

**Example:**
```csharp
var auditRepository = context.GetProperty<IAuditRepository>();
auditRepository.ClearAuditData();
```

---

## Audit Actions

When querying audit records, the `action` field indicates the type of operation:

| Action | Value | Description |
|--------|-------|-------------|
| Create | 1 | Record was created |
| Update | 2 | Record was updated |
| Delete | 3 | Record was deleted |
| Access | 64 | User accessed record (user access auditing) |
| Assign | 101 | Record ownership was transferred |
| Share | 102 | Record was shared with user/team |
| Unshare | 103 | Record sharing was revoked |
| Merge | 104 | Records were merged |

**Reference:** [Audit Actions](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview#audit-operations) - Complete list of audit action types in Dataverse.

**Example:**
```csharp
var auditRecord = auditRepository.GetAllAuditRecords().First();
var action = auditRecord.GetAttributeValue<int>("action");

if (action == AuditAction.Create)
{
    // Handle create operation
}
else if (action == AuditAction.Update)
{
    // Handle update operation
}
```

## See Also

- [Auditing Usage Guide](../usage/auditing.md) - Complete guide to testing audit functionality
- [Security Messages](./security.md) - Related security operations
- [Dataverse Auditing Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview) - Official documentation

---

**Questions?** Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
