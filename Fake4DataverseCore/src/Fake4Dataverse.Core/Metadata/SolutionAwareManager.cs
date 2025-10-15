using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.Metadata
{
    /// <summary>
    /// Manages solution-aware tables and their special columns.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
    /// 
    /// Solution-aware tables have special columns that enable solution management:
    /// - solutionid (Guid) - Associates the component with a solution
    /// - overwritetime (DateTime) - Tracks when the component was last overwritten
    /// - componentstate (int) - State of the component (0=Published, 1=Unpublished, 2=Deleted, 3=Deleted Unpublished)
    /// - ismanaged (bool) - Whether the component is managed by a solution
    /// - [entityname]idunique (Guid) - Unique identifier of the component
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/componentdefinition-entity
    /// The componentdefinition table stores information about which entities are solution-aware.
    /// </summary>
    internal static class SolutionAwareManager
    {
        /// <summary>
        /// System entities that are solution-aware by default.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
        /// </summary>
        private static readonly Dictionary<string, int> DefaultSolutionAwareEntities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Component Type reference from Microsoft documentation
            { "savedquery", 26 },           // SavedQuery = 26
            { "systemform", 60 },           // SystemForm = 60
            { "webresource", 61 },          // WebResource = 61
            { "sitemap", 62 },              // SiteMap = 62
            { "appmodule", 80 },            // AppModule = 80
            { "appmodulecomponent", 103 },  // AppModuleComponent = 103
        };
        
        /// <summary>
        /// Initializes the componentdefinition table with default solution-aware entities.
        /// This method should be called after the componentdefinition entity metadata is loaded.
        /// </summary>
        public static void InitializeComponentDefinitions(XrmFakedContext context)
        {
            // Ensure componentdefinition table exists
            if (!context.Data.ContainsKey("componentdefinition"))
            {
                context.Data["componentdefinition"] = new Dictionary<Guid, Entity>();
            }
            
            // Add default solution-aware entities to componentdefinition
            foreach (var entityInfo in DefaultSolutionAwareEntities)
            {
                var entityName = entityInfo.Key;
                var componentType = entityInfo.Value;
                
                // Check if entity metadata exists
                var entityMetadata = context.GetEntityMetadataByName(entityName);
                if (entityMetadata == null)
                    continue;
                
                // Create componentdefinition record
                var componentDef = new Entity("componentdefinition")
                {
                    Id = Guid.NewGuid()
                };
                
                componentDef["componentdefinitionid"] = componentDef.Id;
                componentDef["logicalname"] = entityName;
                componentDef["primaryentityname"] = entityName;
                componentDef["objecttypecode"] = entityMetadata.ObjectTypeCode ?? componentType;
                componentDef["issolutionaware"] = true;
                componentDef["canbeaddedtosolution"] = true;
                
                // Check if this componentdefinition already exists
                var existing = context.Data["componentdefinition"].Values
                    .FirstOrDefault(e => e.GetAttributeValue<string>("logicalname")?.Equals(entityName, StringComparison.OrdinalIgnoreCase) == true);
                
                if (existing == null)
                {
                    context.Data["componentdefinition"][componentDef.Id] = componentDef;
                }
            }
        }
        
        /// <summary>
        /// Checks if an entity is solution-aware by looking it up in componentdefinition table.
        /// </summary>
        public static bool IsSolutionAware(XrmFakedContext context, string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                return false;
            
            // Check if componentdefinition table exists
            if (!context.Data.ContainsKey("componentdefinition"))
                return false;
            
            // Look up in componentdefinition table
            var componentDef = context.Data["componentdefinition"].Values
                .FirstOrDefault(e => 
                    e.GetAttributeValue<string>("logicalname")?.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase) == true &&
                    e.GetAttributeValue<bool?>("issolutionaware") == true);
            
            return componentDef != null;
        }
        
        /// <summary>
        /// Adds solution-aware columns to an entity's metadata if it's marked as solution-aware.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
        /// </summary>
        public static void EnsureSolutionAwareColumns(EntityMetadata entityMetadata, XrmFakedContext context)
        {
            if (entityMetadata == null || string.IsNullOrWhiteSpace(entityMetadata.LogicalName))
                return;
            
            // Check if entity is solution-aware
            if (!IsSolutionAware(context, entityMetadata.LogicalName))
                return;
            
            // Ensure attributes collection exists
            if (entityMetadata.Attributes == null)
            {
                entityMetadata.SetFieldValue("_attributes", new AttributeMetadata[0]);
            }
            
            var attributes = entityMetadata.Attributes.ToList();
            
            // Add solutionid if not present
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.lookupattributemetadata
            if (!attributes.Any(a => a.LogicalName == "solutionid"))
            {
                var solutionIdAttr = new LookupAttributeMetadata
                {
                    LogicalName = "solutionid",
                    SchemaName = "SolutionId",
                    DisplayName = new Label("Solution", 1033),
                    Description = new Label("Unique identifier of the solution.", 1033)
                };
                solutionIdAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Lookup);
                solutionIdAttr.SetFieldValue("_entityLogicalName", entityMetadata.LogicalName);
                solutionIdAttr.SetSealedPropertyValue("IsValidForCreate", false);
                solutionIdAttr.SetSealedPropertyValue("IsValidForUpdate", false);
                solutionIdAttr.SetSealedPropertyValue("IsValidForRead", true);
                attributes.Add(solutionIdAttr);
            }
            
            // Add overwritetime if not present
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.datetimeattributemetadata
            if (!attributes.Any(a => a.LogicalName == "overwritetime"))
            {
                var overwriteTimeAttr = new DateTimeAttributeMetadata
                {
                    LogicalName = "overwritetime",
                    SchemaName = "OverwriteTime",
                    Format = DateTimeFormat.DateAndTime,
                    DisplayName = new Label("Record Overwrite Time", 1033),
                    Description = new Label("Date and time when the record was last overwritten.", 1033)
                };
                overwriteTimeAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.DateTime);
                overwriteTimeAttr.SetFieldValue("_entityLogicalName", entityMetadata.LogicalName);
                overwriteTimeAttr.SetSealedPropertyValue("IsValidForCreate", false);
                overwriteTimeAttr.SetSealedPropertyValue("IsValidForUpdate", false);
                overwriteTimeAttr.SetSealedPropertyValue("IsValidForRead", true);
                attributes.Add(overwriteTimeAttr);
            }
            
            // Add componentstate if not present
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.picklistattributemetadata
            // ComponentState values: 0=Published, 1=Unpublished, 2=Deleted, 3=Deleted Unpublished
            if (!attributes.Any(a => a.LogicalName == "componentstate"))
            {
                var componentStateAttr = new PicklistAttributeMetadata
                {
                    LogicalName = "componentstate",
                    SchemaName = "ComponentState",
                    DisplayName = new Label("Component State", 1033),
                    Description = new Label("For internal use only.", 1033)
                };
                componentStateAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Picklist);
                componentStateAttr.SetFieldValue("_entityLogicalName", entityMetadata.LogicalName);
                componentStateAttr.SetSealedPropertyValue("IsValidForCreate", false);
                componentStateAttr.SetSealedPropertyValue("IsValidForUpdate", false);
                componentStateAttr.SetSealedPropertyValue("IsValidForRead", true);
                attributes.Add(componentStateAttr);
            }
            
            // Add ismanaged if not present
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.booleanattributemetadata
            if (!attributes.Any(a => a.LogicalName == "ismanaged"))
            {
                var isManagedAttr = new BooleanAttributeMetadata
                {
                    LogicalName = "ismanaged",
                    SchemaName = "IsManaged",
                    DisplayName = new Label("Is Managed", 1033),
                    Description = new Label("Indicates whether the component is managed.", 1033)
                };
                isManagedAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Boolean);
                isManagedAttr.SetFieldValue("_entityLogicalName", entityMetadata.LogicalName);
                isManagedAttr.SetSealedPropertyValue("IsValidForCreate", false);
                isManagedAttr.SetSealedPropertyValue("IsValidForUpdate", false);
                isManagedAttr.SetSealedPropertyValue("IsValidForRead", true);
                attributes.Add(isManagedAttr);
            }
            
            // Add [entityname]idunique if not present
            // This is a unique identifier for the component, e.g., sitemapidunique, formidunique
            var uniqueIdAttributeName = $"{entityMetadata.PrimaryIdAttribute?.Replace("id", "")}idunique";
            if (!string.IsNullOrWhiteSpace(entityMetadata.PrimaryIdAttribute) && 
                !attributes.Any(a => a.LogicalName == uniqueIdAttributeName))
            {
                var uniqueIdAttr = new AttributeMetadata
                {
                    LogicalName = uniqueIdAttributeName,
                    SchemaName = uniqueIdAttributeName.Substring(0, 1).ToUpper() + uniqueIdAttributeName.Substring(1),
                    DisplayName = new Label($"{entityMetadata.LogicalName} Unique Id", 1033),
                    Description = new Label("For internal use only.", 1033)
                };
                uniqueIdAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Uniqueidentifier);
                uniqueIdAttr.SetFieldValue("_entityLogicalName", entityMetadata.LogicalName);
                uniqueIdAttr.SetSealedPropertyValue("IsValidForCreate", false);
                uniqueIdAttr.SetSealedPropertyValue("IsValidForUpdate", false);
                uniqueIdAttr.SetSealedPropertyValue("IsValidForRead", true);
                attributes.Add(uniqueIdAttr);
            }
            
            // Update the entity metadata with new attributes
            entityMetadata.SetFieldValue("_attributes", attributes.ToArray());
        }
        
        /// <summary>
        /// Marks an entity as solution-aware by adding it to the componentdefinition table.
        /// </summary>
        public static void MarkEntityAsSolutionAware(XrmFakedContext context, string entityLogicalName, int? componentType = null)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                throw new ArgumentException("Entity logical name cannot be null or empty", nameof(entityLogicalName));
            
            // Ensure componentdefinition table exists
            if (!context.Data.ContainsKey("componentdefinition"))
            {
                context.Data["componentdefinition"] = new Dictionary<Guid, Entity>();
            }
            
            // Check if already exists
            var existing = context.Data["componentdefinition"].Values
                .FirstOrDefault(e => e.GetAttributeValue<string>("logicalname")?.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase) == true);
            
            if (existing != null)
            {
                // Update existing record
                existing["issolutionaware"] = true;
                existing["canbeaddedtosolution"] = true;
                return;
            }
            
            // Get entity metadata to get ObjectTypeCode
            var entityMetadata = context.GetEntityMetadataByName(entityLogicalName);
            var objectTypeCode = componentType ?? entityMetadata?.ObjectTypeCode ?? 10000; // Default to custom range if not found
            
            // Create new componentdefinition record
            var componentDef = new Entity("componentdefinition")
            {
                Id = Guid.NewGuid()
            };
            
            componentDef["componentdefinitionid"] = componentDef.Id;
            componentDef["logicalname"] = entityLogicalName;
            componentDef["primaryentityname"] = entityLogicalName;
            componentDef["objecttypecode"] = objectTypeCode;
            componentDef["issolutionaware"] = true;
            componentDef["canbeaddedtosolution"] = true;
            
            context.Data["componentdefinition"][componentDef.Id] = componentDef;
            
            // Add solution-aware columns to entity metadata if it exists
            if (entityMetadata != null)
            {
                EnsureSolutionAwareColumns(entityMetadata, context);
                context.SetEntityMetadata(entityMetadata);
            }
        }
    }
}
