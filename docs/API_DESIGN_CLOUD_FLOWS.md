# Cloud Flow Simulation - API Design Specification

## Document Purpose

This document defines the API surface and architecture for the Cloud Flow simulation feature in Fake4Dataverse. It serves as a design specification that will guide implementation and ensure consistency with existing framework patterns.

**Issue:** #14 - Implement Cloud Flows Integration Testing  
**Created:** 2025-10-11  
**Status:** Planning Phase - API Design Complete

## Executive Summary

Cloud Flows (Power Automate flows) are a critical integration pattern in modern Dataverse applications. This feature enables developers to test flows in isolation, verify flow triggers, and validate flow actions without requiring a live Power Automate environment.

### Key Design Principles

1. **Extensibility First** - Support custom connectors via handler registration
2. **Real Definition Support** - Import actual Cloud Flow JSON definitions  
3. **Dataverse Integration** - Seamless integration with CRUD operations and plugin pipeline
4. **Verification APIs** - Assert flow execution and inspect results
5. **Consistent Patterns** - Follow existing framework patterns (PluginPipelineSimulator, Custom API, etc.)

## API Components

### 1. Core Interfaces

#### ICloudFlowSimulator
Main entry point for Cloud Flow simulation. Provides:
- Flow registration (`RegisterFlow`, `RegisterFlowFromJson`)
- Manual trigger simulation (`SimulateTrigger`)
- Connector handler registration (`RegisterConnectorActionHandler`)
- Verification methods (`AssertFlowTriggered`, `GetFlowExecutionResults`)

**Location:** `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/ICloudFlowSimulator.cs`

**Pattern Inspiration:** `IPluginPipelineSimulator` - Similar registration and execution model

#### ICloudFlowDefinition
Represents a complete flow definition with:
- Unique name and display name
- Trigger specification
- List of actions
- Enabled/disabled state
- Metadata dictionary

**Location:** `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/ICloudFlowDefinition.cs`

#### IFlowTrigger
Base interface for flow triggers. Concrete implementations:
- `DataverseTrigger` - Triggers on Dataverse Create/Update/Delete operations

**Location:** `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/IFlowTrigger.cs`

**Extensibility:** Future implementations could include:
- `ScheduleTrigger` - Recurrence-based triggers
- `ManualTrigger` - Manually invoked flows
- `HttpTrigger` - Webhook-style triggers

#### IFlowAction
Base interface for flow actions. Concrete implementations:
- `DataverseAction` - CRUD operations on Dataverse entities

**Location:** `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/IFlowAction.cs`

**Extensibility:** Future implementations could include:
- `ConditionAction` - If/then/else branching
- `ApplyToEachAction` - Loops over collections
- `ComposeAction` - Data transformation
- `ConnectorAction` - Generic connector action

#### IConnectorActionHandler
Extensibility point for custom connector logic:
- `CanHandle(IFlowAction)` - Check if handler supports action
- `Execute(...)` - Execute the action and return outputs

**Location:** `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/IConnectorActionHandler.cs`

**Pattern Inspiration:** Strategy pattern, similar to message executors

### 2. Enums

#### TriggerScope
Defines record scope for Dataverse triggers:
- `Organization` - All records (most common)
- `BusinessUnit` - Same business unit
- `ParentChildBusinessUnits` - Business unit hierarchy
- `User` - User-owned records only

**Reference:** https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger

#### DataverseActionType
Defines Dataverse connector actions:
- `Create`, `Retrieve`, `Update`, `Delete`
- `ListRecords` - Query records
- `Relate`, `Unrelate` - Associations
- `ExecuteAction`, `PerformUnboundAction` - Custom actions/APIs
- `UploadFile`, `DownloadFile` - File operations

**Reference:** https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/

### 3. POCOs

#### CloudFlowDefinition
Concrete implementation of `ICloudFlowDefinition`.

#### DataverseTrigger
Concrete implementation of `IFlowTrigger` for Dataverse events:
- `EntityLogicalName` - Target entity
- `Message` - Create, Update, Delete, CreateOrUpdate
- `Scope` - Organization, BusinessUnit, User
- `FilteredAttributes` - Update trigger filtering
- `Condition` - Optional expression filter

#### DataverseAction
Concrete implementation of `IFlowAction` for Dataverse operations:
- `DataverseActionType` - Specific operation
- `EntityLogicalName` - Target entity
- `EntityId` - For Update/Delete/Retrieve
- `Attributes` - For Create/Update
- `Filter`, `OrderBy`, `Top` - For ListRecords

### 4. Result Types

#### IFlowExecutionResult
Captures flow execution details:
- `FlowName` - Flow identifier
- `TriggeredAt` - Timestamp
- `Succeeded` - Success flag
- `TriggerInputs` - Trigger parameters
- `ActionResults` - Per-action results
- `Errors` - Error messages
- `Duration` - Execution time

