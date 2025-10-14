using Fake4Dataverse.Abstractions.Integrity;

namespace Fake4Dataverse.Integrity
{
    public class IntegrityOptions : IIntegrityOptions
    {
        public bool ValidateEntityReferences { get; set; }
        
        /// <summary>
        /// If true, validates that attribute values match their metadata types and that lookup targets are valid.
        /// This requires metadata to be initialized for all entities being used.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
        /// </summary>
        public bool ValidateAttributeTypes { get; set; }

        public IntegrityOptions()
        {
            ValidateEntityReferences = true;
            ValidateAttributeTypes = true; // Default to true to match real Dataverse behavior
        }
    }
}
