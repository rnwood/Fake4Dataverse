using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions.Plugins.Enums;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Represents a plugin step registration that defines when and how a plugin should execute in the pipeline.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
    /// 
    /// Plugin steps define:
    /// - Message Name: The SDK message (Create, Update, Delete, etc.)
    /// - Primary Entity: The entity logical name
    /// - Stage: Pipeline stage (PreValidation=10, PreOperation=20, PostOperation=40)
    /// - Execution Order (Rank): Order in which plugins execute within a stage (1-n, lower numbers execute first)
    /// - Mode: Synchronous (0) or Asynchronous (1)
    /// - Filtering Attributes: Specific attributes that trigger the plugin (Update message only)
    /// </summary>
    public class PluginStepRegistration
    {
        /// <summary>
        /// Gets or sets the SDK message name (e.g., "Create", "Update", "Delete")
        /// </summary>
        public string MessageName { get; set; }

        /// <summary>
        /// Gets or sets the primary entity logical name
        /// </summary>
        public string PrimaryEntityName { get; set; }

        /// <summary>
        /// Gets or sets the pipeline stage
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
        /// - PreValidation (10): Executes before database transaction begins
        /// - PreOperation (20): Executes within database transaction, before main operation
        /// - PostOperation (40): Executes within database transaction (sync) or queued (async), after main operation
        /// </summary>
        public ProcessingStepStage Stage { get; set; }

        /// <summary>
        /// Gets or sets the execution order (rank) for this plugin step.
        /// When multiple plugins are registered for the same message/entity/stage, they execute in order based on this value.
        /// Lower numbers execute first. If two plugins have the same order, execution order is undefined.
        /// Default value is 1.
        /// </summary>
        public int ExecutionOrder { get; set; } = 1;

        /// <summary>
        /// Gets or sets the processing mode
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
        /// - Synchronous (0): Executes immediately within the transaction
        /// - Asynchronous (1): Queued for execution outside the transaction
        /// </summary>
        public ProcessingStepMode Mode { get; set; } = ProcessingStepMode.Synchronous;

        /// <summary>
        /// Gets or sets the plugin type to execute
        /// </summary>
        public Type PluginType { get; set; }

        /// <summary>
        /// Gets or sets the filtering attributes for Update message.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in#filtering-attributes
        /// When specified, the plugin only executes if one of these attributes is included in the update.
        /// Only applies to Update message; ignored for other messages.
        /// </summary>
        public HashSet<string> FilteringAttributes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the unsecure configuration data passed to the plugin constructor
        /// </summary>
        public string UnsecureConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the secure configuration data passed to the plugin constructor
        /// </summary>
        public string SecureConfiguration { get; set; }

        /// <summary>
        /// Determines whether this plugin step should execute for the given entity and modified attributes.
        /// For Update messages, checks if any filtering attributes were modified.
        /// For other messages, returns true if the entity matches.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name</param>
        /// <param name="modifiedAttributes">The set of modified attribute names (for Update message)</param>
        /// <returns>True if the plugin should execute; otherwise false</returns>
        public bool ShouldExecute(string entityLogicalName, HashSet<string> modifiedAttributes = null)
        {
            // Check entity name matches
            if (!string.Equals(PrimaryEntityName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // For Update message, check filtering attributes
            if (string.Equals(MessageName, "Update", StringComparison.OrdinalIgnoreCase) && 
                FilteringAttributes != null && 
                FilteringAttributes.Count > 0)
            {
                // Plugin only executes if at least one filtering attribute was modified
                if (modifiedAttributes == null || modifiedAttributes.Count == 0)
                {
                    return false;
                }

                return FilteringAttributes.Any(fa => modifiedAttributes.Contains(fa));
            }

            return true;
        }

        /// <summary>
        /// Creates a unique key for this plugin step registration based on message, entity, and stage
        /// </summary>
        public string GetRegistrationKey()
        {
            return $"{MessageName}|{PrimaryEntityName}|{(int)Stage}";
        }
    }
}
