using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Do Until loop action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/do-until-loop
    /// 
    /// Do Until actions repeatedly execute a set of actions until a condition becomes true.
    /// The loop includes safeguards:
    /// - MaxIterations: Maximum number of loop iterations (default 60, max 5000 in Power Automate)
    /// - Timeout: Maximum duration for the loop (default PT1H - 1 hour in Power Automate)
    /// 
    /// Common use cases:
    /// - Poll for record status changes
    /// - Wait for approval or external process completion
    /// - Retry operations until success
    /// - Process records in batches until complete
    /// 
    /// Note: The condition is checked AFTER each iteration (do-while loop).
    /// </summary>
    public class DoUntilAction : IFlowAction
    {
        public DoUntilAction()
        {
            ActionType = "DoUntil";
            Parameters = new Dictionary<string, object>();
            Actions = new List<IFlowAction>();
            MaxIterations = 60; // Default limit in Power Automate
        }

        /// <summary>
        /// Gets or sets the action type. Always "DoUntil" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the condition expression to evaluate.
        /// The loop continues while this expression is false, and stops when it becomes true.
        /// Example: @equals(outputs('Get_Status')['status'], 'Completed')
        /// </summary>
        public object Expression { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute in each loop iteration.
        /// </summary>
        public IList<IFlowAction> Actions { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of iterations.
        /// Default: 60 (matches Power Automate default)
        /// Maximum in Power Automate: 5000
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// Gets or sets the timeout duration in ISO 8601 format.
        /// Example: "PT1H" for 1 hour (Power Automate default)
        /// This is primarily for documentation; simulation doesn't enforce real timeouts.
        /// </summary>
        public string Timeout { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
