using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Cloud Flow definition.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
    /// 
    /// A Cloud Flow contains:
    /// - A trigger that starts the flow (Dataverse event, schedule, manual, HTTP request, etc.)
    /// - One or more actions that execute when the flow is triggered
    /// - Optional configuration for error handling, retry logic, etc.
    /// </summary>
    public interface ICloudFlowDefinition
    {
        /// <summary>
        /// Gets or sets the unique name of the flow.
        /// This is used as the identifier for registration and verification.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the flow.
        /// This is a human-readable name shown in Power Automate.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the trigger that starts this flow.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
        /// </summary>
        IFlowTrigger Trigger { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute when the flow is triggered.
        /// Actions execute sequentially unless parallel branches are specified.
        /// </summary>
        IList<IFlowAction> Actions { get; set; }

        /// <summary>
        /// Gets or sets whether the flow is enabled.
        /// Disabled flows will not trigger automatically.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets optional metadata for the flow.
        /// Can be used to store additional information like description, tags, etc.
        /// </summary>
        IDictionary<string, object> Metadata { get; set; }
    }
}
