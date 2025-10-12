# Cloud Flow Simulation

## Overview

Cloud Flows (Power Automate flows) are an increasingly common integration pattern for Dataverse applications. The Cloud Flow simulation feature in Fake4Dataverse enables developers to test Dataverse-triggered flows, verify flow execution, and validate flow actions/outputs in unit tests.

**Status:** ✅ **Implemented** (October 11, 2025) - Phases 1-4 Complete

**Test Coverage:** 67 unit tests, all passing ✅ (includes 20 JSON import tests)

## Microsoft Documentation

Official references:
- [Cloud Flows Overview](https://learn.microsoft.com/en-us/power-automate/overview-cloud)
- [Dataverse Connector for Power Automate](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)
- [Trigger Conditions](https://learn.microsoft.com/en-us/power-automate/triggers-introduction)
- [Flow Definition Schema](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)

## What are Cloud Flows?

Cloud Flows allow you to:
- Automate business processes triggered by Dataverse events
- Integrate Dataverse with external systems (Outlook, Teams, SharePoint, custom APIs)
- Execute complex multi-step workflows with branching logic
- Transform and manipulate data across systems

## Capabilities

The Cloud Flow simulation feature provides:
1. **Flow Registration** - Register flow definitions programmatically or from JSON
2. **JSON Import** - Import real Cloud Flow definitions exported from Power Automate ✅ **NEW**
3. **Expression Language** - Full Power Automate expression evaluation using Jint.net ✅ **NEW**
4. **Automatic Triggering** - Flows automatically trigger on Create/Update/Delete operations when `UsePipelineSimulation = true`
5. **Manual Triggering** - Manually simulate flow execution with `SimulateTrigger`
6. **Built-in Dataverse Connector** - Full CRUD support (Create, Retrieve, Update, Delete, ListRecords, Relate, Unrelate, ExecuteAction)
7. **Extensible Connector System** - Register custom handlers for Office 365, SharePoint, Teams, HTTP, and any custom connectors
8. **Comprehensive Verification** - Assert flows triggered, inspect execution results, examine action outputs
9. **Filtered Attributes** - Update triggers support filtered attributes (trigger only when specific fields change)
10. **Asynchronous Behavior** - Flow failures don't fail CRUD operations, matching real Dataverse behavior

## API Usage

### Expression Language Support ✅ **NEW**

Fake4Dataverse now supports Power Automate expression language for dynamic values in flow actions:

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "dynamic_task_creation",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "Create_Task",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                // Expressions are evaluated automatically
                ["subject"] = "@concat('Follow up with ', triggerBody()['firstname'], ' ', triggerBody()['lastname'])",
                ["description"] = "@concat('Email: ', triggerBody()['emailaddress1'], ' | Phone: ', triggerBody()['telephone1'])",
                ["scheduledend"] = "@addDays(utcNow(), 7)"  // Due in 7 days
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

**Supported Functions:**
- Reference: `triggerOutputs()`, `triggerBody()`, `outputs('ActionName')`, `body('ActionName')`
- String: `concat()`, `substring()`, `replace()`, `toLower()`, `toUpper()`, `split()`, `trim()`, `guid()`
- Logical: `equals()`, `greater()`, `less()`, `not()`, `empty()`
- Conversion: `string()`, `int()`, `float()`, `bool()`, `base64()`
- Collection: `first()`, `last()`, `take()`, `skip()`, `join()`
- Date/Time: `utcNow()`, `addDays()`, `addHours()`, `formatDateTime()`
- Math: `add()`, `sub()`, `mul()`, `div()`, `min()`, `max()`

**See:** [Expression Language Reference](../expression-language.md) for complete documentation and examples.

### Flow Registration

#### Register a Flow Programmatically
```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Enable automatic triggering
var flowSimulator = context.CloudFlowSimulator;

// Register a flow definition programmatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "notify_on_contact_create",
    DisplayName = "Notify on New Contact",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create",
        Scope = TriggerScope.Organization
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "CreateTask",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Follow up with new contact",
                ["description"] = "Contact the new lead"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

#### Register Multiple Flows
```csharp
var flows = new List<ICloudFlowDefinition>
{
    contactCreateFlow,
    accountUpdateFlow,
    opportunityDeleteFlow
};

flowSimulator.RegisterFlows(flows);
```

#### Unregister Flows
```csharp
// Unregister a specific flow
flowSimulator.UnregisterFlow("notify_on_contact_create");

// Clear all flows
flowSimulator.ClearAllFlows();
```

#### Register a Flow from JSON ✅ **NEW**

You can import real Cloud Flow definitions exported from Power Automate:

```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true;
var flowSimulator = context.CloudFlowSimulator;

// Export your flow from Power Automate as JSON
// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
var flowJson = @"{
  ""name"": ""notify_on_contact_create"",
  ""properties"": {
    ""displayName"": ""Notify on New Contact"",
    ""state"": ""Started"",
    ""definition"": {
      ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
      ""contentVersion"": ""1.0.0.0"",
      ""triggers"": {
        ""When_a_record_is_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Create_a_new_record"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord""
            },
            ""parameters"": {
              ""entityName"": ""task"",
              ""item/subject"": ""Follow up with new contact"",
              ""item/description"": ""Contact the new lead""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

// Import and register the flow
flowSimulator.RegisterFlowFromJson(flowJson);

// The flow works exactly like a programmatically registered flow
var service = context.GetOrganizationService();
service.Create(new Entity("contact") { ["firstname"] = "Jane" });

flowSimulator.AssertFlowTriggered("notify_on_contact_create");
```

**Supported JSON Features:**
- **Triggers:** Dataverse triggers (Create, Update, Delete, CreateOrUpdate)
- **Trigger Scopes:** Organization, BusinessUnit, ParentChildBusinessUnits, User
- **Filtered Attributes:** Update triggers with specific attribute filtering
- **Actions:** Dataverse actions (CreateRecord, UpdateRecord, DeleteRecord, GetItem, ListRecords)
- **Action Parameters:** Entity names, attributes, filters, ordering, top

**Limitations:**
- Expression evaluation is not yet supported (expressions are stored but not evaluated during import)
- Non-Dataverse connectors require custom handlers via `RegisterConnectorActionHandler`
- Advanced control flow (conditions, loops, parallel branches) not yet supported
- Expressions in action parameters (e.g., `@triggerOutputs()`) are preserved but not evaluated

**How to Export a Flow from Power Automate:**
1. Open your Cloud Flow in Power Automate
2. Click the flow menu (three dots) → Export → Package (.zip)
3. Extract the zip file
4. Find the `definition.json` file inside
5. Use that JSON with `RegisterFlowFromJson`



### Automatic Flow Triggering

Flows automatically trigger when matching CRUD operations occur:

```csharp
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Required for automatic triggering
var flowSimulator = context.CloudFlowSimulator;

// Register flow
flowSimulator.RegisterFlow(new CloudFlowDefinition
{
    Name = "on_contact_create",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "CreateTask",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = { ["subject"] = "Follow up" }
        }
    }
});

// Flow triggers automatically on Create!
var service = context.GetOrganizationService();
var contactId = service.Create(new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
});

// Verify flow was triggered
flowSimulator.AssertFlowTriggered("on_contact_create");

// Verify task was created by the flow
var tasks = context.CreateQuery("task").ToList();
Assert.Single(tasks);
Assert.Equal("Follow up", tasks[0]["subject"]);
```

#### Filtered Attributes (Update Triggers Only)
```csharp
// Flow only triggers when specific attributes are modified
var flowDefinition = new CloudFlowDefinition
{
    Name = "on_email_change",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Update",
        FilteredAttributes = new List<string> { "emailaddress1", "emailaddress2" }
    },
    Actions = { /* ... */ }
};

flowSimulator.RegisterFlow(flowDefinition);

// This update triggers the flow (email changed)
contact["emailaddress1"] = "newemail@example.com";
service.Update(contact);
flowSimulator.AssertFlowTriggered("on_email_change");

// This update does NOT trigger the flow (name changed, not email)
contact["firstname"] = "Jane";
service.Update(contact);
flowSimulator.AssertFlowExecutionCount("on_email_change", 1); // Still 1
```

#### CreateOrUpdate Message
```csharp
// Flow triggers on both Create AND Update operations
var flowDefinition = new CloudFlowDefinition
{
    Name = "on_contact_create_or_update",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "CreateOrUpdate"
    },
    Actions = { /* ... */ }
};

