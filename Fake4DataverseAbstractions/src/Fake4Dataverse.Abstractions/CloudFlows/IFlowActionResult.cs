using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents the result of a single action within a flow execution
    /// </summary>
    public interface IFlowActionResult
    {
        /// <summary>
        /// Gets the name of the action that was executed
        /// </summary>
        string ActionName { get; }

        /// <summary>
        /// Gets the type of action that was executed
        /// </summary>
        string ActionType { get; }

        /// <summary>
        /// Gets whether the action succeeded
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        /// Gets the outputs produced by this action.
        /// These outputs can be referenced by subsequent actions.
        /// </summary>
        IReadOnlyDictionary<string, object> Outputs { get; }

        /// <summary>
        /// Gets any error message if the action failed
        /// </summary>
        string ErrorMessage { get; }
    }
}
