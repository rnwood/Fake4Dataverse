using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions.Plugins.Enums;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Represents a plugin step image registration that defines entity snapshots available to plugins.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities
    /// 
    /// Entity images provide a snapshot of the entity's data before or after the core operation.
    /// Pre-images contain the entity state before the operation (available for Update and Delete).
    /// Post-images contain the entity state after the operation (available for Create and Update).
    /// 
    /// Images can be configured with:
    /// - Name: The key used to access the image in the EntityImageCollection
    /// - Entity Alias: The alias used in plugin code to reference the image
    /// - Image Type: Pre-image, Post-image, or Both
    /// - Attributes: Specific attributes to include (filtered attributes) or all attributes
    /// </summary>
    public class PluginStepImageRegistration
    {
        /// <summary>
        /// Gets or sets the unique name/key for this image registration.
        /// This is the key used to access the image from PreEntityImages or PostEntityImages collections.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#access-entity-images-in-plug-in-code
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the entity alias for this image.
        /// This is often the same as Name but can be different for clarity in plugin code.
        /// </summary>
        public string EntityAlias { get; set; }

        /// <summary>
        /// Gets or sets the image type (PreImage, PostImage, or Both).
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
        /// - PreImage: Entity state before the core operation (Update, Delete)
        /// - PostImage: Entity state after the core operation (Create, Update)
        /// - Both: Registers both pre and post images with the same configuration
        /// </summary>
        public ProcessingStepImageType ImageType { get; set; }

        /// <summary>
        /// Gets or sets the attributes to include in the image.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#filter-attributes
        /// 
        /// If null or empty, all attributes are included in the image.
        /// If specified, only the listed attributes are included, reducing payload size and improving performance.
        /// Attribute filtering is particularly important for:
        /// - Large entities with many attributes
        /// - Performance-sensitive scenarios
        /// - Limiting data exposure in plugin code
        /// </summary>
        public HashSet<string> Attributes { get; set; }

        /// <summary>
        /// Creates a new plugin step image registration.
        /// </summary>
        public PluginStepImageRegistration()
        {
            Attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a filtered entity from the source entity based on the configured attributes.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#filter-attributes
        /// 
        /// If Attributes is null or empty, returns a clone of the entire source entity.
        /// If Attributes contains specific attributes, returns a new entity with only those attributes.
        /// </summary>
        /// <param name="sourceEntity">The source entity to create the image from</param>
        /// <returns>A filtered entity containing only the configured attributes, or all attributes if no filter is specified</returns>
        public Entity CreateFilteredImage(Entity sourceEntity)
        {
            if (sourceEntity == null)
            {
                return null;
            }

            // If no attributes are specified, include all attributes
            if (Attributes == null || Attributes.Count == 0)
            {
                // Clone the entire entity
                var clonedEntity = new Entity(sourceEntity.LogicalName, sourceEntity.Id);
                foreach (var attribute in sourceEntity.Attributes)
                {
                    clonedEntity[attribute.Key] = attribute.Value;
                }
                return clonedEntity;
            }

            // Create filtered entity with only specified attributes
            var filteredEntity = new Entity(sourceEntity.LogicalName, sourceEntity.Id);
            foreach (var attributeName in Attributes)
            {
                if (sourceEntity.Contains(attributeName))
                {
                    filteredEntity[attributeName] = sourceEntity[attributeName];
                }
            }

            return filteredEntity;
        }

        /// <summary>
        /// Determines whether this image registration applies to the given message.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
        /// 
        /// Pre-images are available for: Update, Delete
        /// Post-images are available for: Create, Update
        /// </summary>
        /// <param name="messageName">The SDK message name</param>
        /// <param name="isPreImage">True to check if pre-image is available, false for post-image</param>
        /// <returns>True if the image type is valid for the message; otherwise false</returns>
        public bool IsValidForMessage(string messageName, bool isPreImage)
        {
            if (string.IsNullOrEmpty(messageName))
            {
                return false;
            }

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-entities#when-to-use-pre-or-post-images
            // Pre-images: Available for Update and Delete messages
            // Post-images: Available for Create and Update messages
            
            if (isPreImage)
            {
                // Pre-image availability
                bool messageSupportsPreImage = messageName.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                                               messageName.Equals("Delete", StringComparison.OrdinalIgnoreCase);
                
                return messageSupportsPreImage && 
                       (ImageType == ProcessingStepImageType.PreImage || ImageType == ProcessingStepImageType.Both);
            }
            else
            {
                // Post-image availability
                bool messageSupportsPostImage = messageName.Equals("Create", StringComparison.OrdinalIgnoreCase) ||
                                                messageName.Equals("Update", StringComparison.OrdinalIgnoreCase);
                
                return messageSupportsPostImage && 
                       (ImageType == ProcessingStepImageType.PostImage || ImageType == ProcessingStepImageType.Both);
            }
        }
    }
}
