using FakeItEasy;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;

namespace Fake4Dataverse
{
    /// <summary>
    /// Simulates the Dataverse plugin pipeline for testing purposes.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
    /// 
    /// The plugin pipeline executes registered plugins in a specific order:
    /// 1. PreValidation (Stage 10) - Outside transaction
    /// 2. PreOperation (Stage 20) - Inside transaction  
    /// 3. MainOperation (Stage 30) - Core database operation (not customizable)
    /// 4. PostOperation (Stage 40) - Inside transaction (sync) or queued (async)
    /// </summary>
    public class PluginPipelineSimulator : IPluginPipelineSimulator
    {
        private readonly IXrmFakedContext _context;
        private readonly Dictionary<string, List<PluginStepRegistration>> _pluginSteps;
        
        /// <summary>
        /// Maximum allowed pipeline execution depth before throwing an exception.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-recursive-loops
        /// Default value is 8, which matches Dataverse behavior.
        /// Tracks plugin execution depth to prevent infinite loops where a plugin triggers another operation that triggers the same plugin.
        /// </summary>
        public int MaxDepth { get; set; } = 8;

        public PluginPipelineSimulator(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _pluginSteps = new Dictionary<string, List<PluginStepRegistration>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a plugin step to be executed in the pipeline.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
        /// 
        /// Multiple plugin steps can be registered for the same message/entity/stage combination.
        /// They will execute in order based on their ExecutionOrder property (lower values execute first).
        /// </summary>
        public void RegisterPluginStep(PluginStepRegistration registration)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            if (string.IsNullOrWhiteSpace(registration.MessageName))
                throw new ArgumentException("MessageName cannot be null or empty", nameof(registration));

            if (registration.PluginType == null)
                throw new ArgumentException("PluginType cannot be null", nameof(registration));

            var key = registration.GetRegistrationKey();

            if (!_pluginSteps.ContainsKey(key))
            {
                _pluginSteps[key] = new List<PluginStepRegistration>();
            }

            _pluginSteps[key].Add(registration);

            // Sort by execution order (lower numbers execute first)
            _pluginSteps[key] = _pluginSteps[key].OrderBy(r => r.ExecutionOrder).ToList();
        }

        /// <summary>
        /// Registers multiple plugin steps at once
        /// </summary>
        public void RegisterPluginSteps(IEnumerable<PluginStepRegistration> registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            foreach (var registration in registrations)
            {
                RegisterPluginStep(registration);
            }
        }

