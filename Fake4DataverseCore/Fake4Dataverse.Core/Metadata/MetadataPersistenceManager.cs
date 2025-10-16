using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Extensions;

namespace Fake4Dataverse.Metadata
{
    /// <summary>
    /// Manages persistence of entity and attribute metadata to/from Dataverse standard metadata tables.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata
    /// 
    /// In Dataverse, metadata is accessible through special virtual entities:
    /// - EntityDefinition (entity) - Contains entity metadata (accessed via REST as EntityDefinition)
    /// - Attribute (attribute) - Contains attribute metadata
    /// 
    /// This class converts between EntityMetadata objects and entity records in these tables,
    /// allowing metadata to be stored and queried like regular entity data.
    /// </summary>
    internal class MetadataPersistenceManager
    {
        /// <summary>
        /// Converts EntityMetadata to EntityDefinition entity record.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata
        /// </summary>
        public static Entity EntityMetadataToEntityDefinition(EntityMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            
            var entity = new Entity("entity")
            {
                Id = metadata.MetadataId ?? Guid.NewGuid()
            };
            
            entity["metadataid"] = entity.Id;
            entity["logicalname"] = metadata.LogicalName;
            entity["schemaname"] = metadata.SchemaName;
            entity["objecttypecode"] = metadata.ObjectTypeCode;
            entity["iscustomentity"] = metadata.IsCustomEntity;
            entity["ismanaged"] = metadata.IsManaged;
            entity["primaryidattribute"] = metadata.PrimaryIdAttribute;
            entity["primarynameattribute"] = metadata.PrimaryNameAttribute;
            entity["primaryimageattribute"] = metadata.PrimaryImageAttribute;
            
            if (metadata.IsCustomizable != null)
                entity["iscustomizable"] = metadata.IsCustomizable.Value;
            
            if (metadata.IsActivity != null)
                entity["isactivity"] = metadata.IsActivity.Value;
            
            if (metadata.IsValidForQueue != null)
                entity["isvalidforqueue"] = metadata.IsValidForQueue.Value;
            
            if (metadata.IsAuditEnabled != null)
                entity["isauditenabled"] = metadata.IsAuditEnabled.Value;
            
            if (metadata.IsBusinessProcessEnabled != null)
                entity["isbusinessprocessenabled"] = metadata.IsBusinessProcessEnabled;
            
            if (metadata.IsValidForAdvancedFind != null)
                entity["isvalidforadvancedfind"] = metadata.IsValidForAdvancedFind.Value;
            
            if (metadata.OwnershipType != null)
                entity["ownershiptype"] = (int)metadata.OwnershipType.Value;
            
            if (metadata.DisplayName?.UserLocalizedLabel?.Label != null)
                entity["displayname"] = metadata.DisplayName.UserLocalizedLabel.Label;
            
            if (metadata.DisplayCollectionName?.UserLocalizedLabel?.Label != null)
                entity["pluralname"] = metadata.DisplayCollectionName.UserLocalizedLabel.Label;
            
            if (metadata.Description?.UserLocalizedLabel?.Label != null)
                entity["description"] = metadata.Description.UserLocalizedLabel.Label;
            
            return entity;
        }
        
        /// <summary>
        /// Converts AttributeMetadata to Attribute entity record.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata
        /// </summary>
        public static Entity AttributeMetadataToAttribute(AttributeMetadata metadata, string entityLogicalName)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                throw new ArgumentException("Entity logical name is required", nameof(entityLogicalName));
            
            var entity = new Entity("attribute")
            {
                Id = metadata.MetadataId ?? Guid.NewGuid()
            };
            
            entity["metadataid"] = entity.Id;
            entity["logicalname"] = metadata.LogicalName;
            entity["schemaname"] = metadata.SchemaName;
            entity["entitylogicalname"] = entityLogicalName;
            entity["iscustomattribute"] = metadata.IsCustomAttribute;
            entity["ismanaged"] = metadata.IsManaged;
            entity["isprimaryid"] = metadata.IsPrimaryId;
            entity["isprimaryname"] = metadata.IsPrimaryName;
            
            if (metadata.AttributeType != null)
            {
                entity["attributetype"] = (int)metadata.AttributeType.Value;
                entity["attributetypename"] = metadata.AttributeType.Value.ToString();
            }
            
            if (metadata.IsCustomizable != null)
                entity["iscustomizable"] = metadata.IsCustomizable.Value;
            
            if (metadata.IsValidForCreate != null)
                entity["isvalidforcreate"] = metadata.IsValidForCreate.Value;
            
            if (metadata.IsValidForUpdate != null)
                entity["isvalidforupdate"] = metadata.IsValidForUpdate.Value;
            
            if (metadata.IsValidForRead != null)
                entity["isvalidforread"] = metadata.IsValidForRead.Value;
            
