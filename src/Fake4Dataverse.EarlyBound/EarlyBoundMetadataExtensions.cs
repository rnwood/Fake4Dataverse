using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Fake4Dataverse.EarlyBound
{
    /// <summary>
    /// Extension methods for registering early-bound entity metadata with <see cref="FakeOrganizationService"/>.
    /// </summary>
    public static class EarlyBoundMetadataExtensions
    {
        /// <summary>
        /// Scans the specified assembly for classes decorated with <see cref="EntityLogicalNameAttribute"/>
        /// and registers entity metadata for each one, including attribute logical names from
        /// <see cref="AttributeLogicalNameAttribute"/> properties.
        /// </summary>
        public static void RegisterEarlyBoundEntities(this FakeOrganizationService service, Assembly assembly)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes())
            {
                var entityAttr = type.GetCustomAttribute<EntityLogicalNameAttribute>();
                if (entityAttr == null) continue;

                var entityName = entityAttr.LogicalName;

                // Find primary ID attribute from the property decorated with AttributeLogicalName
                // whose name matches "{entityName}id"
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                string? primaryIdAttribute = null;
                string? primaryNameAttribute = null;
                var attributeNames = new List<string>();

                foreach (var prop in properties)
                {
                    var attrLogicalName = prop.GetCustomAttribute<AttributeLogicalNameAttribute>();
                    if (attrLogicalName == null) continue;

                    var logicalName = attrLogicalName.LogicalName;
                    attributeNames.Add(logicalName);

                    if (logicalName == entityName + "id")
                    {
                        primaryIdAttribute = logicalName;
                    }
                    else if (prop.Name == "Name" || logicalName == "name" || logicalName == entityName + "name" || logicalName == "fullname")
                    {
                        primaryNameAttribute = logicalName;
                    }
                }

                var builder = service.MetadataStore.AddEntity(entityName)
                    .WithPrimaryIdAttribute(primaryIdAttribute ?? entityName + "id");
                if (primaryNameAttribute != null)
                    builder.WithPrimaryNameAttribute(primaryNameAttribute);
            }
        }

        /// <summary>
        /// Registers metadata for a single early-bound entity type.
        /// </summary>
        public static void RegisterEarlyBoundEntity<TEntity>(this FakeOrganizationService service) where TEntity : Entity
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var type = typeof(TEntity);
            var entityAttr = type.GetCustomAttribute<EntityLogicalNameAttribute>()
                ?? throw new ArgumentException($"Type {type.Name} is not decorated with [EntityLogicalName].");

            var entityName = entityAttr.LogicalName;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string? primaryIdAttribute = null;
            string? primaryNameAttribute = null;

            foreach (var prop in properties)
            {
                var attrLogicalName = prop.GetCustomAttribute<AttributeLogicalNameAttribute>();
                if (attrLogicalName == null) continue;

                var logicalName = attrLogicalName.LogicalName;

                if (logicalName == entityName + "id")
                {
                    primaryIdAttribute = logicalName;
                }
                else if (prop.Name == "Name" || logicalName == "name" || logicalName == entityName + "name" || logicalName == "fullname")
                {
                    primaryNameAttribute = logicalName;
                }
            }

            var b = service.MetadataStore.AddEntity(entityName)
                .WithPrimaryIdAttribute(primaryIdAttribute ?? entityName + "id");
            if (primaryNameAttribute != null)
                b.WithPrimaryNameAttribute(primaryNameAttribute);
        }
    }
}
