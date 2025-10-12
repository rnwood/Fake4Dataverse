using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Microsoft.Xrm.Sdk;

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
        private readonly Dictionary<string, ICloudFlowDefinition> _flows;
        private readonly Dictionary<string, IConnectorActionHandler> _connectorHandlers;
        private readonly Dictionary<string, List<IFlowExecutionResult>> _executionHistory;

        public CloudFlowSimulator(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _flows = new Dictionary<string, ICloudFlowDefinition>(StringComparer.OrdinalIgnoreCase);
            _connectorHandlers = new Dictionary<string, IConnectorActionHandler>(StringComparer.OrdinalIgnoreCase);
            _executionHistory = new Dictionary<string, List<IFlowExecutionResult>>(StringComparer.OrdinalIgnoreCase);

            // Register built-in Dataverse action handler
            RegisterConnectorActionHandler("Dataverse", new DataverseActionHandler());
        }

        /// <summary>
        /// Registers a Cloud Flow definition to be simulated.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/getting-started
        /// 
        /// Registered flows will automatically trigger when matching Dataverse operations occur
        /// in the fake context (Create, Update, Delete, etc.).
        /// </summary>
        public void RegisterFlow(ICloudFlowDefinition flowDefinition)
        {
            if (flowDefinition == null)
                throw new ArgumentNullException(nameof(flowDefinition));

            if (string.IsNullOrWhiteSpace(flowDefinition.Name))
                throw new ArgumentException("Flow name cannot be null or empty", nameof(flowDefinition));

            if (flowDefinition.Trigger == null)
                throw new ArgumentException("Flow must have a trigger", nameof(flowDefinition));

            _flows[flowDefinition.Name] = flowDefinition;

            // Initialize execution history for this flow
            if (!_executionHistory.ContainsKey(flowDefinition.Name))
            {
                _executionHistory[flowDefinition.Name] = new List<IFlowExecutionResult>();
            }
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

            _flows.Remove(flowName);
        }

        /// <summary>
        /// Clears all registered Cloud Flows
        /// </summary>
        public void ClearAllFlows()
        {
            _flows.Clear();
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

            if (!_flows.TryGetValue(flowName, out var flowDefinition))
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
            return _flows.Keys.ToList().AsReadOnly();
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

            // Find all flows with matching Dataverse triggers
            var matchingFlows = _flows.Values
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

            // Execute each matching flow
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
    }
}
