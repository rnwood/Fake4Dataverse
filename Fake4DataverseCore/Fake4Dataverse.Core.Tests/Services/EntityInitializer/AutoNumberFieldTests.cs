using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Extensions;

namespace Fake4Dataverse.Tests.Services.EntityInitializer
{
    /// <summary>
    /// Tests for auto number field generation during entity creation.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
    /// Auto number fields automatically generate values when a record is created.
    /// </summary>
    public class AutoNumberFieldTests : Fake4DataverseTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public AutoNumberFieldTests()
        {
            _context = base._context;
            _service = base._service;

            // Enable validation to ensure metadata is required
            var integrityOptions = _context.GetProperty<IIntegrityOptions>();
            integrityOptions.ValidateEntityReferences = true;
            integrityOptions.ValidateAttributeTypes = true;
        }

        [Fact]
        public void Should_Generate_Auto_Number_On_Create_With_Metadata()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // When a StringAttributeMetadata has an AutoNumberFormat, the value should be auto-generated.
            
            // Arrange - Create metadata with auto number field
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_ticket"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_ticketid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_ticketnumber",
                AutoNumberFormat = "TICK-{SEQNUM:5}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act - Create entity without setting the auto number field
            var ticket = new Entity("new_ticket");
            var ticketId = _service.Create(ticket);

            // Assert - Auto number should be generated
            var retrieved = _service.Retrieve("new_ticket", ticketId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.NotNull(retrieved["new_ticketnumber"]);
            Assert.Equal("TICK-00001", retrieved["new_ticketnumber"]);
        }

        [Fact]
        public void Should_Increment_Sequential_Numbers_For_Multiple_Entities()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#sequential-number
            // Each new record increments the sequence number.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_order"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_orderid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_ordernumber",
                AutoNumberFormat = "ORD-{SEQNUM:4}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act - Create multiple entities
            var order1 = new Entity("new_order");
            var order1Id = _service.Create(order1);

            var order2 = new Entity("new_order");
            var order2Id = _service.Create(order2);

            var order3 = new Entity("new_order");
            var order3Id = _service.Create(order3);

            // Assert - Each should have incrementing numbers
            var retrieved1 = _service.Retrieve("new_order", order1Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            var retrieved2 = _service.Retrieve("new_order", order2Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            var retrieved3 = _service.Retrieve("new_order", order3Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            Assert.Equal("ORD-0001", retrieved1["new_ordernumber"]);
            Assert.Equal("ORD-0002", retrieved2["new_ordernumber"]);
            Assert.Equal("ORD-0003", retrieved3["new_ordernumber"]);
        }

        [Fact]
        public void Should_Not_Override_User_Provided_Value()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // If the user provides a value, it should not be overridden by auto-generation.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_case"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_caseid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_casenumber",
                AutoNumberFormat = "CASE-{SEQNUM:5}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act - Create entity with user-provided value
            var caseEntity = new Entity("new_case");
            caseEntity["new_casenumber"] = "CUSTOM-12345";
            var caseId = _service.Create(caseEntity);

            // Assert - User value should be preserved
            var retrieved = _service.Retrieve("new_case", caseId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("CUSTOM-12345", retrieved["new_casenumber"]);
        }

        [Fact]
        public void Should_Generate_With_Date_Time_UTC_Token()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#date-and-time
            // {DATETIMEUTC:format} includes current UTC date/time in the generated value.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_document"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_documentid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_documentnumber",
                AutoNumberFormat = "DOC-{DATETIMEUTC:yyyyMMdd}-{SEQNUM:3}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act
            var document = new Entity("new_document");
            var documentId = _service.Create(document);

            // Assert
            var retrieved = _service.Retrieve("new_document", documentId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            var expectedPrefix = $"DOC-{DateTime.UtcNow:yyyyMMdd}-";
            Assert.StartsWith(expectedPrefix, retrieved["new_documentnumber"].ToString());
            Assert.EndsWith("-001", retrieved["new_documentnumber"].ToString());
        }

        [Fact]
        public void Should_Generate_With_Random_String_Token()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields#random-string
            // {RANDSTRING:n} includes a random alphanumeric string in the generated value.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_product"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_productid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_productcode",
                AutoNumberFormat = "PRD-{RANDSTRING:6}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act
            var product1 = new Entity("new_product");
            var product1Id = _service.Create(product1);

            var product2 = new Entity("new_product");
            var product2Id = _service.Create(product2);

            // Assert
            var retrieved1 = _service.Retrieve("new_product", product1Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            var retrieved2 = _service.Retrieve("new_product", product2Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            var code1 = retrieved1["new_productcode"].ToString();
            var code2 = retrieved2["new_productcode"].ToString();

            Assert.StartsWith("PRD-", code1);
            Assert.StartsWith("PRD-", code2);
            Assert.Equal(10, code1.Length); // PRD- (4) + random (6)
            Assert.Equal(10, code2.Length);
            Assert.NotEqual(code1, code2); // Should be different (extremely high probability)
        }

        [Fact]
        public void Should_Support_Complex_Format_With_Multiple_Tokens()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Format can combine multiple token types with static text.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_vehicle"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_vehicleid");

            var autoNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_vin",
                AutoNumberFormat = "VIN-{DATETIMEUTC:yyyy}-{SEQNUM:5}-{RANDSTRING:4}"
            };
            autoNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            autoNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                autoNumberAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act
            var vehicle1 = new Entity("new_vehicle");
            var vehicle1Id = _service.Create(vehicle1);

            var vehicle2 = new Entity("new_vehicle");
            var vehicle2Id = _service.Create(vehicle2);

            // Assert
            var retrieved1 = _service.Retrieve("new_vehicle", vehicle1Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            var retrieved2 = _service.Retrieve("new_vehicle", vehicle2Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            var vin1 = retrieved1["new_vin"].ToString();
            var vin2 = retrieved2["new_vin"].ToString();

            var expectedPrefix = $"VIN-{DateTime.UtcNow:yyyy}-";
            Assert.StartsWith(expectedPrefix, vin1);
            Assert.StartsWith(expectedPrefix, vin2);

            // Sequential numbers should increment
            Assert.Contains("-00001-", vin1);
            Assert.Contains("-00002-", vin2);
        }

        [Fact]
        public void Should_Support_Multiple_Auto_Number_Fields_On_Same_Entity()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // An entity can have multiple auto number fields, each with its own format and sequence.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_project"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_projectid");

            var projectNumberAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_projectnumber",
                AutoNumberFormat = "PRJ-{SEQNUM:4}"
            };
            projectNumberAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            projectNumberAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            projectNumberAttribute.SetSealedPropertyValue("IsValidForRead", true);

            var referenceCodeAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_referencecode",
                AutoNumberFormat = "REF-{RANDSTRING:6}"
            };
            referenceCodeAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            referenceCodeAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            referenceCodeAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                projectNumberAttribute,
                referenceCodeAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act
            var project = new Entity("new_project");
            var projectId = _service.Create(project);

