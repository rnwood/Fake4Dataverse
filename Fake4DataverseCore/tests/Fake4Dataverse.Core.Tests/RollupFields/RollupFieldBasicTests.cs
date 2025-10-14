using Fake4Dataverse.Middleware;
using Fake4Dataverse.RollupFields;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.RollupFields
{
    /// <summary>
    /// Tests for basic rollup field functionality.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Define rollup columns to aggregate values - Create columns that automatically calculate values by aggregating 
    /// values from related child records."
    /// 
    /// These tests verify the core aggregate functions: SUM, COUNT, MIN, MAX, and AVG.
    /// </summary>
    public class RollupFieldBasicTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Count_Related_Records()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "COUNT: Counts all related records"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: count of related contacts
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "contactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Jane",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var contact3 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Bob",
                ["parentcustomerid"] = new EntityReference("account", Guid.NewGuid()) // Different account
            };

            context.Initialize(new[] { account, contact1, contact2, contact3 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(2, account.GetAttributeValue<int>("contactcount"));
        }

        [Fact]
        public void Should_Sum_Decimal_Values()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "SUM: Totals the values of the attribute in the related records"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: sum of revenue from opportunities
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "totalrevenue",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(decimal)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var opp1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Opp 1",
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Opp 2",
                ["estimatedvalue"] = 50000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opp1, opp2 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(150000m, account.GetAttributeValue<decimal>("totalrevenue"));
        }

        [Fact]
        public void Should_Sum_Money_Values()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "SUM: Applicable to Currency (Money) fields"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: sum of annual income from contacts
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "totalincome",
                RelatedEntityLogicalName = "contact",
                AggregateAttributeLogicalName = "annualincome",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(Money)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["annualincome"] = new Money(75000m),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Jane",
                ["annualincome"] = new Money(85000m),
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, contact1, contact2 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            var totalIncome = account.GetAttributeValue<Money>("totalincome");
            Assert.NotNull(totalIncome);
            Assert.Equal(160000m, totalIncome.Value);
        }

        [Fact]
        public void Should_Calculate_Average()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "AVG: Calculates the average value"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: average deal size
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "avgdealsize",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Avg,
                ResultType = typeof(decimal)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var opp1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 50000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp3 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 75000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opp1, opp2, opp3 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(75000m, account.GetAttributeValue<decimal>("avgdealsize"));
        }

        [Fact]
        public void Should_Find_Minimum_Value()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "MIN: Returns the minimum value"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: earliest close date
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "earliestclosedate",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedclosedate",
                AggregateFunction = RollupAggregateFunction.Min,
                ResultType = typeof(DateTime)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var opp1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedclosedate"] = new DateTime(2025, 3, 15),
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedclosedate"] = new DateTime(2025, 1, 10),
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp3 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedclosedate"] = new DateTime(2025, 6, 20),
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opp1, opp2, opp3 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(new DateTime(2025, 1, 10), account.GetAttributeValue<DateTime>("earliestclosedate"));
        }

        [Fact]
        public void Should_Find_Maximum_Value()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "MAX: Returns the maximum value"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define rollup field: largest deal value
            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "largestdeal",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Max,
                ResultType = typeof(decimal)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var opp1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 250000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp3 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 75000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opp1, opp2, opp3 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(250000m, account.GetAttributeValue<decimal>("largestdeal"));
        }

        [Fact]
        public void Should_Return_Null_When_No_Related_Records()
        {
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "totalrevenue",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(decimal)
            };
            evaluator.RegisterRollupField(definition);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            context.Initialize(new[] { account });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.False(account.Contains("totalrevenue") && account["totalrevenue"] != null);
        }

        [Fact]
        public void Should_Return_Zero_Count_When_No_Related_Records()
        {
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "contactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int)
            };
            evaluator.RegisterRollupField(definition);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            context.Initialize(new[] { account });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(0, account.GetAttributeValue<int>("contactcount"));
        }

        [Fact]
        public void TriggerRollupCalculation_Should_Calculate_For_Specific_Record()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "You can manually trigger an immediate calculation of rollup columns using the CalculateRollupField message"
            
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            var definition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "contactcount",
                RelatedEntityLogicalName = "contact",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int)
            };
            evaluator.RegisterRollupField(definition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, contact1 });

            // Act
            evaluator.TriggerRollupCalculation("account", accountId);

            // Assert - Retrieve the account from context to get the updated rollup value
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(1, updatedAccount.GetAttributeValue<int>("contactcount"));
        }

        [Fact]
        public void Should_Handle_Multiple_Rollup_Fields_On_Same_Entity()
        {
            // Arrange
            // Use context from base class
            var context = (XrmFakedContext)_context;
            var evaluator = context.RollupFieldEvaluator;

            // Define multiple rollup fields
            var countDefinition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "opportunitycount",
                RelatedEntityLogicalName = "opportunity",
                AggregateFunction = RollupAggregateFunction.Count,
                ResultType = typeof(int)
            };

            var sumDefinition = new RollupFieldDefinition
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "totalrevenue",
                RelatedEntityLogicalName = "opportunity",
                AggregateAttributeLogicalName = "estimatedvalue",
                AggregateFunction = RollupAggregateFunction.Sum,
                ResultType = typeof(decimal)
            };

            evaluator.RegisterRollupField(countDefinition);
            evaluator.RegisterRollupField(sumDefinition);

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var opp1 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            var opp2 = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["estimatedvalue"] = 50000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opp1, opp2 });

            // Act
            evaluator.EvaluateRollupFields(account);

            // Assert
            Assert.Equal(2, account.GetAttributeValue<int>("opportunitycount"));
            Assert.Equal(150000m, account.GetAttributeValue<decimal>("totalrevenue"));
        }
    }
}
