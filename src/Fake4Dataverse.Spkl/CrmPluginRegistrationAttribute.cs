using System;

namespace Fake4Dataverse.Spkl
{
    /// <summary>
    /// SPKL-style plugin registration attribute used to decorate <see cref="Microsoft.Xrm.Sdk.IPlugin"/> classes.
    /// Supports plugin step, custom API, and workflow-style constructor forms.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CrmPluginRegistrationAttribute : Attribute
    {
        /// <summary>
        /// Initializes a custom API-style registration attribute.
        /// </summary>
        /// <param name="message">The unique message name (typically the custom API unique name).</param>
        public CrmPluginRegistrationAttribute(string message)
        {
            Message = message;
            IsolationMode = IsolationModeEnum.Sandbox;
            ExecutionMode = ExecutionModeEnum.Synchronous;
            Offline = false;
            Server = true;
        }

        /// <summary>
        /// Initializes a plugin-step style registration attribute.
        /// </summary>
        /// <param name="message">Message name (for example: Create, Update, Delete).</param>
        /// <param name="entityLogicalName">Primary entity logical name, or <c>none</c> for no entity filter.</param>
        /// <param name="stage">Execution stage.</param>
        /// <param name="executionMode">Execution mode.</param>
        /// <param name="filteringAttributes">Comma-separated filtering attributes (commonly for Update).</param>
        /// <param name="stepName">Step display name.</param>
        /// <param name="executionOrder">Execution order/rank.</param>
        /// <param name="isolationModel">Isolation mode.</param>
        public CrmPluginRegistrationAttribute(
            string message,
            string entityLogicalName,
            StageEnum stage,
            ExecutionModeEnum executionMode,
            string filteringAttributes,
            string stepName,
            int executionOrder,
            IsolationModeEnum isolationModel)
        {
            Message = message;
            EntityLogicalName = entityLogicalName;
            FilteringAttributes = filteringAttributes;
            Name = stepName;
            ExecutionOrder = executionOrder;
            Stage = stage;
            ExecutionMode = executionMode;
            IsolationMode = isolationModel;
            Offline = false;
            Server = true;
        }

        /// <summary>
        /// Initializes a plugin-step style registration attribute using <see cref="MessageNameEnum"/>.
        /// </summary>
        public CrmPluginRegistrationAttribute(
            MessageNameEnum message,
            string entityLogicalName,
            StageEnum stage,
            ExecutionModeEnum executionMode,
            string filteringAttributes,
            string stepName,
            int executionOrder,
            IsolationModeEnum isolationModel)
            : this(message.ToString(), entityLogicalName, stage, executionMode, filteringAttributes, stepName, executionOrder, isolationModel)
        {
        }

        /// <summary>
        /// Initializes a workflow-activity style registration attribute.
        /// </summary>
        /// <param name="name">Workflow activity type name.</param>
        /// <param name="friendlyName">Workflow activity friendly name.</param>
        /// <param name="description">Workflow activity description.</param>
        /// <param name="groupName">Workflow activity group name.</param>
        /// <param name="isolationModel">Isolation mode.</param>
        public CrmPluginRegistrationAttribute(
            string name,
            string friendlyName,
            string description,
            string groupName,
            IsolationModeEnum isolationModel)
        {
            Name = name;
            FriendlyName = friendlyName;
            Description = description;
            GroupName = groupName;
            IsolationMode = isolationModel;
            ExecutionMode = ExecutionModeEnum.Synchronous;
        }

        /// <summary>Gets or sets an explicit step identifier.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets a friendly name (workflow style).</summary>
        public string? FriendlyName { get; set; }

        /// <summary>Gets or sets a workflow group name.</summary>
        public string? GroupName { get; set; }

        /// <summary>Gets or sets the first image alias name.</summary>
        public string? Image1Name { get; set; }

        /// <summary>Gets or sets the first image attributes (comma-separated).</summary>
        public string? Image1Attributes { get; set; }

        /// <summary>Gets or sets the second image alias name.</summary>
        public string? Image2Name { get; set; }

        /// <summary>Gets or sets the second image attributes (comma-separated).</summary>
        public string? Image2Attributes { get; set; }

        /// <summary>Gets or sets the description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets whether async operation records should be auto-deleted.</summary>
        public bool DeleteAsyncOperation { get; set; }

        /// <summary>Gets or sets unsecure configuration text.</summary>
        public string? UnSecureConfiguration { get; set; }

        /// <summary>Gets or sets secure configuration text.</summary>
        public string? SecureConfiguration { get; set; }

        /// <summary>Gets or sets whether the step is offline-enabled.</summary>
        public bool Offline { get; set; }

        /// <summary>Gets or sets whether the step is server-enabled.</summary>
        public bool Server { get; set; }

        /// <summary>Gets or sets image 1 type.</summary>
        public ImageTypeEnum Image1Type { get; set; }

        /// <summary>Gets or sets image 2 type.</summary>
        public ImageTypeEnum Image2Type { get; set; }

        /// <summary>Gets or sets an optional operation action value.</summary>
        public PluginStepOperationEnum? Action { get; set; }

        /// <summary>Gets the isolation mode.</summary>
        public IsolationModeEnum IsolationMode { get; private set; }

        /// <summary>Gets the message name.</summary>
        public string? Message { get; private set; }

        /// <summary>Gets the primary entity logical name.</summary>
        public string? EntityLogicalName { get; private set; }

        /// <summary>Gets the filtering attributes (comma-separated).</summary>
        public string? FilteringAttributes { get; private set; }

        /// <summary>Gets the step or workflow name, depending on constructor form.</summary>
        public string? Name { get; private set; }

        /// <summary>Gets the execution order/rank.</summary>
        public int ExecutionOrder { get; private set; }

        /// <summary>Gets the stage for plugin-step registrations.</summary>
        public StageEnum? Stage { get; private set; }

        /// <summary>Gets the execution mode.</summary>
        public ExecutionModeEnum ExecutionMode { get; private set; }
    }

