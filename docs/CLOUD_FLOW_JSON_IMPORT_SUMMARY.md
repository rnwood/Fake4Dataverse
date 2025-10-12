# Cloud Flow JSON Import - Implementation Summary

## Overview

This implementation adds support for importing and simulating real Cloud Flow JSON definitions exported from Power Automate into Fake4Dataverse. This allows developers to test their actual production flows in unit tests without modification.

**Implementation Date:** October 12, 2025  
**Issue:** #14 - Implement Cloud Flows Integration Testing (Phase 4)  
**Test Coverage:** 20 new unit tests, all passing ✅

## What Was Implemented

### 1. JSON Schema Models (`CloudFlowJsonModels.cs`)

Created internal model classes that map to the official Logic Apps workflow definition language schema:

- `CloudFlowJsonRoot` - Root structure of exported flows
- `CloudFlowProperties` - Flow metadata (displayName, state, definition)
- `WorkflowDefinition` - The workflow definition with triggers and actions
- `TriggerDefinition` / `TriggerInputs` - Trigger configuration
- `ActionDefinition` / `ActionInputs` - Action configuration
- `HostDefinition` - Connection and operation metadata

**Reference:** [Logic Apps Workflow Definition Language](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)

### 2. JSON Parser (`CloudFlowJsonParser.cs`)

Implemented a comprehensive parser that:

- Deserializes JSON using System.Text.Json
- Maps JSON structures to internal `CloudFlowDefinition` objects
- Handles Dataverse triggers with all properties:
  - Message types (1=Create, 2=Update, 3=Delete, 4=CreateOrUpdate)
  - Entity logical names
  - Trigger scopes (1=Organization, 2=BusinessUnit, 3=ParentChildBusinessUnits, 4=User)
  - Filtered attributes for Update triggers
- Handles Dataverse actions:
  - CreateRecord, UpdateRecord, DeleteRecord, GetItem, ListRecords
  - Entity names, attributes, filters, ordering
  - Action dependencies (runAfter)
- Provides clear error messages for unsupported features
- Safely extracts values from JsonElement objects

### 3. CloudFlowSimulator Integration

Updated `RegisterFlowFromJson` method to:
- Accept JSON string parameter
- Parse JSON using the new parser
- Register the parsed flow definition
- Provide comprehensive documentation on supported features and limitations

### 4. Comprehensive Test Suite (`CloudFlowJsonImportTests.cs`)

Created 20 comprehensive unit tests covering:

**Basic Import Tests (3 tests):**
- Simple Create trigger with one action
- Update trigger with filtered attributes
- Multiple actions with dependencies

**Trigger Parsing Tests (5 tests):**
- Create trigger (message code 1)
- Update trigger (message code 2)
- Delete trigger (message code 3)
- CreateOrUpdate trigger (message code 4)
- Trigger scope parsing (4 variations)

**Action Parsing Tests (2 tests):**
- Create action with attributes
- ListRecords action with filters

**Error Handling Tests (5 tests):**
- Null/empty JSON validation
- Invalid JSON format handling
- Missing trigger detection
- Unsupported trigger types
- Unsupported action types (graceful degradation)

**Integration Tests (1 test):**
- End-to-end JSON import to flow execution

**Scope Tests (4 tests):**
- Organization, BusinessUnit, ParentChildBusinessUnits, User scopes

## Supported Features

### Triggers
✅ Dataverse triggers (OpenApiConnectionWebhook with commondataserviceforapps)  
✅ All message types (Create, Update, Delete, CreateOrUpdate)  
✅ All trigger scopes (Organization, BusinessUnit, ParentChildBusinessUnits, User)  
✅ Filtered attributes for Update triggers  
✅ Entity logical names

### Actions
✅ Dataverse actions (OpenApiConnection with commondataserviceforapps)  
✅ CreateRecord with attributes  
✅ UpdateRecord with attributes and recordId  
✅ DeleteRecord with recordId  
✅ GetItem (Retrieve) with recordId  
✅ ListRecords with $filter, $orderby, $top  
✅ Action dependencies (runAfter)  
✅ Multiple actions in sequence

### Error Handling
✅ Validates JSON structure  
✅ Detects missing triggers  
✅ Provides clear error messages for unsupported features  
✅ Gracefully skips unsupported actions (with console warning)

## Limitations

### Expression Evaluation
❌ Expressions (e.g., `@triggerOutputs()`, `@concat()`) are stored but not evaluated  
- Expressions in action parameters are preserved as strings
- Expression evaluation is planned for a future phase
- Workaround: Use programmatic flow registration for flows with complex expressions

### Unsupported Trigger Types
❌ Manual triggers (Request)  
❌ HTTP triggers  
❌ Schedule/Recurrence triggers  
❌ Custom connector triggers (non-Dataverse)

