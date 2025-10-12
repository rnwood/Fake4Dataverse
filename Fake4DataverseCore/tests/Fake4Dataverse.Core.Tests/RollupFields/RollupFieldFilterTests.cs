using Fake4Dataverse.Middleware;
using Fake4Dataverse.RollupFields;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.RollupFields
{
    /// <summary>
    /// Tests for rollup field filtering capabilities.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "You can optionally specify filters to restrict which records are included"
    /// 
    /// These tests verify state filters and custom filters.
    /// </summary>
    public class RollupFieldFilterTests
    {
        [Fact]
        public void Should_Filter_Active_Records_Only()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "Use filters to specify whether to include only active records, only inactive records, or all records"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "activecontactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int),
                StateFilter = RollupStateFilter.Active
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var activeContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Active",
                ["statecode"] = new OptionSetValue(0),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var inactiveContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Inactive",
                ["statecode"] = new OptionSetValue(1),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, activeContact, inactiveContact });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(1, account.GetAttributeValue<int>("activecontactcount"));
        }

        [Fact]
        public void Should_Filter_Inactive_Records_Only()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "Use filters to specify whether to include only active records, only inactive records, or all records"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "inactivecontactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int),
                StateFilter = RollupStateFilter.Inactive
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var activeContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Active",
                ["statecode"] = new OptionSetValue(0),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var inactiveContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Inactive",
                ["statecode"] = new OptionSetValue(1),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, activeContact, inactiveContact });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(1, account.GetAttributeValue<int>("inactivecontactcount"));
        }

        [Fact]
        public void Should_Include_All_Records_When_StateFilter_Is_All()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "Use filters to specify whether to include only active records, only inactive records, or all records"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "allcontactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int),
                StateFilter = RollupStateFilter.All
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var activeContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Active",
                ["statecode"] = new OptionSetValue(0),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var inactiveContact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Inactive",
                ["statecode"] = new OptionSetValue(1),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, activeContact, inactiveContact });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(2, account.GetAttributeValue<int>("allcontactcount"));
        }

        [Fact]
        public void Should_Apply_Custom_Filter()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "You can optionally specify filters to restrict which records are included"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field with custom filter: only opportunities > 100k
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "largedealcount",
                RelatedEntityLogicalName = "opportunity",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int),
                Filter = entity =>
                {
                    var value = entity.GetAttributeValue<decimal>("estimatedvalue");
                    return value > 100000m;
                }
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var smallDeal = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 50000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var largeDeal1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 150000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var largeDeal2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 200000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, smallDeal, largeDeal1, largeDeal2 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(2, account.GetAttributeValue<int>("largedealcount"));
        }

        [Fact]
        public void Should_Apply_Custom_Filter_And_Sum()
        {
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field with custom filter: sum only won opportunities
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "wonrevenue",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(decimal),
                Filter = entity =>
                {
                    var statusCode = entity.GetAttributeValue<OptionSetValue>("statuscode");
                    return statusCode != null && statusCode.Value == 3; // Won
                }
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var wonDeal = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 100000m,
                ["statuscode"] = new OptionSetValue(3), // Won
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var openDeal = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 50000m,
                ["statuscode"] = new OptionSetValue(1), // Open
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var lostDeal = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 75000m,
                ["statuscode"] = new OptionSetValue(4), // Lost
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, wonDeal, openDeal, lostDeal });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(100000m, account.GetAttributeValue<decimal>("wonrevenue"));
        }

        [Fact]
        public void Should_Combine_StateFilter_And_CustomFilter()
        {
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field with both state and custom filter
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "activehighincome",
                RelatedEntityLogicalName = "contact",
                AggregateAttributeLogicalName = "annualincome",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(Money),
                StateFilter = RollupStateFilter.Active,
                Filter = entity =>
                {
                    var income = entity.GetAttributeValue<Money>("annualincome");
                    return income != null && income.Value > 50000m;
                }
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            // Active, high income - should be included
            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["statecode"] = new OptionSetValue(0),
                ["annualincome"] = new Money(75000m),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            // Active, low income - should be excluded
            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Jane",
                ["statecode"] = new OptionSetValue(0),
                ["annualincome"] = new Money(40000m),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            // Inactive, high income - should be excluded
            var contact3 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Bob",
                ["statecode"] = new OptionSetValue(1),
                ["annualincome"] = new Money(100000m),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, contact1, contact2, contact3 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            var total = account.GetAttributeValue<Money>("activehighincome");
            Assert.NotNull(total);
            Assert.Equal(75000m, total.Value);
        }
    }
}
