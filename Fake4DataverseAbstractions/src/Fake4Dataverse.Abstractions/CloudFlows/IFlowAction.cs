using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Base interface for Cloud Flow actions.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/desktop-flows/actions-reference
    /// 
    /// Actions define what the flow does when triggered. Common action types:
    /// - Dataverse actions (CRUD operations, custom actions)
    /// - Connector actions (Office 365, SharePoint, Teams, etc.)
    /// - Control actions (conditions, loops, parallel branches)
    /// - Data operations (compose, parse JSON, select, filter)
    /// </summary>
    public interface IFlowAction
    {
        /// <summary>
        /// Gets or sets the action type identifier.
        /// Examples: "Dataverse", "Office365", "Condition", "Compose"
        /// </summary>
        string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the name of this action.
        /// Used for referencing action outputs in subsequent actions (e.g., @outputs('ActionName')).
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the action parameters.
        /// The structure depends on the action type.
        /// </summary>
        IDictionary<string, object> Parameters { get; set; }
    }
}
