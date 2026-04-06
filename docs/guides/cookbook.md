# Cookbook — Common Testing Patterns

A collection of self-contained recipes for Fake4Dataverse. Each recipe is a complete
`[Fact]` test you can copy into your project.

> **Detailed guides**: [Pipeline & Plugin Testing](pipeline-plugin-testing.md) ·
> [Security & Access Control](security-access-control.md) ·
> [Assertion Adapters](assertion-adapters.md) ·
> [Querying](querying.md) ·
> [Metadata Validation](metadata-validation.md)

---

## 1. Testing a Real IPlugin End-to-End

Register your actual `IPlugin` class. Fake4Dataverse builds the same `IServiceProvider`
Dataverse provides at runtime — `IPluginExecutionContext`, `IOrganizationServiceFactory`,
and `ITracingService` all resolve automatically.

```csharp
[Fact]
public void MyAccountPlugin_CreatesRelatedContact_OnCreate()
{
    var service = new FakeOrganizationService();
    service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account",
        new MyAccountPlugin());

    var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });

    var contacts = service.RetrieveMultiple(new QueryExpression("contact")
    {
        ColumnSet = new ColumnSet(true)
    });
    Assert.Single(contacts.Entities);
    Assert.Equal("Contoso Primary Contact", contacts.Entities[0]["lastname"]);
}
```

## 2. Simulating Plugin Logic With a Lambda

Test business logic without a full plugin class — useful for rapid prototyping.

```csharp
[Fact]
public void Lambda_CreatesRelatedContact_OnAccountCreate()
{
    var service = new FakeOrganizationService();

    service.Pipeline.RegisterPostOperation("Create", "account", ctx =>
    {
        var target = (Entity)ctx.InputParameters["Target"];
        service.Create(new Entity("contact")
        {
            ["parentcustomerid"] = new EntityReference("account", target.Id),
            ["lastname"] = target["name"] + " Primary Contact"
        });
    });

    service.Create(new Entity("account") { ["name"] = "Contoso" });

    var contacts = service.RetrieveMultiple(new QueryExpression("contact")
    {
        ColumnSet = new ColumnSet(true)
    });
    Assert.Single(contacts.Entities);
    Assert.Equal("Contoso Primary Contact", contacts.Entities[0]["lastname"]);
}
```

## 3. Asserting Plugin Traces

Traces written via `ITracingService` are captured in `Pipeline.Traces`.

```csharp
[Fact]
public void Plugin_TracesAreCaptured()
{
    var service = new FakeOrganizationService();
    service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, new MyLoggingPlugin());

    service.Create(new Entity("account") { ["name"] = "Contoso" });

    Assert.Contains(service.Pipeline.Traces, t => t.Contains("Processing account"));
}
```

## 4. Cascade Delete

Define a one-to-many relationship with cascade delete, then verify children
are removed when the parent is deleted.

```csharp
[Fact]
public void DeleteAccount_CascadeDeletesContacts()
{
    var service = new FakeOrganizationService();
    service.MetadataStore.AddOneToManyRelationship(
        "account", "contact", "parentcustomerid",
        new Fake4Dataverse.Metadata.CascadeConfiguration
        {
            Delete = Fake4Dataverse.Metadata.CascadeType.Cascade
        });

    var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });
    service.Create(new Entity("contact")
    {
        ["parentcustomerid"] = new EntityReference("account", accountId),
        ["lastname"] = "Smith"
    });

    service.Delete("account", accountId);

    var contacts = service.RetrieveMultiple(
        new QueryExpression("contact") { ColumnSet = new ColumnSet(true) });
    Assert.Empty(contacts.Entities);
}
```

## 5. Deterministic Time with FakeClock

Use `FakeClock` to control `UtcNow` and test time-dependent queries.

```csharp
[Fact]
public void Query_LastXDays_WithFakeClock()
{
    var clock = new FakeClock(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    var service = new FakeOrganizationService() { Clock = clock };

    clock.UtcNow = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
    service.Create(new Entity("task") { ["subject"] = "Old task", ["createdon"] = clock.UtcNow });

    clock.UtcNow = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
    service.Create(new Entity("task") { ["subject"] = "New task", ["createdon"] = clock.UtcNow });

    var query = new QueryExpression("task") { ColumnSet = new ColumnSet(true) };
    query.Criteria.AddCondition("createdon", ConditionOperator.LastXDays, 3);
    var results = service.RetrieveMultiple(query);

    Assert.Single(results.Entities);
    Assert.Equal("New task", results.Entities[0]["subject"]);
}
```

## 6. ExecuteMultiple with Error Handling

Validates that `ContinueOnError` lets subsequent requests execute after a failure.

```csharp
[Fact]
public void ExecuteMultiple_ContinuesOnError()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection
        {
            new UpdateRequest { Target = new Entity("account", id) { ["name"] = "Updated" } },
            new DeleteRequest { Target = new EntityReference("account", Guid.NewGuid()) },
            new CreateRequest { Target = new Entity("account") { ["name"] = "New" } }
        },
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = true,
            ReturnResponses = true
        }
    };

    var response = (ExecuteMultipleResponse)service.Execute(request);
    Assert.True(response.IsFaulted);  // second request failed
}
```

