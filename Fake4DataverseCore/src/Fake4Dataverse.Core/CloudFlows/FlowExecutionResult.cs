using System;
using System.Collections.Generic;
using Fake4Dataverse.Abstractions.CloudFlows;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Represents the result of a Cloud Flow execution.
    /// Contains information about the trigger inputs, action results, and execution status.
    /// </summary>
    public class FlowExecutionResult : IFlowExecutionResult
    {
        public FlowExecutionResult(
            string flowName,
            DateTime triggeredAt,
            IReadOnlyDictionary<string, object> triggerInputs)
        {
            FlowName = flowName ?? throw new ArgumentNullException(nameof(flowName));
            TriggeredAt = triggeredAt;
            TriggerInputs = triggerInputs ?? new Dictionary<string, object>();
            ActionResults = new List<IFlowActionResult>();
            Errors = new List<string>();
        }

        /// <summary>
        /// Gets the name of the flow that was executed
        /// </summary>
        public string FlowName { get; }

        /// <summary>
        /// Gets the UTC timestamp when the flow was triggered
        /// </summary>
        public DateTime TriggeredAt { get; }

        /// <summary>
        /// Gets whether the flow execution succeeded.
        /// False if any action failed or threw an exception.
        /// </summary>
        public bool Succeeded { get; internal set; }

        /// <summary>
        /// Gets the trigger inputs that started this flow execution
        /// </summary>
        public IReadOnlyDictionary<string, object> TriggerInputs { get; }

        /// <summary>
        /// Gets the results from each action in the flow
        /// </summary>
        public IReadOnlyList<IFlowActionResult> ActionResults { get; internal set; }

        /// <summary>
        /// Gets any errors that occurred during flow execution
        /// </summary>
        public IReadOnlyList<string> Errors { get; internal set; }

        /// <summary>
        /// Gets the total execution duration
        /// </summary>
        public TimeSpan Duration { get; internal set; }
    }
}
