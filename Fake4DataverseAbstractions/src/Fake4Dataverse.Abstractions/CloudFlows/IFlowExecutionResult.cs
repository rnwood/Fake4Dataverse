using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents the result of a Cloud Flow execution.
    /// Contains information about the trigger inputs, action results, and execution status.
    /// </summary>
    public interface IFlowExecutionResult
    {
        /// <summary>
        /// Gets the name of the flow that was executed
        /// </summary>
        string FlowName { get; }

        /// <summary>
        /// Gets the UTC timestamp when the flow was triggered
        /// </summary>
        DateTime TriggeredAt { get; }

        /// <summary>
        /// Gets whether the flow execution succeeded.
        /// False if any action failed or threw an exception.
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        /// Gets the trigger inputs that started this flow execution
        /// </summary>
        IReadOnlyDictionary<string, object> TriggerInputs { get; }

        /// <summary>
        /// Gets the results from each action in the flow
        /// </summary>
        IReadOnlyList<IFlowActionResult> ActionResults { get; }

        /// <summary>
        /// Gets any errors that occurred during flow execution
        /// </summary>
        IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets the total execution duration
        /// </summary>
        TimeSpan Duration { get; }
    }
}
