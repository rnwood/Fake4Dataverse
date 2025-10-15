using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Simulates Cloud Flows (Power Automate flows) for testing purposes.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
    /// 
    /// Enables testing of:
    /// - Dataverse-triggered flows (Create, Update, Delete)
    /// - Dataverse connector actions within flows
    /// - Custom connector actions (via extensibility)
    /// - Flow execution verification and assertion
    /// </summary>
    public class CloudFlowSimulator : ICloudFlowSimulator
    {
        private readonly IXrmFakedContext _context;
        private readonly Dictionary<string, ICloudFlowDefinition> _flowsCache;  // In-memory cache for backwards compatibility
        private readonly Dictionary<string, IConnectorActionHandler> _connectorHandlers;
        private readonly Dictionary<string, List<IFlowExecutionResult>> _executionHistory;

        // Workflow table constants
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/workflow
        // The workflow entity (also known as process) stores process definitions including business rules, workflows, and cloud flows
        private const string WorkflowEntityName = "workflow";
        private const int CategoryModernFlow = 6; // Modern Flow/Cloud Flow category
        private const int StateCodeActivated = 1;
        private const int TypeDefinition = 1;

        public CloudFlowSimulator(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _flowsCache = new Dictionary<string, ICloudFlowDefinition>(StringComparer.OrdinalIgnoreCase);
            _connectorHandlers = new Dictionary<string, IConnectorActionHandler>(StringComparer.OrdinalIgnoreCase);
            _executionHistory = new Dictionary<string, List<IFlowExecutionResult>>(StringComparer.OrdinalIgnoreCase);

            // Register built-in action handlers
            RegisterConnectorActionHandler("Dataverse", new DataverseActionHandler());
            RegisterConnectorActionHandler("Compose", new ComposeActionHandler());

            // Load any existing flows from workflow table into cache
            // This allows flows to be pre-populated via CRUD operations
            LoadFlowsFromWorkflowTable();
        }

        /// <summary>
        /// Registers a Cloud Flow definition to be simulated.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/getting-started
        /// 
        /// Registered flows will automatically trigger when matching Dataverse operations occur
        /// in the fake context (Create, Update, Delete, etc.).
        /// 
        /// This method attempts to store the flow definition in the workflow table (process table),
        /// mirroring how real Dataverse stores cloud flows. Flows are also cached in memory for
        /// backwards compatibility and when the workflow entity is not available.
        /// </summary>
        public void RegisterFlow(ICloudFlowDefinition flowDefinition)
        {
            if (flowDefinition == null)
                throw new ArgumentNullException(nameof(flowDefinition));

            if (string.IsNullOrWhiteSpace(flowDefinition.Name))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowDefinition));

            if (flowDefinition.Trigger == null)
                throw new ArgumentException("Flow must have a trigger", nameof(flowDefinition));

            // Store in memory cache (always works, provides backwards compatibility)
            _flowsCache[flowDefinition.Name] = flowDefinition;

            // Try to store flow in workflow table (this mirrors real Dataverse behavior)
            // But handle gracefully if the entity doesn't exist
            try
            {
                StoreFlowInWorkflowTable(flowDefinition);
            }
            catch (Exception)
            {
                // If workflow table is not available or not properly initialized,
                // flow will still work from the in-memory cache
            }

            // Initialize execution history for this flow
            if (!_executionHistory.ContainsKey(flowDefinition.Name))
            {
                _executionHistory[flowDefinition.Name] = new List<IFlowExecutionResult>();
            }
        }

        /// <summary>
        /// Internal method to store flow in workflow table
        /// </summary>
        private void StoreFlowInWorkflowTable(ICloudFlowDefinition flowDefinition)
        {
            // Store flow in workflow table
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/workflow
            // Workflow entity stores process definitions including cloud flows (category=6)
            var service = _context.GetOrganizationService();
            
            // Check if flow already exists by name
            var query = new QueryExpression(WorkflowEntityName)
            {
                ColumnSet = new ColumnSet("workflowid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, flowDefinition.Name),
                        new ConditionExpression("category", ConditionOperator.Equal, CategoryModernFlow)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            
            var workflowEntity = new Entity(WorkflowEntityName);
            if (existing != null)
            {
                workflowEntity.Id = existing.Id;
            }
            else
            {
                workflowEntity.Id = Guid.NewGuid();
            }

            // Set workflow attributes
            workflowEntity["name"] = flowDefinition.DisplayName ?? flowDefinition.Name;
            workflowEntity["uniquename"] = flowDefinition.Name;
            workflowEntity["category"] = CategoryModernFlow; // Modern Flow/Cloud Flow
            workflowEntity["type"] = TypeDefinition; // Definition (not activation/instance)
            // Note: statecode and statuscode will be set after create/update
            
            // Get description from metadata if available
            string description = null;
            if (flowDefinition.Metadata != null && flowDefinition.Metadata.ContainsKey("Description"))
            {
                description = flowDefinition.Metadata["Description"]?.ToString();
            }
            workflowEntity["description"] = description;

            // Set trigger-specific attributes
            if (flowDefinition.Trigger is DataverseTrigger dataverseTrigger)
            {
                workflowEntity["primaryentity"] = dataverseTrigger.EntityLogicalName;
                
                // Set trigger flags based on message
                workflowEntity["triggeroncreate"] = dataverseTrigger.Message == "Create" || dataverseTrigger.Message == "CreateOrUpdate";
                workflowEntity["triggeronupdate"] = dataverseTrigger.Message == "Update" || dataverseTrigger.Message == "CreateOrUpdate";
                workflowEntity["triggerondelete"] = dataverseTrigger.Message == "Delete";
                
                // Store scope
                workflowEntity["scope"] = (int)dataverseTrigger.Scope;
            }

            // Serialize flow definition to clientdata field
            // Note: We store a simplified representation, not the exact Power Automate format
            // This is acceptable per the issue requirements: "it's acceptable for what we store in the detail field to not be exact"
            var flowData = new
            {
                flowDefinition.Name,
                flowDefinition.DisplayName,
                flowDefinition.IsEnabled,
                flowDefinition.Metadata,
                Trigger = SerializeTrigger(flowDefinition.Trigger),
                Actions = flowDefinition.Actions?.Select(SerializeAction).ToList()
            };
            workflowEntity["clientdata"] = JsonSerializer.Serialize(flowData);

            if (existing != null)
            {
                service.Update(workflowEntity);
            }
            else
            {
                service.Create(workflowEntity);
            }

            // Set state after creation (statecode cannot be set during Create in Fake4Dataverse)
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata#statecode-and-statuscode
            var stateEntity = new Entity(WorkflowEntityName)
            {
                Id = workflowEntity.Id,
                ["statecode"] = flowDefinition.IsEnabled ? StateCodeActivated : 0,
                ["statuscode"] = flowDefinition.IsEnabled ? 2 : 1
            };
            service.Update(stateEntity);
        }

        /// <summary>
        /// Registers a Cloud Flow from an exported JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
        /// 
        /// Allows importing real Cloud Flow definitions exported from Power Automate,
        /// enabling realistic simulation that matches production behavior.
        /// 
        /// Supported features:
        /// - Dataverse triggers (Create, Update, Delete, CreateOrUpdate)
        /// - Dataverse actions (Create, Update, Delete, Retrieve, ListRecords)
        /// - Trigger scopes (Organization, BusinessUnit, ParentChildBusinessUnits, User)
        /// - Filtered attributes for Update triggers
        /// 
        /// Limitations:
        /// - Expression evaluation is not yet supported (expressions are stored but not evaluated)
        /// - Non-Dataverse connectors require custom handlers via RegisterConnectorActionHandler
        /// - Advanced control flow (conditions, loops, parallel branches) not yet supported
        /// </summary>
        public void RegisterFlowFromJson(string flowJson)
        {
            if (string.IsNullOrWhiteSpace(flowJson))
                throw new ArgumentException("Flow JSON cannot be null or empty", nameof(flowJson));

            var parser = new JsonImport.CloudFlowJsonParser();
            var flowDefinition = parser.Parse(flowJson);
            
            RegisterFlow(flowDefinition);
        }

        /// <summary>
        /// Registers multiple Cloud Flows at once
        /// </summary>
        public void RegisterFlows(IEnumerable<ICloudFlowDefinition> flowDefinitions)
        {
            if (flowDefinitions == null)
                throw new ArgumentNullException(nameof(flowDefinitions));

            foreach (var flowDefinition in flowDefinitions)
            {
                RegisterFlow(flowDefinition);
            }
        }

        /// <summary>
        /// Unregisters a previously registered Cloud Flow
        /// </summary>
        public void UnregisterFlow(string flowName)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowName));

            // Remove from cache
            _flowsCache.Remove(flowName);

            // Try to delete from workflow table
            try
            {
                var service = _context.GetOrganizationService();
                var query = new QueryExpression(WorkflowEntityName)
                {
                    ColumnSet = new ColumnSet("workflowid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("uniquename", ConditionOperator.Equal, flowName),
                            new ConditionExpression("category", ConditionOperator.Equal, CategoryModernFlow)
                        }
                    }
                };

                var existing = service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (existing != null)
                {
                    service.Delete(WorkflowEntityName, existing.Id);
                }
            }
            catch (Exception)
            {
                // If workflow table is not available, silently continue
            }
        }

        /// <summary>
        /// Clears all registered Cloud Flows
        /// </summary>
        public void ClearAllFlows()
        {
            // Clear cache
            _flowsCache.Clear();

            // Try to delete all cloud flows from workflow table
            try
            {
                var service = _context.GetOrganizationService();
                var query = new QueryExpression(WorkflowEntityName)
                {
                    ColumnSet = new ColumnSet("workflowid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("category", ConditionOperator.Equal, CategoryModernFlow)
                        }
                    }
                };

                var flows = service.RetrieveMultiple(query).Entities;
                foreach (var flow in flows)
                {
                    service.Delete(WorkflowEntityName, flow.Id);
                }
            }
            catch (Exception)
            {
                // If workflow table is not available, silently continue
            }
        }

        /// <summary>
        /// Manually triggers a Cloud Flow with specific inputs.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
        /// 
        /// Useful for testing flow logic with controlled inputs without triggering
        /// an actual Dataverse operation.
        /// </summary>
        public IFlowExecutionResult SimulateTrigger(string flowName, Dictionary<string, object> triggerInputs)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowName));

            // Try to get flow from cache first
            ICloudFlowDefinition flowDefinition = null;
            if (_flowsCache.TryGetValue(flowName, out flowDefinition))
            {
                // Found in cache
            }
            else
            {
                // Try to load from workflow table
                flowDefinition = LoadFlowFromWorkflowTable(flowName);
            }

            if (flowDefinition == null)
                throw new InvalidOperationException($"Flow '{flowName}' is not registered");

            if (!flowDefinition.IsEnabled)
                throw new InvalidOperationException($"Flow '{flowName}' is not enabled");

            triggerInputs = triggerInputs ?? new Dictionary<string, object>();

            return ExecuteFlow(flowDefinition, triggerInputs);
        }

        /// <summary>
        /// Registers a connector action handler for non-Dataverse connectors.
        /// Reference: https://learn.microsoft.com/en-us/connectors/
        /// 
        /// Allows test writers to provide custom logic for handling connector actions
        /// (Office 365, SharePoint, Teams, custom APIs, etc.) in flows.
        /// </summary>
        public void RegisterConnectorActionHandler(string connectorType, IConnectorActionHandler handler)
        {
            if (string.IsNullOrWhiteSpace(connectorType))
                throw new ArgumentException("Connector type cannot be null or empty", nameof(connectorType));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _connectorHandlers[connectorType] = handler;
        }

        /// <summary>
        /// Gets the registered connector action handler for a specific connector type
        /// </summary>
        public IConnectorActionHandler GetConnectorHandler(string connectorType)
        {
            if (string.IsNullOrWhiteSpace(connectorType))
                return null;

            _connectorHandlers.TryGetValue(connectorType, out var handler);
            return handler;
        }

        /// <summary>
        /// Asserts that a flow was triggered at least once
        /// </summary>
        public void AssertFlowTriggered(string flowName)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowName));

            var count = GetFlowExecutionCount(flowName);
            if (count == 0)
                throw new InvalidOperationException($"Flow '{flowName}' was not triggered");
        }

        /// <summary>
        /// Asserts that a flow was NOT triggered
        /// </summary>
        public void AssertFlowNotTriggered(string flowName)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowName));

            var count = GetFlowExecutionCount(flowName);
            if (count > 0)
                throw new InvalidOperationException($"Flow '{flowName}' was triggered {count} time(s)");
        }

        /// <summary>
        /// Gets the number of times a flow was executed
        /// </summary>
        public int GetFlowExecutionCount(string flowName)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                return 0;

            if (_executionHistory.TryGetValue(flowName, out var executions))
            {
                return executions.Count;
            }

            return 0;
        }

        /// <summary>
        /// Gets all execution results for a specific flow
        /// </summary>
        public IReadOnlyList<IFlowExecutionResult> GetFlowExecutionResults(string flowName)
        {
            if (string.IsNullOrWhiteSpace(flowName))
                return new List<IFlowExecutionResult>();

            if (_executionHistory.TryGetValue(flowName, out var executions))
            {
                return executions.AsReadOnly();
            }

            return new List<IFlowExecutionResult>();
        }

        /// <summary>
        /// Gets all registered flow names
        /// </summary>
        public IReadOnlyList<string> GetRegisteredFlowNames()
        {
            // Return flow names from cache (which includes flows from both registration and workflow table)
            return _flowsCache.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Clears execution history for all flows
        /// </summary>
        public void ClearExecutionHistory()
        {
            foreach (var history in _executionHistory.Values)
            {
                history.Clear();
            }
        }

        /// <summary>
        /// Triggers all flows matching a Dataverse operation (Create, Update, Delete).
        /// This is called automatically by CRUD operations when pipeline simulation is enabled.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
        /// 
        /// Checks both the in-memory cache and the workflow table for matching flows.
        /// </summary>
        /// <param name="message">The Dataverse message (Create, Update, Delete)</param>
        /// <param name="entityLogicalName">The entity logical name</param>
        /// <param name="entity">The entity that triggered the flow</param>
        /// <param name="modifiedAttributes">Optional set of modified attributes (for Update triggers)</param>
        public void TriggerDataverseFlows(
            string message,
            string entityLogicalName,
            Entity entity,
            HashSet<string> modifiedAttributes = null)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(entityLogicalName) || entity == null)
                return;

            // Find matching flows from cache
            var matchingFlows = _flowsCache.Values
                .Where(flow => flow.IsEnabled && flow.Trigger is DataverseTrigger)
                .Select(flow => new { Flow = flow, Trigger = flow.Trigger as DataverseTrigger })
                .Where(x => 
                {
                    var trigger = x.Trigger;
                    
                    // Match entity name
                    if (!string.Equals(trigger.EntityLogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                        return false;

                    // Match message
                    if (!string.Equals(trigger.Message, message, StringComparison.OrdinalIgnoreCase) &&
                        !(trigger.Message == "CreateOrUpdate" && (message == "Create" || message == "Update")))
                        return false;

                    // For Update triggers, check filtered attributes if specified
                    if (message == "Update" && trigger.FilteredAttributes != null && trigger.FilteredAttributes.Any())
                    {
                        if (modifiedAttributes == null || !modifiedAttributes.Any())
                            return false;

                        // Trigger only if at least one filtered attribute was modified
                        if (!trigger.FilteredAttributes.Any(fa => modifiedAttributes.Contains(fa, StringComparer.OrdinalIgnoreCase)))
                            return false;
                    }

                    return true;
                })
                .ToList();

            // Execute each matching flow from cache
            foreach (var match in matchingFlows)
            {
                try
                {
                    // Build trigger inputs from entity
                    var triggerInputs = new Dictionary<string, object>
                    {
                        [entityLogicalName + "id"] = entity.Id.ToString(),
                        ["id"] = entity.Id.ToString()
                    };

                    // Add all entity attributes to trigger inputs
                    foreach (var attr in entity.Attributes)
                    {
                        triggerInputs[attr.Key] = attr.Value;
                    }

                    // Execute the flow
                    ExecuteFlow(match.Flow, triggerInputs);
                }
                catch (Exception)
                {
                    // Log error but don't fail the CRUD operation
                    // In real Dataverse, flow failures don't fail the triggering operation
                    // Flows run asynchronously
                }
            }
        }

        /// <summary>
        /// Internal method to execute a flow with the given trigger inputs
        /// </summary>
        private IFlowExecutionResult ExecuteFlow(
            ICloudFlowDefinition flowDefinition,
            Dictionary<string, object> triggerInputs)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            var result = new FlowExecutionResult(
                flowDefinition.Name,
                startTime,
                triggerInputs);

            var actionResults = new List<IFlowActionResult>();
            var errors = new List<string>();
            var succeeded = true;

            // Create execution context
            var executionContext = new FlowExecutionContext(triggerInputs);

            try
            {
                // Execute each action in sequence
                foreach (var action in flowDefinition.Actions ?? new List<IFlowAction>())
                {
                    var actionResult = ExecuteAction(action, executionContext);
                    actionResults.Add(actionResult);

                    if (!actionResult.Succeeded)
                    {
                        succeeded = false;
                        errors.Add($"Action '{actionResult.ActionName}' failed: {actionResult.ErrorMessage}");
                        break; // Stop on first error
                    }

                    // Add action outputs to context for next actions
                    if (actionResult.Outputs != null && !string.IsNullOrEmpty(actionResult.ActionName))
                    {
                        executionContext.AddActionOutputs(actionResult.ActionName, 
                            (IDictionary<string, object>)actionResult.Outputs);
                    }
                }
            }
            catch (Exception ex)
            {
                succeeded = false;
                errors.Add($"Flow execution failed: {ex.Message}");
            }

            stopwatch.Stop();

            // Update result
            result.Succeeded = succeeded;
            result.ActionResults = actionResults.AsReadOnly();
            result.Errors = errors.AsReadOnly();
            result.Duration = stopwatch.Elapsed;

            // Store in execution history
            if (!_executionHistory.ContainsKey(flowDefinition.Name))
            {
                _executionHistory[flowDefinition.Name] = new List<IFlowExecutionResult>();
            }
            _executionHistory[flowDefinition.Name].Add(result);

            return result;
        }

        /// <summary>
        /// Internal method to execute a single action
        /// </summary>
        private IFlowActionResult ExecuteAction(IFlowAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? action.ActionType,
                action.ActionType);

            try
            {
                // Special handling for control flow actions
                if (action is ApplyToEachAction applyToEachAction)
                {
                    return ExecuteApplyToEachAction(applyToEachAction, executionContext);
                }
                else if (action is ConditionAction conditionAction)
                {
                    return ExecuteConditionAction(conditionAction, executionContext);
                }
                else if (action is SwitchAction switchAction)
                {
                    return ExecuteSwitchAction(switchAction, executionContext);
                }
                else if (action is ParallelBranchAction parallelAction)
                {
                    return ExecuteParallelBranchAction(parallelAction, executionContext);
                }
                else if (action is DoUntilAction doUntilAction)
                {
                    return ExecuteDoUntilAction(doUntilAction, executionContext);
                }

                // Find appropriate handler
                IConnectorActionHandler handler = null;

                if (_connectorHandlers.TryGetValue(action.ActionType, out var registeredHandler))
                {
                    if (registeredHandler.CanHandle(action))
                    {
                        handler = registeredHandler;
                    }
                }

                if (handler == null)
                {
                    // No handler registered - this is an error
                    actionResult.Succeeded = false;
                    actionResult.ErrorMessage = $"No connector handler registered for action type '{action.ActionType}'";
                    return actionResult;
                }

                // Execute the action
                var outputs = handler.Execute(action, _context, executionContext);

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>(outputs ?? new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Execute an Apply to Each action with loop iteration
        /// Reference: https://learn.microsoft.com/en-us/power-automate/apply-to-each
        /// </summary>
        private IFlowActionResult ExecuteApplyToEachAction(ApplyToEachAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? "Apply_to_each",
                "ApplyToEach");

            try
            {
                // Evaluate the collection expression
                var evaluator = new Expressions.ExpressionEvaluator(executionContext);
                var collectionValue = evaluator.Evaluate(action.Collection);

                // Convert to enumerable
                System.Collections.IEnumerable collection = null;
                if (collectionValue is System.Collections.IEnumerable enumerable)
                {
                    collection = enumerable;
                }
                else if (collectionValue != null)
                {
                    // Wrap single value in array
                    collection = new[] { collectionValue };
                }

                if (collection == null)
                {
                    actionResult.Succeeded = false;
                    actionResult.ErrorMessage = "Collection evaluated to null";
                    return actionResult;
                }

                // Iterate over collection
                var itemResults = new List<Dictionary<string, object>>();
                var flowContext = executionContext as FlowExecutionContext;
                
                foreach (var item in collection)
                {
                    // Push item onto stack for @item() access
                    flowContext?.PushLoopItem(item);

                    try
                    {
                        // Execute actions for this item
                        var itemActionResults = new Dictionary<string, object>();
                        
                        foreach (var loopAction in action.Actions ?? new List<IFlowAction>())
                        {
                            var loopActionResult = ExecuteAction(loopAction, executionContext);
                            
                            if (!loopActionResult.Succeeded)
                            {
                                actionResult.Succeeded = false;
                                actionResult.ErrorMessage = $"Action '{loopActionResult.ActionName}' failed in loop: {loopActionResult.ErrorMessage}";
                                return actionResult;
                            }

                            // Store action outputs
                            if (loopActionResult.Outputs != null && !string.IsNullOrEmpty(loopActionResult.ActionName))
                            {
                                itemActionResults[loopActionResult.ActionName] = loopActionResult.Outputs;
                                if (flowContext != null)
                                {
                                    flowContext.AddActionOutputs(loopActionResult.ActionName, 
                                        (IDictionary<string, object>)loopActionResult.Outputs);
                                }
                            }
                        }

                        itemResults.Add(itemActionResults);
                    }
                    finally
                    {
                        // Pop item from stack
                        flowContext?.PopLoopItem();
                    }
                }

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>
                {
                    ["itemResults"] = itemResults
                };
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Execute a Condition action with if/then/else branching
        /// Reference: https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions
        /// 
        /// The Condition action evaluates an expression and executes either TrueActions or FalseActions.
        /// This enables conditional logic and branching within flows.
        /// </summary>
        private IFlowActionResult ExecuteConditionAction(ConditionAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? "Condition",
                "Condition");

            try
            {
                // Evaluate the condition expression
                var evaluator = new Expressions.ExpressionEvaluator(executionContext);
                var conditionResult = evaluator.Evaluate(action.Expression);

                // Convert to boolean
                bool isTrue = false;
                if (conditionResult is bool boolValue)
                {
                    isTrue = boolValue;
                }
                else if (conditionResult != null)
                {
                    // Try to parse as boolean
                    isTrue = Convert.ToBoolean(conditionResult);
                }

                // Select which branch to execute
                var actionsToExecute = isTrue ? action.TrueActions : action.FalseActions;

                // Execute the selected branch
                var branchResults = new List<Dictionary<string, object>>();
                foreach (var branchAction in actionsToExecute ?? new List<IFlowAction>())
                {
                    var branchActionResult = ExecuteAction(branchAction, executionContext);

                    if (!branchActionResult.Succeeded)
                    {
                        actionResult.Succeeded = false;
                        actionResult.ErrorMessage = $"Action '{branchActionResult.ActionName}' failed in condition branch: {branchActionResult.ErrorMessage}";
                        return actionResult;
                    }

                    // Store action outputs
                    if (branchActionResult.Outputs != null && !string.IsNullOrEmpty(branchActionResult.ActionName))
                    {
                        branchResults.Add(new Dictionary<string, object>
                        {
                            ["actionName"] = branchActionResult.ActionName,
                            ["outputs"] = branchActionResult.Outputs
                        });

                        var flowContext = executionContext as FlowExecutionContext;
                        if (flowContext != null)
                        {
                            flowContext.AddActionOutputs(branchActionResult.ActionName,
                                (IDictionary<string, object>)branchActionResult.Outputs);
                        }
                    }
                }

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>
                {
                    ["conditionResult"] = isTrue,
                    ["branchExecuted"] = isTrue ? "true" : "false",
                    ["branchResults"] = branchResults
                };
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Execute a Switch action with multi-case branching
        /// Reference: https://learn.microsoft.com/en-us/power-automate/use-switch-action
        /// 
        /// The Switch action evaluates an expression and executes the matching case's actions.
        /// If no case matches, the default actions are executed.
        /// </summary>
        private IFlowActionResult ExecuteSwitchAction(SwitchAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? "Switch",
                "Switch");

            try
            {
                // Evaluate the switch expression
                var evaluator = new Expressions.ExpressionEvaluator(executionContext);
                var switchValue = evaluator.Evaluate(action.Expression);

                // Convert to string for case matching
                var switchValueStr = switchValue?.ToString() ?? string.Empty;

                // Find matching case
                IList<IFlowAction> actionsToExecute = null;
                string matchedCase = "default";

                if (action.Cases != null)
                {
                    foreach (var caseEntry in action.Cases)
                    {
                        if (string.Equals(caseEntry.Key, switchValueStr, StringComparison.OrdinalIgnoreCase))
                        {
                            actionsToExecute = caseEntry.Value;
                            matchedCase = caseEntry.Key;
                            break;
                        }
                    }
                }

                // If no case matched, use default
                if (actionsToExecute == null)
                {
                    actionsToExecute = action.DefaultActions ?? new List<IFlowAction>();
                }

                // Execute the matched case actions
                var caseResults = new List<Dictionary<string, object>>();
                foreach (var caseAction in actionsToExecute)
                {
                    var caseActionResult = ExecuteAction(caseAction, executionContext);

                    if (!caseActionResult.Succeeded)
                    {
                        actionResult.Succeeded = false;
                        actionResult.ErrorMessage = $"Action '{caseActionResult.ActionName}' failed in switch case '{matchedCase}': {caseActionResult.ErrorMessage}";
                        return actionResult;
                    }

                    // Store action outputs
                    if (caseActionResult.Outputs != null && !string.IsNullOrEmpty(caseActionResult.ActionName))
                    {
                        caseResults.Add(new Dictionary<string, object>
                        {
                            ["actionName"] = caseActionResult.ActionName,
                            ["outputs"] = caseActionResult.Outputs
                        });

                        var flowContext = executionContext as FlowExecutionContext;
                        if (flowContext != null)
                        {
                            flowContext.AddActionOutputs(caseActionResult.ActionName,
                                (IDictionary<string, object>)caseActionResult.Outputs);
                        }
                    }
                }

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>
                {
                    ["switchValue"] = switchValueStr,
                    ["matchedCase"] = matchedCase,
                    ["caseResults"] = caseResults
                };
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Execute a Parallel Branch action
        /// Reference: https://learn.microsoft.com/en-us/power-automate/use-parallel-branches
        /// 
        /// In simulation, branches execute sequentially but are logically independent.
        /// All branches must complete successfully for the action to succeed.
        /// </summary>
        private IFlowActionResult ExecuteParallelBranchAction(ParallelBranchAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? "Parallel",
                "ParallelBranch");

            try
            {
                var branchResults = new List<Dictionary<string, object>>();

                // Execute each branch (sequentially in simulation, but logically parallel)
                foreach (var branch in action.Branches ?? new List<ParallelBranch>())
                {
                    var branchResult = new Dictionary<string, object>
                    {
                        ["branchName"] = branch.Name ?? "Unnamed",
                        ["actions"] = new List<Dictionary<string, object>>()
                    };

                    var actionList = (List<Dictionary<string, object>>)branchResult["actions"];

                    // Execute actions in this branch
                    foreach (var branchAction in branch.Actions ?? new List<IFlowAction>())
                    {
                        var branchActionResult = ExecuteAction(branchAction, executionContext);

                        if (!branchActionResult.Succeeded)
                        {
                            actionResult.Succeeded = false;
                            actionResult.ErrorMessage = $"Action '{branchActionResult.ActionName}' failed in parallel branch '{branch.Name}': {branchActionResult.ErrorMessage}";
                            return actionResult;
                        }

                        // Store action outputs
                        if (branchActionResult.Outputs != null && !string.IsNullOrEmpty(branchActionResult.ActionName))
                        {
                            actionList.Add(new Dictionary<string, object>
                            {
                                ["actionName"] = branchActionResult.ActionName,
                                ["outputs"] = branchActionResult.Outputs
                            });

                            var flowContext = executionContext as FlowExecutionContext;
                            if (flowContext != null)
                            {
                                flowContext.AddActionOutputs(branchActionResult.ActionName,
                                    (IDictionary<string, object>)branchActionResult.Outputs);
                            }
                        }
                    }

                    branchResults.Add(branchResult);
                }

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>
                {
                    ["branchResults"] = branchResults
                };
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Execute a Do Until loop action
        /// Reference: https://learn.microsoft.com/en-us/power-automate/do-until-loop
        /// 
        /// The Do Until action repeatedly executes actions until a condition becomes true.
        /// The condition is checked AFTER each iteration (do-while loop).
        /// The loop has a maximum iteration limit to prevent infinite loops.
        /// </summary>
        private IFlowActionResult ExecuteDoUntilAction(DoUntilAction action, IFlowExecutionContext executionContext)
        {
            var actionResult = new FlowActionResult(
                action.Name ?? "Do_until",
                "DoUntil");

            try
            {
                var evaluator = new Expressions.ExpressionEvaluator(executionContext);
                var iterationResults = new List<Dictionary<string, object>>();
                int iteration = 0;
                bool conditionMet = false;

                // Execute loop - check condition AFTER each iteration
                do
                {
                    iteration++;

                    // Check max iterations
                    if (iteration > action.MaxIterations)
                    {
                        actionResult.Succeeded = false;
                        actionResult.ErrorMessage = $"Do Until loop exceeded maximum iterations ({action.MaxIterations})";
                        return actionResult;
                    }

                    var iterationResult = new Dictionary<string, object>
                    {
                        ["iteration"] = iteration,
                        ["actions"] = new List<Dictionary<string, object>>()
                    };

                    var actionList = (List<Dictionary<string, object>>)iterationResult["actions"];

                    // Execute actions in this iteration
                    foreach (var loopAction in action.Actions ?? new List<IFlowAction>())
                    {
                        var loopActionResult = ExecuteAction(loopAction, executionContext);

                        if (!loopActionResult.Succeeded)
                        {
                            actionResult.Succeeded = false;
                            actionResult.ErrorMessage = $"Action '{loopActionResult.ActionName}' failed in Do Until iteration {iteration}: {loopActionResult.ErrorMessage}";
                            return actionResult;
                        }

                        // Store action outputs
                        if (loopActionResult.Outputs != null && !string.IsNullOrEmpty(loopActionResult.ActionName))
                        {
                            actionList.Add(new Dictionary<string, object>
                            {
                                ["actionName"] = loopActionResult.ActionName,
                                ["outputs"] = loopActionResult.Outputs
                            });

                            var flowContext = executionContext as FlowExecutionContext;
                            if (flowContext != null)
                            {
                                flowContext.AddActionOutputs(loopActionResult.ActionName,
                                    (IDictionary<string, object>)loopActionResult.Outputs);
                            }
                        }
                    }

                    iterationResults.Add(iterationResult);

                    // Evaluate condition AFTER iteration
                    var conditionValue = evaluator.Evaluate(action.Expression);

                    // Convert to boolean
                    if (conditionValue is bool boolValue)
                    {
                        conditionMet = boolValue;
                    }
                    else if (conditionValue != null)
                    {
                        conditionMet = Convert.ToBoolean(conditionValue);
                    }

                } while (!conditionMet);

                actionResult.Succeeded = true;
                actionResult.Outputs = new Dictionary<string, object>
                {
                    ["iterations"] = iteration,
                    ["iterationResults"] = iterationResults,
                    ["conditionMet"] = true
                };
            }
            catch (Exception ex)
            {
                actionResult.Succeeded = false;
                actionResult.ErrorMessage = ex.Message;
            }

            return actionResult;
        }

        /// <summary>
        /// Helper method to serialize a trigger for storage in clientdata field
        /// </summary>
        private object SerializeTrigger(IFlowTrigger trigger)
        {
            if (trigger is DataverseTrigger dataverseTrigger)
            {
                return new
                {
                    Type = "Dataverse",
                    dataverseTrigger.EntityLogicalName,
                    dataverseTrigger.Message,
                    dataverseTrigger.Scope,
                    dataverseTrigger.FilteredAttributes
                };
            }
            return new { Type = trigger?.GetType().Name ?? "Unknown" };
        }

        /// <summary>
        /// Helper method to serialize an action for storage in clientdata field
        /// </summary>
        private object SerializeAction(IFlowAction action)
        {
            if (action is DataverseAction dataverseAction)
            {
                return new
                {
                    action.ActionType,
                    action.Name,
                    dataverseAction.DataverseActionType,
                    dataverseAction.EntityLogicalName,
                    dataverseAction.EntityId,
                    dataverseAction.Attributes,
                    dataverseAction.Parameters
                };
            }
            else if (action is ApplyToEachAction applyToEach)
            {
                return new
                {
                    action.ActionType,
                    action.Name,
                    applyToEach.Collection,
                    Actions = applyToEach.Actions?.Select(SerializeAction).ToList()
                };
            }
            else if (action is ConditionAction condition)
            {
                return new
                {
                    action.ActionType,
                    action.Name,
                    condition.Expression,
                    TrueActions = condition.TrueActions?.Select(SerializeAction).ToList(),
                    FalseActions = condition.FalseActions?.Select(SerializeAction).ToList()
                };
            }
            else if (action is ComposeAction compose)
            {
                return new
                {
                    action.ActionType,
                    action.Name,
                    compose.Inputs
                };
            }
            return new { action.ActionType, action.Name };
        }

        /// <summary>
        /// Loads a flow definition from the workflow table
        /// </summary>
        private ICloudFlowDefinition LoadFlowFromWorkflowTable(string flowName)
        {
            var service = _context.GetOrganizationService();
            var query = new QueryExpression(WorkflowEntityName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, flowName),
                        new ConditionExpression("category", ConditionOperator.Equal, CategoryModernFlow)
                    }
                }
            };

            var workflowEntity = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (workflowEntity == null)
                return null;

            return DeserializeFlowFromWorkflowEntity(workflowEntity);
        }

        /// <summary>
        /// Deserializes a flow definition from a workflow entity
        /// </summary>
        private ICloudFlowDefinition DeserializeFlowFromWorkflowEntity(Entity workflowEntity)
        {
            try
            {
                var clientData = workflowEntity.GetAttributeValue<string>("clientdata");
                if (string.IsNullOrWhiteSpace(clientData))
                {
                    // If no clientdata, create a minimal flow definition from workflow attributes
                    return CreateMinimalFlowDefinitionFromWorkflowEntity(workflowEntity);
                }

                // Deserialize from JSON
                using (var doc = JsonDocument.Parse(clientData))
                {
                    var root = doc.RootElement;
                    
                    var flowDefinition = new CloudFlowDefinition
                    {
                        Name = root.GetProperty("Name").GetString(),
                        DisplayName = root.TryGetProperty("DisplayName", out var displayName) ? displayName.GetString() : null,
                        IsEnabled = root.TryGetProperty("IsEnabled", out var isEnabled) && isEnabled.GetBoolean()
                    };

                    // Deserialize metadata
                    if (root.TryGetProperty("Metadata", out var metadataElement) && metadataElement.ValueKind == JsonValueKind.Object)
                    {
                        flowDefinition.Metadata = new Dictionary<string, object>();
                        foreach (var prop in metadataElement.EnumerateObject())
                        {
                            flowDefinition.Metadata[prop.Name] = GetJsonValue(prop.Value);
                        }
                    };

                    // Deserialize trigger
                    if (root.TryGetProperty("Trigger", out var triggerElement))
                    {
                        flowDefinition.Trigger = DeserializeTrigger(triggerElement);
                    }

                    // Deserialize actions
                    if (root.TryGetProperty("Actions", out var actionsElement) && actionsElement.ValueKind == JsonValueKind.Array)
                    {
                        flowDefinition.Actions = new List<IFlowAction>();
                        foreach (var actionElement in actionsElement.EnumerateArray())
                        {
                            var action = DeserializeAction(actionElement);
                            if (action != null)
                            {
                                flowDefinition.Actions.Add(action);
                            }
                        }
                    }

                    return flowDefinition;
                }
            }
            catch
            {
                // If deserialization fails, return null
                return null;
            }
        }

        /// <summary>
        /// Creates a minimal flow definition when clientdata is not available
        /// </summary>
        private ICloudFlowDefinition CreateMinimalFlowDefinitionFromWorkflowEntity(Entity workflowEntity)
        {
            var flowDefinition = new CloudFlowDefinition
            {
                Name = workflowEntity.GetAttributeValue<string>("uniquename"),
                DisplayName = workflowEntity.GetAttributeValue<string>("name"),
                IsEnabled = workflowEntity.GetAttributeValue<int>("statecode") == StateCodeActivated,
                Metadata = new Dictionary<string, object>()
            };

            // Add description to metadata if available
            var description = workflowEntity.GetAttributeValue<string>("description");
            if (!string.IsNullOrEmpty(description))
            {
                flowDefinition.Metadata["Description"] = description;
            }

            // Create minimal trigger from workflow attributes
            var primaryEntity = workflowEntity.GetAttributeValue<string>("primaryentity");
            var triggerOnCreate = workflowEntity.GetAttributeValue<bool>("triggeroncreate");
            var triggerOnUpdate = workflowEntity.GetAttributeValue<bool>("triggeronupdate");
            var triggerOnDelete = workflowEntity.GetAttributeValue<bool>("triggerondelete");

            string message = "Create"; // Default
            if (triggerOnCreate && triggerOnUpdate)
                message = "CreateOrUpdate";
            else if (triggerOnUpdate)
                message = "Update";
            else if (triggerOnDelete)
                message = "Delete";

            flowDefinition.Trigger = new DataverseTrigger
            {
                EntityLogicalName = primaryEntity,
                Message = message,
                Scope = (TriggerScope)(workflowEntity.Contains("scope") ? workflowEntity.GetAttributeValue<int>("scope") : 0)
            };

            flowDefinition.Actions = new List<IFlowAction>();

            return flowDefinition;
        }

        /// <summary>
        /// Deserializes a trigger from JSON
        /// </summary>
        private IFlowTrigger DeserializeTrigger(JsonElement triggerElement)
        {
            var type = triggerElement.TryGetProperty("Type", out var typeElement) ? typeElement.GetString() : null;
            
            if (type == "Dataverse")
            {
                var trigger = new DataverseTrigger
                {
                    EntityLogicalName = triggerElement.TryGetProperty("EntityLogicalName", out var entityName) ? entityName.GetString() : null,
                    Message = triggerElement.TryGetProperty("Message", out var message) ? message.GetString() : "Create",
                    Scope = triggerElement.TryGetProperty("Scope", out var scope) ? (TriggerScope)scope.GetInt32() : TriggerScope.Organization
                };

                if (triggerElement.TryGetProperty("FilteredAttributes", out var filteredAttrs) && filteredAttrs.ValueKind == JsonValueKind.Array)
                {
                    trigger.FilteredAttributes = filteredAttrs.EnumerateArray().Select(e => e.GetString()).ToList();
                }

                return trigger;
            }

            return null;
        }

        /// <summary>
        /// Deserializes an action from JSON
        /// </summary>
        private IFlowAction DeserializeAction(JsonElement actionElement)
        {
            var actionType = actionElement.TryGetProperty("ActionType", out var typeElement) ? typeElement.GetString() : null;
            var name = actionElement.TryGetProperty("Name", out var nameElement) ? nameElement.GetString() : null;

            if (actionType == "Dataverse")
            {
                var action = new DataverseAction
                {
                    Name = name,
                    EntityLogicalName = actionElement.TryGetProperty("EntityLogicalName", out var entityName) ? entityName.GetString() : null
                };

                if (actionElement.TryGetProperty("DataverseActionType", out var dataverseActionType))
                {
                    action.DataverseActionType = (DataverseActionType)dataverseActionType.GetInt32();
                }

                if (actionElement.TryGetProperty("EntityId", out var entityId) && entityId.ValueKind == JsonValueKind.String)
                {
                    if (Guid.TryParse(entityId.GetString(), out var guid))
                    {
                        action.EntityId = guid;
                    }
                }

                if (actionElement.TryGetProperty("Attributes", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
                {
                    action.Attributes = new Dictionary<string, object>();
                    foreach (var prop in attributes.EnumerateObject())
                    {
                        action.Attributes[prop.Name] = GetJsonValue(prop.Value);
                    }
                }

                if (actionElement.TryGetProperty("Parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
                {
                    action.Parameters = new Dictionary<string, object>();
                    foreach (var prop in parameters.EnumerateObject())
                    {
                        action.Parameters[prop.Name] = GetJsonValue(prop.Value);
                    }
                }

                return action;
            }
            else if (actionType == "Compose")
            {
                var action = new ComposeAction
                {
                    Name = name
                };

                if (actionElement.TryGetProperty("Inputs", out var inputs))
                {
                    action.Inputs = GetJsonValue(inputs);
                }

                return action;
            }
            // Add more action types as needed

            return null;
        }

        /// <summary>
        /// Extracts a value from a JsonElement
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = GetJsonValue(prop.Value);
                    }
                    return dict;
                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(GetJsonValue).ToList();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Loads flows from the workflow table during initialization.
        /// This allows flows to be pre-populated via CRUD operations on the workflow table.
        /// Loaded flows are added to the in-memory cache.
        /// </summary>
        private void LoadFlowsFromWorkflowTable()
        {
            try
            {
                var service = _context.GetOrganizationService();
                var query = new QueryExpression(WorkflowEntityName)
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("category", ConditionOperator.Equal, CategoryModernFlow)
                        }
                    }
                };

                var workflowEntities = service.RetrieveMultiple(query).Entities;
                
                // Load each flow into cache
                foreach (var workflowEntity in workflowEntities)
                {
                    var flowDefinition = DeserializeFlowFromWorkflowEntity(workflowEntity);
                    if (flowDefinition != null && !string.IsNullOrEmpty(flowDefinition.Name))
                    {
                        _flowsCache[flowDefinition.Name] = flowDefinition;
                        
                        // Initialize execution history
                        if (!_executionHistory.ContainsKey(flowDefinition.Name))
                        {
                            _executionHistory[flowDefinition.Name] = new List<IFlowExecutionResult>();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If workflow table is not available, that's okay - flows can still be registered manually
            }
        }
    }
}