            // Assert
            var retrieved = _service.Retrieve("new_project", projectId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            
            Assert.NotNull(retrieved["new_projectnumber"]);
            Assert.NotNull(retrieved["new_referencecode"]);
            
            Assert.Equal("PRJ-0001", retrieved["new_projectnumber"]);
            Assert.StartsWith("REF-", retrieved["new_referencecode"].ToString());
            Assert.Equal(10, retrieved["new_referencecode"].ToString().Length); // REF- (4) + random (6)
        }

        [Fact]
        public void Should_Not_Generate_Auto_Number_If_No_AutoNumberFormat_Set()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Only string attributes with AutoNumberFormat should be auto-generated.
            
            // Arrange
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_customer"
            };
            entityMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "new_customerid");

            var normalStringAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_customername",
                MaxLength = 100
                // No AutoNumberFormat set
            };
            normalStringAttribute.SetSealedPropertyValue("IsValidForCreate", true);
            normalStringAttribute.SetSealedPropertyValue("IsValidForUpdate", true);
            normalStringAttribute.SetSealedPropertyValue("IsValidForRead", true);

            entityMetadata.SetSealedPropertyValue("Attributes", new AttributeMetadata[]
            {
                normalStringAttribute
            });

            _context.InitializeMetadata(entityMetadata);

            // Act
            var customer = new Entity("new_customer");
            var customerId = _service.Create(customer);

            // Assert - No auto-generation should occur
            var retrieved = _service.Retrieve("new_customer", customerId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.False(retrieved.Contains("new_customername"));
        }

        [Fact]
        public void Should_Work_Without_Metadata_If_No_Auto_Number_Fields()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields
            // Entities without auto number fields should work normally even without metadata.
            
            // Arrange - Disable validation for this test
            var integrityOptions = _context.GetProperty<IIntegrityOptions>();
            integrityOptions.ValidateEntityReferences = false;
            integrityOptions.ValidateAttributeTypes = false;

            // Act
            var simpleEntity = new Entity("new_simple");
            simpleEntity["new_name"] = "Test";
            var simpleId = _service.Create(simpleEntity);

            // Assert
            var retrieved = _service.Retrieve("new_simple", simpleId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("Test", retrieved["new_name"]);
        }
    }
}
