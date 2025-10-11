using System.Collections.Generic;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Dataverse trigger for Cloud Flows.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
    /// 
    /// Dataverse triggers fire when records are created, updated, or deleted in Dataverse.
    /// They are the most common trigger type for Dataverse-centric flows.
    /// </summary>
    public class DataverseTrigger : IFlowTrigger
    {
        public DataverseTrigger()
        {
            TriggerType = "Dataverse";
            Name = "When a record is created, updated or deleted";
            Scope = TriggerScope.Organization;
            FilteredAttributes = new List<string>();
        }

        /// <summary>
        /// Gets or sets the trigger type. Always "Dataverse" for this trigger.
        /// </summary>
        public string TriggerType { get; set; }

        /// <summary>
        /// Gets or sets the trigger name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the entity that triggers this flow.
        /// Example: "account", "contact", "opportunity"
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the message that triggers this flow.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations
        /// 
        /// Common values: "Create", "Update", "Delete", "CreateOrUpdate"
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the scope of records that can trigger this flow.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
        /// </summary>
        public TriggerScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the filtered attributes for Update triggers.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger#trigger-conditions
        /// 
        /// When specified, the flow only triggers if one of these attributes was modified.
        /// Only applicable for Update message.
        /// </summary>
        public IList<string> FilteredAttributes { get; set; }

        /// <summary>
        /// Gets or sets an optional condition expression that must be true for the trigger to fire.
        /// Example: "@greater(triggerBody()?['estimatedvalue'], 100000)"
        /// </summary>
        public string Condition { get; set; }
    }
}
