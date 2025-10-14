namespace Fake4Dataverse.Abstractions.Integrity
{
    public interface IIntegrityOptions
    {
        //If true, will validate that when adding / updating an entity reference property the associated record will exist
        bool ValidateEntityReferences { get; set; }

        /// <summary>
        /// If true, validates that attribute values match their metadata types and that lookup targets are valid.
        /// This requires metadata to be initialized for all entities being used.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
        /// </summary>
        bool ValidateAttributeTypes { get; set; }
    }
}
