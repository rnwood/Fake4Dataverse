# Cloud Flow API Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Test Code (User)                                     │
│                                                                              │
│  var context = XrmFakedContextFactory.New();                                │
│  var flowSimulator = context.CloudFlowSimulator;                            │
│                                                                              │
│  // Register flow                                                           │
│  flowSimulator.RegisterFlow(flowDefinition);                                │
│  flowSimulator.RegisterConnectorActionHandler("Office365", emailHandler);   │
│                                                                              │
│  // Execute operation (triggers flow automatically)                         │
│  service.Create(contact);                                                   │
│                                                                              │
│  // Verify                                                                  │
│  flowSimulator.AssertFlowTriggered("my_flow");                              │
└────────────────────────────┬────────────────────────────────────────────────┘
                             │
                             │ Uses
                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ICloudFlowSimulator                                       │
│  ┌──────────────────────────────────────────────────────────────┐          │
│  │ + RegisterFlow(ICloudFlowDefinition)                          │          │
│  │ + RegisterFlowFromJson(string)                                │          │
│  │ + SimulateTrigger(string, Dictionary<string, object>)         │          │
│  │ + RegisterConnectorActionHandler(string, IConnectorHandler)   │          │
│  │ + AssertFlowTriggered(string)                                 │          │
│  │ + GetFlowExecutionResults(string)                             │          │
│  └──────────────────────────────────────────────────────────────┘          │
└────────────────────┬───────────────────────────┬────────────────────────────┘
                     │                           │
                     │ Manages                   │ Triggers
                     ▼                           ▼
      ┌─────────────────────────┐    ┌─────────────────────────┐
      │  ICloudFlowDefinition   │    │  Flow Execution Engine   │
      │  ┌───────────────────┐  │    │  ┌───────────────────┐  │
      │  │ Name              │  │    │  │ 1. Match trigger  │  │
      │  │ Trigger           │  │    │  │ 2. Execute actions│  │
      │  │ Actions           │  │    │  │ 3. Track results  │  │
      │  │ IsEnabled         │  │    │  └───────────────────┘  │
      │  └───────────────────┘  │    └────────┬────────────────┘
      └────────┬────────────────┘             │
               │                              │
               │ Contains                     │ Executes
               ▼                              ▼
    ┌──────────────────────┐       ┌──────────────────────┐
    │   IFlowTrigger       │       │    IFlowAction       │
    │  ┌────────────────┐  │       │  ┌────────────────┐  │
    │  │ TriggerType    │  │       │  │ ActionType     │  │
    │  │ Name           │  │       │  │ Name           │  │
    │  └────────────────┘  │       │  │ Parameters     │  │
    │         △            │       │  └────────────────┘  │
    │         │            │       │         △            │
    │    Implemented by    │       │    Implemented by    │
    │         │            │       │         │            │
    │  ┌──────────────┐   │       │  ┌──────────────┐   │
    │  │DataverseTrigger│  │       │  │DataverseAction│  │
    │  ├──────────────┤   │       │  ├──────────────┤   │
    │  │Entity        │   │       │  │ActionType    │   │
    │  │Message       │   │       │  │Entity        │   │
    │  │Scope         │   │       │  │Attributes    │   │
    │  │FilteredAttrs │   │       │  │Filter        │   │
    │  └──────────────┘   │       │  └──────────────┘   │
    └──────────────────────┘       └──────┬───────────────┘
                                          │
                                          │ Executed by
                                          ▼
                            ┌───────────────────────────────┐
                            │  IConnectorActionHandler      │
                            │  ┌─────────────────────────┐  │
                            │  │ ConnectorType           │  │
                            │  │ CanHandle(action)       │  │
                            │  │ Execute(action, ctx)    │  │
                            │  └─────────────────────────┘  │
                            │           △                   │
                            │           │                   │
                            │    Implementations:           │
                            │           │                   │
                            │  ┌────────┴────────┐         │
                            │  │                 │         │
                            │  ▼                 ▼         │
                            │ Built-in        Custom       │
                            │ Dataverse       Handlers     │
                            │ Handler         (Tests)      │
                            └───────────────────────────────┘
                                          │
                                          │ Returns
                                          ▼
                            ┌───────────────────────────────┐
                            │  IFlowExecutionResult         │
                            │  ┌─────────────────────────┐  │
                            │  │ FlowName                │  │
                            │  │ TriggeredAt             │  │
                            │  │ Succeeded               │  │
                            │  │ TriggerInputs           │  │
                            │  │ ActionResults           │  │
                            │  │   └─ IFlowActionResult  │  │
                            │  │      ├─ ActionName      │  │
                            │  │      ├─ Succeeded       │  │
                            │  │      └─ Outputs         │  │
                            │  │ Errors                  │  │
                            │  │ Duration                │  │
                            │  └─────────────────────────┘  │
                            └───────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                    Integration with XrmFakedContext                          │