## 7. Test Isolation with Scopes

`Scope()` takes a snapshot and restores it on dispose — perfect for
shared-service test fixtures.

```csharp
[Fact]
public void ScopedTest_AutoRollback()
{
    var service = new FakeOrganizationService();
    service.Create(new Entity("account") { ["name"] = "Permanent" });

    using (service.Scope())
    {
        service.Create(new Entity("account") { ["name"] = "Temporary" });
        var all = service.RetrieveMultiple(
            new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
        Assert.Equal(2, all.Entities.Count);
    }

    var remaining = service.RetrieveMultiple(
        new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
    Assert.Single(remaining.Entities);
}
```

## 8. Verifying Operations with the Operation Log

The built-in operation log records every call for post-hoc assertions.

```csharp
[Fact]
public void OperationLog_RecordsAllCalls()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
    service.Update(new Entity("account", id) { ["name"] = "Updated" });
    service.Delete("account", id);

    Assert.True(service.OperationLog.HasCreated("account", id));
    Assert.True(service.OperationLog.HasUpdated("account", id));
    Assert.True(service.OperationLog.HasDeleted("account", id));

    var creates = service.OperationLog.GetOperations("Create");
    Assert.Single(creates);
}
```

## 9. Alternate Key Upsert

Register an alternate key, then upsert by key attributes — first call creates,
second call updates.

```csharp
[Fact]
public void Upsert_ByAlternateKey_CreatesOrUpdates()
{
    var service = new FakeOrganizationService();
    service.MetadataStore.AddEntity("account", "accountid", "name");
    service.MetadataStore.AddAlternateKey("account", "ak_account_number", "accountnumber");

    var target = new Entity("account") { ["accountnumber"] = "ACC-001", ["name"] = "Contoso" };
    target.KeyAttributes["accountnumber"] = "ACC-001";

    var response = (UpsertResponse)service.Execute(new UpsertRequest { Target = target });
    Assert.True(response.RecordCreated);

    target["name"] = "Contoso Updated";
    var response2 = (UpsertResponse)service.Execute(new UpsertRequest { Target = target });
    Assert.False(response2.RecordCreated);
}
```

## 10. Seeding Test Data from CSV

Load bulk test data in one call with inline CSV or from a file.

```csharp
[Fact]
public void SeedFromCsv_LoadsTestData()
{
    var service = new FakeOrganizationService();
    service.SeedFromCsv(@"logicalname,name,revenue
account,Contoso,1000000
account,Fabrikam,2000000
account,Northwind,500000");

    var results = service.RetrieveMultiple(
        new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
    Assert.Equal(3, results.Entities.Count);
}
```

File-based seeding is also available:

```csharp
service.SeedFromJsonFile("seed-data.json");
service.SeedFromCsvFile("seed-data.csv");
```

## 11. Associate / Disassociate (N:N Relationships)

Test many-to-many relationship operations. Associations are stored as queryable
`association_<relationshipname>` records.

```csharp
[Fact]
public void Associate_And_Disassociate_ManyToMany()
{
    var service = new FakeOrganizationService();
    var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });
    var contactId = service.Create(new Entity("contact") { ["lastname"] = "Doe" });

    service.Associate("account", accountId,
        new Relationship("account_contacts"),
        new EntityReferenceCollection { new EntityReference("contact", contactId) });

    var associations = service.RetrieveMultiple(
        new QueryExpression("association_account_contacts") { ColumnSet = new ColumnSet(true) });
    Assert.Single(associations.Entities);

    service.Disassociate("account", accountId,
        new Relationship("account_contacts"),
        new EntityReferenceCollection { new EntityReference("contact", contactId) });

    associations = service.RetrieveMultiple(
        new QueryExpression("association_account_contacts") { ColumnSet = new ColumnSet(true) });
    Assert.Empty(associations.Entities);
}
```

## 12. Testing WhoAmI

`WhoAmIRequest` is handled out of the box. Override the handler to control the
returned user/org/business-unit IDs.

```csharp
[Fact]
public void WhoAmI_ReturnsConfiguredUser()
{
    var service = new FakeOrganizationService();
    var customUserId = Guid.NewGuid();
    service.HandlerRegistry.Register(new Handlers.WhoAmIRequestHandler
    {
        UserId = customUserId
    });

    var response = (WhoAmIResponse)service.Execute(new WhoAmIRequest());

    Assert.Equal(customUserId, response.UserId);
    Assert.NotEqual(Guid.Empty, response.OrganizationId);
}
```

## 13. Testing SetState (Activate / Deactivate)

Use `SetStateRequest` to change `statecode` and `statuscode`, then verify.

