using System.Collections.Generic;
using Fake4Dataverse.Abstractions.CloudFlows;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Represents the result of a single action within a flow execution
    /// </summary>
    public class FlowActionResult : IFlowActionResult
    {
        public FlowActionResult(string actionName, string actionType)
        {
            ActionName = actionName;
            ActionType = actionType;
            Outputs = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the name of the action that was executed
        /// </summary>
        public string ActionName { get; }

        /// <summary>
        /// Gets the type of action that was executed
        /// </summary>
        public string ActionType { get; }

        /// <summary>
        /// Gets whether the action succeeded
        /// </summary>
        public bool Succeeded { get; internal set; }

        /// <summary>
        /// Gets the outputs produced by this action.
        /// These outputs can be referenced by subsequent actions.
        /// </summary>
        public IReadOnlyDictionary<string, object> Outputs { get; internal set; }

        /// <summary>
        /// Gets any error message if the action failed
        /// </summary>
        public string ErrorMessage { get; internal set; }
    }
}
