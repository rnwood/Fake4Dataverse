namespace Fake4Dataverse.Abstractions.Enums
{
    /// <summary>
    /// Represents the status reason of an asynchronous operation (asyncoperation entity).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation
    /// 
    /// StatusCode values indicate the detailed state of the async operation.
    /// Different StatusCode values correspond to different StateCode values.
    /// </summary>
    public enum AsyncOperationStatus
    {
        /// <summary>
        /// Waiting for resources (Ready state).
        /// StatusCode = 0, StateCode = Ready (0)
        /// </summary>
        WaitingForResources = 0,

        /// <summary>
        /// Waiting (Ready state).
        /// StatusCode = 10, StateCode = Ready (0)
        /// </summary>
        Waiting = 10,

        /// <summary>
        /// In progress (Ready state).
        /// StatusCode = 20, StateCode = Ready (0)
        /// </summary>
        InProgress = 20,

        /// <summary>
        /// Pausing (Ready state).
        /// StatusCode = 21, StateCode = Ready (0)
        /// </summary>
        Pausing = 21,

        /// <summary>
        /// Canceling (Ready state).
        /// StatusCode = 22, StateCode = Ready (0)
        /// </summary>
        Canceling = 22,

        /// <summary>
        /// Succeeded (Completed state).
        /// StatusCode = 30, StateCode = Completed (3)
        /// </summary>
        Succeeded = 30,

        /// <summary>
        /// Failed (Completed state).
        /// StatusCode = 31, StateCode = Completed (3)
        /// </summary>
        Failed = 31,

        /// <summary>
        /// Canceled (Completed state).
        /// StatusCode = 32, StateCode = Completed (3)
        /// </summary>
        Canceled = 32
    }
}
