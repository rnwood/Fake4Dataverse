# Cloud Flow Simulation

## Overview

Cloud Flows (Power Automate flows) are an increasingly common integration pattern for Dataverse applications. The Cloud Flow simulation feature in Fake4Dataverse enables developers to test Dataverse-triggered flows, verify flow execution, and validate flow actions/outputs in unit tests.

**Status:** Planning Phase - API Design (Issue #14)

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

## API Design Goals

The Cloud Flow simulation API is designed to:
1. **Support real flow definitions** - Import and consume exported Cloud Flow JSON definitions
2. **Simulate Dataverse triggers** - Trigger flows based on record changes (create, update, delete)
3. **Handle connector actions** - Built-in support for Dataverse actions, extensibility for other connectors
4. **Verify flow execution** - Assert that flows were triggered and inspect their results
5. **Enable custom logic** - Allow test writers to mock non-Dataverse connectors and data transformations

## Planned API Surface

### Flow Registration

#### Register from JSON Definition
```csharp
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
var flowSimulator = context.CloudFlowSimulator;

// Import a real Cloud Flow definition (exported from Power Automate)
string flowJson = File.ReadAllText("MyDataverseFlow.json");
flowSimulator.RegisterFlowFromJson(flowJson);

// Or register a flow definition programmatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "When a contact is created",
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
            ActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Follow up with @{triggerBody()?['fullname']}",
                ["regardingobjectid"] = "@{triggerBody()?['contactid']}"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

#### Built-in Dataverse Trigger Support
```csharp
// Flows are automatically triggered when operations occur in the fake context
var service = context.GetOrganizationService();

var contact = new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe",
    ["emailaddress1"] = "john.doe@example.com"
};

// Create triggers the registered flow
service.Create(contact);

// Verify the flow was triggered
flowSimulator.AssertFlowTriggered("When a contact is created");
```

### Trigger Simulation

```csharp
// Manually trigger a flow with specific inputs
var triggerInputs = new Dictionary<string, object>
{
    ["contactid"] = Guid.NewGuid(),
    ["fullname"] = "Jane Smith",
    ["emailaddress1"] = "jane.smith@example.com"
};

flowSimulator.SimulateTrigger("When a contact is created", triggerInputs);
```

### Connector Action Handling

#### Dataverse Connector Actions (Built-in)
```csharp
// Dataverse connector actions execute against the fake context automatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "Create related records",
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
            ActionType = DataverseActionType.Create,
            EntityLogicalName = "contact",
            Attributes = new Dictionary<string, object>
            {
                ["firstname"] = "Default",
                ["parentcustomerid"] = "@{triggerBody()?['accountid']}"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Create account triggers flow, which creates contact
var account = new Entity("account") { ["name"] = "Contoso" };
service.Create(account);

// Verify contact was created by the flow
var contacts = service.RetrieveMultiple(new QueryByAttribute("contact")
{
    Attributes = { "parentcustomerid" },
    Values = { account.Id }
}).Entities;

Assert.Single(contacts);
```

#### Custom Connector Actions (Extensibility)
```csharp
// Register a handler for non-Dataverse connectors
flowSimulator.RegisterConnectorActionHandler("Office365", new Office365ConnectorHandler());
flowSimulator.RegisterConnectorActionHandler("SharePoint", new SharePointConnectorHandler());

// Implement custom connector handler
public class Office365ConnectorHandler : IConnectorActionHandler
{
    public bool CanHandle(IFlowAction action)
    {
        return action.ConnectorType == "Office365" && 
               action.ActionName == "SendEmailV2";
    }

    public object Execute(IFlowAction action, IXrmFakedContext context, 
                          Dictionary<string, object> parameters)
    {
        // Mock sending email - capture the request
        var to = parameters["To"] as string;
        var subject = parameters["Subject"] as string;
        var body = parameters["Body"] as string;

        // Store for verification
        EmailsSent.Add(new EmailCapture
        {
            To = to,
            Subject = subject,
            Body = body
        });

        // Return success
        return new { StatusCode = 200, MessageId = Guid.NewGuid().ToString() };
    }
}

// Use in test
service.Create(account);

// Verify flow triggered email
var emailHandler = (Office365ConnectorHandler)flowSimulator
    .GetConnectorHandler("Office365");
Assert.Single(emailHandler.EmailsSent);
Assert.Equal("john.doe@example.com", emailHandler.EmailsSent[0].To);
```

### Verification APIs

```csharp
// Assert a flow was triggered
flowSimulator.AssertFlowTriggered("When a contact is created");

// Assert a flow was NOT triggered
flowSimulator.AssertFlowNotTriggered("When account is updated");

// Get execution count
int count = flowSimulator.GetFlowExecutionCount("When a contact is created");
Assert.Equal(3, count);

// Get flow execution results
var results = flowSimulator.GetFlowExecutionResults("When a contact is created");
Assert.Equal(3, results.Count);

foreach (var result in results)
{
    Assert.True(result.Succeeded);
    Assert.Empty(result.Errors);
    
    // Inspect trigger inputs
    var contactId = result.TriggerInputs["contactid"];
    
    // Inspect action outputs
    var emailSent = result.ActionResults
        .OfType<ConnectorActionResult>()
        .First(a => a.ActionName == "SendEmailV2");
    Assert.Equal(200, emailSent.StatusCode);
}
```

### Advanced Features

#### Conditional Logic
```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "Conditional processing",
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
            TrueActions = new List<IFlowAction>
            {
                new DataverseAction
                {
                    ActionType = DataverseActionType.Update,
                    EntityLogicalName = "lead",
                    EntityId = "@{triggerBody()?['leadid']}",
                    Attributes = new Dictionary<string, object>
                    {
                        ["prioritycode"] = new OptionSetValue(1) // High priority
                    }
                }
            },
            FalseActions = new List<IFlowAction>
            {
                // No action for low-value leads
            }
        }
    }
};
```

#### Error Simulation
```csharp
// Configure a connector to fail
var failingHandler = new FailingConnectorHandler
{
    ErrorMessage = "External service unavailable",
    ErrorCode = 503
};