            if (metadata.RequiredLevel != null)
                entity["requiredlevel"] = (int)metadata.RequiredLevel.Value;
            
            if (metadata.IsAuditEnabled != null)
                entity["isauditenabled"] = metadata.IsAuditEnabled.Value;
            
            if (metadata.IsSecured != null)
                entity["issecured"] = metadata.IsSecured.Value;
            
            if (metadata.DisplayName?.UserLocalizedLabel?.Label != null)
                entity["displayname"] = metadata.DisplayName.UserLocalizedLabel.Label;
            
            if (metadata.Description?.UserLocalizedLabel?.Label != null)
                entity["description"] = metadata.Description.UserLocalizedLabel.Label;
            
            // Type-specific properties
            if (metadata is StringAttributeMetadata stringAttr && stringAttr.MaxLength != null)
                entity["maxlength"] = stringAttr.MaxLength.Value;
            
            if (metadata is MemoAttributeMetadata memoAttr && memoAttr.MaxLength != null)
                entity["maxlength"] = memoAttr.MaxLength.Value;
            
            if (metadata is DecimalAttributeMetadata decimalAttr)
            {
                if (decimalAttr.Precision != null)
                    entity["precision"] = decimalAttr.Precision.Value;
                if (decimalAttr.MinValue != null)
                    entity["minvalue"] = decimalAttr.MinValue.Value;
                if (decimalAttr.MaxValue != null)
                    entity["maxvalue"] = decimalAttr.MaxValue.Value;
            }
            
            if (metadata is MoneyAttributeMetadata moneyAttr)
            {
                if (moneyAttr.Precision != null)
                    entity["precision"] = moneyAttr.Precision.Value;
                if (moneyAttr.MinValue != null)
                    entity["minvalue"] = (decimal)moneyAttr.MinValue.Value;
                if (moneyAttr.MaxValue != null)
                    entity["maxvalue"] = (decimal)moneyAttr.MaxValue.Value;
            }
            
            if (metadata is IntegerAttributeMetadata intAttr)
            {
                if (intAttr.MinValue != null)
                    entity["minvalue"] = (decimal)intAttr.MinValue.Value;
                if (intAttr.MaxValue != null)
                    entity["maxvalue"] = (decimal)intAttr.MaxValue.Value;
            }
            
            if (metadata is DoubleAttributeMetadata doubleAttr)
            {
                if (doubleAttr.Precision != null)
                    entity["precision"] = doubleAttr.Precision.Value;
                if (doubleAttr.MinValue != null)
                    entity["minvalue"] = (decimal)doubleAttr.MinValue.Value;
                if (doubleAttr.MaxValue != null)
                    entity["maxvalue"] = (decimal)doubleAttr.MaxValue.Value;
            }
            
            return entity;
        }
        
        /// <summary>
        /// Converts EntityDefinition entity record to EntityMetadata.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata
        /// </summary>
        public static EntityMetadata EntityDefinitionToEntityMetadata(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            if (entity.LogicalName != "entity")
                throw new ArgumentException("Entity must be of type entity", nameof(entity));
            
            var metadata = new EntityMetadata();
            
            if (entity.Contains("metadataid"))
                metadata.SetSealedPropertyValue("MetadataId", entity.GetAttributeValue<Guid>("metadataid"));
            
            if (entity.Contains("logicalname"))
                metadata.SetSealedPropertyValue("LogicalName", entity.GetAttributeValue<string>("logicalname"));
            
            if (entity.Contains("schemaname"))
                metadata.SetSealedPropertyValue("SchemaName", entity.GetAttributeValue<string>("schemaname"));
            
            if (entity.Contains("objecttypecode"))
                metadata.SetSealedPropertyValue("ObjectTypeCode", entity.GetAttributeValue<int?>("objecttypecode"));
            
            if (entity.Contains("iscustomentity"))
                metadata.SetSealedPropertyValue("IsCustomEntity", entity.GetAttributeValue<bool?>("iscustomentity"));
            
            if (entity.Contains("ismanaged"))
                metadata.SetSealedPropertyValue("IsManaged", entity.GetAttributeValue<bool?>("ismanaged"));
            
            if (entity.Contains("primaryidattribute"))
                metadata.SetSealedPropertyValue("PrimaryIdAttribute", entity.GetAttributeValue<string>("primaryidattribute"));
            
            if (entity.Contains("primarynameattribute"))
                metadata.SetSealedPropertyValue("PrimaryNameAttribute", entity.GetAttributeValue<string>("primarynameattribute"));
            
            if (entity.Contains("primaryimageattribute"))
                metadata.SetSealedPropertyValue("PrimaryImageAttribute", entity.GetAttributeValue<string>("primaryimageattribute"));
            
            return metadata;
        }
    }
}