#### IFlowActionResult
Captures individual action results:
- `ActionName` - Action identifier
- `ActionType` - Action category
- `Succeeded` - Success flag
- `Outputs` - Action outputs
- `ErrorMessage` - If failed

#### IFlowExecutionContext
Provides context during action execution:
- `TriggerInputs` - Original trigger data
- `GetActionOutputs(name)` - Retrieve previous action outputs
- `AllActionOutputs` - All outputs

## Integration Points

### With XrmFakedContext

Add property to `IXrmFakedContext`:
```csharp
ICloudFlowSimulator CloudFlowSimulator { get; }
```

**Status:** ✅ Added to interface

### With CRUD Message Executors

Flow triggers should fire automatically when:
- `CreateRequestExecutor` succeeds → Trigger flows with `Message = "Create"`
- `UpdateRequestExecutor` succeeds → Trigger flows with `Message = "Update"` or `"CreateOrUpdate"`
- `DeleteRequestExecutor` succeeds → Trigger flows with `Message = "Delete"`

**Implementation Pattern:**
```csharp
// In CreateRequestExecutor.Execute():
if (context.UsePipelineSimulation)
{
    // Execute PostOperation plugins
}

// NEW: Trigger Cloud Flows
context.CloudFlowSimulator.TriggerDataverseFlows("Create", entityLogicalName, createdEntity);
```

**Key Design Decision:** Should flows trigger before or after PostOperation plugins?
- **Recommendation:** After PostOperation plugins (matches real behavior - flows are asynchronous)
- Flows should see the final committed state of the record

### With Plugin Pipeline

Cloud Flows are conceptually similar to async plugins:
- Both trigger after Dataverse operations
- Both can execute additional actions
- Both should respect transaction boundaries

**Design Consideration:** Should Cloud Flow simulation respect `UsePipelineSimulation` flag?
- **Recommendation:** Yes - Align with plugin behavior
- Add separate `UseCloudFlowSimulation` flag if needed

## Implementation Phases

### Phase 1: Core Infrastructure ✅ PLANNED
- [x] Define `ICloudFlowSimulator` interface
- [x] Define `ICloudFlowDefinition`, `IFlowTrigger`, `IFlowAction` interfaces
- [x] Define `IFlowExecutionResult`, `IFlowActionResult` interfaces
- [x] Define `IConnectorActionHandler`, `IFlowExecutionContext` interfaces
- [x] Create enums: `TriggerScope`, `DataverseActionType`
- [x] Create POCOs: `CloudFlowDefinition`, `DataverseTrigger`, `DataverseAction`
- [x] Add `CloudFlowSimulator` property to `IXrmFakedContext`
- [x] Create comprehensive API documentation

### Phase 2: Core Simulator Implementation (TODO)
- [ ] Implement `CloudFlowSimulator` class
- [ ] Flow registration and storage
- [ ] Manual trigger simulation (`SimulateTrigger`)
- [ ] Execution context management
- [ ] Result tracking and history
- [ ] Verification methods (`AssertFlowTriggered`, etc.)
- [ ] Unit tests for core simulator

### Phase 3: Dataverse Integration (TODO)
- [ ] Implement `DataverseActionHandler` (built-in handler)
- [ ] Handle Create, Update, Delete actions
- [ ] Handle ListRecords with filtering
- [ ] Handle Relate/Unrelate actions
- [ ] Handle custom actions/APIs
- [ ] Integrate with CRUD message executors for automatic triggering
- [ ] Filtered attributes support
- [ ] Trigger condition evaluation
- [ ] Unit tests for Dataverse integration

### Phase 4: JSON Import (TODO)
- [ ] Parse Cloud Flow JSON schema
- [ ] Extract trigger definition
- [ ] Extract action definitions
- [ ] Map to internal flow definition
- [ ] Handle common expression patterns
- [ ] Unit tests for JSON import

### Phase 5: Advanced Features (FUTURE)
- [ ] Conditional logic (`ConditionAction`)
- [ ] Apply to each (`ApplyToEachAction`)
- [ ] Compose actions and expression evaluation
- [ ] Parallel branches
- [ ] Error handling and retry logic
- [ ] Schedule triggers
- [ ] HTTP triggers

## Usage Examples

### Basic Registration and Verification
```csharp
var context = XrmFakedContextFactory.New();
var flowSimulator = context.CloudFlowSimulator;

// Register a flow
var flowDefinition = new CloudFlowDefinition
{
    Name = "notify_on_contact_create",
    DisplayName = "Notify on New Contact",
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
                ["subject"] = "Follow up with new contact"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Perform operation
var service = context.GetOrganizationService();
var contact = new Entity("contact") { ["firstname"] = "John" };
service.Create(contact);

// Verify flow triggered
flowSimulator.AssertFlowTriggered("notify_on_contact_create");

// Inspect results
var results = flowSimulator.GetFlowExecutionResults("notify_on_contact_create");
Assert.Single(results);
Assert.True(results[0].Succeeded);
```