These trigger types throw `NotSupportedException` with a clear message directing users to use programmatic registration.

### Unsupported Action Types
❌ Office 365 actions (Send Email, etc.)  
❌ SharePoint actions  
❌ Teams actions  
❌ HTTP actions  
❌ Condition actions  
❌ Apply to Each loops  
❌ Parallel branches

These action types are silently skipped during import. Users can register custom handlers via `RegisterConnectorActionHandler` for non-Dataverse actions.

### Advanced Features
❌ Condition evaluation  
❌ Loop execution  
❌ Parallel branch execution  
❌ Error handling/retry logic  
❌ Variables and composition

## Usage Example

```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Middleware;

// Setup
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true;
var flowSimulator = context.CloudFlowSimulator;

// Export your flow from Power Automate and get the JSON
var flowJson = File.ReadAllText("MyFlow.json");

// Import and register
flowSimulator.RegisterFlowFromJson(flowJson);

// Test it!
var service = context.GetOrganizationService();
service.Create(new Entity("contact") { ["firstname"] = "John" });

// Verify
flowSimulator.AssertFlowTriggered("my_flow_name");
var results = flowSimulator.GetFlowExecutionResults("my_flow_name");
Assert.Single(results);
Assert.True(results[0].Succeeded);
```

## How to Export a Flow from Power Automate

1. Open your Cloud Flow in Power Automate
2. Click the flow menu (three dots) → Export → Package (.zip)
3. Extract the zip file
4. Find the `definition.json` file inside
5. Use that JSON with `RegisterFlowFromJson`

## Documentation Updates

Updated the following documentation:

1. **`/docs/usage/cloud-flows.md`**
   - Added "Register a Flow from JSON" section with comprehensive examples
   - Listed supported JSON features
   - Listed limitations
   - Added instructions for exporting flows from Power Automate
   - Updated test coverage count (67 tests)

2. **`/docs/API_DESIGN_CLOUD_FLOWS.md`**
   - Marked Phase 4 as complete with checklist
   - Added JSON import usage example
   - Updated implementation status

## Technical Implementation Details

### JsonElement Handling

The parser includes helper methods to safely extract values from JsonElement objects:

- `GetIntValue()` - Extracts integers from JsonElement or direct values
- `GetStringValue()` - Extracts strings from JsonElement or direct values
- `GetJsonElementValue()` - Converts JsonElement to appropriate .NET type

This is necessary because System.Text.Json deserializes parameters as JsonElement objects that require special handling.

### Topological Sorting

Actions with dependencies (specified via `runAfter`) are sorted topologically to ensure correct execution order. Actions with no dependencies execute first, followed by their dependents.

### Error Recovery

The parser uses a graceful degradation approach:
- Unsupported actions are skipped with a warning
- The flow is still imported with the supported actions
- This allows partial testing of flows with mixed connector types

## Testing

All tests follow best practices:
- Reference Microsoft documentation in comments
- Test one concept per test
- Use descriptive test names
- Include both positive and negative test cases
- Cover edge cases and error conditions

**Test Execution Time:** ~200ms for all 67 CloudFlow tests

## Future Enhancements

Possible future enhancements (not in scope for this implementation):

1. **Expression Engine**
   - Evaluate `@triggerOutputs()`, `@outputs()`, `@concat()`, etc.
   - Support Power Fx expressions
   - Variable resolution

2. **Advanced Control Flow**
   - Condition actions (if/then/else)
   - Apply to Each loops
   - Parallel branches

3. **Additional Trigger Types**
   - Manual/HTTP triggers
   - Schedule triggers
   - Custom connector triggers

4. **Additional Action Types**
   - Office 365 connector actions
   - SharePoint connector actions
   - Teams connector actions
   - Generic HTTP actions

## Migration from NotImplementedException

The previous implementation threw `NotImplementedException` for `RegisterFlowFromJson`. This has been replaced with the full implementation, and the test `Should_ThrowNotImplementedException_ForJsonImport` was updated to `Should_ImportFlowFromJson_Successfully` to reflect the new functionality.

## References

- [Logic Apps Workflow Definition Language](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)
- [Logic Apps Schema Reference](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language-schema-reference)
- [Power Automate Cloud Flows Overview](https://learn.microsoft.com/en-us/power-automate/overview-cloud)
- [Dataverse Connector Reference](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)
- [Create/Update/Delete Triggers](https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger)
- [Trigger Conditions](https://learn.microsoft.com/en-us/power-automate/triggers-introduction)

## Conclusion

This implementation successfully adds first-class JSON import support to Fake4Dataverse, enabling developers to test real Cloud Flow definitions in unit tests. The implementation is well-tested, documented, and follows the existing patterns in the framework.

**Status:** ✅ Complete and Ready for Review
