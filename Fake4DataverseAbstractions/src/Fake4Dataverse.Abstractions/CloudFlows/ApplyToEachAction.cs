using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents an Apply to Each (loop) action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/apply-to-each
    /// 
    /// Apply to Each actions iterate over a collection and execute a set of actions for each item.
    /// The current item can be accessed using the @item() expression within the loop.
    /// 
    /// Common use cases:
    /// - Process each record from a list query
    /// - Send emails to multiple recipients
    /// - Create or update multiple records
    /// - Transform each item in an array
    /// </summary>
    public class ApplyToEachAction : IFlowAction
    {
        public ApplyToEachAction()
        {
            ActionType = "ApplyToEach";
            Parameters = new Dictionary<string, object>();
            Actions = new List<IFlowAction>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "ApplyToEach" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection to iterate over.
        /// This is typically an expression like @outputs('List_Records')['value']
        /// or @triggerBody()['items']
        /// </summary>
        public object Collection { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute for each item in the collection.
        /// Within these actions, @item() references the current item.
        /// </summary>
        public IList<IFlowAction> Actions { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
