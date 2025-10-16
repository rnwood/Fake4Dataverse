# Cloud Flow Abstractions

This directory contains the abstractions, interfaces, POCOs, and enums for the Cloud Flow simulation feature in Fake4Dataverse.

## Purpose

Cloud Flows (Power Automate flows) are a critical integration pattern in modern Dataverse applications. These abstractions enable:
- Testing Dataverse-triggered flows without Power Automate
- Verifying flow execution and results
- Mocking connector actions for external integrations
- Importing real Cloud Flow definitions from JSON

## Status

**Implemented** ✅ - API design complete, core implementation complete, JSON import complete (2025-10-12).

Features implemented:
- Flow registration and execution engine
- **JSON import from real Power Automate flows** ✅ **NEW**
- Automatic triggering on CRUD operations
- Built-in Dataverse connector with full CRUD support
- Extensible connector system for mocking external systems
- Filtered attributes support (Update triggers)
- Comprehensive verification APIs
- 67 unit tests, all passing ✅ (includes 20 JSON import tests)

See [User Guide](../../../docs/usage/cloud-flows.md) for usage examples and [API Design Document](../../../docs/API_DESIGN_CLOUD_FLOWS.md) for detailed specifications.

## Structure

### Interfaces

#### ICloudFlowSimulator
Main entry point for Cloud Flow simulation. Provides methods to:
- Register flows programmatically (`RegisterFlow`)
- **Import real Power Automate flows from JSON** (`RegisterFlowFromJson`) ✅ **NEW**
- Manually trigger flows (`SimulateTrigger`)
- Register connector handlers (`RegisterConnectorActionHandler`)
- Verify flow execution (`AssertFlowTriggered`, `GetFlowExecutionResults`)

#### ICloudFlowDefinition
Represents a complete flow definition with trigger, actions, and metadata.

#### IFlowTrigger
Base interface for flow triggers. Concrete implementation:
- `DataverseTrigger` - Triggers on Create/Update/Delete operations

#### IFlowAction
Base interface for flow actions. Concrete implementation:
- `DataverseAction` - CRUD operations on Dataverse entities

#### IConnectorActionHandler
Extensibility point for custom connector logic. Allows test writers to mock:
- Office 365 actions (send email, create calendar events)
- SharePoint actions (upload files, create items)
- Teams actions (post messages, create channels)
- Custom API calls

#### IFlowExecutionResult
Contains execution details including trigger inputs, action results, and errors.

#### IFlowActionResult
Contains per-action execution details and outputs.

#### IFlowExecutionContext
Provides context during action execution (trigger inputs, previous action outputs).

### Enums

#### TriggerScope
Defines the scope of Dataverse triggers:
- `Organization` - All records
- `BusinessUnit` - Same business unit
- `ParentChildBusinessUnits` - Business unit hierarchy
- `User` - User-owned records only

#### DataverseActionType
Defines Dataverse connector actions:
- `Create`, `Retrieve`, `Update`, `Delete`
- `ListRecords`, `Relate`, `Unrelate`
- `ExecuteAction`, `PerformUnboundAction`
- `UploadFile`, `DownloadFile`

### POCOs

#### CloudFlowDefinition
Concrete implementation of `ICloudFlowDefinition`.

#### DataverseTrigger
Concrete Dataverse trigger with:
- Entity logical name
- Message (Create, Update, Delete)
- Scope
- Filtered attributes (Update only)
- Optional condition expression

#### DataverseAction
Concrete Dataverse action with:
- Action type
- Entity details (name, ID, attributes)
- Query parameters (filter, order by, top)

## Usage Example

```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;

// Define a flow
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
            Name = "Create_Task",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Follow up with new contact"
            }
        }
    }
};

// Register and test (implementation in Core)
var context = XrmFakedContextFactory.New();
context.CloudFlowSimulator.RegisterFlow(flowDefinition);

// Create contact - flow triggers automatically
var contact = new Entity("contact") { ["firstname"] = "John" };
service.Create(contact);

// Verify
context.CloudFlowSimulator.AssertFlowTriggered("notify_on_contact_create");
```

## Design Principles

1. **Extensibility First** - Support custom connectors via `IConnectorActionHandler`
2. **Real Flow Support** - Import actual Cloud Flow JSON definitions
3. **Consistent Patterns** - Follow existing patterns (PluginPipelineSimulator, Custom API)
4. **Verification APIs** - Assert and inspect flow execution
5. **Dataverse Integration** - Seamless integration with CRUD operations

## Microsoft Documentation

Official references:
- [Cloud Flows Overview](https://learn.microsoft.com/en-us/power-automate/overview-cloud)
- [Dataverse Connector](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)
- [Trigger Conditions](https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger)
- [Workflow Definition Language](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)

## Related Files

- **User Documentation:** [docs/usage/cloud-flows.md](../../../docs/usage/cloud-flows.md)
- **API Design:** [docs/API_DESIGN_CLOUD_FLOWS.md](../../../docs/API_DESIGN_CLOUD_FLOWS.md)
- **Feature Tracking:** [FEATURE_PARITY_ISSUES.md](../../../FEATURE_PARITY_ISSUES.md) - Issue #14

## Implementation

Implementation will be in `Fake4DataverseCore/Fake4Dataverse.Core/CloudFlows/`:
- `CloudFlowSimulator.cs` - Main simulator implementation
- `DataverseActionHandler.cs` - Built-in Dataverse connector handler
- `FlowExecutionResult.cs`, `FlowActionResult.cs` - Result POCOs
- `FlowExecutionContext.cs` - Execution context implementation

## Implementation Status

### ✅ Completed
1. **Core Implementation** - `CloudFlowSimulator` class fully implemented
2. **Dataverse Integration** - Integrated with CRUD message executors
3. **JSON Import** - Parse and map Cloud Flow JSON definitions ✅ **COMPLETE** (2025-10-12)

### 🔄 Future Enhancements
1. **Expression Evaluation** - Evaluate Power Fx expressions in actions
2. **Advanced Features** - Conditional logic, loops, parallel branches
3. **Additional Connectors** - HTTP, Office 365, SharePoint connectors

## Contributing

This feature is in planning phase. Contributions welcome:
- Review API design and provide feedback
- Suggest additional use cases
- Contribute to implementation
- Write tests and documentation

See Issue #14 in FEATURE_PARITY_ISSUES.md for tracking.
