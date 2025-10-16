using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Integrity;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.MetadataPersistence
{
    /// <summary>
    /// Tests for metadata persistence to EntityDefinition and Attribute tables.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata
    /// 
    /// In Dataverse, metadata is accessible through special virtual entities:
    /// - EntityDefinition (entitydefinition) - Contains entity metadata
    /// - Attribute (attribute) - Contains attribute metadata
    /// 
    /// These tests verify that when entity metadata is initialized, it's also persisted
    /// to these standard tables so it can be queried like regular entity data.
    /// 
    /// Note: System entity metadata (including entitydefinition and attribute tables) is 
    /// automatically initialized in the context constructor, so no explicit initialization is needed.
    /// </summary>
    public class MetadataPersistenceTests : Fake4DataverseTests
    {

        [Fact]
        public void Should_Persist_EntityMetadata_To_EntityDefinition_Table_When_Metadata_Initialized()
        {
            // Arrange
            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "testentity",
                SchemaName = "TestEntity"
            };
            entityMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "testentityid");
            entityMetadata.SetSealedPropertyValue("PrimaryNameAttribute", "name");

            // Act
            _context.InitializeMetadata(entityMetadata);

            // Assert - Verify metadata is in dictionary
            var retrievedMetadata = _context.GetEntityMetadataByName("testentity");
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("testentity", retrievedMetadata.LogicalName);

            // Assert - Verify metadata is persisted to entitydefinition table
            var query = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("logicalname", ConditionOperator.Equal, "testentity")
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);
            Assert.Single(results.Entities);
            
            var entityDefRecord = results.Entities[0];
            Assert.Equal("testentity", entityDefRecord.GetAttributeValue<string>("logicalname"));
            Assert.Equal("TestEntity", entityDefRecord.GetAttributeValue<string>("schemaname"));
        }

        [Fact]
        public void Should_Persist_AttributeMetadata_To_Attribute_Table_When_Metadata_Initialized()
        {
            // Arrange
            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "testentity",
                SchemaName = "TestEntity"
            };
            entityMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "testentityid");

            var stringMetadata = new StringAttributeMetadata()
            {
                SchemaName = "name",
                LogicalName = "name",
                MaxLength = 100
            };
            stringMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());
            stringMetadata.SetSealedPropertyValue("IsValidForCreate", true);
            stringMetadata.SetSealedPropertyValue("IsValidForUpdate", true);
            stringMetadata.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetAttributeCollection(new[] { stringMetadata });

            // Act
            _context.InitializeMetadata(entityMetadata);

            // Assert - Verify attribute metadata is persisted to attribute table
            var query = new QueryExpression("attribute")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("entitylogicalname", ConditionOperator.Equal, "testentity"),
                        new ConditionExpression("logicalname", ConditionOperator.Equal, "name")
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);
            Assert.Single(results.Entities);
            
            var attrRecord = results.Entities[0];
            Assert.Equal("name", attrRecord.GetAttributeValue<string>("logicalname"));
            Assert.Equal("testentity", attrRecord.GetAttributeValue<string>("entitylogicalname"));
            Assert.Equal(100, attrRecord.GetAttributeValue<int>("maxlength"));
            Assert.True(attrRecord.GetAttributeValue<bool>("isvalidforcreate"));
        }

        [Fact]
        public void Should_Update_EntityDefinition_Record_When_Metadata_Updated()
        {
            // Arrange
            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "testentity",
                SchemaName = "TestEntity"
            };
            entityMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());
            
            _context.InitializeMetadata(entityMetadata);

            // Act - Update metadata
            var updatedMetadata = new EntityMetadata()
            {
                LogicalName = "testentity",
                SchemaName = "UpdatedTestEntity"
            };
            updatedMetadata.SetSealedPropertyValue("MetadataId", entityMetadata.MetadataId);
            _context.SetEntityMetadata(updatedMetadata);

            // Assert - Verify only one record exists and it's updated
            var query = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("logicalname", ConditionOperator.Equal, "testentity")
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);
            Assert.Single(results.Entities);
            
            var entityDefRecord = results.Entities[0];
            Assert.Equal("UpdatedTestEntity", entityDefRecord.GetAttributeValue<string>("schemaname"));
        }

        [Fact]
        public void Should_Have_Metadata_Tables_Initialized_Automatically()
        {
            // Arrange - Create new context
            var context = XrmFakedContextFactory.New(new IntegrityOptions
            {
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false
            });
            var service = context.GetOrganizationService();

            // Assert - Metadata tables should be automatically initialized
            var entityDefMetadata = context.GetEntityMetadataByName("entity");
            Assert.NotNull(entityDefMetadata);
            Assert.Equal("entity", entityDefMetadata.LogicalName);
            
            var attributeMetadata = context.GetEntityMetadataByName("attribute");
            Assert.NotNull(attributeMetadata);
            Assert.Equal("attribute", attributeMetadata.LogicalName);
            
            // Verify we can query the tables
            var query = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet("logicalname")
            };
            
            var results = service.RetrieveMultiple(query);
            Assert.NotNull(results);
            Assert.NotEmpty(results.Entities);
        }

        [Fact]
        public void Should_Query_All_EntityDefinitions()
        {
            // Arrange - Initialize multiple entity metadata
            var entity1 = new EntityMetadata()
            {
                LogicalName = "entity1",
                SchemaName = "Entity1"
            };
            entity1.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

            var entity2 = new EntityMetadata()
            {
                LogicalName = "entity2",
                SchemaName = "Entity2"
            };
            entity2.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

            _context.InitializeMetadata(new[] { entity1, entity2 });

            // Act - Query all entity definitions
            var query = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet("logicalname", "schemaname")
            };

            var results = _service.RetrieveMultiple(query);

            // Assert - Should include our test entities plus system entities
            Assert.True(results.Entities.Count >= 2);
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("logicalname") == "entity1");
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("logicalname") == "entity2");
        }

        [Fact]
        public void Should_Query_Attributes_By_EntityLogicalName()
        {
            // Arrange
            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "testentity",
                SchemaName = "TestEntity"
            };
            entityMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

            var attr1 = new StringAttributeMetadata()
            {
                LogicalName = "name",
                SchemaName = "Name",
                MaxLength = 100
            };
            attr1.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

            var attr2 = new IntegerAttributeMetadata()
            {
                LogicalName = "count",
                SchemaName = "Count"
            };
            attr2.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

            entityMetadata.SetAttributeCollection(new AttributeMetadata[] { attr1, attr2 });
            _context.InitializeMetadata(entityMetadata);

            // Act - Query attributes for entity
            var query = new QueryExpression("attribute")
            {
                ColumnSet = new ColumnSet("logicalname", "schemaname", "entitylogicalname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("entitylogicalname", ConditionOperator.Equal, "testentity")
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Assert
            Assert.Equal(2, results.Entities.Count);
            Assert.All(results.Entities, e => 
                Assert.Equal("testentity", e.GetAttributeValue<string>("entitylogicalname")));
        }
    }
}