│                                                                              │
│  CRUD Operations ───────┐                                                   │
│  (CreateExecutor, etc)  │                                                   │
│                         │                                                   │
│                         ├─► Execute Operation                               │
│                         │                                                   │
│                         ├─► Run Plugin Pipeline (if enabled)                │
│                         │                                                   │
│                         └─► Trigger Cloud Flows ◄── CloudFlowSimulator     │
│                              (if matching trigger)                           │
└─────────────────────────────────────────────────────────────────────────────┘


Key Design Patterns:
══════════════════════════════════════════════════════════════════════════════

1. Strategy Pattern
   - IConnectorActionHandler allows pluggable connector behavior
   - Test writers provide custom handlers for external systems

2. Builder Pattern
   - CloudFlowDefinition, DataverseTrigger, DataverseAction
   - Fluent configuration of flows

3. Observer Pattern (implicit)
   - CloudFlowSimulator observes CRUD operations
   - Automatically triggers matching flows

4. Repository Pattern
   - CloudFlowSimulator stores and retrieves flow definitions
   - Maintains execution history

5. Factory Pattern (integration)
   - XrmFakedContextFactory creates context with CloudFlowSimulator


Extensibility Points:
══════════════════════════════════════════════════════════════════════════════

1. Custom Triggers (Future)
   - Implement IFlowTrigger
   - Add ScheduleTrigger, HttpTrigger, ManualTrigger

2. Custom Actions (Future)
   - Implement IFlowAction
   - Add ConditionAction, ApplyToEachAction, ComposeAction

3. Custom Connector Handlers
   - Implement IConnectorActionHandler
   - Mock Office365, SharePoint, Teams, custom APIs

4. Expression Engine (Future)
   - Evaluate Power Fx expressions
   - Support @triggerBody(), @outputs(), @concat(), etc.


Data Flow Example:
══════════════════════════════════════════════════════════════════════════════

User Test Code:
  service.Create(contact)
       │
       ▼
  CreateRequestExecutor
       │
       ├─► Execute Create in fake DB
       │
       ├─► Run PostOperation plugins (if UsePipelineSimulation)
       │
       └─► CloudFlowSimulator.TriggerDataverseFlows("Create", "contact", entity)
                │
                ├─► Find matching flows (trigger.Entity == "contact" && trigger.Message == "Create")
                │
                ├─► For each matching flow:
                │    │
                │    ├─► Evaluate trigger condition (if specified)
                │    │
                │    ├─► Execute each action:
                │    │    │
                │    │    ├─► If Dataverse action: Use DataverseActionHandler
                │    │    │                        ├─► Create/Update/Delete in fake DB
                │    │    │                        └─► Return outputs
                │    │    │
                │    │    └─► If custom connector: Find registered handler
                │    │                             ├─► Call handler.Execute()
                │    │                             └─► Return outputs
                │    │
                │    └─► Build FlowExecutionResult
                │         ├─► Trigger inputs
                │         ├─► Action results
                │         └─► Success/errors
                │
                └─► Store results for verification
                     │
                     └─► Available via:
                          - AssertFlowTriggered()
                          - GetFlowExecutionResults()
```

## Example: Office 365 Email Handler

```csharp
public class Office365EmailHandler : IConnectorActionHandler
{
    public string ConnectorType => "Office365";
    public List<EmailCapture> SentEmails = new();

    public bool CanHandle(IFlowAction action)
    {
        return action.ActionType == "Office365" && 
               action.Parameters["ActionName"].ToString() == "SendEmailV2";
    }

    public IDictionary<string, object> Execute(
        IFlowAction action, 
        IXrmFakedContext context, 
        IFlowExecutionContext flowContext)
    {
        // Extract parameters (can reference trigger or previous action outputs)
        var to = action.Parameters["To"].ToString();
        var subject = action.Parameters["Subject"].ToString();
        
        // Mock the email send
        SentEmails.Add(new EmailCapture { To = to, Subject = subject });
        
        // Return outputs (can be referenced by subsequent actions)
        return new Dictionary<string, object>
        {
            ["StatusCode"] = 200,
            ["MessageId"] = Guid.NewGuid().ToString(),
            ["SentAt"] = DateTime.UtcNow
        };
    }
}

// Usage in test:
flowSimulator.RegisterConnectorActionHandler("Office365", new Office365EmailHandler());
```

## Trigger Matching Logic

```
Operation: service.Create(contact)
           ├─ Entity: "contact"
           ├─ Message: "Create"
           └─ Modified attributes: { "firstname", "lastname", "emailaddress1" }

Flow 1: Create Contact Flow
    Trigger:
        ├─ Entity: "contact" ✓ Match
        ├─ Message: "Create" ✓ Match
        ├─ Scope: Organization ✓ Match (no filtering)
        └─ FilteredAttrs: null ✓ Match (not Update message)
    Result: TRIGGERED

Flow 2: Update Contact Email Flow
    Trigger:
        ├─ Entity: "contact" ✓ Match
        ├─ Message: "Update" ✗ No Match
        └─ ...
    Result: NOT TRIGGERED

Flow 3: Create Account Flow
    Trigger:
        ├─ Entity: "account" ✗ No Match
        └─ ...
    Result: NOT TRIGGERED
```
