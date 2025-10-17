using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests
{
    public class ValidateAttributeTypesTests : Fake4DataverseTests
    {
        private readonly IXrmFakedContext _contextWithValidation;
        private readonly IOrganizationService _serviceWithValidation;
        
        public ValidateAttributeTypesTests()
        {
            // Context with validation enabled (default behavior) and metadata loaded from early-bound types
            _contextWithValidation = XrmFakedContextFactory.New();
            
            // Set up a valid caller to avoid validation errors on audit fields
            var systemUser = new Entity("systemuser")
            {
                Id = Guid.NewGuid(),
                ["fullname"] = "Test User"
            };
            _contextWithValidation.CallerProperties.CallerId = systemUser.ToEntityReference();
            _contextWithValidation.Initialize(systemUser);
            
            // Use early-bound assembly to generate metadata for Account and Contact
            // This provides real metadata structure matching Dataverse
            _contextWithValidation.InitializeMetadata(typeof(Account).Assembly);
            
            // Verify metadata was loaded
            var accountMetadata = _contextWithValidation.GetEntityMetadataByName("account");
            if (accountMetadata == null)
                throw new Exception("Account metadata was not loaded from early-bound assembly");
                
            var contactMetadata = _contextWithValidation.GetEntityMetadataByName("contact");
            if (contactMetadata == null)
                throw new Exception("Contact metadata was not loaded from early-bound assembly");
            
            _serviceWithValidation = _contextWithValidation.GetOrganizationService();
        }

        [Fact]
        public void Should_Allow_Valid_String_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // String attributes should accept string values
            // Metadata already loaded from CDM in constructor
            
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
            // Metadata already loaded from CDM in constructor
            
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
            // Metadata already loaded from CDM in constructor
            
            
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
            // Using lastusedincampaign from CDM metadata
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["lastusedincampaign"] = DateTime.UtcNow
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Allow_Valid_OptionSet_Attribute()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // OptionSet attributes should accept OptionSetValue values
            
            
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
            // Metadata for account and contact already loaded from CDM in constructor
            
            var contact = new Entity("contact")
            {
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };
            var contactId = _serviceWithValidation.Create(contact);
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["primarycontactid"] = new EntityReference("contact", contactId)
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Validate_On_Update()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Validation should also apply to update operations
            
            
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
        public void Should_Throw_Error_When_No_Metadata_Available()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // When metadata is not initialized and validation is enabled, should throw error like Dataverse
            
            // Create a fresh context with validation enabled but no metadata
            // NOTE: Cannot use base class context as it has validation disabled for backward compatibility
            var freshContext = XrmFakedContextFactory.New();  // Validation enabled by default
            var freshService = freshContext.GetOrganizationService();
            
            // Don't initialize metadata - this should cause an error
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = 100
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                freshService.Create(account));
            
            Assert.Contains("Could not find entity", ex.Message);
            Assert.Contains("account", ex.Message);
            Assert.Contains("metadata", ex.Message.ToLower());
        }

        [Fact]
        public void Should_Allow_Numeric_Type_Conversions()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
            // Numeric types should allow compatible conversions (int to long, etc.)
            
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["numberofemployees"] = 100L  // Long instead of int - should be allowed
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Should_Reject_StateCode_During_Create_When_Validation_Enabled()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate
            // IsValidForCreate: Gets whether the attribute can be set in a Create message.
            // For statecode, IsValidForCreate is false - state must be set after creation using SetState
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["statecode"] = new OptionSetValue(1)  // Trying to set statecode during create
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Create(account));
            
            Assert.Contains("statecode", ex.Message);
            Assert.Contains("not valid for Create", ex.Message);
        }

        [Fact]
        public void Should_Reject_StatusCode_During_Create_When_Validation_Enabled()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate
            // IsValidForCreate: Gets whether the attribute can be set in a Create message.
            // For statuscode, IsValidForCreate is false - managed by statecode transitions
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["statuscode"] = new OptionSetValue(2)  // Trying to set statuscode during create
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => 
                _serviceWithValidation.Create(account));
            
            Assert.Contains("statuscode", ex.Message);
            Assert.Contains("not valid for Create", ex.Message);
        }

        [Fact]
        public void Should_Allow_StateCode_During_Update_When_Validation_Enabled()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforupdate
            // IsValidForUpdate: Gets whether the attribute can be set in an Update message.
            // For statecode, IsValidForUpdate is true - can be updated (typically via SetState)
            
            // First create an account
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };
            var id = _serviceWithValidation.Create(account);

            // Now update with statecode - should work
            var updateAccount = new Entity("account")
            {
                Id = id,
                ["statecode"] = new OptionSetValue(1)
            };

            // Should not throw
            _serviceWithValidation.Update(updateAccount);

            // Verify it was updated
            var retrieved = _serviceWithValidation.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet("statecode"));
            Assert.Equal(1, ((OptionSetValue)retrieved["statecode"]).Value);
        }

        [Fact]
        public void Should_Allow_Regular_Attributes_During_Create()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate
            // IsValidForCreate: Most regular attributes have IsValidForCreate=true
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account",
                ["telephone1"] = "555-1234",
                ["numberofemployees"] = 100,
                ["revenue"] = new Money(1000000m)
            };

            var id = _serviceWithValidation.Create(account);
            Assert.NotEqual(Guid.Empty, id);

            // Verify attributes were set
            var retrieved = _serviceWithValidation.Retrieve("account", id, 
                new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "telephone1", "numberofemployees", "revenue"));
            Assert.Equal("Test Account", retrieved["name"]);
            Assert.Equal("555-1234", retrieved["telephone1"]);
            Assert.Equal(100, retrieved["numberofemployees"]);
            Assert.Equal(1000000m, ((Money)retrieved["revenue"]).Value);
        }


    }
}
