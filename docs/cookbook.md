# Cookbook — Common Testing Patterns

Recipes for common Fake4Dataverse testing scenarios.

---

## 1. Testing a Real IPlugin End-to-End

Register an actual `IPlugin` implementation. Fake4Dataverse constructs the same `IServiceProvider`
that Dataverse passes at runtime, so your plugin code runs unchanged.

```csharp
[Fact]
public void MyAccountPlugin_CreatesRelatedContact_OnCreate()
{
    var service = new FakeOrganizationService();

    // Register the real plugin — no adapters or wrappers needed
    service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, "account",
        new MyAccountPlugin());

    // Act
    var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });

    // Assert — verify side effects the plugin produced
    var contacts = service.RetrieveMultiple(new QueryExpression("contact")
    {
        ColumnSet = new ColumnSet(true)
    });
    Assert.Single(contacts.Entities);
    Assert.Equal("Contoso Primary Contact", contacts.Entities[0]["lastname"]);
}
```

Inside `MyAccountPlugin.Execute(IServiceProvider)` the service provider resolves:
- `IPluginExecutionContext` — message, entity name, input/output parameters, images
- `IOrganizationServiceFactory` — creates an `IOrganizationService` backed by the fake store
- `ITracingService` — traces are captured and readable via `service.Pipeline.Traces`

## 1b. Simulating Plugin Logic With a Lambda

When testing business logic in isolation (without the plugin class itself), use a callback:

```csharp
[Fact]
public void Plugin_CreatesRelatedContact_OnAccountCreate()
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

    var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });

    var contacts = service.RetrieveMultiple(new QueryExpression("contact")
    {
        ColumnSet = new ColumnSet(true)
    });
    Assert.Single(contacts.Entities);
    Assert.Equal("Contoso Primary Contact", contacts.Entities[0]["lastname"]);
}
```

## 1c. Asserting Plugin Traces

```csharp
[Fact]
public void Plugin_TracesAreCapturableAfterExecution()
{
    var service = new FakeOrganizationService();
    service.Pipeline.RegisterStep("Create", PipelineStage.PostOperation, new MyLoggingPlugin());

    service.Create(new Entity("account") { ["name"] = "Contoso" });

    Assert.Contains(service.Pipeline.Traces, t => t.Contains("Processing account"));
}
```

## 2. Testing Cascade Delete

```csharp
[Fact]
public void DeleteAccount_CascadeDeletesContacts()
{
    var service = new FakeOrganizationService();

    // Define cascade relationship
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

    // Delete parent → children are cascade-deleted
    service.Delete("account", accountId);

    var contacts = service.RetrieveMultiple(new QueryExpression("contact") { ColumnSet = new ColumnSet(true) });
    Assert.Empty(contacts.Entities);
}
```

## 3. Testing Security Role Enforcement

```csharp
[Fact]
public void UserWithoutCreatePrivilege_CannotCreateAccount()
{
    var service = new FakeOrganizationService(FakeOrganizationServiceOptions.Strict);
    var userId = Guid.NewGuid();
    service.CallerId = userId;

    // Grant only Read privilege, not Create
    service.Security.AddRole(userId, "ReadOnly");
    service.Security.AddPrivilege("ReadOnly", "account",
        Security.PrivilegeType.Read, Security.PrivilegeDepth.Organization);

    Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
        service.Create(new Entity("account") { ["name"] = "Contoso" }));
}
```

## 4. Testing with Deterministic Time

```csharp
[Fact]
public void Query_LastXDays_WithFakeClock()
{
    var clock = new FakeClock(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    var service = new FakeOrganizationService() { Clock = clock };

    // Create an entity "5 days ago"
    clock.UtcNow = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
    service.Create(new Entity("task") { ["subject"] = "Old task", ["createdon"] = clock.UtcNow });

    // Create an entity "today"
    clock.UtcNow = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
    service.Create(new Entity("task") { ["subject"] = "New task", ["createdon"] = clock.UtcNow });

    // Query last 3 days
    var query = new QueryExpression("task") { ColumnSet = new ColumnSet(true) };
    query.Criteria.AddCondition("createdon", ConditionOperator.LastXDays, 3);
    var results = service.RetrieveMultiple(query);

    Assert.Single(results.Entities);
    Assert.Equal("New task", results.Entities[0]["subject"]);
}
```

