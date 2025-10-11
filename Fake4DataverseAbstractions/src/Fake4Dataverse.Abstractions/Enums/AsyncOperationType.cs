namespace Fake4Dataverse.Abstractions.Enums
{
    /// <summary>
    /// Represents the type of asynchronous operation.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// Reference: https://learn.microsoft.com/en-us/dynamics365/customerengagement/on-premises/developer/entities/asyncoperation
    /// 
    /// OperationType determines what kind of asynchronous job is being executed.
    /// </summary>
    public enum AsyncOperationType
    {
        /// <summary>
        /// System event (not used in standard scenarios).
        /// OperationType = 1
        /// </summary>
        SystemEvent = 1,

        /// <summary>
        /// Bulk email (mass email campaigns).
        /// OperationType = 2
        /// </summary>
        BulkEmail = 2,

        /// <summary>
        /// Import file parse (data import parsing).
        /// OperationType = 3
        /// </summary>
        ImportFileParseAsync = 3,

        /// <summary>
        /// Transform parse data (data transformation).
        /// OperationType = 4
        /// </summary>
        TransformParseData = 4,

        /// <summary>
        /// Import (data import).
        /// OperationType = 5
        /// </summary>
        Import = 5,

        /// <summary>
        /// Activity propagation (activity relationship updates).
        /// OperationType = 6
        /// </summary>
        ActivityPropagation = 6,

        /// <summary>
        /// Duplicate detection rule publish.
        /// OperationType = 7
        /// </summary>
        DuplicateDetectionRulePublish = 7,

        /// <summary>
        /// Bulk duplicate detection (detecting duplicates in bulk).
        /// OperationType = 8
        /// </summary>
        BulkDuplicateDetection = 8,

        /// <summary>
        /// SQM data collection (quality monitoring).
        /// OperationType = 9
        /// </summary>
        SQMDataCollection = 9,

        /// <summary>
        /// Workflow execution (background workflow).
        /// OperationType = 10
        /// </summary>
        Workflow = 10,

        /// <summary>
        /// Quick campaign (marketing quick campaigns).
        /// OperationType = 11
        /// </summary>
        QuickCampaign = 11,

        /// <summary>
        /// Matchcode update (duplicate detection matchcode update).
        /// OperationType = 12
        /// </summary>
        MatchcodeUpdate = 12,

        /// <summary>
        /// Bulk delete (bulk deletion of records).
        /// OperationType = 13
        /// </summary>
        BulkDelete = 13,

        /// <summary>
        /// Deletion service (cleanup service).
        /// OperationType = 14
        /// </summary>
        DeletionService = 14,

        /// <summary>
        /// Index management (search index management).
        /// OperationType = 15
        /// </summary>
        IndexManagement = 15,

        /// <summary>
        /// Collect organization statistics.
        /// OperationType = 16
        /// </summary>
        CollectOrganizationStatistics = 16,

        /// <summary>
        /// Import subprocess (sub-process during import).
        /// OperationType = 17
        /// </summary>
        ImportSubprocess = 17,

        /// <summary>
        /// Calculate organization storage size.
        /// OperationType = 18
        /// </summary>
        CalculateOrganizationStorageSize = 18,

        /// <summary>
        /// Collect organization database statistics.
        /// OperationType = 19
        /// </summary>
        CollectOrganizationDatabaseStatistics = 19,

        /// <summary>
        /// Collection organization size statistics.
        /// OperationType = 20
        /// </summary>
        CollectionOrganizationSizeStatistics = 20,

        /// <summary>
        /// Database tuning (performance tuning).
        /// OperationType = 21
        /// </summary>
        DatabaseTuning = 21,

        /// <summary>
        /// Calculate max organization storage size.
        /// OperationType = 22
        /// </summary>
        CalculateMaxStorageSize = 22,

        /// <summary>
        /// Bulk delete subprocess (sub-process during bulk delete).
        /// OperationType = 23
        /// </summary>
        BulkDeleteSubprocess = 23,

        /// <summary>
        /// Update statistic intervals.
        /// OperationType = 24
        /// </summary>
        UpdateStatisticIntervals = 24,

        /// <summary>
        /// Organization full text catalog index.
        /// OperationType = 25
        /// </summary>
        OrganizationFullTextCatalogIndex = 25,

        /// <summary>
        /// Database log backup.
        /// OperationType = 26
        /// </summary>
        DatabaseLogBackup = 26,

        /// <summary>
        /// Update contract states.
        /// OperationType = 27
        /// </summary>
        UpdateContractStates = 27,

        /// <summary>
        /// DBCC SHRINKDATABASE maintenance job.
        /// OperationType = 28
        /// </summary>
        DBCCShrinkDatabase = 28,

        /// <summary>
        /// DBCC SHRINKFILE maintenance job.
        /// OperationType = 29
        /// </summary>
        DBCCShrinkFile = 29,

        /// <summary>
        /// Reindex all indices maintenance job.
        /// OperationType = 30
        /// </summary>
        ReindexAllIndices = 30,

        /// <summary>
        /// Storage limit notification.
        /// OperationType = 31
        /// </summary>
        StorageLimitNotification = 31,

        /// <summary>
        /// Cleanup inactive workflow assemblies.
        /// OperationType = 32
        /// </summary>
        CleanupInactiveWorkflowAssemblies = 32,

        /// <summary>
        /// Recurring series expansion.
        /// OperationType = 35
        /// </summary>
        RecurringSeriesExpansion = 35,

        /// <summary>
        /// Import sample data.
        /// OperationType = 38
        /// </summary>
        ImportSampleData = 38,

        /// <summary>
        /// Goal roll up.
        /// OperationType = 40
        /// </summary>
        GoalRollUp = 40,

        /// <summary>
        /// Audit partition creation.
        /// OperationType = 41
        /// </summary>
        AuditPartitionCreation = 41,

        /// <summary>
        /// Update organization database.
        /// OperationType = 42
        /// </summary>
        UpdateOrganizationDatabase = 42,

        /// <summary>
        /// Update solution.
        /// OperationType = 43
        /// </summary>
        UpdateSolution = 43,

        /// <summary>
        /// Regenerate entity row count snapshot.
        /// OperationType = 44
        /// </summary>
        RegenerateEntityRowCountSnapshot = 44,

        /// <summary>
        /// Regenerate read share snapshot.
        /// OperationType = 45
        /// </summary>
        RegenerateReadShareSnapshot = 45,

        /// <summary>
        /// Outgoing activity (activity party async operation).
        /// OperationType = 50
        /// </summary>
        OutgoingActivity = 50,

        /// <summary>
        /// Incoming email processing.
        /// OperationType = 51
        /// </summary>
        IncomingEmailProcessing = 51,

        /// <summary>
        /// Mailbox test access.
        /// OperationType = 52
        /// </summary>
        MailboxTestAccess = 52,

        /// <summary>
        /// Encryption health check.
        /// OperationType = 53
        /// </summary>
        EncryptionHealthCheck = 53,

        /// <summary>
        /// Execute async request (generic async request execution).
        /// OperationType = 54
        /// </summary>
        ExecuteAsyncRequest = 54,

        /// <summary>
        /// Update entitlement states.
        /// OperationType = 56
        /// </summary>
        UpdateEntitlementStates = 56,

        /// <summary>
        /// Calculate rollup field.
        /// OperationType = 57
        /// </summary>
        CalculateRollupField = 57,

        /// <summary>
        /// Mass calculate rollup field.
        /// OperationType = 58
        /// </summary>
        MassCalculateRollupField = 58,

        /// <summary>
        /// Import translation.
        /// OperationType = 59
        /// </summary>
        ImportTranslation = 59,

        /// <summary>
        /// Convert date and time behavior.
        /// OperationType = 62
        /// </summary>
        ConvertDateAndTimeBehavior = 62,

        /// <summary>
        /// Entity key index creation.
        /// OperationType = 63
        /// </summary>
        EntityKeyIndexCreation = 63,

        /// <summary>
        /// Update knowledge article states.
        /// OperationType = 65
        /// </summary>
        UpdateKnowledgeArticleStates = 65,

        /// <summary>
        /// Resource booking sync.
        /// OperationType = 68
        /// </summary>
        ResourceBookingSync = 68,

        /// <summary>
        /// Relationship assistant cards.
        /// OperationType = 69
        /// </summary>
        RelationshipAssistantCards = 69,

        /// <summary>
        /// Apply record creation and update rules.
        /// OperationType = 71
        /// </summary>
        ApplyRecordCreationAndUpdateRules = 71,

        /// <summary>
        /// Update modern flow async operation.
        /// OperationType = 75
        /// </summary>
        UpdateModernFlowAsyncOperation = 75,

        /// <summary>
        /// Cascade assign.
        /// OperationType = 90
        /// </summary>
        CascadeAssign = 90,

        /// <summary>
        /// Cascade delete.
        /// OperationType = 91
        /// </summary>
        CascadeDelete = 91,

        /// <summary>
        /// Event hub listener (for event hub integration).
        /// OperationType = 92
        /// </summary>
        EventHubListener = 92,

        /// <summary>
        /// Cascade flow session permissions async operation.
        /// OperationType = 93
        /// </summary>
        CascadeFlowSessionPermissionsAsyncOperation = 93,

        /// <summary>
        /// AI Builder training events.
        /// OperationType = 190
        /// </summary>
        AIBuilderTrainingEvents = 190,

        /// <summary>
        /// AI Builder prediction events.
        /// OperationType = 191
        /// </summary>
        AIBuilderPredictionEvents = 191,

        /// <summary>
        /// Create or refresh virtual entity (elastic table).
        /// OperationType = 201
        /// </summary>
        CreateOrRefreshVirtualEntity = 201,

        /// <summary>
        /// Update modern flow async operation (modern flows).
        /// OperationType = 207
        /// </summary>
        UpdateModernFlowAsync = 207,

        /// <summary>
        /// Execute plugin - Asynchronous plugin execution.
        /// This is the most common type for testing async plugins.
        /// OperationType = 211
        /// </summary>
        ExecutePlugin = 211
    }
}
