using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Provides context for flow execution, including trigger inputs and action outputs.
    /// Used by connector action handlers to access data from earlier steps in the flow.
    /// </summary>
    public interface IFlowExecutionContext
    {
        /// <summary>
        /// Gets the trigger inputs for this flow execution
        /// </summary>
        IReadOnlyDictionary<string, object> TriggerInputs { get; }

        /// <summary>
        /// Gets the outputs from a previously executed action by name.
        /// This allows actions to reference outputs from earlier actions
        /// using expressions like @outputs('ActionName').
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>The action outputs, or null if the action hasn't executed</returns>
        IReadOnlyDictionary<string, object> GetActionOutputs(string actionName);

        /// <summary>
        /// Gets all action outputs indexed by action name
        /// </summary>
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> AllActionOutputs { get; }

        /// <summary>
        /// Sets a flow variable value.
        /// Flow variables are used to store state during flow execution.
        /// </summary>
        /// <param name="variableName">The name of the variable</param>
        /// <param name="value">The value to store</param>
        void SetVariable(string variableName, object value);

        /// <summary>
        /// Gets a flow variable value.
        /// Returns null if the variable doesn't exist.
        /// </summary>
        /// <param name="variableName">The name of the variable</param>
        /// <returns>The variable value, or null if not found</returns>
        object GetVariable(string variableName);
    }
}
