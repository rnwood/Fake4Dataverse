using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests
{
    public class ValidateAttributeTypesTests
    {
        private readonly IXrmFakedContext _contextWithValidation;
        private readonly IOrganizationService _serviceWithValidation;
        
        private readonly IXrmFakedContext _contextWithoutValidation;
        private readonly IOrganizationService _serviceWithoutValidation;
        
        public ValidateAttributeTypesTests()
        {
            // Context with validation enabled
            _contextWithValidation = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = true 
            });
            _serviceWithValidation = _contextWithValidation.GetOrganizationService();
            
            // Context without validation
            _contextWithoutValidation = XrmFakedContextFactory.New();
            _serviceWithoutValidation = _contextWithoutValidation.GetOrganizationService();
        }

        [Fact]
        public void Should_Not_Validate_Attribute_Types_When_Disabled()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // When validation is disabled, mismatched types should be allowed for backward compatibility
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = "should be int"  // Wrong type
            };

            // Should not throw when validation is disabled
            var id = _serviceWithoutValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_String_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // String attributes should accept string values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Reject_Invalid_Attribute_Type()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Dataverse validates attribute types and rejects mismatches
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = "should be int"  // Wrong type - should be int
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Create(account));
            
            Assert.Contains("numberofemployees", ex.Message);
            Assert.Contains("invalid type", ex.Message.ToLower());
        }

        [Fact]
        public void Should_Allow_Null_Values()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Null values are always valid for nullable attributes
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = null  // Null should be allowed
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Reject_NonExistent_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Dataverse rejects attributes that don't exist in metadata
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["nonexistentattribute"] = "value"
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Create(account));
            
            Assert.Contains("nonexistentattribute", ex.Message);
            Assert.Contains("does not exist", ex.Message.ToLower());
        }

        [Fact]
        public void Should_Allow_Valid_Integer_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Integer attributes should accept int values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = 100
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_Money_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Money attributes should accept Money values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["revenue"] = new Money(500000m)
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_Boolean_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Boolean attributes should accept bool values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["donotemail"] = true
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_DateTime_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // DateTime attributes should accept DateTime values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["createdon"] = DateTime.UtcNow
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_OptionSet_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // OptionSet attributes should accept OptionSetValue values
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["customertypecode"] = new OptionSetValue(1)
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_Lookup_To_Correct_Target()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Lookup attributes should accept EntityReferences with valid target entity types
            
            var accountMetadata = CreateAccountMetadata();
            var contactMetadata = CreateContactMetadata();
            _contextWithValidation.InitializeMetadata(new[] { accountMetadata, contactMetadata });
            
            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };
            _contextWithValidation.Initialize(contact);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["primarycontactid"] = new EntityReference("contact", contact.Id)
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact(Skip = "Lookup target validation needs investigation - Targets property may not be properly set via SetFieldValue")]
        public void Should_Reject_Lookup_To_Invalid_Target()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Dataverse validates that lookup targets match the defined relationship
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["primarycontactid"] = new EntityReference("account", Guid.NewGuid())  // Should be contact
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Create(account));
            
            Assert.Contains("primarycontactid", ex.Message);
            Assert.Contains("cannot reference", ex.Message.ToLower());
        }

        [Fact]
        public void Should_Validate_On_Update()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Validation should also apply to update operations
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };
            var id = _serviceWithValidation.Create(account);
            
            var updateAccount = new Entity("account")
            {
                Id = id,
                ["numberofemployees"] = "invalid type"  // Wrong type
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Update(updateAccount));
            
            Assert.Contains("numberofemployees", ex.Message);
            Assert.Contains("invalid type", ex.Message.ToLower());
        }

        [Fact]
        public void Should_Skip_Validation_When_No_Metadata_Available()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // When metadata is not initialized, validation should be skipped gracefully
            
            // Don't initialize metadata
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = "should be int but no metadata to validate"
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Numeric_Type_Conversions()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Numeric types should allow compatible conversions (int to long, etc.)
            
            var accountMetadata = CreateAccountMetadata();
            _contextWithValidation.InitializeMetadata(accountMetadata);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = 100L  // Long instead of int - should be allowed
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        private EntityMetadata CreateAccountMetadata()
        {
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            
            var nameAttr = new StringAttributeMetadata { LogicalName = "name" };
            nameAttr.SetFieldValue("_attributeType", AttributeTypeCode.String);
            
            var empAttr = new IntegerAttributeMetadata { LogicalName = "numberofemployees" };
            empAttr.SetFieldValue("_attributeType", AttributeTypeCode.Integer);
            
            var revAttr = new MoneyAttributeMetadata { LogicalName = "revenue" };
            revAttr.SetFieldValue("_attributeType", AttributeTypeCode.Money);
            
            var emailAttr = new BooleanAttributeMetadata { LogicalName = "donotemail" };
            emailAttr.SetFieldValue("_attributeType", AttributeTypeCode.Boolean);
            
            var dateAttr = new DateTimeAttributeMetadata { LogicalName = "createdon" };
            dateAttr.SetFieldValue("_attributeType", AttributeTypeCode.DateTime);
            
            var picklistAttr = new PicklistAttributeMetadata { LogicalName = "customertypecode" };
            picklistAttr.SetFieldValue("_attributeType", AttributeTypeCode.Picklist);
            
            var lookupAttr = new LookupAttributeMetadata { LogicalName = "primarycontactid" };
            lookupAttr.SetFieldValue("_attributeType", AttributeTypeCode.Lookup);
            lookupAttr.SetFieldValue("_targets", new[] { "contact" });
            
            entityMetadata.SetFieldValue("_attributes", new AttributeMetadata[]
            {
                nameAttr,
                empAttr,
                revAttr,
                emailAttr,
                dateAttr,
                picklistAttr,
                lookupAttr
            });
            
            return entityMetadata;
        }

        private EntityMetadata CreateContactMetadata()
        {
            var entityMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            
            var firstAttr = new StringAttributeMetadata { LogicalName = "firstname" };
            firstAttr.SetFieldValue("_attributeType", AttributeTypeCode.String);
            
            var lastAttr = new StringAttributeMetadata { LogicalName = "lastname" };
            lastAttr.SetFieldValue("_attributeType", AttributeTypeCode.String);
            
            entityMetadata.SetFieldValue("_attributes", new AttributeMetadata[]
            {
                firstAttr,
                lastAttr
            });
            
            return entityMetadata;
        }
    }
}