```csharp
[Fact]
public void SetState_DeactivatesAndReactivates()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

    // Deactivate
    service.Execute(new SetStateRequest
    {
        EntityMoniker = new EntityReference("account", id),
        State = new OptionSetValue(1),   // Inactive
        Status = new OptionSetValue(2)
    });

    var deactivated = service.Retrieve("account", id, new ColumnSet("statecode", "statuscode"));
    Assert.Equal(1, deactivated.GetAttributeValue<OptionSetValue>("statecode").Value);
    Assert.Equal(2, deactivated.GetAttributeValue<OptionSetValue>("statuscode").Value);

    // Reactivate
    service.Execute(new SetStateRequest
    {
        EntityMoniker = new EntityReference("account", id),
        State = new OptionSetValue(0),   // Active
        Status = new OptionSetValue(1)
    });

    var reactivated = service.Retrieve("account", id, new ColumnSet("statecode"));
    Assert.Equal(0, reactivated.GetAttributeValue<OptionSetValue>("statecode").Value);
}
```

## 14. Testing Assign Request

`AssignRequest` changes the `ownerid` of a record.

```csharp
[Fact]
public void Assign_ChangesRecordOwner()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
    var newOwner = new EntityReference("systemuser", Guid.NewGuid());

    service.Execute(new AssignRequest
    {
        Target = new EntityReference("account", id),
        Assignee = newOwner
    });

    var retrieved = service.Retrieve("account", id, new ColumnSet("ownerid"));
    Assert.Equal(newOwner.Id, retrieved.GetAttributeValue<EntityReference>("ownerid").Id);
}
```

## 15. Custom Request Handlers

Register a handler for a custom API or override a built-in one. Use
`RegisterCustomApi` for simple cases, or implement `IOrganizationRequestHandler`
for full control.

```csharp
[Fact]
public void CustomApi_ReturnsExpectedOutput()
{
    var service = new FakeOrganizationService();

    service.RegisterCustomApi("myorg_ApproveOrder", (request, svc) =>
    {
        var orderId = (Guid)request["OrderId"];
        svc.Update(new Entity("salesorder", orderId) { ["statuscode"] = new OptionSetValue(100) });
        var response = new OrganizationResponse();
        response.Results["IsApproved"] = true;
        return response;
    });

    var orderId = service.Create(new Entity("salesorder") { ["name"] = "SO-001" });
    var req = new OrganizationRequest("myorg_ApproveOrder") { ["OrderId"] = orderId };
    var resp = service.Execute(req);

    Assert.True((bool)resp.Results["IsApproved"]);
    var order = service.Retrieve("salesorder", orderId, new ColumnSet("statuscode"));
    Assert.Equal(100, order.GetAttributeValue<OptionSetValue>("statuscode").Value);
}
```

## 16. Early-Bound Entities

Fake4Dataverse works seamlessly with early-bound (generated) entity classes.
Create, retrieve, and cast just like production code.

```csharp
// Simple early-bound class (typically generated by CrmSvcUtil / pac modelbuilder)
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("account")]
public sealed class Account : Entity
{
    public const string EntityLogicalName = "account";
    public Account() : base(EntityLogicalName) { }

    public string? Name
    {
        get => GetAttributeValue<string>("name");
        set => this["name"] = value;
    }
}

[Fact]
public void EarlyBound_CreateAndRetrieve()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Account { Name = "Contoso" });

    var retrieved = service.Retrieve("account", id, new ColumnSet(true));
    var typed = retrieved.ToEntity<Account>();

    Assert.Equal("Contoso", typed.Name);
}
```

## 17. Combining Pipeline and Security in One Test

Verifies that a plugin fires **and** security is enforced in the same scenario.

```csharp
[Fact]
public void Pipeline_And_Security_WorkTogether()
{
    var service = new FakeOrganizationService();
    service.Security.EnforceSecurityRoles = true;

    // Grant the caller Create + Read on account
    var role = new Security.SecurityRole("Sales")
        .AddPrivilege("account", Security.PrivilegeType.Create, Security.PrivilegeDepth.Organization)
        .AddPrivilege("account", Security.PrivilegeType.Read, Security.PrivilegeDepth.Organization);
    service.Security.AssignRole(service.CallerId, role);

    // Register a post-operation plugin that stamps a field
    service.Pipeline.RegisterPostOperation("Create", "account", ctx =>
    {
        var target = (Entity)ctx.InputParameters["Target"];
        service.Update(new Entity("account", target.Id) { ["description"] = "Processed" });
    });

    // Also grant Update so the plugin can write back
    role.AddPrivilege("account", Security.PrivilegeType.Write, Security.PrivilegeDepth.Organization);

    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

    var account = service.Retrieve("account", id, new ColumnSet("description"));
    Assert.Equal("Processed", account["description"]);

    // Without Create privilege, a different user is blocked
    var otherUser = Guid.NewGuid();
    service.CallerId = otherUser;
    Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
        service.Create(new Entity("account") { ["name"] = "Blocked" }));
}
```

> For in-depth security configuration see the [Security & Access Control guide](security-access-control.md).
> For full pipeline registration options see the [Pipeline & Plugin Testing guide](pipeline-plugin-testing.md).