flowSimulator.RegisterConnectorActionHandler("CustomAPI", failingHandler);

// Execute operation that triggers flow
service.Create(account);

// Verify flow handled error
var results = flowSimulator.GetFlowExecutionResults("When account is created");
Assert.False(results[0].Succeeded);
Assert.Contains("External service unavailable", results[0].Errors[0]);
```

#### Data Transformation
```csharp
// Flows can transform data using expressions
var flowDefinition = new CloudFlowDefinition
{
    Name = "Transform contact data",
    Actions = new List<IFlowAction>
    {
        new ComposeAction
        {
            Name = "FormatFullName",
            Expression = "@concat(triggerBody()?['firstname'], ' ', triggerBody()?['lastname'])"
        },
        new DataverseAction
        {
            ActionType = DataverseActionType.Update,
            Attributes = new Dictionary<string, object>
            {
                ["fullname"] = "@{outputs('FormatFullName')}"
            }
        }
    }
};
```

## Trigger Types

### Dataverse Triggers

| Trigger Type | Description | When It Fires |
|-------------|-------------|---------------|
| **Create** | When a record is created | After successful Create operation |
| **Update** | When a record is updated | After successful Update operation |
| **Delete** | When a record is deleted | After successful Delete operation |
| **CreateOrUpdate** | When a record is created or updated | After Create or Update |

### Trigger Scope

| Scope | Description |
|-------|-------------|
| **Organization** | All records in the organization |
| **BusinessUnit** | Records owned by users in the same business unit |
| **User** | Records owned by the triggering user |

### Filtered Attributes (Update Trigger Only)
```csharp
var trigger = new DataverseTrigger
{
    EntityLogicalName = "account",
    Message = "Update",
    FilteredAttributes = new[] { "name", "revenue" } // Only trigger on these fields
};
```

## Connector Types

### Built-in Connectors
- **Dataverse** - Full support for CRUD operations, custom actions, custom APIs
- **HTTP** - Basic support (extensibility point for custom logic)

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

### Notify on High-Value Opportunity
```csharp
// Flow: Send email when opportunity value exceeds threshold
var flowDefinition = new CloudFlowDefinition
{
    Name = "Notify on high-value opportunity",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "opportunity",
        Message = "Update",
        FilteredAttributes = new[] { "estimatedvalue" }
    },
    Actions = new List<IFlowAction>
    {
        new ConditionAction
        {
            Condition = "@greater(triggerBody()?['estimatedvalue'], 500000)",
            TrueActions = new List<IFlowAction>
            {
                new ConnectorAction
                {
                    ConnectorType = "Office365",
                    ActionName = "SendEmailV2",
                    Parameters = new Dictionary<string, object>
                    {
                        ["To"] = "sales.manager@example.com",
                        ["Subject"] = "High-Value Opportunity Alert",
                        ["Body"] = "Opportunity @{triggerBody()?['name']} is worth @{triggerBody()?['estimatedvalue']}"
                    }
                }
            }
        }
    }
};

// Test
flowSimulator.RegisterFlow(flowDefinition);
var emailHandler = new CaptureEmailHandler();
flowSimulator.RegisterConnectorActionHandler("Office365", emailHandler);

