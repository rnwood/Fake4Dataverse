using System;
using System.Collections.Generic;
using Fake4Dataverse.CloudFlows;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for OData value conversion to Dataverse SDK types.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
    /// 
    /// Power Automate's Dataverse connector uses OData/Web API conventions where:
    /// - OptionSet values are integers
    /// - Money values are decimals
    /// - EntityReferences use @odata.bind notation
    /// - Lookup fields use _fieldname_value for GUIDs
    /// 
    /// These tests verify that the ODataValueConverter correctly transforms these
    /// OData representations to their SDK equivalents (OptionSetValue, Money, EntityReference).
    /// </summary>
    public class ODataValueConverterTests
    {
        #region OptionSet Conversion Tests

        [Fact]
        public void Should_Convert_Integer_To_OptionSetValue_For_Code_Fields()
        {
            // Reference: OptionSet fields typically end with "code" in Dataverse
            // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#options
            
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("prioritycode", 1);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(1, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Convert_Integer_To_OptionSetValue_For_StateCode()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("statecode", 0);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(0, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Convert_Integer_To_OptionSetValue_For_StatusCode()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("statuscode", 1);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(1, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Convert_Integer_To_OptionSetValue_For_TypeCode_Fields()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("accounttypecode", 3);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(3, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Not_Convert_Integer_For_Non_OptionSet_Fields()
        {
            // Arrange & Act - regular integer fields should remain as integers
            var result = ODataValueConverter.ConvertODataValue("numberofemployees", 100);

            // Assert
            Assert.IsType<int>(result);
            Assert.Equal(100, result);
        }

        [Fact]
        public void Should_Return_Existing_OptionSetValue_Unchanged()
        {
            // Arrange
            var optionSetValue = new OptionSetValue(5);

            // Act
            var result = ODataValueConverter.ConvertODataValue("prioritycode", optionSetValue);

            // Assert
            Assert.Same(optionSetValue, result);
        }

        #endregion

        #region Money Conversion Tests

        [Fact]
        public void Should_Convert_Decimal_To_Money_For_Amount_Fields()
        {
            // Reference: Money fields often contain "amount" in the name
            // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#money
            
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("totalamount", 1500.50m);

            // Assert
            Assert.IsType<Money>(result);
            Assert.Equal(1500.50m, ((Money)result).Value);
        }

        [Fact]
        public void Should_Convert_Double_To_Money_For_Revenue_Fields()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("revenue", 50000.75);

            // Assert
            Assert.IsType<Money>(result);
            Assert.Equal(50000.75m, ((Money)result).Value);
        }

        [Fact]
        public void Should_Convert_Integer_To_Money_For_Budget_Fields()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("budget", 10000);

            // Assert
            Assert.IsType<Money>(result);
            Assert.Equal(10000m, ((Money)result).Value);
        }

        [Fact]
        public void Should_Convert_Value_Field_To_Money()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("estimatedvalue", 75000.00m);

            // Assert
            Assert.IsType<Money>(result);
            Assert.Equal(75000.00m, ((Money)result).Value);
        }

        [Fact]
        public void Should_Return_Existing_Money_Unchanged()
        {
            // Arrange
            var money = new Money(1000m);

            // Act
            var result = ODataValueConverter.ConvertODataValue("revenue", money);

            // Assert
            Assert.Same(money, result);
        }

        #endregion

        #region EntityReference Conversion Tests

        [Fact]
        public void Should_Convert_OData_Bind_To_EntityReference()
        {
            // Reference: @odata.bind notation is used in Web API for lookups
            // Format: "entityname(guid)"
            // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/associate-disassociate-entities-using-web-api
            
            // Arrange
            var guid = Guid.NewGuid();
            var odataValue = $"accounts({guid})";

            // Act
            var result = ODataValueConverter.ConvertODataValue("accountid", odataValue);

            // Assert
            Assert.IsType<EntityReference>(result);
            var entityRef = (EntityReference)result;
            Assert.Equal("accounts", entityRef.LogicalName);
            Assert.Equal(guid, entityRef.Id);
        }

        [Fact]
        public void Should_Convert_OData_Bind_With_Different_Entity()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var odataValue = $"contacts({guid})";

            // Act
            var result = ODataValueConverter.ConvertODataValue("primarycontactid", odataValue);

            // Assert
            Assert.IsType<EntityReference>(result);
            var entityRef = (EntityReference)result;
            Assert.Equal("contacts", entityRef.LogicalName);
            Assert.Equal(guid, entityRef.Id);
        }

        [Fact]
        public void Should_Return_Existing_EntityReference_Unchanged()
        {
            // Arrange
            var entityRef = new EntityReference("account", Guid.NewGuid());

            // Act
            var result = ODataValueConverter.ConvertODataValue("accountid", entityRef);

            // Assert
            Assert.Same(entityRef, result);
        }

        #endregion

        #region DateTime Conversion Tests

        [Fact]
        public void Should_Convert_ISO_DateTime_String()
        {
            // Arrange
            var dateString = "2025-10-12T10:30:00Z";

            // Act
            var result = ODataValueConverter.ConvertODataValue("createdon", dateString);

            // Assert
            Assert.IsType<DateTime>(result);
        }

        [Fact]
        public void Should_Return_Existing_DateTime_Unchanged()
        {
            // Arrange
            var dateTime = DateTime.Now;

            // Act
            var result = ODataValueConverter.ConvertODataValue("createdon", dateTime);

            // Assert
            Assert.Equal(dateTime, result);
        }

        #endregion

        #region Null and String Tests

        [Fact]
        public void Should_Return_Null_For_Null_Value()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("anyfield", null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Should_Return_String_Unchanged_For_Text_Fields()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("name", "Test Account");

            // Assert
            Assert.IsType<string>(result);
            Assert.Equal("Test Account", result);
        }

        #endregion

        #region Batch Conversion Tests

        [Fact]
        public void Should_Convert_Multiple_OData_Attributes()
        {
            // Arrange
            var attributes = new Dictionary<string, object>
            {
                ["name"] = "Test Account",
                ["prioritycode"] = 1,
                ["revenue"] = 50000.50m,
                ["numberofemployees"] = 100
            };

            // Act
            var result = ODataValueConverter.ConvertODataAttributes(attributes, "account");

            // Assert
            Assert.Equal(4, result.Count);
            Assert.IsType<string>(result["name"]);
            Assert.IsType<OptionSetValue>(result["prioritycode"]);
            Assert.IsType<Money>(result["revenue"]);
            Assert.IsType<int>(result["numberofemployees"]);
        }

        [Fact]
        public void Should_Handle_Null_Attributes_Dictionary()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataAttributes(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Should_Convert_Mixed_Types_In_Attributes()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var attributes = new Dictionary<string, object>
            {
                ["subject"] = "Test Task",
                ["prioritycode"] = 2,
                ["scheduledend"] = "2025-12-31T23:59:59Z",
                ["regardingobjectid"] = $"accounts({guid})"
            };

            // Act
            var result = ODataValueConverter.ConvertODataAttributes(attributes, "task");

            // Assert
            Assert.Equal(4, result.Count);
            Assert.IsType<string>(result["subject"]);
            Assert.IsType<OptionSetValue>(result["prioritycode"]);
            Assert.Equal(2, ((OptionSetValue)result["prioritycode"]).Value);
            Assert.IsType<DateTime>(result["scheduledend"]);
            Assert.IsType<EntityReference>(result["regardingobjectid"]);
            Assert.Equal("accounts", ((EntityReference)result["regardingobjectid"]).LogicalName);
            Assert.Equal(guid, ((EntityReference)result["regardingobjectid"]).Id);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Should_Handle_Long_As_OptionSet()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("statuscode", 1L);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(1, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Handle_Double_As_OptionSet_When_Whole_Number()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("prioritycode", 2.0);

            // Assert
            Assert.IsType<OptionSetValue>(result);
            Assert.Equal(2, ((OptionSetValue)result).Value);
        }

        [Fact]
        public void Should_Not_Convert_Decimal_For_Non_Money_Fields()
        {
            // Arrange & Act
            var result = ODataValueConverter.ConvertODataValue("customfield", 123.45m);

            // Assert
            Assert.IsType<decimal>(result);
            Assert.Equal(123.45m, result);
        }

        #endregion
    }
}
