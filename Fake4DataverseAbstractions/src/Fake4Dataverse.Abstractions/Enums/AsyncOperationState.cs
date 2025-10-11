namespace Fake4Dataverse.Abstractions.Enums
{
    /// <summary>
    /// Represents the state of an asynchronous operation (asyncoperation entity).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation
    /// 
    /// The asyncoperation entity tracks asynchronous system jobs including:
    /// - Asynchronous plugin executions
    /// - Workflow jobs
    /// - Bulk delete operations
    /// - Duplicate detection jobs
    /// </summary>
    public enum AsyncOperationState
    {
        /// <summary>
        /// The system job is ready to execute (Ready).
        /// StateCode = 0
        /// </summary>
        Ready = 0,

        /// <summary>
        /// The system job has been suspended (Suspended).
        /// StateCode = 1
        /// </summary>
        Suspended = 1,

        /// <summary>
        /// The system job is locked for execution (Locked).
        /// StateCode = 2
        /// </summary>
        Locked = 2,

        /// <summary>
        /// The system job has completed (Completed).
        /// StateCode = 3
        /// </summary>
        Completed = 3
    }
}