flowSimulator.RegisterFlow(flowDefinition);

// Both operations trigger the flow
service.Create(contact);  // Triggers flow
service.Update(contact);  // Also triggers flow
Assert.Equal(2, flowSimulator.GetFlowExecutionCount("on_contact_create_or_update"));
```

### Manual Trigger Simulation

You can also manually trigger a flow without performing a CRUD operation:

```csharp
// Manually trigger a flow with specific inputs
var triggerInputs = new Dictionary<string, object>
{
    ["contactid"] = Guid.NewGuid(),
    ["firstname"] = "Jane",
    ["lastname"] = "Smith",
    ["emailaddress1"] = "jane.smith@example.com"
};

var result = flowSimulator.SimulateTrigger("on_contact_create", triggerInputs);

// Check result
Assert.True(result.Succeeded);
Assert.Empty(result.Errors);
Assert.NotEmpty(result.ActionResults);
```

### Connector Action Handling

#### Dataverse Connector Actions (Built-in)
```csharp
// Dataverse connector actions execute against the fake context automatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "create_related_records",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        // This action will execute against the fake context
        new DataverseAction
        {
            Name = "CreateContact",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "contact",
            Attributes = new Dictionary<string, object>
            {
                ["firstname"] = "Default"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Create account triggers flow, which creates contact
var service = context.GetOrganizationService();
var account = new Entity("account") { ["name"] = "Contoso" };
var accountId = service.Create(account);

// Verify contact was created by the flow
var contacts = context.CreateQuery("contact")
    .Where(c => c.GetAttributeValue<string>("firstname") == "Default")
    .ToList();

Assert.Single(contacts);
```

**Supported Dataverse Action Types:**
- `Create` - Create new records
- `Retrieve` - Retrieve a single record by ID
- `Update` - Update existing records
- `Delete` - Delete records
- `ListRecords` - Query multiple records with filtering, ordering, and top limit
- `Relate` - Associate records (many-to-many or one-to-many)
- `Unrelate` - Disassociate records
- `ExecuteAction` - Execute custom actions or custom APIs

#### Custom Connector Actions (Extensibility)
```csharp
// Implement a custom connector handler
public class Office365EmailHandler : IConnectorActionHandler
{
    public string ConnectorType => "Office365";
    public List<EmailCapture> EmailsSent { get; } = new List<EmailCapture>();

    public IDictionary<string, object> Execute(
        IFlowAction action,
        IXrmFakedContext context,
        IFlowExecutionContext flowContext)
    {
        // Extract email parameters from action
        var emailAction = action as CustomFlowAction;
        var to = emailAction?.Parameters.GetValueOrDefault("To") as string;
        var subject = emailAction?.Parameters.GetValueOrDefault("Subject") as string;
        var body = emailAction?.Parameters.GetValueOrDefault("Body") as string;

        // Mock sending email - capture for verification
        EmailsSent.Add(new EmailCapture
        {
            To = to,
            Subject = subject,
            Body = body
        });

        // Return success with outputs
        return new Dictionary<string, object>
        {
            ["StatusCode"] = 200,
            ["MessageId"] = Guid.NewGuid().ToString()
        };
    }
}

public class EmailCapture
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}

// Register the handler
var emailHandler = new Office365EmailHandler();
flowSimulator.RegisterConnectorActionHandler("Office365", emailHandler);

// Define a flow that uses the custom connector
var flowDefinition = new CloudFlowDefinition
{
    Name = "send_email_on_account_create",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new CustomFlowAction
        {
            Name = "SendEmail",
            ConnectorType = "Office365",
            Parameters = new Dictionary<string, object>
            {
                ["To"] = "sales@example.com",
                ["Subject"] = "New Account Created",
                ["Body"] = "A new account was created"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Create account - flow triggers and "sends" email
service.Create(new Entity("account") { ["name"] = "Contoso" });

// Verify email was "sent"
Assert.Single(emailHandler.EmailsSent);
Assert.Equal("sales@example.com", emailHandler.EmailsSent[0].To);
Assert.Contains("New Account", emailHandler.EmailsSent[0].Subject);
```

**Note:** You'll need to create a `CustomFlowAction` class that implements `IFlowAction` for non-Dataverse connectors. The framework provides `DataverseAction` built-in, but custom connectors require your own action implementation.

### Verification APIs

```csharp
// Assert a flow was triggered
flowSimulator.AssertFlowTriggered("on_contact_create");

// Assert a flow was NOT triggered
flowSimulator.AssertFlowNotTriggered("on_account_update");

// Get execution count
int count = flowSimulator.GetFlowExecutionCount("on_contact_create");
Assert.Equal(3, count);

// Get flow execution results
var results = flowSimulator.GetFlowExecutionResults("on_contact_create");
Assert.Equal(3, results.Count);

foreach (var result in results)
{
    Assert.True(result.Succeeded);
    Assert.Empty(result.Errors);
    
    // Inspect trigger inputs
    var contactId = result.TriggerInputs["contactid"];
    var firstname = result.TriggerInputs["firstname"];
    
    // Inspect action outputs
    var taskCreated = result.ActionResults
        .FirstOrDefault(a => a.ActionName == "CreateTask");
    
    if (taskCreated != null)
    {
        Assert.True(taskCreated.Succeeded);
        Assert.NotNull(taskCreated.Outputs);
    }
}
```

### Advanced Features (Future Enhancements)

The following features are planned for future releases. Currently, you can work around these by using custom flow actions and handlers.

#### Conditional Logic (Future)
```csharp
// PLANNED: Conditional logic support with ConditionAction
// Current workaround: Use multiple flows or custom action handlers

// Future syntax (not yet implemented):
var flowDefinition = new CloudFlowDefinition
{
    Name = "conditional_processing",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "lead",
        Message = "Update",
        FilteredAttributes = new[] { "estimatedvalue" }
    },
    Actions = new List<IFlowAction>
    {
        new ConditionAction
        {
            Condition = "@greater(triggerBody()?['estimatedvalue'], 100000)",
            TrueActions = new List<IFlowAction> { /* ... */ },
            FalseActions = new List<IFlowAction> { /* ... */ }
        }
    }
};
```

#### Error Simulation
You can simulate errors in custom connector handlers:

```csharp
// Configure a connector to fail
public class FailingConnectorHandler : IConnectorActionHandler
{
    public string ConnectorType => "CustomAPI";
    
    public IDictionary<string, object> Execute(
        IFlowAction action,
        IXrmFakedContext context,
        IFlowExecutionContext flowContext)
    {
        // Simulate a failure
        throw new InvalidPluginExecutionException("External service unavailable");
    }
}

flowSimulator.RegisterConnectorActionHandler("CustomAPI", new FailingConnectorHandler());

// Execute operation that triggers flow
service.Create(account);

// Verify flow handled error
var results = flowSimulator.GetFlowExecutionResults("send_email_on_account_create");
Assert.False(results[0].Succeeded);
Assert.Single(results[0].Errors);
Assert.Contains("External service unavailable", results[0].Errors[0]);
```

## Trigger Types

### Dataverse Triggers

| Trigger Type | Description | When It Fires |
|-------------|-------------|---------------|
| **Create** | When a record is created | After successful Create operation (PostOperation stage) |
| **Update** | When a record is updated | After successful Update operation (PostOperation stage) |
| **Delete** | When a record is deleted | After successful Delete operation (PostOperation stage) |
| **CreateOrUpdate** | When a record is created or updated | After Create or Update |

**Note:** Automatic triggering requires `context.UsePipelineSimulation = true`.

### Trigger Scope

| Scope | Description | Status |
|-------|-------------|--------|
| **Organization** | All records in the organization | ✅ Supported |
| **BusinessUnit** | Records owned by users in the same business unit | ⏳ Planned |
| **ParentChildBusinessUnits** | Records in parent/child business units | ⏳ Planned |
| **User** | Records owned by the triggering user | ⏳ Planned |

Currently, only Organization scope is fully implemented. Other scopes are planned for future releases.

### Filtered Attributes (Update Trigger Only)
```csharp
var trigger = new DataverseTrigger
{
    EntityLogicalName = "account",
    Message = "Update",
    FilteredAttributes = new List<string> { "name", "revenue" } // Only trigger on these fields
};
```

## Connector Types

### Built-in Connectors
- **Dataverse** - Full support for CRUD operations (Create, Retrieve, Update, Delete, ListRecords, Relate, Unrelate, ExecuteAction)

### Extensible Connectors
Test writers can register handlers for:
- Office 365 Outlook
- SharePoint
- Microsoft Teams
- OneDrive
- Azure services
- Custom connectors and APIs

## Best Practices

1. **Test Flow Logic Separately**: Test the flow definition and business logic independently from integration points
2. **Mock External Services**: Use connector action handlers to mock external dependencies
3. **Verify Dataverse Changes**: Assert that flows make expected changes to Dataverse records
4. **Use Real Flow Definitions**: Import actual flow JSON to ensure tests match production behavior
5. **Test Error Scenarios**: Simulate connector failures to verify error handling
6. **Validate Trigger Conditions**: Ensure flows only trigger under the right conditions

## Common Use Cases

### Create Follow-up Task on Contact Creation
```csharp
// Flow: Create a task when a new contact is created
var flowDefinition = new CloudFlowDefinition
{
    Name = "create_followup_task",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "CreateTask",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Follow up with new contact",
                ["description"] = "Reach out to the new lead",
                ["scheduledstart"] = DateTime.Now.AddDays(1)
            }
        }
    }
};