## 5. Testing ExecuteMultiple with Error Handling

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
            // Valid update
            new UpdateRequest { Target = new Entity("account", id) { ["name"] = "Updated" } },
            // Invalid — entity doesn't exist
            new DeleteRequest { Target = new EntityReference("account", Guid.NewGuid()) },
            // Another valid create
            new CreateRequest { Target = new Entity("account") { ["name"] = "New" } }
        },
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = true,
            ReturnResponses = true
        }
    };

    var response = (ExecuteMultipleResponse)service.Execute(request);
    Assert.True(response.IsFaulted);
    // First and third requests succeed, second fails
}
```

## 6. Test Isolation with Scopes

```csharp
[Fact]
public void ScopedTest_AutoRollback()
{
    var service = new FakeOrganizationService();
    service.Create(new Entity("account") { ["name"] = "Permanent" });

    using (service.Scope())
    {
        service.Create(new Entity("account") { ["name"] = "Temporary" });
        var all = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
        Assert.Equal(2, all.Entities.Count);
    }

    // After scope disposal, only the permanent record remains
    var remaining = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
    Assert.Single(remaining.Entities);
}
```

## 7. Verifying Operations with the Operation Log

```csharp
[Fact]
public void OperationLog_RecordsAllCalls()
{
    var service = new FakeOrganizationService();
    var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
    service.Update(new Entity("account", id) { ["name"] = "Updated" });
    service.Delete("account", id);

    // Check specific operations
    Assert.True(service.OperationLog.HasCreated("account", id));
    Assert.True(service.OperationLog.HasUpdated("account", id));
    Assert.True(service.OperationLog.HasDeleted("account", id));

    // Or use fluent assertions
    service.Should()
        .HaveCreated("account", id)
        .HaveUpdated("account", id)
        .HaveDeleted("account", id);

    // Get all Create operations
    var creates = service.OperationLog.GetOperations("Create");
    Assert.Single(creates);
}
```

## 8. Testing Alternate Key Upsert

```csharp
[Fact]
public void Upsert_ByAlternateKey_CreatesOrUpdates()
{
    var service = new FakeOrganizationService();
    service.MetadataStore.AddEntity("account", "accountid", "name");
    service.MetadataStore.AddAlternateKey("account", "ak_account_number", "accountnumber");

    // First upsert creates
    var ref1 = new EntityReference("account")
    {
        KeyAttributes = { { "accountnumber", "ACC-001" } }
    };
    var target = new Entity("account") { ["accountnumber"] = "ACC-001", ["name"] = "Contoso" };
    target.KeyAttributes["accountnumber"] = "ACC-001";

    var response = (UpsertResponse)service.Execute(new UpsertRequest { Target = target });
    Assert.True(response.RecordCreated);

    // Second upsert updates
    target["name"] = "Contoso Updated";
    var response2 = (UpsertResponse)service.Execute(new UpsertRequest { Target = target });
    Assert.False(response2.RecordCreated);
}
```

## 9. Seeding from CSV/JSON

```csharp
[Fact]
public void SeedFromCsv_LoadsTestData()
{
    var service = new FakeOrganizationService();
    service.SeedFromCsv(@"logicalname,name,revenue
account,Contoso,1000000
account,Fabrikam,2000000
account,Northwind,500000");

    var results = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
    Assert.Equal(3, results.Entities.Count);
}
```

## 10. Using the Moq Companion Package

```csharp
// When your production code requires Mock<IOrganizationService>
using Fake4Dataverse.Moq;

var service = new FakeOrganizationService();
Mock<IOrganizationService> mock = service.AsMock();

// Pass mock.Object to production code that expects IOrganizationService
var myPlugin = new MyPlugin(mock.Object);

// All calls go through the full in-memory fake
// Plus you can still use Moq's Verify():
mock.Verify(m => m.Create(It.IsAny<Entity>()), Times.Once);
```
