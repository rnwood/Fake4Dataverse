using Microsoft.Xrm.Sdk;
using System;
using Fake4Dataverse.Abstractions.Enums;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Represents a queued asynchronous operation (mirrors the asyncoperation entity in Dataverse).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation
    /// 
    /// The asyncoperation entity in Dataverse tracks all asynchronous system jobs including:
    /// - Asynchronous plugin executions
    /// - Workflow jobs
    /// - Bulk operations
    /// - Scheduled jobs
    /// 
    /// This class simulates the asyncoperation entity for testing purposes, allowing developers to:
    /// - Queue async plugins for later execution
    /// - Monitor the status of async operations
    /// - Retrieve execution results and errors
    /// - Test async plugin behavior without a live Dataverse instance
    /// </summary>
    public class AsyncOperation
    {
        /// <summary>
        /// Unique identifier for the async operation (asyncoperationid).
        /// </summary>
        public Guid AsyncOperationId { get; set; }

        /// <summary>
        /// Name of the async operation.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of operation being executed.
        /// For async plugins, this is typically ExecutePlugin (211).
        /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation#operationtype
        /// </summary>
        public AsyncOperationType OperationType { get; set; }

        /// <summary>
        /// State of the async operation (Ready, Suspended, Locked, Completed).
        /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation#statecode
        /// </summary>
        public AsyncOperationState StateCode { get; set; }

        /// <summary>
        /// Status reason of the async operation (WaitingForResources, Waiting, InProgress, Succeeded, Failed, Canceled).
        /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation#statuscode
        /// </summary>
        public AsyncOperationStatus StatusCode { get; set; }

        /// <summary>
        /// SDK message name that triggered the async operation (e.g., "Create", "Update").
        /// </summary>
        public string MessageName { get; set; }

        /// <summary>
        /// Primary entity name involved in the operation.
        /// </summary>
        public string PrimaryEntityName { get; set; }

        /// <summary>
        /// The plugin step registration that triggered this async operation.
        /// </summary>
        public PluginStepRegistration PluginStepRegistration { get; set; }

        /// <summary>
        /// The target entity for the operation.
        /// </summary>
        public Entity TargetEntity { get; set; }

        /// <summary>
        /// Pre-operation entity images.
        /// </summary>
        public EntityImageCollection PreEntityImages { get; set; }

        /// <summary>
        /// Post-operation entity images.
        /// </summary>
        public EntityImageCollection PostEntityImages { get; set; }

        /// <summary>
        /// User ID that initiated the operation (ownerid).
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Correlation ID for tracking related operations.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Date and time when the operation was created (createdon).
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Date and time when the operation started (startedon).
        /// </summary>
        public DateTime? StartedOn { get; set; }

        /// <summary>
        /// Date and time when the operation completed (completedon).
        /// </summary>
        public DateTime? CompletedOn { get; set; }

        /// <summary>
        /// Error message if the operation failed (message).
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception thrown during execution, if any.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Execution depth when this operation was queued.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Number of retry attempts (retrycount).
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Friendly message describing the operation (friendlymessage).
        /// </summary>
        public string FriendlyMessage { get; set; }

        /// <summary>
        /// Additional data needed for execution (stored as JSON or serialized string).
        /// This can include input parameters, shared variables, etc.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets whether the async operation has completed (regardless of success/failure).
        /// </summary>
        public bool IsCompleted => StateCode == AsyncOperationState.Completed;

        /// <summary>
        /// Gets whether the async operation completed successfully.
        /// </summary>
        public bool IsSuccessful => StateCode == AsyncOperationState.Completed && StatusCode == AsyncOperationStatus.Succeeded;

        /// <summary>
        /// Gets whether the async operation failed.
        /// </summary>
        public bool IsFailed => StateCode == AsyncOperationState.Completed && StatusCode == AsyncOperationStatus.Failed;

        /// <summary>
        /// Gets whether the async operation was canceled.
        /// </summary>
        public bool IsCanceled => StateCode == AsyncOperationState.Completed && StatusCode == AsyncOperationStatus.Canceled;

        /// <summary>
        /// Creates a new AsyncOperation for a plugin execution.
        /// </summary>
        public static AsyncOperation CreateForPlugin(
            PluginStepRegistration stepRegistration,
            string messageName,
            string entityLogicalName,
            Entity targetEntity,
            EntityImageCollection preImages,
            EntityImageCollection postImages,
            Guid userId,
            Guid organizationId,
            Guid correlationId,
            int depth)
        {
            return new AsyncOperation
            {
                AsyncOperationId = Guid.NewGuid(),
                Name = $"Plugin: {stepRegistration.PluginType.FullName}",
                OperationType = AsyncOperationType.ExecutePlugin,
                StateCode = AsyncOperationState.Ready,
                StatusCode = AsyncOperationStatus.WaitingForResources,
                MessageName = messageName,
                PrimaryEntityName = entityLogicalName,
                PluginStepRegistration = stepRegistration,
                TargetEntity = targetEntity,
                PreEntityImages = preImages,
                PostEntityImages = postImages,
                OwnerId = userId,
                OrganizationId = organizationId,
                CorrelationId = correlationId,
                CreatedOn = DateTime.UtcNow,
                Depth = depth,
                RetryCount = 0,
                FriendlyMessage = $"Async plugin execution: {messageName} on {entityLogicalName}"
            };
        }

        /// <summary>
        /// Converts this AsyncOperation to an Entity (asyncoperation entity).
        /// </summary>
        public Entity ToEntity()
        {
            var entity = new Entity("asyncoperation")
            {
                Id = AsyncOperationId
            };

            entity["asyncoperationid"] = AsyncOperationId;
            entity["name"] = Name;
            entity["operationtype"] = new OptionSetValue((int)OperationType);
            entity["statecode"] = new OptionSetValue((int)StateCode);
            entity["statuscode"] = new OptionSetValue((int)StatusCode);
            entity["messagename"] = MessageName;
            entity["primaryentitytype"] = PrimaryEntityName;
            entity["ownerid"] = new EntityReference("systemuser", OwnerId);
            entity["regardingobjectid"] = TargetEntity != null ? TargetEntity.ToEntityReference() : null;
            entity["correlationid"] = CorrelationId;
            entity["createdon"] = CreatedOn;
            entity["depth"] = Depth;
            entity["retrycount"] = RetryCount;
            entity["friendlymessage"] = FriendlyMessage;

            if (StartedOn.HasValue)
                entity["startedon"] = StartedOn.Value;

            if (CompletedOn.HasValue)
                entity["completedon"] = CompletedOn.Value;

            if (!string.IsNullOrEmpty(ErrorMessage))
                entity["message"] = ErrorMessage;

            if (!string.IsNullOrEmpty(Data))
                entity["data"] = Data;

            return entity;
        }
    }
}
