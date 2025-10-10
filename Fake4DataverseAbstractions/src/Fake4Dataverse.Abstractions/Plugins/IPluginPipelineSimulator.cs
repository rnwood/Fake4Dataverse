using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Interface for simulating the Dataverse plugin pipeline.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
    /// 
    /// The plugin pipeline executes registered plugins in a specific order:
    /// 1. PreValidation (Stage 10) - Outside transaction
    /// 2. PreOperation (Stage 20) - Inside transaction
    /// 3. MainOperation (Stage 30) - Core database operation (not customizable)
    /// 4. PostOperation (Stage 40) - Inside transaction (sync) or queued (async)
    /// 
    /// Multiple plugins can be registered for the same message/entity/stage and execute in order based on their ExecutionOrder (rank).
    /// </summary>
    public interface IPluginPipelineSimulator
    {
        /// <summary>
        /// Registers a plugin step to be executed in the pipeline.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
        /// 
        /// Multiple plugin steps can be registered for the same message/entity/stage combination.
        /// They will execute in order based on their ExecutionOrder property.
        /// </summary>
        /// <param name="registration">The plugin step registration details</param>
        void RegisterPluginStep(PluginStepRegistration registration);

        /// <summary>
        /// Registers multiple plugin steps at once
        /// </summary>
        /// <param name="registrations">The plugin step registrations to register</param>
        void RegisterPluginSteps(IEnumerable<PluginStepRegistration> registrations);

        /// <summary>
        /// Unregisters a plugin step from the pipeline
        /// </summary>
        /// <param name="registration">The plugin step registration to remove</param>
        void UnregisterPluginStep(PluginStepRegistration registration);

        /// <summary>
        /// Clears all registered plugin steps
        /// </summary>
        void ClearAllPluginSteps();

        /// <summary>
        /// Gets all registered plugin steps for a specific message, entity, and stage
        /// </summary>
        /// <param name="messageName">The SDK message name (e.g., "Create", "Update")</param>
        /// <param name="entityLogicalName">The entity logical name</param>
        /// <param name="stage">The pipeline stage</param>
        /// <returns>List of registered plugin steps, ordered by ExecutionOrder</returns>
        IEnumerable<PluginStepRegistration> GetRegisteredPluginSteps(
            string messageName, 
            string entityLogicalName, 
            Enums.ProcessingStepStage stage);

        /// <summary>
        /// Executes all registered plugins for a specific pipeline stage.
        /// Plugins execute in order based on their ExecutionOrder property.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
        /// </summary>
        /// <param name="messageName">The SDK message name</param>
        /// <param name="entityLogicalName">The entity logical name</param>
        /// <param name="stage">The pipeline stage to execute</param>
        /// <param name="targetEntity">The target entity for the operation</param>
        /// <param name="modifiedAttributes">The set of modified attributes (for Update message)</param>
        /// <param name="preEntityImages">Pre-operation entity images</param>
        /// <param name="postEntityImages">Post-operation entity images</param>
        /// <param name="userId">The user ID executing the operation</param>
        /// <param name="organizationId">The organization ID</param>
        /// <param name="currentDepth">The current execution depth (for tracking recursive calls)</param>
        void ExecutePipelineStage(
            string messageName,
            string entityLogicalName,
            Enums.ProcessingStepStage stage,
            Entity targetEntity,
            HashSet<string> modifiedAttributes = null,
            EntityImageCollection preEntityImages = null,
            EntityImageCollection postEntityImages = null,
            Guid? userId = null,
            Guid? organizationId = null,
            int currentDepth = 1);

        /// <summary>
        /// Gets the maximum allowed pipeline execution depth before throwing an exception.
        /// Default is typically 8 in Dataverse.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-recursive-loops
        /// </summary>
        int MaxDepth { get; set; }
    }
}
