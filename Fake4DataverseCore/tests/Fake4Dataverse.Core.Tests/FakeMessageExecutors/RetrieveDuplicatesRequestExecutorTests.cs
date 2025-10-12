using System;
using System.Collections.Generic;
using System.Linq;
using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.FakeMessageExecutors
{
    /// <summary>
    /// Tests for RetrieveDuplicatesRequest message executor
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
    /// 
    /// RetrieveDuplicatesRequest detects and retrieves duplicate records for a specified record.
    /// The request evaluates active and published duplicate detection rules to find matching records.
    /// </summary>
    public class RetrieveDuplicatesRequestExecutorTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public RetrieveDuplicatesRequestExecutorTests()
        {
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }

        /// <summary>
        /// Test: RetrieveDuplicates returns empty collection when no duplicate rules exist
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data
        /// 
        /// Duplicate detection requires active and published duplicaterule entities.
        /// When no rules are configured, no duplicates can be detected.
        /// </summary>
        [Fact]
        public void Should_Return_Empty_Collection_When_No_Duplicate_Rules_Exist()
        {
            // Arrange
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd"
            };
            _context.Initialize(new[] { account });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Empty(response.DuplicateCollection.Entities);
        }

        /// <summary>
        /// Test: RetrieveDuplicates finds exact match duplicates based on single attribute
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// When a duplicaterule is active and published (statecode=0, statuscode=2) with an 
        /// exact match condition (operatorcode=0) on a single attribute, records with matching 
        /// values in that attribute should be returned as duplicates.
        /// </summary>
        [Fact]
        public void Should_Find_Duplicate_With_Exact_Match_On_Single_Attribute()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = "ACC-001"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Corporation",
                AccountNumber = "ACC-001" // Duplicate account number
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Fabrikam Inc",
                AccountNumber = "ACC-002" // Different account number
            };

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
            // Create a duplicate detection rule for account entity
            // statecode = 0 (Active), statuscode = 2 (Published)
            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0), // Active
                ["statuscode"] = new OptionSetValue(2)  // Published
            };

            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
            // Create a condition to check for exact match on accountnumber
            // operatorcode = 0 (ExactMatch)
            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0) // ExactMatch
            };

            _context.Initialize(new Entity[] { account1, account2, account3, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Single(response.DuplicateCollection.Entities);
            Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
        }

        /// <summary>
        /// Test: RetrieveDuplicates finds duplicates matching multiple conditions
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// A duplicaterule can have multiple duplicaterulecondition entities.
        /// All conditions must match for a record to be considered a duplicate (AND logic).
        /// </summary>
        [Fact]
        public void Should_Find_Duplicate_Matching_Multiple_Conditions()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = "ACC-001",
                ["websiteurl"] = "www.contoso.com"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Corporation",
                AccountNumber = "ACC-001",
                ["websiteurl"] = "www.contoso.com" // Matches both conditions
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Inc",
                AccountNumber = "ACC-001",
                ["websiteurl"] = "www.different.com" // Matches only account number
            };

            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0),
                ["statuscode"] = new OptionSetValue(2)
            };

            // Multiple conditions - both must match
            var condition1 = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            var condition2 = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "websiteurl",
                ["matchingattributename"] = "websiteurl",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { 
                account1, account2, account3, 
                duplicateRule, condition1, condition2 
            });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Single(response.DuplicateCollection.Entities);
            Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
        }

        /// <summary>
        /// Test: RetrieveDuplicates excludes the source record from results
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
        /// 
        /// When checking for duplicates within the same entity, the BusinessEntity record itself 
        /// should not be included in the duplicate results even if it matches all conditions.
        /// </summary>
        [Fact]
        public void Should_Exclude_Source_Record_From_Duplicates()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = "ACC-001"
            };

            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0),
                ["statuscode"] = new OptionSetValue(2)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { account1, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Empty(response.DuplicateCollection.Entities);
        }

        /// <summary>
        /// Test: RetrieveDuplicates ignores inactive duplicate rules
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data
        /// 
        /// Only duplicate rules with statecode=0 (Active) and statuscode=2 (Published) are evaluated.
        /// Inactive or unpublished rules should not affect duplicate detection.
        /// </summary>
        [Fact]
        public void Should_Ignore_Inactive_Duplicate_Rules()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = "ACC-001"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Corporation",
                AccountNumber = "ACC-001"
            };

            // Inactive rule (statecode = 1)
            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(1), // Inactive
                ["statuscode"] = new OptionSetValue(2)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { account1, account2, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert - Should be empty because rule is inactive
            Assert.NotNull(response.DuplicateCollection);
            Assert.Empty(response.DuplicateCollection.Entities);
        }

        /// <summary>
        /// Test: RetrieveDuplicates ignores unpublished duplicate rules
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/detect-duplicate-data
        /// 
        /// Duplicate rules must be published (statuscode=2) to be active.
        /// Unpublished rules (statuscode=0 for Draft or statuscode=1 for Inactive) should not affect detection.
        /// </summary>
        [Fact]
        public void Should_Ignore_Unpublished_Duplicate_Rules()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = "ACC-001"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Corporation",
                AccountNumber = "ACC-001"
            };

            // Unpublished rule (statuscode = 0 for Draft)
            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0), // Active
                ["statuscode"] = new OptionSetValue(0)  // Draft (not published)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { account1, account2, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert - Should be empty because rule is not published
            Assert.NotNull(response.DuplicateCollection);
            Assert.Empty(response.DuplicateCollection.Entities);
        }

        /// <summary>
        /// Test: RetrieveDuplicates handles null attribute values correctly
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// Null values should not match other null values or non-null values in duplicate detection.
        /// Records with null values in comparison attributes should not be considered duplicates.
        /// </summary>
        [Fact]
        public void Should_Not_Match_Records_With_Null_Attributes()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                AccountNumber = null  // Null value
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Fabrikam Inc",
                AccountNumber = null  // Also null
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Adventure Works",
                AccountNumber = "ACC-001"  // Non-null
            };

            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0),
                ["statuscode"] = new OptionSetValue(2)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { account1, account2, account3, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert - Should be empty because null values don't match
            Assert.NotNull(response.DuplicateCollection);
            Assert.Empty(response.DuplicateCollection.Entities);
        }

        /// <summary>
        /// Test: RetrieveDuplicates finds duplicates across different entity types
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// Duplicate rules can check for duplicates between different entity types.
        /// For example, checking if a Contact's email matches an Account's email address.
        /// </summary>
        [Fact]
        public void Should_Find_Duplicates_Across_Different_Entity_Types()
        {
            // Arrange
            var contact1 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                EMailAddress1 = "john@contoso.com"
            };

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd",
                ["emailaddress1"] = "john@contoso.com"  // Matching email
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Fabrikam Inc",
                ["emailaddress1"] = "info@fabrikam.com"  // Different email
            };

            // Rule for checking contact email against account email
            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Contact.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0),
                ["statuscode"] = new OptionSetValue(2)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "emailaddress1",
                ["matchingattributename"] = "emailaddress1",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { contact1, account1, account2, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = contact1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Single(response.DuplicateCollection.Entities);
            Assert.Equal(account1.Id, response.DuplicateCollection.Entities[0].Id);
        }

        /// <summary>
        /// Test: RetrieveDuplicates throws exception when BusinessEntity is null
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
        /// 
        /// BusinessEntity is a required property. The request should fail if it's not provided.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_BusinessEntity_Is_Null()
        {
            // Arrange
            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = null,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Execute(request));
        }

        /// <summary>
        /// Test: RetrieveDuplicates throws exception when MatchingEntityName is null
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveduplicatesrequest
        /// 
        /// MatchingEntityName is a required property. The request should fail if it's not provided.
        /// </summary>
        [Fact]
        public void Should_Throw_Exception_When_MatchingEntityName_Is_Null()
        {
            // Arrange
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Ltd"
            };

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account,
                MatchingEntityName = null,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Execute(request));
        }

        /// <summary>
        /// Test: RetrieveDuplicates performs case-insensitive comparison
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/duplicaterule-entities
        /// 
        /// String comparisons in duplicate detection should be case-insensitive.
        /// "CONTOSO" should match "contoso" and "Contoso".
        /// </summary>
        [Fact]
        public void Should_Perform_Case_Insensitive_Comparison()
        {
            // Arrange
            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "CONTOSO LTD",
                AccountNumber = "ACC-001"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Contoso Corporation",
                AccountNumber = "acc-001"  // Different case
            };

            var duplicateRule = new Entity("duplicaterule")
            {
                Id = Guid.NewGuid(),
                ["baseentityname"] = Account.EntityLogicalName,
                ["matchingentityname"] = Account.EntityLogicalName,
                ["statecode"] = new OptionSetValue(0),
                ["statuscode"] = new OptionSetValue(2)
            };

            var duplicateRuleCondition = new Entity("duplicaterulecondition")
            {
                Id = Guid.NewGuid(),
                ["duplicateruleid"] = duplicateRule.ToEntityReference(),
                ["baseattributename"] = "accountnumber",
                ["matchingattributename"] = "accountnumber",
                ["operatorcode"] = new OptionSetValue(0)
            };

            _context.Initialize(new Entity[] { account1, account2, duplicateRule, duplicateRuleCondition });

            var request = new RetrieveDuplicatesRequest
            {
                BusinessEntity = account1,
                MatchingEntityName = Account.EntityLogicalName,
                PagingInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo { PageNumber = 1, Count = 50 }
            };

            // Act
            var response = (RetrieveDuplicatesResponse)_service.Execute(request);

            // Assert
            Assert.NotNull(response.DuplicateCollection);
            Assert.Single(response.DuplicateCollection.Entities);
            Assert.Equal(account2.Id, response.DuplicateCollection.Entities[0].Id);
        }
    }
}