    /// <summary>Plugin execution mode.</summary>
    public enum ExecutionModeEnum
    {
        /// <summary>Asynchronous mode.</summary>
        Asynchronous,

        /// <summary>Synchronous mode.</summary>
        Synchronous
    }

    /// <summary>Step image type.</summary>
    public enum ImageTypeEnum
    {
        /// <summary>Pre-image only.</summary>
        PreImage = 0,

        /// <summary>Post-image only.</summary>
        PostImage = 1,

        /// <summary>Both pre-image and post-image.</summary>
        Both = 2
    }

    /// <summary>Plugin assembly isolation mode.</summary>
    public enum IsolationModeEnum
    {
        /// <summary>No sandbox isolation.</summary>
        None = 0,

        /// <summary>Sandbox isolation.</summary>
        Sandbox = 1
    }

    /// <summary>
    /// Common Dataverse message names used with plugin-step registration.
    /// </summary>
#pragma warning disable CS1591
    public enum MessageNameEnum
    {
        AddItem,
        AddListMembers,
        AddMember,
        AddMembers,
        AddPrincipalToQueue,
        AddPrivileges,
        AddProductToKit,
        AddRecurrence,
        AddToQueue,
        AddUserToRecordTeam,
        ApplyRecordCreationAndUpdateRule,
        Assign,
        Associate,
        BackgroundSend,
        Book,
        CalculatePrice,
        Cancel,
        CheckIncoming,
        CheckPromote,
        Clone,
        CloneMobileOfflineProfile,
        CloneProduct,
        Close,
        CopyDynamicListToStatic,
        CopySystemForm,
        Create,
        CreateException,
        CreateInstance,
        CreateKnowledgeArticleTranslation,
        CreateKnowledgeArticleVersion,
        Delete,
        DeleteOpenInstances,
        DeliverIncoming,
        DeliverPromote,
        Disassociate,
        Execute,
        ExecuteById,
        Export,
        GenerateSocialProfile,
        GetDefaultPriceLevel,
        GrantAccess,
        Import,
        LockInvoicePricing,
        LockSalesOrderPricing,
        Lose,
        Merge,
        ModifyAccess,
        PickFromQueue,
        Publish,
        PublishAll,
        PublishTheme,
        QualifyLead,
        Recalculate,
        ReleaseToQueue,
        RemoveFromQueue,
        RemoveItem,
        RemoveMember,
        RemoveMembers,
        RemovePrivilege,
        RemoveProductFromKit,
        RemoveRelated,
        RemoveUserFromRecordTeam,
        ReplacePrivileges,
        Reschedule,
        Retrieve,
        RetrieveExchangeRate,
        RetrieveFilteredForms,
        RetrieveMultiple,
        RetrievePersonalWall,
        RetrievePrincipalAccess,
        RetrieveRecordWall,
        RetrieveSharedPrincipalsAndAccess,
        RetrieveUnpublished,
        RetrieveUnpublishedMultiple,
        RetrieveUserQueues,
        RevokeAccess,
        RouteTo,
        Send,
        SendFromTemplate,
        SetLocLabels,
        SetRelated,
        SetState,
        TriggerServiceEndpointCheck,
        UnlockInvoicePricing,
        UnlockSalesOrderPricing,
        Update,
        ValidateRecurrenceRule,
        Win
    }
#pragma warning restore CS1591

    /// <summary>Additional step operation hints.</summary>
    public enum PluginStepOperationEnum
    {
        /// <summary>Delete operation.</summary>
        Delete = 0,

        /// <summary>Deactivate operation.</summary>
        Deactivate = 1
    }

    /// <summary>Plugin pipeline stage.</summary>
    public enum StageEnum
    {
        /// <summary>Pre-validation stage.</summary>
        PreValidation = 10,

        /// <summary>Pre-operation stage.</summary>
        PreOperation = 20,

        /// <summary>Post-operation stage.</summary>
        PostOperation = 40
    }
}