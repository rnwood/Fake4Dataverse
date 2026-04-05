using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Pipeline
{
    /// <summary>
    /// In-memory implementation of <see cref="IPluginExecutionContext"/> passed to pipeline step
    /// callbacks and <see cref="IPlugin"/> instances during test execution.
    /// </summary>
    public sealed class FakePipelineContext : IPluginExecutionContext
    {
        // ── IExecutionContext ────────────────────────────────────────────────

        /// <inheritdoc />
        public string MessageName { get; }

        /// <inheritdoc />
        public string PrimaryEntityName { get; }

        /// <inheritdoc />
        public Guid PrimaryEntityId { get; set; }

        /// <inheritdoc />
        public ParameterCollection InputParameters { get; }

        /// <inheritdoc />
        public ParameterCollection OutputParameters { get; }

        /// <inheritdoc />
        public EntityImageCollection PreEntityImages { get; } = new EntityImageCollection();

        /// <inheritdoc />
        public EntityImageCollection PostEntityImages { get; } = new EntityImageCollection();

        /// <summary>
        /// Gets the current pipeline stage integer value as defined by <see cref="IExecutionContext"/>.
        /// The typed equivalent is available via <see cref="PipelineStage"/>.
        /// </summary>
        public int Stage { get; internal set; }

        /// <summary>
        /// Gets the current pipeline stage as a typed <see cref="Fake4Dataverse.Pipeline.PipelineStage"/> value.
        /// </summary>
        public PipelineStage PipelineStage => (PipelineStage)Stage;

        /// <inheritdoc />
        public int Depth { get; }

        /// <inheritdoc />
        public Guid UserId { get; }

        /// <inheritdoc />
        public Guid InitiatingUserId { get; }

        /// <inheritdoc />
        public Guid BusinessUnitId { get; }

        /// <inheritdoc />
        public Guid OrganizationId { get; }

        /// <inheritdoc />
        public string OrganizationName { get; }

        /// <inheritdoc />
        public ParameterCollection SharedVariables { get; } = new ParameterCollection();

        /// <inheritdoc />
        public Guid CorrelationId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public Guid OperationId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public DateTime OperationCreatedOn { get; }

        /// <inheritdoc />
        public Guid? RequestId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string SecondaryEntityName => string.Empty;

        /// <inheritdoc />
        public EntityReference OwningExtension => new EntityReference();

        /// <inheritdoc />
        public bool IsExecutingOffline => false;

        /// <inheritdoc />
        public bool IsOfflinePlayback => false;

        /// <inheritdoc />
        public bool IsInTransaction => true;

        /// <inheritdoc />
        public int IsolationMode => 1; // Sandbox

        /// <inheritdoc />
        public int Mode => 0; // Synchronous

        // ── IPluginExecutionContext ──────────────────────────────────────────

        /// <inheritdoc />
        public IPluginExecutionContext ParentContext => null!;

        // ── Constructor ─────────────────────────────────────────────────────

        /// <summary>
        /// Initializes a new <see cref="FakePipelineContext"/>.
        /// </summary>
        internal FakePipelineContext(
            string messageName,
            string primaryEntityName,
            ParameterCollection inputParameters,
            Guid userId,
            Guid initiatingUserId,
            Guid businessUnitId,
            Guid organizationId,
            string organizationName,
            DateTime operationCreatedOn,
            int depth = 1)
        {
            MessageName = messageName ?? throw new ArgumentNullException(nameof(messageName));
            PrimaryEntityName = primaryEntityName ?? string.Empty;
            InputParameters = inputParameters ?? throw new ArgumentNullException(nameof(inputParameters));
            OutputParameters = new ParameterCollection();
            UserId = userId;
            InitiatingUserId = initiatingUserId;
            BusinessUnitId = businessUnitId;
            OrganizationId = organizationId;
            OrganizationName = organizationName ?? string.Empty;
            OperationCreatedOn = operationCreatedOn;
            Depth = depth;
        }
    }
}