### Custom Connector Handler
```csharp
// Implement custom connector
public class TestEmailHandler : IConnectorActionHandler
{
    public string ConnectorType => "Office365";
    public List<EmailCapture> SentEmails = new List<EmailCapture>();

    public bool CanHandle(IFlowAction action)
    {
        return action.Parameters.ContainsKey("ActionName") &&
               action.Parameters["ActionName"].ToString() == "SendEmailV2";
    }

    public IDictionary<string, object> Execute(IFlowAction action, 
        IXrmFakedContext context, IFlowExecutionContext flowContext)
    {
        SentEmails.Add(new EmailCapture
        {
            To = action.Parameters["To"].ToString(),
            Subject = action.Parameters["Subject"].ToString()
        });
        
        return new Dictionary<string, object>
        {
            ["StatusCode"] = 200,
            ["MessageId"] = Guid.NewGuid().ToString()
        };
    }
}

// Use in test
var emailHandler = new TestEmailHandler();
flowSimulator.RegisterConnectorActionHandler("Office365", emailHandler);

// ... execute flow ...

Assert.Single(emailHandler.SentEmails);
Assert.Equal("test@example.com", emailHandler.SentEmails[0].To);
```

## API Design Decisions

### 1. Registration Model: Explicit vs. Attribute-Based

**Decision:** Explicit registration via `RegisterFlow` and `RegisterFlowFromJson`

**Rationale:**
- Consistent with existing pattern (`PluginPipelineSimulator.RegisterPluginStep`)
- More flexible - supports runtime flow creation
- Easier to debug and understand
- Supports JSON import from real flows

**Alternative Considered:** Attribute-based registration (similar to SPKL plugin attributes)
- Rejected: Less flexible, harder to work with real flow definitions

### 2. Automatic vs. Manual Triggering

**Decision:** Both supported
- Automatic: Flows trigger when matching CRUD operations occur
- Manual: `SimulateTrigger` for controlled testing

**Rationale:**
- Automatic triggering matches real behavior
- Manual triggering enables isolated flow logic testing
- Best of both worlds

### 3. Expression Engine

**Decision:** Simplified subset initially, full Power Fx in future

**Rationale:**
- Power Fx is complex - full implementation is a large effort
- Most tests need simple property access: `@triggerBody()?['fieldname']`
- Can expand incrementally as needed

**Phase 1 Support:**
- Property access: `@triggerBody()?['field']`
- Action outputs: `@outputs('ActionName')?['field']`
- String interpolation: `@concat(...)`, `@{...}`

**Future Support:**
- Conditional expressions: `@greater(...)`, `@equals(...)`
- Date/time functions
- Array functions
- Full Power Fx compatibility

### 4. Connector Handler Registration

**Decision:** Global registration per connector type

**Rationale:**
- Simpler API - one handler per connector type
- Most tests mock entire connectors, not individual actions
- Can be refined later if needed

**Alternative Considered:** Per-action handler registration
- Rejected: More complex API, less common use case

### 5. Scope and Filtering

**Decision:** Support scope and filtered attributes in trigger definition, but simple evaluation initially

**Rationale:**
- Scope checking requires user/business unit context
- Simple implementation: Organization scope always matches
- Can be enhanced later based on user feedback

## Key Differences from FakeXrmEasy v2+

**Important:** This API design differs from FakeXrmEasy v2+ commercial version in several ways:

### Registration
- **FakeXrmEasy v2+**: Unknown (proprietary)
- **Fake4Dataverse**: Explicit registration with `RegisterFlow` or `RegisterFlowFromJson`

### JSON Import
- **FakeXrmEasy v2+**: Unknown (proprietary)
- **Fake4Dataverse**: Planned first-class support via `RegisterFlowFromJson`

### Connector Extensibility
- **FakeXrmEasy v2+**: Unknown (proprietary)
- **Fake4Dataverse**: `IConnectorActionHandler` interface with strategy pattern

### Expression Support
- **FakeXrmEasy v2+**: Unknown (proprietary)
- **Fake4Dataverse**: Simplified subset initially, expandable to full Power Fx

## Testing Strategy

### Unit Tests

**CloudFlowSimulator Tests:**
- Flow registration and unregistration
- Manual trigger simulation
- Execution result tracking
- Verification methods (AssertFlowTriggered, etc.)
- Connector handler registration
- Error handling

**Dataverse Integration Tests:**
- Automatic triggering on Create/Update/Delete
- Filtered attributes (Update only)
- Scope evaluation
- Dataverse action execution
- Custom action invocation

