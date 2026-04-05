using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Pipeline
{
    /// <summary>
    /// Manages a plugin-like execution pipeline with pre-validation, pre-operation,
    /// core operation, and post-operation stages.
    /// Supports both lambda callbacks (<see cref="Action{T}"/> of <see cref="IPluginExecutionContext"/>)
    /// and real <see cref="IPlugin"/> instances for end-to-end plugin testing.
    /// </summary>
    public sealed class PipelineManager
    {
        private readonly object _lock = new object();
        private readonly List<StepEntry> _steps = new List<StepEntry>();
        private readonly Func<Guid?, IOrganizationService>? _serviceFactory;
        private readonly FakeTracingService _tracingService = new FakeTracingService();

        /// <summary>
        /// Gets all trace messages written by plugins since the last <see cref="ClearTraces"/> call.
        /// </summary>
        public IReadOnlyList<string> Traces => _tracingService.Traces;

        /// <summary>Removes all captured plugin trace messages.</summary>
        public void ClearTraces() => _tracingService.Clear();

        /// <summary>
        /// Initializes a <see cref="PipelineManager"/> with no service factory.
        /// Registering <see cref="IPlugin"/> steps requires using the factory-enabled overload
        /// (done automatically when the pipeline is created by <see cref="FakeOrganizationService"/>).
        /// </summary>
        public PipelineManager() : this(null) { }

        /// <summary>
        /// Initializes a <see cref="PipelineManager"/> with a service factory used to supply
        /// <see cref="IOrganizationServiceFactory"/> when executing <see cref="IPlugin"/> steps.
        /// </summary>
        /// <param name="serviceFactory">
        /// Factory delegate invoked with an optional user ID; return the <see cref="IOrganizationService"/>
        /// that plugin code should use.
        /// </param>
        internal PipelineManager(Func<Guid?, IOrganizationService>? serviceFactory)
        {
            _serviceFactory = serviceFactory;
        }

        // ── RegisterStep (callback) ──────────────────────────────────────────

        /// <summary>
        /// Registers a pipeline step callback for a specific message and stage (all entities).
        /// Returns a disposable handle to unregister the step.
        /// </summary>
        /// <param name="messageName">The message name, e.g. "Create", "Update", "Delete".</param>
        /// <param name="stage">The pipeline stage to fire at.</param>
        /// <param name="callback">The callback to invoke with the execution context.</param>
        public PipelineStepRegistration RegisterStep(
            string messageName,
            PipelineStage stage,
            Action<IPluginExecutionContext> callback)
        {
            return RegisterStep(messageName, stage, null, callback);
        }

        /// <summary>
        /// Registers a pipeline step callback scoped to a specific entity, message, and stage.
        /// Returns a disposable handle to unregister the step.
        /// </summary>
        /// <param name="messageName">The message name, e.g. "Create", "Update", "Delete".</param>
        /// <param name="stage">The pipeline stage to fire at.</param>
        /// <param name="entityName">Entity logical name to scope to, or <c>null</c> for all entities.</param>
        /// <param name="callback">The callback to invoke with the execution context.</param>
        public PipelineStepRegistration RegisterStep(
            string messageName,
            PipelineStage stage,
            string? entityName,
            Action<IPluginExecutionContext> callback)
        {
            if (messageName == null) throw new ArgumentNullException(nameof(messageName));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var entry = new StepEntry(messageName, stage, entityName, callback);
            lock (_lock) { _steps.Add(entry); }
            return new PipelineStepRegistration(() => { lock (_lock) { _steps.Remove(entry); } });
        }

        // ── RegisterStep (IPlugin) ───────────────────────────────────────────

        /// <summary>
        /// Registers a real <see cref="IPlugin"/> instance for a specific message and stage (all entities).
        /// The plugin's <see cref="IPlugin.Execute"/> method is called with a fully-populated
        /// <see cref="IServiceProvider"/> that resolves <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationServiceFactory"/>, and <see cref="ITracingService"/>.
        /// Returns a disposable handle to unregister the step.
        /// </summary>
        /// <param name="messageName">The message name, e.g. "Create", "Update", "Delete".</param>
        /// <param name="stage">The pipeline stage to fire at.</param>
        /// <param name="plugin">The plugin instance to execute.</param>
        public PipelineStepRegistration RegisterStep(
            string messageName,
            PipelineStage stage,
            IPlugin plugin)
        {
            return RegisterStep(messageName, stage, null, plugin);
        }

        /// <summary>
        /// Registers a real <see cref="IPlugin"/> instance scoped to a specific entity, message, and stage.
        /// The plugin's <see cref="IPlugin.Execute"/> method is called with a fully-populated
        /// <see cref="IServiceProvider"/> that resolves <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationServiceFactory"/>, and <see cref="ITracingService"/>.
        /// Returns a disposable handle to unregister the step.
        /// </summary>
        /// <param name="messageName">The message name, e.g. "Create", "Update", "Delete".</param>
        /// <param name="stage">The pipeline stage to fire at.</param>
        /// <param name="entityName">Entity logical name to scope to, or <c>null</c> for all entities.</param>
        /// <param name="plugin">The plugin instance to execute.</param>
        public PipelineStepRegistration RegisterStep(
            string messageName,
            PipelineStage stage,
            string? entityName,
            IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            return RegisterStep(messageName, stage, entityName, ctx =>
            {
                if (_serviceFactory == null)
                    throw new InvalidOperationException(
                        "IPlugin steps require a service factory. Create PipelineManager through " +
                        "FakeOrganizationService.Pipeline rather than instantiating it directly.");

                var factory = new FakeOrganizationServiceFactory(_serviceFactory);
                var serviceProvider = new FakePluginServiceProvider(ctx, factory, _tracingService);
                plugin.Execute(serviceProvider);
            });
        }

        // ── Convenience helpers ──────────────────────────────────────────────

        /// <summary>Registers a pre-validation callback (stage 10) for a message across all entities.</summary>
        public PipelineStepRegistration RegisterPreValidation(string messageName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PreValidation, callback);

        /// <summary>Registers a pre-validation callback (stage 10) scoped to a specific entity.</summary>
        public PipelineStepRegistration RegisterPreValidation(string messageName, string? entityName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PreValidation, entityName, callback);

        /// <summary>Registers a pre-operation callback (stage 20) for a message across all entities.</summary>
        public PipelineStepRegistration RegisterPreOperation(string messageName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PreOperation, callback);

        /// <summary>Registers a pre-operation callback (stage 20) scoped to a specific entity.</summary>
        public PipelineStepRegistration RegisterPreOperation(string messageName, string? entityName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PreOperation, entityName, callback);

        /// <summary>Registers a post-operation callback (stage 40) for a message across all entities.</summary>
        public PipelineStepRegistration RegisterPostOperation(string messageName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PostOperation, callback);

        /// <summary>Registers a post-operation callback (stage 40) scoped to a specific entity.</summary>
        public PipelineStepRegistration RegisterPostOperation(string messageName, string? entityName, Action<IPluginExecutionContext> callback)
            => RegisterStep(messageName, PipelineStage.PostOperation, entityName, callback);

        // ── Internal execution ───────────────────────────────────────────────

        /// <summary>
        /// Executes the pipeline: fires pre-stages, runs the core operation, then fires post-stage.
        /// </summary>
        internal FakePipelineContext Execute(
            string messageName,
            string entityName,
            ParameterCollection inputParams,
            Func<FakePipelineContext, ParameterCollection> coreOperation,
            Guid userId,
            Guid initiatingUserId,
            Guid businessUnitId,
            Guid organizationId,
            string organizationName,
            DateTime operationCreatedOn)
        {
            var context = new FakePipelineContext(
                messageName, entityName, inputParams,
                userId, initiatingUserId, businessUnitId,
                organizationId, organizationName, operationCreatedOn);

            // Pre-validation
            FireStage(context, PipelineStage.PreValidation);

            // Pre-operation
            FireStage(context, PipelineStage.PreOperation);

            // Core operation
            var outputParams = coreOperation(context);
            foreach (var kvp in outputParams)
                context.OutputParameters[kvp.Key] = kvp.Value;

            // Post-operation
            FireStage(context, PipelineStage.PostOperation);

            return context;
        }

        internal bool HasSteps
        {
            get { lock (_lock) { return _steps.Count > 0; } }
        }

        private void FireStage(FakePipelineContext context, PipelineStage stage)
        {
            context.Stage = (int)stage;
            List<StepEntry> matching;
            lock (_lock)
            {
                matching = _steps
                    .Where(s =>
                        string.Equals(s.MessageName, context.MessageName, StringComparison.OrdinalIgnoreCase)
                        && s.Stage == stage
                        && (s.EntityName == null || string.Equals(s.EntityName, context.PrimaryEntityName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            foreach (var step in matching)
                step.Callback(context);
        }

        private sealed class StepEntry
        {
            public string MessageName { get; }
            public PipelineStage Stage { get; }
            public string? EntityName { get; }
            public Action<IPluginExecutionContext> Callback { get; }

            public StepEntry(string messageName, PipelineStage stage, string? entityName, Action<IPluginExecutionContext> callback)
            {
                MessageName = messageName;
                Stage = stage;
                EntityName = entityName;
                Callback = callback;
            }
        }
    }
}
