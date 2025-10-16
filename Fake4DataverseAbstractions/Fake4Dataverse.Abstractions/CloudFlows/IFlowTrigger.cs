namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Base interface for Cloud Flow triggers.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
    /// 
    /// Triggers define when a flow should execute. Common trigger types:
    /// - Dataverse triggers (when a record is created, updated, deleted)
    /// - Schedule triggers (run on a schedule)
    /// - Manual triggers (started manually or by another flow)
    /// - HTTP request triggers (webhook-style triggers)
    /// </summary>
    public interface IFlowTrigger
    {
        /// <summary>
        /// Gets or sets the trigger type identifier.
        /// Examples: "Dataverse", "Recurrence", "Manual", "HTTP"
        /// </summary>
        string TriggerType { get; set; }

        /// <summary>
        /// Gets or sets the name of this trigger.
        /// Used for referencing trigger outputs in actions (e.g., @triggerBody()).
        /// </summary>
        string Name { get; set; }
    }
}
