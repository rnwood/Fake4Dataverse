using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Fake4Dataverse.Abstractions.Plugins.Enums;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Provides functionality to auto-discover and register plugins from assemblies.
    /// Supports scanning for IPlugin implementations and converting registration attributes to plugin step registrations.
    /// </summary>
    public class PluginDiscoveryService
    {
        /// <summary>
        /// Discovers all IPlugin types in the provided assemblies and creates plugin step registrations.
        /// By default, looks for SPKL CrmPluginRegistrationAttribute(s) without requiring a reference to the SPKL package.
        /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl
        /// </summary>
        /// <param name="assemblies">Assemblies to scan for plugins</param>
        /// <param name="customConverter">Optional custom function to convert plugin types to registrations</param>
        /// <returns>Collection of plugin step registrations</returns>
        public static IEnumerable<PluginStepRegistration> DiscoverPlugins(
            IEnumerable<Assembly> assemblies,
            Func<Type, IEnumerable<PluginStepRegistration>> customConverter = null)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            var registrations = new List<PluginStepRegistration>();

            foreach (var assembly in assemblies)
            {
                // Find all types that implement IPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && 
                               !t.IsAbstract && 
                               !t.IsInterface)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    IEnumerable<PluginStepRegistration> pluginRegistrations;

                    // Use custom converter if provided
                    if (customConverter != null)
                    {
                        pluginRegistrations = customConverter(pluginType);
                    }
                    else
                    {
                        // Default: Look for SPKL-style attributes
                        pluginRegistrations = ConvertFromAttributes(pluginType);
                    }

                    if (pluginRegistrations != null)
                    {
                        registrations.AddRange(pluginRegistrations);
                    }
                }
            }

            return registrations;
        }

        /// <summary>
        /// Discovers plugins using a custom attribute converter.
        /// This allows users to provide their own logic for extracting registration information from attributes.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan for plugins</param>
        /// <param name="attributeConverter">Function to convert attribute instances to registrations</param>
        /// <param name="attributeType">Type of attribute to look for (e.g., typeof(CrmPluginRegistrationAttribute))</param>
        /// <returns>Collection of plugin step registrations</returns>
        public static IEnumerable<PluginStepRegistration> DiscoverPluginsWithAttributeConverter(
            IEnumerable<Assembly> assemblies,
            Type attributeType,
            Func<Type, Attribute, PluginStepRegistration> attributeConverter)
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

            var registrations = new List<PluginStepRegistration>();

            foreach (var assembly in assemblies)
            {
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && 
                               !t.IsAbstract && 
                               !t.IsInterface)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    // Get all attributes of the specified type
                    var attributes = pluginType.GetCustomAttributes(attributeType, inherit: true);

                    foreach (var attribute in attributes)
                    {
                        var registration = attributeConverter(pluginType, (Attribute)attribute);
                        if (registration != null)
                        {
                            registrations.Add(registration);
                        }
                    }
                }
            }

            return registrations;
        }

        /// <summary>
        /// Converts SPKL CrmPluginRegistrationAttribute(s) to plugin step registrations using reflection.
        /// This method uses duck typing to read attribute properties without requiring a reference to the SPKL package.
        /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl#plugin-registration
        /// 
        /// SPKL CrmPluginRegistrationAttribute properties:
        /// - MessageName: The SDK message name (e.g., "Create", "Update")
        /// - EntityLogicalName: The primary entity logical name
        /// - Stage: Pipeline stage (PreValidation, PreOperation, PostOperation)
        /// - ExecutionOrder: Execution order (rank)
        /// - FilteringAttributes: Comma-separated list of attributes for Update message
        /// - ExecutionMode: Synchronous or Asynchronous
        /// - Offline: Whether the plugin runs offline
        /// - DeleteAsyncOperation: Whether to delete async operation
        /// 
        /// SPKL CrmPluginRegistrationImage properties:
        /// - ImageType: PreImage, PostImage, or Both
        /// - Name: The name/key for accessing the image
        /// - EntityAlias: The entity alias (optional)
        /// - Attributes: Comma-separated list of attributes to include in the image
        /// </summary>
        private static IEnumerable<PluginStepRegistration> ConvertFromAttributes(Type pluginType)
        {
            var registrations = new List<PluginStepRegistration>();

            // Look for CrmPluginRegistrationAttribute using reflection (without referencing the package)
            var stepAttributes = pluginType.GetCustomAttributes(inherit: true)
                .Where(a => a.GetType().Name == "CrmPluginRegistrationAttribute")
                .ToList();

            if (!stepAttributes.Any())
            {
                // No attributes found - return empty list
                return registrations;
            }

            // Look for CrmPluginRegistrationImage attributes
            var imageAttributes = pluginType.GetCustomAttributes(inherit: true)
                .Where(a => a.GetType().Name == "CrmPluginRegistrationImageAttribute" || 
                           a.GetType().Name == "CrmPluginRegistrationImage")
                .ToList();

            foreach (var attribute in stepAttributes)
            {
                try
                {
                    var registration = ConvertAttributeToRegistration(pluginType, attribute);
                    if (registration != null)
                    {
                        // Process image attributes and add them to the registration
                        ProcessImageAttributes(registration, imageAttributes, attribute);
                        registrations.Add(registration);
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue with next attribute
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to convert attribute on {pluginType.FullName}: {ex.Message}");
                }
            }

            return registrations;
        }

        /// <summary>
        /// Converts a single CrmPluginRegistrationAttribute instance to a PluginStepRegistration using reflection.
        /// Uses duck typing to read properties without requiring a package reference.
        /// </summary>
        private static PluginStepRegistration ConvertAttributeToRegistration(Type pluginType, object attribute)
        {
            var attributeType = attribute.GetType();
            
            // Read properties using reflection
            var messageName = GetPropertyValue<string>(attribute, "MessageName") ?? 
                             GetPropertyValue<string>(attribute, "Message");
            var entityLogicalName = GetPropertyValue<string>(attribute, "EntityLogicalName") ?? 
                                   GetPropertyValue<string>(attribute, "EntityName");
            var stageValue = GetPropertyValue<object>(attribute, "Stage");
            var executionOrder = GetPropertyValue<int?>(attribute, "ExecutionOrder") ?? 
                                GetPropertyValue<int?>(attribute, "Rank") ?? 1;
            var filteringAttributes = GetPropertyValue<string>(attribute, "FilteringAttributes");
            var executionModeValue = GetPropertyValue<object>(attribute, "ExecutionMode");

            // Validate required properties
            if (string.IsNullOrEmpty(messageName))
            {
                return null;
            }

            var registration = new PluginStepRegistration
            {
                PluginType = pluginType,
                MessageName = messageName,
                PrimaryEntityName = entityLogicalName ?? string.Empty,
                ExecutionOrder = executionOrder
            };

            // Convert stage enum (handle both string and enum values)
            if (stageValue != null)
            {
                registration.Stage = ConvertToStage(stageValue);
            }

            // Convert execution mode (handle both string and enum values)
            if (executionModeValue != null)
            {
                registration.Mode = ConvertToMode(executionModeValue);
            }

            // Parse filtering attributes
            if (!string.IsNullOrEmpty(filteringAttributes))
            {
                var attributes = filteringAttributes
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a));

                registration.FilteringAttributes = new HashSet<string>(attributes, StringComparer.OrdinalIgnoreCase);
            }

            return registration;
        }

        /// <summary>
        /// Processes CrmPluginRegistrationImage attributes and adds them to the plugin step registration.
        /// Links image attributes to their parent step based on message name and entity.
        /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl#image-registration
        /// </summary>
        private static void ProcessImageAttributes(
            PluginStepRegistration registration, 
            List<object> imageAttributes, 
            object stepAttribute)
        {
            if (imageAttributes == null || !imageAttributes.Any())
            {
                return;
            }

            // Get step identifiers for matching
            var stepMessageName = GetPropertyValue<string>(stepAttribute, "MessageName") ?? 
                                 GetPropertyValue<string>(stepAttribute, "Message");
            var stepEntityName = GetPropertyValue<string>(stepAttribute, "EntityLogicalName") ?? 
                                GetPropertyValue<string>(stepAttribute, "EntityName");

            foreach (var imageAttr in imageAttributes)
            {
                try
                {
                    // Check if this image attribute belongs to this step
                    // Image attributes can specify MessageName and EntityLogicalName to link to specific steps
                    var imageMessageName = GetPropertyValue<string>(imageAttr, "MessageName");
                    var imageEntityName = GetPropertyValue<string>(imageAttr, "EntityLogicalName");

                    // If image specifies message/entity, it must match the step
                    if (!string.IsNullOrEmpty(imageMessageName) && 
                        !string.Equals(imageMessageName, stepMessageName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(imageEntityName) && 
                        !string.Equals(imageEntityName, stepEntityName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var imageRegistration = ConvertImageAttributeToRegistration(imageAttr);
                    if (imageRegistration != null)
                    {
                        // Add to appropriate collection based on image type
                        if (imageRegistration.ImageType == ProcessingStepImageType.PreImage)
                        {
                            registration.PreImages.Add(imageRegistration);
                        }
                        else if (imageRegistration.ImageType == ProcessingStepImageType.PostImage)
                        {
                            registration.PostImages.Add(imageRegistration);
                        }
                        else if (imageRegistration.ImageType == ProcessingStepImageType.Both)
                        {
                            // For "Both", create separate pre and post image registrations
                            var preImage = new PluginStepImageRegistration
                            {
                                Name = imageRegistration.Name,
                                EntityAlias = imageRegistration.EntityAlias,
                                ImageType = ProcessingStepImageType.PreImage,
                                Attributes = new HashSet<string>(imageRegistration.Attributes, StringComparer.OrdinalIgnoreCase)
                            };
                            var postImage = new PluginStepImageRegistration
                            {
                                Name = imageRegistration.Name,
                                EntityAlias = imageRegistration.EntityAlias,
                                ImageType = ProcessingStepImageType.PostImage,
                                Attributes = new HashSet<string>(imageRegistration.Attributes, StringComparer.OrdinalIgnoreCase)
                            };
                            registration.PreImages.Add(preImage);
                            registration.PostImages.Add(postImage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to convert image attribute: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Converts a CrmPluginRegistrationImage attribute to a PluginStepImageRegistration using reflection.
        /// Reference: https://github.com/scottdurow/SparkleXrm/wiki/spkl#image-registration
        /// 
        /// SPKL CrmPluginRegistrationImage properties:
        /// - ImageType: PreImage (0), PostImage (1), or Both (2)
        /// - Name: The name/key for accessing the image in plugin code
        /// - EntityAlias: The entity alias (optional, defaults to Name)
        /// - Attributes: Comma-separated list of attributes to include in the image
        /// </summary>
        private static PluginStepImageRegistration ConvertImageAttributeToRegistration(object imageAttribute)
        {
            // Read image properties using reflection
            var name = GetPropertyValue<string>(imageAttribute, "Name");
            var entityAlias = GetPropertyValue<string>(imageAttribute, "EntityAlias");
            var imageTypeValue = GetPropertyValue<object>(imageAttribute, "ImageType");
            var attributes = GetPropertyValue<string>(imageAttribute, "Attributes");

            // Name is required
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var imageRegistration = new PluginStepImageRegistration
            {
                Name = name,
                EntityAlias = entityAlias ?? name, // Default to name if not specified
                Attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };

            // Convert image type
            if (imageTypeValue != null)
            {
                imageRegistration.ImageType = ConvertToImageType(imageTypeValue);
            }
            else
            {
                // Default to PreImage if not specified
                imageRegistration.ImageType = ProcessingStepImageType.PreImage;
            }

            // Parse attributes
            if (!string.IsNullOrEmpty(attributes))
            {
                var attributeList = attributes
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a));

                imageRegistration.Attributes = new HashSet<string>(attributeList, StringComparer.OrdinalIgnoreCase);
            }

            return imageRegistration;
        }

        /// <summary>
        /// Converts an image type value (string, enum, or int) to ProcessingStepImageType.
        /// </summary>
        private static ProcessingStepImageType ConvertToImageType(object imageTypeValue)
        {
            if (imageTypeValue == null)
            {
                return ProcessingStepImageType.PreImage; // Default
            }

            // Handle string values
            if (imageTypeValue is string imageTypeString)
            {
                if (imageTypeString.Contains("Pre", StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingStepImageType.PreImage;
                }
                if (imageTypeString.Contains("Post", StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingStepImageType.PostImage;
                }
                if (imageTypeString.Contains("Both", StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingStepImageType.Both;
                }
            }

            // Handle numeric values
            if (imageTypeValue is int imageTypeInt)
            {
                switch (imageTypeInt)
                {
                    case 0: return ProcessingStepImageType.PreImage;
                    case 1: return ProcessingStepImageType.PostImage;
                    case 2: return ProcessingStepImageType.Both;
                }
            }

            // Handle enum values
            var imageTypeName = imageTypeValue.ToString();
            if (Enum.TryParse<ProcessingStepImageType>(imageTypeName, true, out var result))
            {
                return result;
            }

            // Default
            return ProcessingStepImageType.PreImage;
        }

        /// <summary>
        /// Gets a property value from an object using reflection
        /// </summary>
        private static T GetPropertyValue<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName, 
                BindingFlags.Public | BindingFlags.Instance);
            
            if (property == null)
            {
                return default(T);
            }

            var value = property.GetValue(obj);
            if (value == null)
            {
                return default(T);
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Converts a stage value (string or enum) to ProcessingStepStage
        /// </summary>
        private static ProcessingStepStage ConvertToStage(object stageValue)
        {
            if (stageValue == null)
            {
                return ProcessingStepStage.Preoperation; // Default
            }

            // Handle string values
            if (stageValue is string stageString)
            {
                if (stageString.Contains("PreValidation", StringComparison.OrdinalIgnoreCase) ||
                    stageString.Contains("10"))
                {
                    return ProcessingStepStage.Prevalidation;
                }
                if (stageString.Contains("PreOperation", StringComparison.OrdinalIgnoreCase) ||
                    stageString.Contains("20"))
                {
                    return ProcessingStepStage.Preoperation;
                }
                if (stageString.Contains("PostOperation", StringComparison.OrdinalIgnoreCase) ||
                    stageString.Contains("40"))
                {
                    return ProcessingStepStage.Postoperation;
                }
            }

            // Handle numeric values
            if (stageValue is int stageInt)
            {
                switch (stageInt)
                {
                    case 10: return ProcessingStepStage.Prevalidation;
                    case 20: return ProcessingStepStage.Preoperation;
                    case 40: return ProcessingStepStage.Postoperation;
                }
            }

            // Handle enum values (from SPKL or similar)
            var stageName = stageValue.ToString();
            if (Enum.TryParse<ProcessingStepStage>(stageName, true, out var result))
            {
                return result;
            }

            // Default
            return ProcessingStepStage.Preoperation;
        }

        /// <summary>
        /// Converts an execution mode value (string or enum) to ProcessingStepMode
        /// </summary>
        private static ProcessingStepMode ConvertToMode(object modeValue)
        {
            if (modeValue == null)
            {
                return ProcessingStepMode.Synchronous; // Default
            }

            // Handle string values
            if (modeValue is string modeString)
            {
                if (modeString.Contains("Async", StringComparison.OrdinalIgnoreCase) ||
                    modeString.Contains("1"))
                {
                    return ProcessingStepMode.Asynchronous;
                }
            }

            // Handle numeric values
            if (modeValue is int modeInt)
            {
                return modeInt == 1 ? ProcessingStepMode.Asynchronous : ProcessingStepMode.Synchronous;
            }

            // Handle enum values
            var modeName = modeValue.ToString();
            if (Enum.TryParse<ProcessingStepMode>(modeName, true, out var result))
            {
                return result;
            }

            // Default
            return ProcessingStepMode.Synchronous;
        }
    }
}