**Connector Handler Tests:**
- Built-in Dataverse handler
- Custom connector handler registration
- Handler selection logic
- Error propagation

**End-to-End Tests:**
- Complete flow scenarios
- Multiple actions
- Action chaining (outputs from one action to another)
- Error scenarios
- Real-world use cases

### Test File Organization
```
Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/
  CloudFlows/
    CloudFlowSimulatorTests.cs
    DataverseTriggerTests.cs
    DataverseActionHandlerTests.cs
    ConnectorHandlerTests.cs
    FlowExecutionResultTests.cs
    JsonImportTests.cs (future)
```

## Documentation Plan

### User Documentation

1. **Primary Guide:** `docs/usage/cloud-flows.md` ✅ CREATED
   - Overview and concepts
   - Registration examples
   - Trigger types and configuration
   - Action types and handlers
   - Verification APIs
   - Custom connector handlers
   - Common use cases
   - Best practices

2. **API Reference:** `docs/api/cloud-flow-simulator.md` (TODO)
   - Interface documentation
   - Method signatures
   - Parameter descriptions
   - Return types

3. **Migration Guide:** Update `docs/migration/from-v3.md` (TODO)
   - Differences from FakeXrmEasy v2+
   - Migration examples
   - Feature comparison

4. **Quick Start:** Update `docs/getting-started/quickstart.md` (TODO)
   - Add Cloud Flow testing example

### Internal Documentation

1. **Architecture:** `docs/concepts/cloud-flow-architecture.md` (TODO)
   - Component relationships
   - Execution flow
   - Integration with CRUD operations
   - Extension points

2. **Implementation Notes:** Code comments and XML documentation
   - Reference Microsoft documentation URLs
   - Explain design decisions
   - Note limitations and future enhancements

## Success Criteria

This API design is successful if:

1. ✅ **Complete Interface Definition** - All public interfaces defined
2. ✅ **Comprehensive Documentation** - User guide with examples
3. ✅ **Extensibility Model** - Clear pattern for custom connectors
4. ✅ **Consistent Patterns** - Follows existing framework conventions
5. ⏳ **Community Feedback** - Reviewed and approved by maintainers/users
6. ⏳ **Implementation Roadmap** - Clear phases for development
7. ⏳ **Test Strategy** - Defined test coverage and organization

## Next Steps

1. **Gather Feedback** (1-2 weeks)
   - Share design with maintainers
   - Get community input on API surface
   - Refine based on feedback

2. **Phase 2: Core Implementation** (2-3 weeks)
   - Implement `CloudFlowSimulator` class
   - Basic registration and execution
   - Unit tests

3. **Phase 3: Dataverse Integration** (2-3 weeks)
   - Implement `DataverseActionHandler`
   - Integrate with CRUD executors
   - End-to-end tests

4. **Phase 4: JSON Import** (1-2 weeks)
   - JSON parsing and mapping
   - Tests with real flow definitions

5. **Future Phases**
   - Advanced features as needed
   - Expression engine enhancements
   - Additional trigger types

## Related Documentation

- [Cloud Flows User Guide](../docs/usage/cloud-flows.md) ✅
- [Plugin Pipeline Simulator](../docs/usage/testing-plugins.md) - Similar patterns
- [Custom API Support](../docs/usage/custom-api.md) - Related feature
- [CRUD Operations](../docs/usage/crud-operations.md) - Integration point
- [Feature Parity Issues](../FEATURE_PARITY_ISSUES.md) - Issue #14

## Maintainer Notes

**API Stability:** This API is in planning phase. Breaking changes are expected before v1.0.

**Feedback Welcome:** Please provide feedback via:
- GitHub Issues
- Pull Requests
- Discussion threads

**Implementation Priority:** Medium-Low
- High value for modern Dataverse apps
- Complex feature requiring careful design
- Not blocking other features

## Appendix: Microsoft Documentation References

### Power Automate
- [Cloud Flows Overview](https://learn.microsoft.com/en-us/power-automate/overview-cloud)
- [Getting Started](https://learn.microsoft.com/en-us/power-automate/getting-started)
- [Triggers Introduction](https://learn.microsoft.com/en-us/power-automate/triggers-introduction)

### Dataverse Connector
- [Dataverse Connector Reference](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)
- [Create/Update/Delete Trigger](https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger)
- [Trigger Conditions](https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger#trigger-conditions)

### Connectors
- [Connector Reference](https://learn.microsoft.com/en-us/connectors/)
- [Standard Connectors](https://learn.microsoft.com/en-us/connectors/connector-reference/connector-reference-standard-connectors)

### Flow Definition
- [Workflow Definition Language](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)
- [Schema Reference](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language-schema-reference)

### Dataverse
- [Entity Operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations)
- [Query Data](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api)
