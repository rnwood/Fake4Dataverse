namespace Fake4Dataverse.Abstractions.CloudFlows.Enums
{
    /// <summary>
    /// Defines the scope of a Dataverse trigger in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
    /// 
    /// The scope determines which records can trigger the flow:
    /// - Organization: All records in the organization (requires appropriate privileges)
    /// - BusinessUnit: Records owned by users in the same business unit as the flow owner
    /// - ParentChildBusinessUnits: Records in the business unit hierarchy
    /// - User: Only records owned by the flow owner
    /// </summary>
    public enum TriggerScope
    {
        /// <summary>
        /// Organization scope - All records in the organization can trigger the flow.
        /// This is the most common scope for system-level automation.
        /// </summary>
        Organization = 1,

        /// <summary>
        /// Business unit scope - Only records owned by users in the same business unit
        /// as the flow owner can trigger the flow.
        /// </summary>
        BusinessUnit = 2,

        /// <summary>
        /// Parent and child business units scope - Records in the business unit hierarchy
        /// can trigger the flow.
        /// </summary>
        ParentChildBusinessUnits = 3,

        /// <summary>
        /// User scope - Only records owned by the flow owner can trigger the flow.
        /// This is the most restrictive scope.
        /// </summary>
        User = 4
    }
}
