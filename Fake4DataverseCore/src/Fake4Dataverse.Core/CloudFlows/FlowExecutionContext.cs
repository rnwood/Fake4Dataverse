using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions.CloudFlows;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Provides context for flow execution, including trigger inputs and action outputs.
    /// Used by connector action handlers to access data from earlier steps in the flow.
    /// </summary>
    public class FlowExecutionContext : IFlowExecutionContext
    {
        private readonly Dictionary<string, Dictionary<string, object>> _actionOutputs;
        private readonly Dictionary<string, object> _variables;

        public FlowExecutionContext(IReadOnlyDictionary<string, object> triggerInputs)
        {
            TriggerInputs = triggerInputs ?? new Dictionary<string, object>();
            _actionOutputs = new Dictionary<string, Dictionary<string, object>>();
            _variables = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the trigger inputs for this flow execution
        /// </summary>
        public IReadOnlyDictionary<string, object> TriggerInputs { get; }

        /// <summary>
        /// Gets the outputs from a previously executed action by name.
        /// This allows actions to reference outputs from earlier actions
        /// using expressions like @outputs('ActionName').
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>The action outputs, or null if the action hasn't executed</returns>
        public IReadOnlyDictionary<string, object> GetActionOutputs(string actionName)
        {
            if (_actionOutputs.TryGetValue(actionName, out var outputs))
            {
                return outputs;
            }
            return null;
        }

        /// <summary>
        /// Gets all action outputs indexed by action name
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> AllActionOutputs
        {
            get
            {
                return _actionOutputs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyDictionary<string, object>)kvp.Value);
            }
        }

        /// <summary>
        /// Internal method to add action outputs to the context
        /// </summary>
        internal void AddActionOutputs(string actionName, IDictionary<string, object> outputs)
        {
            _actionOutputs[actionName] = new Dictionary<string, object>(outputs);
        }

        /// <summary>
        /// Sets a flow variable value
        /// </summary>
        public void SetVariable(string variableName, object value)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                throw new ArgumentException("Variable name cannot be null or empty", nameof(variableName));

            _variables[variableName] = value;
        }

        /// <summary>
        /// Gets a flow variable value
        /// </summary>
        public object GetVariable(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                return null;

            return _variables.TryGetValue(variableName, out var value) ? value : null;
        }

        /// <summary>
        /// Gets all variable names
        /// </summary>
        public IReadOnlyCollection<string> GetVariableNames()
        {
            return _variables.Keys.ToList().AsReadOnly();
        }
    }
}