        /// <summary>
        /// Unregisters a plugin step from the pipeline
        /// </summary>
        public void UnregisterPluginStep(PluginStepRegistration registration)
        {
            if (registration == null)
                return;

            var key = registration.GetRegistrationKey();

            if (_pluginSteps.ContainsKey(key))
            {
                _pluginSteps[key].Remove(registration);

                if (_pluginSteps[key].Count == 0)
                {
                    _pluginSteps.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all registered plugin steps
        /// </summary>
        public void ClearAllPluginSteps()
        {
            _pluginSteps.Clear();
        }

        /// <summary>
        /// Gets all registered plugin steps for a specific message, entity, and stage
        /// </summary>
        public IEnumerable<PluginStepRegistration> GetRegisteredPluginSteps(
            string messageName,
            string entityLogicalName,
            ProcessingStepStage stage)
        {
            var key = $"{messageName}|{entityLogicalName}|{(int)stage}";

            if (_pluginSteps.ContainsKey(key))
            {
                return _pluginSteps[key].AsReadOnly();
            }

            return Enumerable.Empty<PluginStepRegistration>();
        }

        /// <summary>
        /// Executes all registered plugins for a specific pipeline stage.
        /// Plugins execute in order based on their ExecutionOrder property.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
        /// </summary>
        public void ExecutePipelineStage(
            string messageName,
            string entityLogicalName,
            ProcessingStepStage stage,
            Entity targetEntity,
            HashSet<string> modifiedAttributes = null,
            EntityImageCollection preEntityImages = null,
            EntityImageCollection postEntityImages = null,
            Guid? userId = null,
            Guid? organizationId = null,
            int currentDepth = 1)
        {
            // Check depth to prevent infinite loops
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-recursive-loops
            if (currentDepth > MaxDepth)
            {
                throw new InvalidPluginExecutionException($"Maximum plugin execution depth ({MaxDepth}) exceeded. This typically indicates a recursive loop in plugin logic.");
            }

            var key = $"{messageName}|{entityLogicalName}|{(int)stage}";

            if (!_pluginSteps.ContainsKey(key))
            {
                return; // No plugins registered for this stage
            }

            var pluginSteps = _pluginSteps[key]
                .Where(step => step.ShouldExecute(entityLogicalName, modifiedAttributes))
                .OrderBy(step => step.ExecutionOrder)
                .ToList();

            foreach (var step in pluginSteps)
            {
                // Skip asynchronous plugins in stages where they shouldn't execute immediately
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
                // Async plugins are queued for later execution and don't run in the current transaction
                if (step.Mode == ProcessingStepMode.Asynchronous)
                {
                    // In a real system, async plugins would be queued
                    // For testing purposes, we can either skip them or execute them synchronously
                    // For now, we'll execute them synchronously to allow testing async plugin logic
                }

                // Create entity images based on step registration
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
                var stepPreImages = CreateEntityImages(step, messageName, targetEntity, preEntityImages, true);
                var stepPostImages = CreateEntityImages(step, messageName, targetEntity, postEntityImages, false);

                ExecutePlugin(
                    step,
                    messageName,
                    entityLogicalName,
                    stage,
                    targetEntity,
                    stepPreImages,
                    stepPostImages,
                    userId,
                    organizationId,
                    currentDepth);
            }
        }

        private void ExecutePlugin(
            PluginStepRegistration step,
            string messageName,
            string entityLogicalName,
            ProcessingStepStage stage,
            Entity targetEntity,
            EntityImageCollection preEntityImages,
            EntityImageCollection postEntityImages,
            Guid? userId,
            Guid? organizationId,
            int currentDepth)
        {
            // Create plugin instance
            IPlugin plugin;
            
            try
            {
                // Check if plugin has a constructor with configuration parameters
                var constructorWithConfig = step.PluginType.GetConstructor(new[] { typeof(string), typeof(string) });
                if (constructorWithConfig != null)
                {
                    plugin = (IPlugin)Activator.CreateInstance(
                        step.PluginType,
                        step.UnsecureConfiguration,
                        step.SecureConfiguration);
                }
                else
                {
                    // Use parameterless constructor
                    plugin = (IPlugin)Activator.CreateInstance(step.PluginType);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(
                    $"Failed to create instance of plugin type '{step.PluginType.FullName}': {ex.Message}",
                    ex);
            }

            // Create plugin execution context
            var pluginContext = new XrmFakedPluginExecutionContext
            {
                Depth = currentDepth,
                MessageName = messageName,
                PrimaryEntityName = entityLogicalName,
                Stage = (int)stage,
                Mode = (int)step.Mode,
                UserId = userId ?? _context.CallerProperties.CallerId.Id,
                InitiatingUserId = userId ?? _context.CallerProperties.CallerId.Id,
                OrganizationId = organizationId ?? Guid.NewGuid(),
                BusinessUnitId = _context.CallerProperties.BusinessUnitId.Id,
                CorrelationId = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                OperationCreatedOn = DateTime.UtcNow,
                InputParameters = new ParameterCollection(),
                OutputParameters = new ParameterCollection(),
                PreEntityImages = preEntityImages ?? new EntityImageCollection(),
                PostEntityImages = postEntityImages ?? new EntityImageCollection(),
                SharedVariables = new ParameterCollection()
            };

            // Add target entity to input parameters
            if (targetEntity != null)
            {
                pluginContext.InputParameters["Target"] = targetEntity;
                pluginContext.PrimaryEntityId = targetEntity.Id;
            }

            // Create service provider
            var serviceProvider = _context.PluginContextProperties.GetServiceProvider(pluginContext);

            try
            {
                // Execute the plugin
                plugin.Execute(serviceProvider);
            }
            catch (InvalidPluginExecutionException)
            {
                // Re-throw plugin execution exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in InvalidPluginExecutionException
                throw new InvalidPluginExecutionException(
                    $"Plugin '{step.PluginType.FullName}' failed during execution: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Creates entity images based on the plugin step registration configuration.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
        /// 
        /// Entity images provide snapshots of entity data at different points in the pipeline.
        /// Pre-images: Entity state before the core operation (available for Update, Delete)
        /// Post-images: Entity state after the core operation (available for Create, Update)
        /// 
        /// Images can be filtered to include only specific attributes for performance and security.
        /// Multiple images can be registered with different attribute filters.
        /// </summary>
        /// <param name="step">The plugin step registration containing image configurations</param>
        /// <param name="messageName">The SDK message name (Create, Update, Delete, etc.)</param>
        /// <param name="targetEntity">The target entity for the operation</param>
        /// <param name="providedImages">Any images provided by the caller (can be null)</param>
        /// <param name="isPreImage">True for pre-images, false for post-images</param>
        /// <returns>EntityImageCollection containing the configured images</returns>
        private EntityImageCollection CreateEntityImages(
            PluginStepRegistration step,
            string messageName,
            Entity targetEntity,
            EntityImageCollection providedImages,
            bool isPreImage)
        {
            // Start with provided images or create new collection
            var images = providedImages ?? new EntityImageCollection();

            // If no target entity, return the provided images
            if (targetEntity == null)
            {
                return images;
            }

            // Determine which image collection to use from the step registration
            var imageRegistrations = isPreImage ? step.PreImages : step.PostImages;

            // If no image registrations configured, return provided images
            if (imageRegistrations == null || imageRegistrations.Count == 0)
            {
                return images;
            }

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // For pre-images: Need to retrieve current state from context for Update/Delete
            // For post-images: Use target entity for Create, retrieve updated state for Update
            Entity sourceEntity = targetEntity;

            // For pre-images on Update/Delete, we need to retrieve the current state from the context
            if (isPreImage && (messageName.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                               messageName.Equals("Delete", StringComparison.OrdinalIgnoreCase)))
            {
                // Try to retrieve the existing entity from the context
                try
                {
                    var service = _context.GetOrganizationService();
                    var existingEntity = service.Retrieve(targetEntity.LogicalName, targetEntity.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                    sourceEntity = existingEntity;
                }
                catch
                {
                    // If retrieval fails, use the target entity as fallback
                    sourceEntity = targetEntity;
                }
            }

            // Create images based on registrations
            foreach (var imageReg in imageRegistrations)
            {
                // Check if this image registration is valid for the current message
                if (!imageReg.IsValidForMessage(messageName, isPreImage))
                {
                    continue;
                }

                // Create filtered image based on configured attributes
                var filteredImage = imageReg.CreateFilteredImage(sourceEntity);

                if (filteredImage != null)
                {
                    // Add image to collection using the configured name
                    // If an image with this name already exists (from providedImages), it will be replaced
                    var imageName = !string.IsNullOrEmpty(imageReg.Name) ? imageReg.Name : 
                                    (!string.IsNullOrEmpty(imageReg.EntityAlias) ? imageReg.EntityAlias : "Image");
                    
                    images[imageName] = filteredImage;
                }
            }

            return images;
        }

        /// <summary>
        /// Discovers and registers plugins from the provided assemblies.
        /// By default, scans for IPlugin implementations with SPKL CrmPluginRegistrationAttribute(s).
        /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl
        /// </summary>
        /// <param name="assemblies">Assemblies to scan for plugins</param>
        /// <param name="customConverter">Optional custom function to convert plugin types to registrations</param>
        /// <returns>Number of plugin steps registered</returns>
        public int DiscoverAndRegisterPlugins(
            IEnumerable<System.Reflection.Assembly> assemblies,
            Func<Type, IEnumerable<PluginStepRegistration>> customConverter = null)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            var discoveredRegistrations = PluginDiscoveryService.DiscoverPlugins(assemblies, customConverter);
            var registrationsList = discoveredRegistrations.ToList();

            RegisterPluginSteps(registrationsList);

            return registrationsList.Count;
        }

        /// <summary>
        /// Discovers and registers plugins using a custom attribute converter.
        /// Allows users to provide their own logic for extracting registration information from custom attributes.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan for plugins</param>
        /// <param name="attributeType">Type of attribute to look for</param>
        /// <param name="attributeConverter">Function to convert attribute instances to registrations</param>
        /// <returns>Number of plugin steps registered</returns>
        public int DiscoverAndRegisterPluginsWithAttributeConverter(
            IEnumerable<System.Reflection.Assembly> assemblies,
            Type attributeType,
            Func<Type, System.Attribute, PluginStepRegistration> attributeConverter)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            if (attributeType == null)
            {
                throw new ArgumentNullException(nameof(attributeType));
            }
            if (attributeConverter == null)
            {
                throw new ArgumentNullException(nameof(attributeConverter));
            }

            var discoveredRegistrations = PluginDiscoveryService.DiscoverPluginsWithAttributeConverter(
                assemblies, attributeType, attributeConverter);
            var registrationsList = discoveredRegistrations.ToList();

            RegisterPluginSteps(registrationsList);

            return registrationsList.Count;
        }
    }
}
