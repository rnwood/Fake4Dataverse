using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Switch action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-switch-action
    /// 
    /// Switch actions evaluate an expression and execute different branches based on matching cases.
    /// Each case compares the expression result against a specific value.
    /// A default case is executed if no cases match.
    /// 
    /// Common use cases:
    /// - Multi-way branching based on status codes
    /// - Different processing for different record types
    /// - Routing based on categorical values
    /// - Priority-based workflow routing
    /// </summary>
    public class SwitchAction : IFlowAction
    {
        public SwitchAction()
        {
            ActionType = "Switch";
            Parameters = new Dictionary<string, object>();
            Cases = new Dictionary<string, IList<IFlowAction>>();
            DefaultActions = new List<IFlowAction>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "Switch" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the expression to evaluate for comparison.
        /// The result of this expression is compared against each case value.
        /// Example: @triggerBody()['status'] or @outputs('Get_Record')['statecode']
        /// </summary>
        public object Expression { get; set; }

        /// <summary>
        /// Gets or sets the switch cases.
        /// Key: The case value to match (as a string)
        /// Value: The list of actions to execute if this case matches
        /// </summary>
        public IDictionary<string, IList<IFlowAction>> Cases { get; set; }

        /// <summary>
        /// Gets or sets the default actions to execute when no case matches.
        /// </summary>
        public IList<IFlowAction> DefaultActions { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
