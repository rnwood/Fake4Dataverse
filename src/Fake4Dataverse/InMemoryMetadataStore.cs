using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse
{
    /// <summary>
    /// Thread-safe in-memory store for entity and attribute metadata.
    /// Supports fluent configuration, validation, and auto-discovery.
    /// </summary>
    public sealed class InMemoryMetadataStore
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, EntityMetadataInfo> _entities =
            new Dictionary<string, EntityMetadataInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, OneToManyRelationshipInfo> _oneToManyRelationships =
            new Dictionary<string, OneToManyRelationshipInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ManyToManyRelationshipInfo> _manyToManyRelationships =
            new Dictionary<string, ManyToManyRelationshipInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets whether metadata is automatically inferred from entities on Create.
        /// When <c>true</c>, attribute types are discovered from the values in created entities.
        /// </summary>
        public bool AutoDiscoverMetadata { get; set; }

        /// <summary>
        /// Adds entity metadata and returns a fluent builder for further configuration.
        /// If metadata for the entity already exists, the existing metadata is returned for modification.
        /// </summary>
        public EntityMetadataBuilder AddEntity(string logicalName)
        {
            if (string.IsNullOrEmpty(logicalName))
                throw new ArgumentException("Entity logical name is required.", nameof(logicalName));

            lock (_lock)
            {
                if (!_entities.TryGetValue(logicalName, out var entity))
                {
                    entity = new EntityMetadataInfo(logicalName);
                    _entities[logicalName] = entity;
                }
                return new EntityMetadataBuilder(entity, this);
            }
        }

        /// <summary>
        /// Registers a one-to-many (1:N) relationship.
        /// </summary>
        public void AddOneToManyRelationship(
            string schemaName,
            string referencedEntity,
            string referencedAttribute,
            string referencingEntity,
            string referencingAttribute)
        {
            var info = new OneToManyRelationshipInfo(
                schemaName, referencedEntity, referencedAttribute, referencingEntity, referencingAttribute);
            lock (_lock)
            {
                _oneToManyRelationships[schemaName] = info;
            }
        }

        /// <summary>
        /// Registers a one-to-many (1:N) relationship with cascade configuration.
        /// </summary>
        public void AddOneToManyRelationship(
            string schemaName,
            string referencedEntity,
            string referencedAttribute,
            string referencingEntity,
            string referencingAttribute,
            Metadata.CascadeConfiguration cascade)
        {
            var info = new OneToManyRelationshipInfo(
                schemaName, referencedEntity, referencedAttribute, referencingEntity, referencingAttribute, cascade);
            lock (_lock)
            {
                _oneToManyRelationships[schemaName] = info;
            }
        }

        /// <summary>
        /// Registers a many-to-many (N:N) relationship.
        /// </summary>
        public void AddManyToManyRelationship(
            string schemaName,
            string entity1LogicalName,
            string entity2LogicalName,
            string? intersectEntityName = null)
        {
            var info = new ManyToManyRelationshipInfo(schemaName, entity1LogicalName, entity2LogicalName, intersectEntityName);
            lock (_lock)
            {
                _manyToManyRelationships[schemaName] = info;
            }
        }

        internal EntityMetadataInfo? GetEntityMetadataInfo(string logicalName)
        {
            lock (_lock)
            {
                _entities.TryGetValue(logicalName, out var entity);
                return entity;
            }
        }

        internal IReadOnlyList<EntityMetadataInfo> GetAllEntityMetadataInfo()
        {
            lock (_lock)
            {
                return _entities.Values.ToList();
            }
        }

        /// <summary>
        /// Gets all 1:N relationships where the specified entity is the parent (referenced entity).
        /// </summary>
        internal IReadOnlyList<OneToManyRelationshipInfo> GetChildRelationships(string parentEntity)
        {
            lock (_lock)
            {
                return _oneToManyRelationships.Values
                    .Where(r => string.Equals(r.ReferencedEntity, parentEntity, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        internal IReadOnlyList<OneToManyRelationshipInfo> GetOneToManyRelationships(string? entityLogicalName = null)
        {
            lock (_lock)
            {
                if (entityLogicalName == null)
                    return _oneToManyRelationships.Values.ToList();

                return _oneToManyRelationships.Values
                    .Where(r => string.Equals(r.ReferencedEntity, entityLogicalName, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(r.ReferencingEntity, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        internal IReadOnlyList<ManyToManyRelationshipInfo> GetManyToManyRelationships(string? entityLogicalName = null)
        {
            lock (_lock)
            {
                if (entityLogicalName == null)
                    return _manyToManyRelationships.Values.ToList();

                return _manyToManyRelationships.Values
                    .Where(r => string.Equals(r.Entity1LogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(r.Entity2LogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        internal void ValidateOnCreate(Entity entity)
        {
            lock (_lock)
            {
                if (!_entities.TryGetValue(entity.LogicalName, out var entityMeta))
                    return;

                // Check required fields are present and non-null.
                foreach (var attrMeta in entityMeta.Attributes.Values)
                {
                    if (attrMeta.RequiredLevel == AttributeRequiredLevel.SystemRequired
                        || attrMeta.RequiredLevel == AttributeRequiredLevel.ApplicationRequired)
                    {
                        if (!entity.Contains(attrMeta.LogicalName) || entity[attrMeta.LogicalName] == null)
                        {
                            throw DataverseFault.Create(
                                DataverseFault.InvalidArgument,
                                $"Required attribute '{attrMeta.LogicalName}' is missing on create of '{entity.LogicalName}'.");
                        }
                    }
                }

                // Validate constraints on provided attributes.
                ValidateAttributeConstraints(entity, entityMeta);
            }
        }

        internal void ValidateOnUpdate(Entity entity)
        {
            lock (_lock)
            {
                if (!_entities.TryGetValue(entity.LogicalName, out var entityMeta))
                    return;

                // On update, if a required field is being set to null, reject it.
                foreach (var attr in entity.Attributes)
                {
                    if (entityMeta.Attributes.TryGetValue(attr.Key, out var attrMeta))
                    {
                        if ((attrMeta.RequiredLevel == AttributeRequiredLevel.SystemRequired
                             || attrMeta.RequiredLevel == AttributeRequiredLevel.ApplicationRequired)
                            && attr.Value == null)
                        {
                            throw DataverseFault.Create(
                                DataverseFault.InvalidArgument,
                                $"Required attribute '{attrMeta.LogicalName}' cannot be set to null on update of '{entity.LogicalName}'.");
                        }
                    }
                }

                ValidateAttributeConstraints(entity, entityMeta);
            }
        }

        internal void AutoDiscover(Entity entity)
        {
            if (!AutoDiscoverMetadata) return;

            lock (_lock)
            {
                if (!_entities.TryGetValue(entity.LogicalName, out var entityMeta))
                {
                    entityMeta = new EntityMetadataInfo(entity.LogicalName);
                    _entities[entity.LogicalName] = entityMeta;
                }

                foreach (var attr in entity.Attributes)
                {
                    if (entityMeta.Attributes.ContainsKey(attr.Key)) continue;
                    if (attr.Value == null) continue;

                    var inferred = InferAttributeType(attr.Value);
                    if (inferred.HasValue)
                    {
                        entityMeta.Attributes[attr.Key] = new AttributeMetadataInfo(attr.Key, inferred.Value);
                    }
                }
            }
        }

        internal void ValidateRelationship(
            string entityName,
            Relationship relationship,
            EntityReferenceCollection relatedEntities)
        {
            lock (_lock)
            {
                // Check one-to-many relationships.
                if (_oneToManyRelationships.TryGetValue(relationship.SchemaName, out var otm))
                {
                    foreach (var related in relatedEntities)
                    {
                        bool valid =
                            (string.Equals(otm.ReferencedEntity, entityName, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(otm.ReferencingEntity, related.LogicalName, StringComparison.OrdinalIgnoreCase))
                            ||
                            (string.Equals(otm.ReferencingEntity, entityName, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(otm.ReferencedEntity, related.LogicalName, StringComparison.OrdinalIgnoreCase));

                        if (!valid)
                        {
                            throw DataverseFault.Create(
                                DataverseFault.InvalidArgument,
                                $"Entity '{related.LogicalName}' is not valid for relationship '{relationship.SchemaName}' with entity '{entityName}'.");
                        }
                    }
                    return;
                }

                // Check many-to-many relationships.
                if (_manyToManyRelationships.TryGetValue(relationship.SchemaName, out var mtm))
                {
                    foreach (var related in relatedEntities)
                    {
                        bool valid =
                            (string.Equals(mtm.Entity1LogicalName, entityName, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(mtm.Entity2LogicalName, related.LogicalName, StringComparison.OrdinalIgnoreCase))
                            ||
                            (string.Equals(mtm.Entity2LogicalName, entityName, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(mtm.Entity1LogicalName, related.LogicalName, StringComparison.OrdinalIgnoreCase));

                        if (!valid)
                        {
                            throw DataverseFault.Create(
                                DataverseFault.InvalidArgument,
                                $"Entity '{related.LogicalName}' is not valid for relationship '{relationship.SchemaName}' with entity '{entityName}'.");
                        }
                    }
                }

                // If the relationship schema name is not defined in metadata, skip validation.
            }
        }

        private static void ValidateAttributeConstraints(Entity entity, EntityMetadataInfo entityMeta)
        {
            foreach (var attr in entity.Attributes)
            {
                if (attr.Value == null) continue;
                if (!entityMeta.Attributes.TryGetValue(attr.Key, out var attrMeta)) continue;

                switch (attrMeta.AttributeType)
                {
                    case AttributeTypeCode.String:
                    case AttributeTypeCode.Memo:
                        ValidateStringLength(attr.Value, attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Integer:
                        ValidateNumericRange(Convert.ToDouble(attr.Value), attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Decimal:
                        if (attr.Value is decimal decVal)
                            ValidateNumericRange((double)decVal, attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Double:
                        if (attr.Value is double dblVal)
                            ValidateNumericRange(dblVal, attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Money:
                        if (attr.Value is Money moneyVal)
                            ValidateNumericRange((double)moneyVal.Value, attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Picklist:
                        ValidateOptionSetValue(attr.Value, attrMeta, entity.LogicalName);
                        break;

                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Owner:
                        ValidateEntityReferenceTarget(attr.Value, attrMeta, entity.LogicalName);
                        break;
                }
            }
        }

        private static void ValidateStringLength(object value, AttributeMetadataInfo attrMeta, string entityName)
        {
            if (!attrMeta.MaxLength.HasValue) return;
            if (value is string str && str.Length > attrMeta.MaxLength.Value)
            {
                throw DataverseFault.Create(
                    DataverseFault.InvalidArgument,
                    $"Attribute '{attrMeta.LogicalName}' on '{entityName}' exceeds maximum length of {attrMeta.MaxLength.Value}. Actual length: {str.Length}.");
            }
        }

        private static void ValidateNumericRange(double numericValue, AttributeMetadataInfo attrMeta, string entityName)
        {
            if (attrMeta.MinValue.HasValue && numericValue < attrMeta.MinValue.Value)
            {
                throw DataverseFault.Create(
                    DataverseFault.InvalidArgument,
                    $"Attribute '{attrMeta.LogicalName}' on '{entityName}' value {numericValue} is below minimum {attrMeta.MinValue.Value}.");
            }
            if (attrMeta.MaxValue.HasValue && numericValue > attrMeta.MaxValue.Value)
            {
                throw DataverseFault.Create(
                    DataverseFault.InvalidArgument,
                    $"Attribute '{attrMeta.LogicalName}' on '{entityName}' value {numericValue} is above maximum {attrMeta.MaxValue.Value}.");
            }
        }

        private static void ValidateOptionSetValue(object value, AttributeMetadataInfo attrMeta, string entityName)
        {
            if (attrMeta.ValidOptionSetValues == null) return;
            if (value is OptionSetValue osv && !attrMeta.ValidOptionSetValues.Contains(osv.Value))
            {
                throw DataverseFault.Create(
                    DataverseFault.InvalidArgument,
                    $"Attribute '{attrMeta.LogicalName}' on '{entityName}' has invalid option set value {osv.Value}.");
            }
        }

        private static void ValidateEntityReferenceTarget(object value, AttributeMetadataInfo attrMeta, string entityName)
        {
            if (attrMeta.ValidTargetEntityTypes == null) return;
            if (value is EntityReference er && !attrMeta.ValidTargetEntityTypes.Contains(er.LogicalName))
            {
                throw DataverseFault.Create(
                    DataverseFault.InvalidArgument,
                    $"Attribute '{attrMeta.LogicalName}' on '{entityName}' does not accept entity type '{er.LogicalName}'.");
            }
        }

        private static AttributeTypeCode? InferAttributeType(object value)
        {
            switch (value)
            {
                case string _: return AttributeTypeCode.String;
                case int _: return AttributeTypeCode.Integer;
                case decimal _: return AttributeTypeCode.Decimal;
                case double _: return AttributeTypeCode.Double;
                case float _: return AttributeTypeCode.Double;
                case bool _: return AttributeTypeCode.Boolean;
                case DateTime _: return AttributeTypeCode.DateTime;
                case Guid _: return AttributeTypeCode.Uniqueidentifier;
                case Money _: return AttributeTypeCode.Money;
                case OptionSetValue _: return AttributeTypeCode.Picklist;
                case EntityReference _: return AttributeTypeCode.Lookup;
                case EntityCollection _: return AttributeTypeCode.PartyList;
                default: return null;
            }
        }
    }
}
