using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fake4Dataverse.Metadata;
using Fake4Dataverse.Tests;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Metadata
{
    /// <summary>
    /// Tests for CDM (Common Data Model) JSON import functionality.
    /// Reference: https://github.com/microsoft/CDM
    /// 
    /// These tests verify that CDM JSON files can be parsed and converted to EntityMetadata
    /// objects that can be used to initialize metadata in XrmFakedContext.
    /// </summary>
    public class CdmImportTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Parse_Simple_CDM_Entity()
        {
            // Arrange
            // Reference: https://github.com/microsoft/CDM/blob/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon/Account.cdm.json
            // This is a simplified version of the Account entity CDM schema
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""imports"": [],
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Account"",
            ""description"": ""Business entity"",
            ""sourceName"": ""account"",
            ""hasAttributes"": [
                {
                    ""name"": ""accountId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""accountid"",
                    ""isPrimaryKey"": true,
                    ""description"": ""Unique identifier""
                },
                {
                    ""name"": ""name"",
                    ""dataType"": ""string"",
                    ""sourceName"": ""name"",
                    ""maximumLength"": 160,
                    ""description"": ""Account name""
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            Assert.NotNull(entityMetadataList);
            Assert.Single(entityMetadataList);
            
            var entityMetadata = entityMetadataList.First();
            Assert.Equal("account", entityMetadata.LogicalName);
            Assert.Equal("accountid", entityMetadata.PrimaryIdAttribute);
            Assert.NotNull(entityMetadata.Attributes);
            // CDM parser adds 4 system owner attributes (ownerid, owninguser, owningteam, owningbusinessunit) 
            // and 7 system audit attributes (createdon, createdby, modifiedon, modifiedby, statecode, statuscode, overriddencreatedon) to all entities
            Assert.Equal(13, entityMetadata.Attributes.Length);
            
            var accountIdAttr = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "accountid");
            Assert.NotNull(accountIdAttr);
            Assert.Equal("account", accountIdAttr.EntityLogicalName);
            
            var nameAttr = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "name");
            Assert.NotNull(nameAttr);
            Assert.IsType<StringAttributeMetadata>(nameAttr);
            Assert.Equal(160, ((StringAttributeMetadata)nameAttr).MaxLength);
        }

        [Fact]
        public void Should_Initialize_Context_Metadata_From_CDM_File()
        {
            // Arrange
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Contact"",
            ""sourceName"": ""contact"",
            ""hasAttributes"": [
                {
                    ""name"": ""contactId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""contactid"",
                    ""isPrimaryKey"": true
                },
                {
                    ""name"": ""firstName"",
                    ""dataType"": ""string"",
                    ""sourceName"": ""firstname"",
                    ""maximumLength"": 50
                }
            ]
        }
    ]
}";

            var tempFile = WriteTempFile(cdmJson);

            // Act
            _context.InitializeMetadataFromCdmFile(tempFile);

            // Assert
            var metadata = _context.GetEntityMetadataByName("contact");
            Assert.NotNull(metadata);
            Assert.Equal("contact", metadata.LogicalName);
            Assert.Equal("contactid", metadata.PrimaryIdAttribute);
        }

        [Fact]
        public void Should_Parse_Multiple_Entities_From_Single_CDM_File()
        {
            // Arrange
            // CDM files can contain multiple entity definitions
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Account"",
            ""sourceName"": ""account"",
            ""hasAttributes"": [
                {
                    ""name"": ""accountId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""accountid"",
                    ""isPrimaryKey"": true
                }
            ]
        },
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Contact"",
            ""sourceName"": ""contact"",
            ""hasAttributes"": [
                {
                    ""name"": ""contactId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""contactid"",
                    ""isPrimaryKey"": true
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            Assert.NotNull(entityMetadataList);
            Assert.Equal(2, entityMetadataList.Count());
            Assert.Contains(entityMetadataList, e => e.LogicalName == "account");
            Assert.Contains(entityMetadataList, e => e.LogicalName == "contact");
        }

        [Fact]
        public void Should_Handle_Various_Data_Types()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributetypecode
            // AttributeTypeCode defines the supported data types in Dataverse
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""TestEntity"",
            ""sourceName"": ""testentity"",
            ""hasAttributes"": [
                {
                    ""name"": ""id"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""testentityid"",
                    ""isPrimaryKey"": true
                },
                {
                    ""name"": ""textField"",
                    ""dataType"": ""string"",
                    ""sourceName"": ""textfield""
                },
                {
                    ""name"": ""numberField"",
                    ""dataType"": ""integer"",
                    ""sourceName"": ""numberfield""
                },
                {
                    ""name"": ""decimalField"",
                    ""dataType"": ""decimal"",
                    ""sourceName"": ""decimalfield""
                },
                {
                    ""name"": ""dateField"",
                    ""dataType"": ""datetime"",
                    ""sourceName"": ""datefield""
                },
                {
                    ""name"": ""booleanField"",
                    ""dataType"": ""boolean"",
                    ""sourceName"": ""booleanfield""
                },
                {
                    ""name"": ""moneyField"",
                    ""dataType"": ""money"",
                    ""sourceName"": ""moneyfield""
                },
                {
                    ""name"": ""lookupField"",
                    ""dataType"": ""lookup"",
                    ""sourceName"": ""lookupfield""
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            var entityMetadata = entityMetadataList.First();
            // CDM parser adds 4 system owner attributes (ownerid, owninguser, owningteam, owningbusinessunit) 
            // and 7 system audit attributes (createdon, createdby, modifiedon, modifiedby, statecode, statuscode, overriddencreatedon) to all entities
            Assert.Equal(19, entityMetadata.Attributes.Length);
            
            Assert.IsType<StringAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "textfield"));
            Assert.IsType<IntegerAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "numberfield"));
            Assert.IsType<DecimalAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "decimalfield"));
            Assert.IsType<DateTimeAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "datefield"));
            Assert.IsType<BooleanAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "booleanfield"));
            Assert.IsType<MoneyAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "moneyfield"));
            Assert.IsType<LookupAttributeMetadata>(entityMetadata.Attributes.First(a => a.LogicalName == "lookupfield"));
        }

        [Fact]
        public void Should_Use_SourceName_As_LogicalName()
        {
            // Arrange
            // In CDM, the "name" is typically the schema name (PascalCase)
            // and "sourceName" is the logical name used in Dataverse (lowercase)
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Account"",
            ""sourceName"": ""account"",
            ""hasAttributes"": [
                {
                    ""name"": ""AccountId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""accountid"",
                    ""isPrimaryKey"": true
                },
                {
                    ""name"": ""AccountName"",
                    ""dataType"": ""string"",
                    ""sourceName"": ""name""
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            var entityMetadata = entityMetadataList.First();
            Assert.Equal("account", entityMetadata.LogicalName);
            
            var nameAttr = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "name");
            Assert.NotNull(nameAttr);
        }

        [Fact]
        public void Should_Fallback_To_Lowercase_Name_When_SourceName_Missing()
        {
            // Arrange
            // If sourceName is not provided, we should use the name field converted to lowercase
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""CustomEntity"",
            ""hasAttributes"": [
                {
                    ""name"": ""CustomEntityId"",
                    ""dataType"": ""guid"",
                    ""isPrimaryKey"": true
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            var entityMetadata = entityMetadataList.First();
            Assert.Equal("customentity", entityMetadata.LogicalName);
            
            var idAttr = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "customentityid");
            Assert.NotNull(idAttr);
        }

        [Fact]
        public void Should_Initialize_Metadata_From_Multiple_CDM_Files()
        {
            // Arrange
            var cdmJson1 = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Account"",
            ""sourceName"": ""account"",
            ""hasAttributes"": [
                {
                    ""name"": ""accountId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""accountid"",
                    ""isPrimaryKey"": true
                }
            ]
        }
    ]
}";

            var cdmJson2 = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Contact"",
            ""sourceName"": ""contact"",
            ""hasAttributes"": [
                {
                    ""name"": ""contactId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""contactid"",
                    ""isPrimaryKey"": true
                }
            ]
        }
    ]
}";

            var file1 = WriteTempFile(cdmJson1);
            var file2 = WriteTempFile(cdmJson2);

            // Act
            _context.InitializeMetadataFromCdmFiles(new[] { file1, file2 });

            // Assert
            var accountMetadata = _context.GetEntityMetadataByName("account");
            Assert.NotNull(accountMetadata);
            Assert.Equal("account", accountMetadata.LogicalName);
            
            var contactMetadata = _context.GetEntityMetadataByName("contact");
            Assert.NotNull(contactMetadata);
            Assert.Equal("contact", contactMetadata.LogicalName);
        }

        [Fact]
        public void Should_Throw_Exception_For_Invalid_JSON()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                MetadataGenerator.FromCdmJsonFile(WriteTempFile(invalidJson)));
            
            Assert.Contains("Failed to parse CDM JSON", ex.Message);
        }

        [Fact]
        public void Should_Throw_Exception_For_Missing_File()
        {
            // Arrange
            var nonExistentPath = "/tmp/nonexistent_cdm_file.json";

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() =>
                MetadataGenerator.FromCdmJsonFile(nonExistentPath));
            
            Assert.Contains("CDM JSON file not found", ex.Message);
        }

        [Fact]
        public void Should_Throw_Exception_For_Empty_JSON()
        {
            // Arrange
            var emptyJson = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MetadataGenerator.FromCdmJsonFile(WriteTempFile(emptyJson)));
        }

        [Fact]
        public void Should_Throw_Exception_For_CDM_With_No_Definitions()
        {
            // Arrange
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": []
}";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson)));
            
            Assert.Contains("no entity definitions", ex.Message);
        }

        [Fact]
        public void Should_Skip_Non_LocalEntity_Definitions()
        {
            // Arrange
            // CDM files can contain other definition types besides LocalEntity
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""DataType"",
            ""name"": ""CustomDataType""
        },
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""Account"",
            ""sourceName"": ""account"",
            ""hasAttributes"": [
                {
                    ""name"": ""accountId"",
                    ""dataType"": ""guid"",
                    ""sourceName"": ""accountid"",
                    ""isPrimaryKey"": true
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            Assert.Single(entityMetadataList);
            Assert.Equal("account", entityMetadataList.First().LogicalName);
        }

        [Fact]
        public void Should_Handle_Attributes_Without_SourceName()
        {
            // Arrange
            var cdmJson = @"
{
    ""jsonSchemaSemanticVersion"": ""1.0.0"",
    ""definitions"": [
        {
            ""$type"": ""LocalEntity"",
            ""name"": ""TestEntity"",
            ""sourceName"": ""testentity"",
            ""hasAttributes"": [
                {
                    ""name"": ""TestEntityId"",
                    ""dataType"": ""guid"",
                    ""isPrimaryKey"": true
                },
                {
                    ""name"": ""TestField"",
                    ""dataType"": ""string""
                }
            ]
        }
    ]
}";

            // Act
            var entityMetadataList = MetadataGenerator.FromCdmJsonFile(WriteTempFile(cdmJson));

            // Assert
            var entityMetadata = entityMetadataList.First();
            Assert.NotNull(entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "testentityid"));
            Assert.NotNull(entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == "testfield"));
        }

        /// <summary>
        /// Helper method to write a temporary CDM JSON file for testing.
        /// </summary>
        private string WriteTempFile(string content)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"cdm_test_{Guid.NewGuid()}.json");
            File.WriteAllText(tempFile, content);
            return tempFile;
        }
    }
}
