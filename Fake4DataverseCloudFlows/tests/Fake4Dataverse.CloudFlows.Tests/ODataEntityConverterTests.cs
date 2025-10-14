#if !NET462
using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.CloudFlows;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for OData entity conversion from Dataverse SDK types to OData JSON.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
    /// 
    /// The Dataverse Web API uses OData v4.0 conventions for JSON responses:
    /// - Entities are represented as JSON objects with type-appropriate values
    /// - Lookups use _fieldname_value pattern for GUIDs
    /// - OptionSets are integers
    /// - Money fields are decimals
    /// - DateTime values are ISO 8601 strings
    /// 
    /// These tests verify that SDK types are correctly converted to OData JSON format.
    /// </summary>
    public class ODataEntityConverterTests
    {
        [Fact]
        public void Should_Convert_Simple_Entity_To_OData()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-entity-using-web-api
            // A basic entity should include the primary key and all attributes in OData format
            
            // Arrange
            var accountId = Guid.NewGuid();
            var entity = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["revenue"] = new Money(100000m),
                ["numberofemployees"] = 50
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal($"#Microsoft.Dynamics.CRM.account/$entity", result["@odata.context"]);
            Assert.Equal(accountId, result["accountid"]);
            Assert.Equal("Test Account", result["name"]);
            Assert.Equal(100000m, result["revenue"]);
            Assert.Equal(50, result["numberofemployees"]);
        }

        [Fact]
        public void Should_Convert_OptionSet_To_Integer()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#picklist-choices
            // OptionSet (picklist) values are represented as integers in OData JSON
            
            // Arrange
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["prioritycode"] = new OptionSetValue(2)
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal(2, result["prioritycode"]);
        }

        [Fact]
        public void Should_Convert_Money_To_Decimal()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#money
            // Money values are represented as decimal numbers in OData JSON
            
            // Arrange
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["revenue"] = new Money(123456.78m)
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal(123456.78m, result["revenue"]);
        }

        [Fact]
        public void Should_Convert_EntityReference_To_Lookup_Value()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#lookup-properties
            // Lookup fields use _fieldname_value notation containing the GUID
            
            // Arrange
            var contactId = Guid.NewGuid();
            var entityRef = new EntityReference("contact", contactId);
            entityRef.Name = "John Doe";
            
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["primarycontactid"] = entityRef
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal(contactId, result["_primarycontactid_value"]);
            Assert.Equal("John Doe", result["_primarycontactid_value@OData.Community.Display.V1.FormattedValue"]);
        }

        [Fact]
        public void Should_Convert_DateTime_To_ISO8601()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#date-and-time
            // DateTime values are represented as ISO 8601 strings in OData JSON
            
            // Arrange
            var createDate = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["createdon"] = createDate
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal("2024-01-15T10:30:45Z", result["createdon"]);
        }

        [Fact]
        public void Should_Convert_EntityCollection_To_OData()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
            // Collection responses include @odata.context and a value array
            
            // Arrange
            var entities = new EntityCollection
            {
                EntityName = "account",
                Entities =
                {
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2" },
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 3" }
                }
            };

            // Act
            var result = ODataEntityConverter.ToODataCollection(entities, "account");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("#Microsoft.Dynamics.CRM.account", result["@odata.context"]);
            Assert.True(result.ContainsKey("value"));
            
            var valueArray = result["value"] as List<Dictionary<string, object>>;
            Assert.NotNull(valueArray);
            Assert.Equal(3, valueArray.Count);
            Assert.Equal("Account 1", valueArray[0]["name"]);
            Assert.Equal("Account 2", valueArray[1]["name"]);
            Assert.Equal("Account 3", valueArray[2]["name"]);
        }

        [Fact]
        public void Should_Include_Count_When_Requested()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#count-number-of-rows
            // The @odata.count annotation provides the total count of records
            
            // Arrange
            var entities = new EntityCollection
            {
                EntityName = "account",
                TotalRecordCount = 100,
                Entities =
                {
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" }
                }
            };

            // Act
            var result = ODataEntityConverter.ToODataCollection(entities, "account", includeCount: true);

            // Assert
            Assert.Equal(100, result["@odata.count"]);
        }

        [Fact]
        public void Should_Include_NextLink_For_Pagination()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#paging
            // The @odata.nextLink annotation provides the URL for the next page of results
            
            // Arrange
            var entities = new EntityCollection
            {
                EntityName = "account",
                Entities =
                {
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" }
                }
            };

            // Act
            var result = ODataEntityConverter.ToODataCollection(
                entities, 
                "account", 
                nextLink: "/api/data/v9.2/accounts?$skip=10");

            // Assert
            Assert.Equal("/api/data/v9.2/accounts?$skip=10", result["@odata.nextLink"]);
        }

        [Fact]
        public void Should_Handle_Null_Entity()
        {
            // Act
            var result = ODataEntityConverter.ToODataEntity(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Should_Convert_Guid_To_String()
        {
            // Arrange
            var customGuid = Guid.NewGuid();
            var entity = new Entity("custom_entity")
            {
                Id = Guid.NewGuid(),
                ["custom_guidfield"] = customGuid
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal(customGuid.ToString(), result["custom_guidfield"]);
        }

        [Fact]
        public void Should_Convert_Boolean_Values()
        {
            // Arrange
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["donotemail"] = true,
                ["donotphone"] = false
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.True((bool)result["donotemail"]);
            Assert.False((bool)result["donotphone"]);
        }

        [Fact]
        public void Should_Convert_Multiple_Attribute_Types()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
            // Entities often contain multiple attribute types that all need proper conversion
            
            // Arrange
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var createDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            
            var contactRef = new EntityReference("contact", contactId);
            contactRef.Name = "Jane Smith";
            
            var entity = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Company",
                ["revenue"] = new Money(500000m),
                ["numberofemployees"] = 100,
                ["prioritycode"] = new OptionSetValue(1),
                ["primarycontactid"] = contactRef,
                ["createdon"] = createDate,
                ["donotemail"] = true
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert - verify all types are converted correctly
            Assert.Equal(accountId, result["accountid"]);
            Assert.Equal("Test Company", result["name"]);
            Assert.Equal(500000m, result["revenue"]);
            Assert.Equal(100, result["numberofemployees"]);
            Assert.Equal(1, result["prioritycode"]);
            Assert.Equal(contactId, result["_primarycontactid_value"]);
            Assert.Equal("2024-01-01T12:00:00Z", result["createdon"]);
            Assert.True((bool)result["donotemail"]);
        }

        [Fact]
        public void Should_Create_Error_Response()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/compose-http-requests-handle-errors
            // Error responses follow OData error format with error object containing code and message
            
            // Act
            var result = ODataEntityConverter.CreateErrorResponse("0x80040217", "Record not found");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("error"));
            
            var error = result["error"] as Dictionary<string, object>;
            Assert.NotNull(error);
            Assert.Equal("0x80040217", error["code"]);
            Assert.Equal("Record not found", error["message"]);
        }

        [Fact]
        public void Should_Include_Inner_Exception_In_Error()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/compose-http-requests-handle-errors
            // Error responses can include innererror with exception details
            
            // Arrange
            var innerException = new InvalidOperationException("Invalid operation");

            // Act
            var result = ODataEntityConverter.CreateErrorResponse(
                "0x80040217", 
                "An error occurred", 
                innerException);

            // Assert
            var error = result["error"] as Dictionary<string, object>;
            Assert.True(error.ContainsKey("innererror"));
            
            var innererror = error["innererror"] as Dictionary<string, object>;
            Assert.Equal("Invalid operation", innererror["message"]);
            Assert.Equal("InvalidOperationException", innererror["type"]);
        }

        [Fact]
        public void Should_Not_Include_OData_Metadata_When_Disabled()
        {
            // Arrange
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity, includeODataMetadata: false);

            // Assert
            Assert.False(result.ContainsKey("@odata.context"));
            Assert.False(result.ContainsKey("@odata.etag"));
            Assert.True(result.ContainsKey("accountid"));
            Assert.Equal("Test Account", result["name"]);
        }

        [Fact]
        public void Should_Include_Formatted_Values()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#formatted-values
            // Formatted values are included with @OData.Community.Display.V1.FormattedValue annotation
            
            // Arrange
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["prioritycode"] = new OptionSetValue(1)
            };
            entity.FormattedValues["prioritycode"] = "High";

            // Act
            var result = ODataEntityConverter.ToODataEntity(entity);

            // Assert
            Assert.Equal(1, result["prioritycode"]);
            Assert.Equal("High", result["prioritycode@OData.Community.Display.V1.FormattedValue"]);
        }
    }
}
#endif
