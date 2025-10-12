using Fake4Dataverse.Middleware;
using Fake4Dataverse.RollupFields;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.RollupFields
{
    /// <summary>
    /// Tests for automatic rollup field refresh when related records change.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "When you create, update, or delete a record, the rollup columns on related records are recalculated"
    /// 
    /// These tests verify that rollup fields are automatically recalculated when related child records
    /// are created, updated, or deleted.
    /// </summary>
    public class RollupFieldAutoRefreshTests
    {
        [Fact]
        public void Should_Auto_Refresh_Rollup_When_Related_Record_Created()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register rollup field: count of contacts
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

            context.Initialize(new[] { account });

            // Initial calculation - should be 0
            evaluator.EvaluateRollupFields(account);
            Assert.Equal(0, context.Data["account"][accountId].GetAttributeValue<int>("contactcount"));

            // Act - Create a related contact
            var contactId = service.Create(new Entity("contact")
            {
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            });

            // Assert - Rollup should be automatically refreshed
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(1, updatedAccount.GetAttributeValue<int>("contactcount"));
        }

        [Fact]
        public void Should_Auto_Refresh_Rollup_When_Related_Record_Updated()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register rollup field: sum of opportunity values
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

            var oppId = Guid.NewGuid();
            var opportunity = new Entity("opportunity")
            {
                Id = oppId,
                ["name"] = "Test Opp",
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, opportunity });

            // Initial calculation
            evaluator.EvaluateRollupFields(account);
            Assert.Equal(100000m, context.Data["account"][accountId].GetAttributeValue<decimal>("totalrevenue"));

            // Act - Update the opportunity value
            service.Update(new Entity("opportunity")
            {
                Id = oppId,
                ["estimatedvalue"] = 150000m
            });

            // Assert - Rollup should be automatically refreshed
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(150000m, updatedAccount.GetAttributeValue<decimal>("totalrevenue"));
        }

        [Fact]
        public void Should_Auto_Refresh_Rollup_When_Related_Record_Deleted()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register rollup field: count of contacts
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

            var contact1Id = Guid.NewGuid();
            var contact1 = new Entity("contact")
            {
                Id = contact1Id,
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var contact2Id = Guid.NewGuid();
            var contact2 = new Entity("contact")
            {
                Id = contact2Id,
                ["firstname"] = "Jane",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, contact1, contact2 });

            // Initial calculation - should be 2
            evaluator.EvaluateRollupFields(account);
            Assert.Equal(2, context.Data["account"][accountId].GetAttributeValue<int>("contactcount"));

            // Act - Delete one contact
            service.Delete("contact", contact1Id);

            // Assert - Rollup should be automatically refreshed to 1
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(1, updatedAccount.GetAttributeValue<int>("contactcount"));
        }

        [Fact]
        public void Should_Auto_Refresh_Multiple_Rollup_Fields_On_Same_Entity()
        {
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register multiple rollup fields
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

            context.Initialize(new[] { account });

            // Initial calculation
            evaluator.EvaluateRollupFields(account);
            Assert.Equal(0, context.Data["account"][accountId].GetAttributeValue<int>("opportunitycount"));

            // Act - Create a related opportunity
            var oppId = service.Create(new Entity("opportunity")
            {
                ["name"] = "Test Opp",
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            });

            // Assert - Both rollup fields should be automatically refreshed
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(1, updatedAccount.GetAttributeValue<int>("opportunitycount"));
            Assert.Equal(100000m, updatedAccount.GetAttributeValue<decimal>("totalrevenue"));
        }

        [Fact]
        public void Should_Auto_Refresh_When_Lookup_Field_Changes()
        {
            // Test scenario: Moving an opportunity from one account to another
            // should trigger rollup refresh on both accounts
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register rollup field: sum of opportunity values
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

            var account1Id = Guid.NewGuid();
            var account1 = new Entity("account")
            {
                Id = account1Id,
                ["name"] = "Account 1"
            };

            var account2Id = Guid.NewGuid();
            var account2 = new Entity("account")
            {
                Id = account2Id,
                ["name"] = "Account 2"
            };

            var oppId = Guid.NewGuid();
            var opportunity = new Entity("opportunity")
            {
                Id = oppId,
                ["name"] = "Test Opp",
                ["estimatedvalue"] = 100000m,
                ["parentaccountid"] = new EntityReference("account", account1Id)
            };

            context.Initialize(new[] { account1, account2, opportunity });

            // Initial calculation
            evaluator.EvaluateRollupFields(account1);
            evaluator.EvaluateRollupFields(account2);
            Assert.Equal(100000m, context.Data["account"][account1Id].GetAttributeValue<decimal>("totalrevenue"));
            Assert.False(context.Data["account"][account2Id].Contains("totalrevenue") && 
                        context.Data["account"][account2Id]["totalrevenue"] != null);

            // Act - Move opportunity to account2
            service.Update(new Entity("opportunity")
            {
                Id = oppId,
                ["parentaccountid"] = new EntityReference("account", account2Id)
            });

            // Assert - Account 2 should now have the revenue (automatically refreshed)
            var updatedAccount2 = context.Data["account"][account2Id];
            Assert.Equal(100000m, updatedAccount2.GetAttributeValue<decimal>("totalrevenue"));
        }

        [Fact]
        public void Should_Not_Fail_When_Parent_Entity_Has_No_Rollup_Fields()
        {
            // Verify that auto-refresh doesn't break when the parent entity has no rollup fields defined
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            // Note: NOT registering any rollup fields

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            context.Initialize(new[] { account });

            // Act - Create a related contact (should not throw)
            var contactId = service.Create(new Entity("contact")
            {
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            });

            // Assert - Operation should succeed without errors
            Assert.NotEqual(Guid.Empty, contactId);
        }

        [Fact]
        public void Should_Auto_Refresh_With_State_Filter()
        {
            // Verify that auto-refresh respects state filters
            
            // Arrange
            var context = (XrmFakedContext)XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            var evaluator = context.RollupFieldEvaluator;

            // Register rollup field: count only active contacts
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

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John",
                ["statecode"] = new OptionSetValue(0), // Active
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new[] { account, contact });

            // Initial calculation - should be 1
            evaluator.EvaluateRollupFields(account);
            Assert.Equal(1, context.Data["account"][accountId].GetAttributeValue<int>("activecontactcount"));

            // Act - Deactivate the contact
            service.Update(new Entity("contact")
            {
                Id = contactId,
                ["statecode"] = new OptionSetValue(1) // Inactive
            });

            // Assert - Rollup should be automatically refreshed to 0
            var updatedAccount = context.Data["account"][accountId];
            Assert.Equal(0, updatedAccount.GetAttributeValue<int>("activecontactcount"));
        }
    }
}
