namespace Fake4Dataverse.Abstractions.CloudFlows.Enums
{
    /// <summary>
    /// Defines the type of Dataverse action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
    /// 
    /// These action types correspond to operations available in the Dataverse connector.
    /// </summary>
    public enum DataverseActionType
    {
        /// <summary>
        /// Create a new record
        /// </summary>
        Create,

        /// <summary>
        /// Retrieve a single record by ID
        /// </summary>
        Retrieve,

        /// <summary>
        /// Update an existing record
        /// </summary>
        Update,

        /// <summary>
        /// Delete a record
        /// </summary>
        Delete,

        /// <summary>
        /// List records matching filter criteria
        /// </summary>
        ListRecords,

        /// <summary>
        /// Relate two records (Associate)
        /// </summary>
        Relate,

        /// <summary>
        /// Unrelate two records (Disassociate)
        /// </summary>
        Unrelate,

        /// <summary>
        /// Execute a custom action or API
        /// </summary>
        ExecuteAction,

        /// <summary>
        /// Perform an unbound action (global custom action/API)
        /// </summary>
        PerformUnboundAction,

        /// <summary>
        /// Upload a file or image column
        /// </summary>
        UploadFile,

        /// <summary>
        /// Download a file or image column
        /// </summary>
        DownloadFile
    }
}