var opp = new Entity("opportunity")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Deal",
    ["estimatedvalue"] = new Money(750000)
};

context.Initialize(opp);
opp["estimatedvalue"] = new Money(600000);
service.Update(opp);

// Verify email sent
Assert.Single(emailHandler.EmailsSent);
Assert.Contains("Contoso Deal", emailHandler.EmailsSent[0].Body);
```

### Cascade Record Updates
```csharp
// Flow: When account updated, update all related contacts
var flowDefinition = new CloudFlowDefinition
{
    Name = "Cascade account updates",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Update",
        FilteredAttributes = new[] { "address1_city" }
    },
    Actions = new List<IFlowAction>
    {
        // List related contacts
        new DataverseAction
        {
            ActionType = DataverseActionType.ListRecords,
            EntityLogicalName = "contact",
            Filter = "parentcustomerid eq @{triggerBody()?['accountid']}"
        },
        // Apply to each contact
        new ApplyToEachAction
        {
            Source = "@outputs('ListContacts')?['value']",
            Actions = new List<IFlowAction>
            {
                new DataverseAction
                {
                    ActionType = DataverseActionType.Update,
                    EntityLogicalName = "contact",
                    EntityId = "@{items('Apply_to_each')?['contactid']}",
                    Attributes = new Dictionary<string, object>
                    {
                        ["address1_city"] = "@{triggerBody()?['address1_city']}"
                    }
                }
            }
        }
    }
};
```

## Implementation Status

**Current Status:** Planning Phase

This document outlines the planned API design for Cloud Flow simulation. The following components need to be implemented:

### Phase 1: Core Infrastructure (Planned)
- [ ] `ICloudFlowSimulator` interface
- [ ] `CloudFlowDefinition` class
- [ ] `IFlowTrigger` interface and implementations
- [ ] `IFlowAction` interface and base implementations
- [ ] Flow registration and storage
- [ ] Basic trigger simulation

### Phase 2: Dataverse Integration (Planned)
- [ ] `DataverseTrigger` implementation
- [ ] `DataverseAction` implementation
- [ ] Integration with CRUD message executors
- [ ] Trigger condition evaluation
- [ ] Filtered attributes support

### Phase 3: Connector Extensibility (Planned)
- [ ] `IConnectorActionHandler` interface
- [ ] Connector action handler registration
- [ ] Built-in HTTP connector support
- [ ] Expression evaluation for data transformation

### Phase 4: Verification APIs (Planned)
- [ ] Flow execution tracking
- [ ] `AssertFlowTriggered` / `AssertFlowNotTriggered`
- [ ] `GetFlowExecutionResults`
- [ ] Action result inspection

### Phase 5: Advanced Features (Future)
- [ ] JSON flow definition import
- [ ] Conditional logic (if/then/else)
- [ ] Apply to each (loops)
- [ ] Compose actions and expressions
- [ ] Error handling and retry logic
- [ ] Parallel branches

## Key Differences from FakeXrmEasy v2

**Important**: The Cloud Flow simulation feature in Fake4Dataverse differs from FakeXrmEasy v2+ in several ways:

### API Design Differences

| Feature | FakeXrmEasy v2+ | Fake4Dataverse (Planned) |
|---------|----------------|-------------------------|
| **Flow Registration** | Attribute-based | Explicit registration with `RegisterFlow` or `RegisterFlowFromJson` |
| **Connector Handlers** | Built-in for common connectors | Extensibility-first with `IConnectorActionHandler` |
| **JSON Import** | Unknown | Planned first-class support |
| **Expression Engine** | Unknown | Simplified subset of Power Automate expressions |

### Setup Differences

**Fake4Dataverse (Planned):**
```csharp
// Explicit registration
var context = XrmFakedContextFactory.New();
var flowSimulator = context.CloudFlowSimulator;
flowSimulator.RegisterFlowFromJson(flowJson);
```

**FakeXrmEasy v2+ (for reference):**
```csharp
// Attribute-based (assumed pattern)
[Flow("When contact is created")]
public class MyFlowTest { }
```

## Related Documentation

- [Testing Plugins](testing-plugins.md) - Similar patterns for testing business logic
- [Custom API Support](custom-api.md) - Custom APIs can be triggered by flows
- [CRUD Operations](crud-operations.md) - Dataverse operations that trigger flows
- [Message Executors](../messages/README.md) - Integration points for flow triggers

## Contributing

This feature is in the planning phase. We welcome feedback on the API design! Please:
- Review the proposed API surface
- Share your Cloud Flow testing use cases
- Suggest improvements to the extensibility model
- Contribute to the implementation

See [FEATURE_PARITY_ISSUES.md](../../FEATURE_PARITY_ISSUES.md) for tracking.