// Test
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true;
var flowSimulator = context.CloudFlowSimulator;
flowSimulator.RegisterFlow(flowDefinition);

var service = context.GetOrganizationService();
var contactId = service.Create(new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
});

// Verify task was created
flowSimulator.AssertFlowTriggered("create_followup_task");
var tasks = context.CreateQuery("task").ToList();
Assert.Single(tasks);
Assert.Equal("Follow up with new contact", tasks[0]["subject"]);
```

### Update Related Records
```csharp
// Flow: When account is updated, create a note
var flowDefinition = new CloudFlowDefinition
{
    Name = "log_account_update",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Update",
        FilteredAttributes = new List<string> { "address1_city" }
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "CreateNote",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "annotation",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Account address updated",
                ["notetext"] = "City was changed"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Update account - triggers flow only when city changes
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso",
    ["address1_city"] = "Seattle"
};
context.Initialize(account);

// This triggers the flow (city changed)
account["address1_city"] = "Portland";
service.Update(account);

flowSimulator.AssertFlowTriggered("log_account_update");
var notes = context.CreateQuery("annotation").ToList();
Assert.Single(notes);

// This doesn't trigger the flow (other field changed)
account["phone"] = "555-1234";
service.Update(account);
Assert.Equal(1, flowSimulator.GetFlowExecutionCount("log_account_update")); // Still 1
```

## Implementation Status

**Current Status:** ✅ **Completed** (October 11, 2025)

All core phases have been implemented and tested. The Cloud Flow simulation feature is fully functional for testing Dataverse-triggered flows.

### Phase 1: Core Infrastructure ✅ **COMPLETED**
- ✅ `ICloudFlowSimulator` interface
- ✅ `CloudFlowDefinition` class
- ✅ `IFlowTrigger` interface and implementations
- ✅ `IFlowAction` interface and base implementations
- ✅ Flow registration and storage
- ✅ Basic trigger simulation

### Phase 2: Dataverse Integration ✅ **COMPLETED**
- ✅ `DataverseTrigger` implementation
- ✅ `DataverseAction` implementation with full CRUD support
- ✅ Integration with CRUD operations
- ✅ Trigger condition evaluation
- ✅ Filtered attributes support

### Phase 3: Connector Extensibility ✅ **COMPLETED**
- ✅ `IConnectorActionHandler` interface
- ✅ Connector action handler registration
- ✅ Built-in Dataverse connector
- ✅ Extensibility for custom connectors

### Phase 4: Verification APIs ✅ **COMPLETED**
- ✅ Flow execution tracking
- ✅ `AssertFlowTriggered` / `AssertFlowNotTriggered`
- ✅ `GetFlowExecutionResults`
- ✅ Action result inspection
- ✅ Execution history

### Phase 5: Advanced Features (Future Enhancements)
- ⏳ JSON flow definition import (`RegisterFlowFromJson`)
- ⏳ Conditional logic (if/then/else)
- ⏳ Apply to each (loops)
- ⏳ Compose actions and expression evaluation
- ⏳ Error handling and retry logic
- ⏳ Parallel branches

**Test Coverage:** 47 unit tests, all passing ✅
- 22 tests for core simulator functionality
- 12 tests for Dataverse connector actions
- 13 tests for automatic flow triggering

## Key Differences from FakeXrmEasy v2

**Important**: The Cloud Flow simulation feature in Fake4Dataverse differs from FakeXrmEasy v2+ in several ways:

### API Design Differences

| Feature | FakeXrmEasy v2+ | Fake4Dataverse |
|---------|----------------|----------------|
| **Flow Registration** | Unknown | Explicit registration with `RegisterFlow` |
| **Automatic Triggering** | Unknown | Requires `UsePipelineSimulation = true` |
| **Connector Handlers** | Unknown | Extensibility-first with `IConnectorActionHandler` |
| **JSON Import** | Unknown | Planned for future release |
| **Expression Engine** | Unknown | Basic support, full expressions planned |

### Setup Differences

**Fake4Dataverse:**
```csharp
// Explicit registration with automatic triggering
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Required for automatic triggering
var flowSimulator = context.CloudFlowSimulator;

flowSimulator.RegisterFlow(new CloudFlowDefinition
{
    Name = "my_flow",
    Trigger = new DataverseTrigger { /* ... */ },
    Actions = new List<IFlowAction> { /* ... */ }
});

// Flows trigger automatically on CRUD operations
service.Create(entity);
```

**FakeXrmEasy v2+ (reference):**
Specific implementation details for FakeXrmEasy v2+ are not publicly documented. Consult FakeXrmEasy v2+ documentation for comparison.

## Related Documentation

- [Testing Plugins](testing-plugins.md) - Similar patterns for testing business logic
- [Custom API Support](custom-api.md) - Custom APIs can be triggered by flows
- [CRUD Operations](crud-operations.md) - Dataverse operations that trigger flows
- [Message Executors](../messages/README.md) - Integration points for flow triggers

## Contributing

This feature is now implemented and functional! We welcome:
- Bug reports and issue submissions
- Feature requests for Phase 5 enhancements
- Pull requests with improvements
- Test case contributions
- Documentation improvements

See [FEATURE_PARITY_ISSUES.md](../../FEATURE_PARITY_ISSUES.md) for tracking and [GitHub Issues](https://github.com/rnwood/Fake4Dataverse/issues) to report bugs or request features.
