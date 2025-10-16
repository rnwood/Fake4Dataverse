using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Cloud Flow definition.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
    /// </summary>
    public class CloudFlowDefinition : ICloudFlowDefinition
    {
        public CloudFlowDefinition()
        {
            IsEnabled = true;
            Actions = new List<IFlowAction>();
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the unique name of the flow
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the flow
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the trigger that starts this flow
        /// </summary>
        public IFlowTrigger Trigger { get; set; }

        /// <summary>
        /// Gets or sets the actions to execute when the flow is triggered
        /// </summary>
        public IList<IFlowAction> Actions { get; set; }

        /// <summary>
        /// Gets or sets whether the flow is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets optional metadata for the flow
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; }
    }
}
