using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Compose action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/data-operations#use-the-compose-action
    /// 
    /// Compose actions create data transformations or compose new objects from expressions.
    /// They are commonly used to:
    /// - Transform data from previous steps
    /// - Create structured objects or arrays
    /// - Perform calculations or string manipulations
    /// - Format data before passing to subsequent actions
    /// 
    /// The output of a Compose action can be referenced in later actions using @outputs('ActionName').
    /// </summary>
    public class ComposeAction : IFlowAction
    {
        public ComposeAction()
        {
            ActionType = "Compose";
            Parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "Compose" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the inputs to compose.
        /// This can be any value: a simple value, an expression, an object, or an array.
        /// Expressions in the inputs will be evaluated at runtime.
        /// </summary>
        public object Inputs { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
