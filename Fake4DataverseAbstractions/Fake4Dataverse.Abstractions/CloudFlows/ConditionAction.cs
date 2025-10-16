using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Condition (if/then/else) action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions
    /// 
    /// Condition actions evaluate an expression and execute different branches based on the result.
    /// If the condition evaluates to true, actions in TrueActions are executed.
    /// If the condition evaluates to false, actions in FalseActions are executed.
    /// 
    /// Common use cases:
    /// - Check field values before performing actions
    /// - Branch workflow based on business logic
    /// - Validate data and handle different scenarios
    /// - Conditional approval workflows
    /// </summary>
    public class ConditionAction : IFlowAction
    {
        public ConditionAction()
        {
            ActionType = "Condition";
            Parameters = new Dictionary<string, object>();
            TrueActions = new List<IFlowAction>();
            FalseActions = new List<IFlowAction>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "Condition" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the condition expression to evaluate.
        /// This should be a boolean expression like @equals(triggerBody()['status'], 'active')
        /// or @greater(triggerBody()['amount'], 1000)
        /// </summary>
        public object Expression { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute when the condition is true (if branch).
        /// </summary>
        public IList<IFlowAction> TrueActions { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute when the condition is false (else branch).
        /// </summary>
        public IList<IFlowAction> FalseActions { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
