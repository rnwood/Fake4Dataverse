using Fake4Dataverse.BusinessRules;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.BusinessRules
{
    /// <summary>
    /// Tests for business rule execution functionality.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
    /// "Business rules provide a simple interface to implement and maintain fast-changing and commonly used rules.
    /// They can be applied to a table (entity) or a form depending on the scope selected."
    /// 
    /// These tests verify that business rules execute correctly during CRUD operations.
    /// </summary>
    public class BusinessRuleExecutorTests
    {
        [Fact]
        public void Should_Execute_Set_Field_Value_Action_When_Condition_Is_Met()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-field-value
            // "Set Field Value: Set a column to a specific value, clear a column value, or set a column value based on another column"
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "SetDescriptionRule",
                EntityLogicalName = "account",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnCreate,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "creditonhold",
                        Operator = ConditionOperator.Equal,
                        Value = true
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "description",
                        Value = "Account is on credit hold"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["creditonhold"] = true
            };
            
            // Act
            var service = context.GetOrganizationService();
            var createdId = service.Create(account);
            
            // Assert
            var retrieved = service.Retrieve("account", createdId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("Account is on credit hold", retrieved["description"]);
        }
        
        [Fact]
        public void Should_Throw_Error_When_Business_Rule_Validation_Fails()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#show-error-message
            // "Show Error Message: Display a custom error message and prevent the record from being saved"
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "ValidateNameLengthRule",
                EntityLogicalName = "account",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnCreate,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "name",
                        Operator = ConditionOperator.Null
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.ShowErrorMessage,
                        FieldName = "name",
                        Message = "Account name is required"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var account = new Entity("account")
            {
                Id = Guid.NewGuid()
                // name is not set, so condition will be true
            };
            
            // Act & Assert
            var service = context.GetOrganizationService();
            var ex = Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(() =>
            {
                service.Create(account);
            });
            
            Assert.Contains("Account name is required", ex.Message);
        }
        
        [Fact]
        public void Should_Execute_Multiple_Actions_When_Conditions_Are_Met()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#add-actions
            // "Add one or more actions that should be performed when the conditions are true"
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "MultiActionRule",
                EntityLogicalName = "contact",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnCreate,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "lastname",
                        Operator = ConditionOperator.NotNull
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "fullname",
                        Value = "Mr. Smith"
                    },
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "statuscode",
                        Value = new OptionSetValue(1)
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["lastname"] = "Smith"
            };
            
            // Act
            var service = context.GetOrganizationService();
            var createdId = service.Create(contact);
            
            // Assert
            var retrieved = service.Retrieve("contact", createdId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("Mr. Smith", retrieved["fullname"]);
            Assert.Equal(1, ((OptionSetValue)retrieved["statuscode"]).Value);
        }
        
        [Fact]
        public void Should_Execute_Else_Actions_When_Conditions_Are_Not_Met()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#else-actions
            // "You can define actions to take when conditions are not met using the Else branch"
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "ConditionalSetRule",
                EntityLogicalName = "account",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnCreate,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "revenue",
                        Operator = ConditionOperator.GreaterThan,
                        Value = 1000000m
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "customertypecode",
                        Value = new OptionSetValue(3) // Enterprise
                    }
                },
                ElseActions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "customertypecode",
                        Value = new OptionSetValue(1) // Small Business
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Small Company",
                ["revenue"] = new Money(50000m)
            };
            
            // Act
            var service = context.GetOrganizationService();
            var createdId = service.Create(account);
            
            // Assert
            var retrieved = service.Retrieve("account", createdId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal(1, ((OptionSetValue)retrieved["customertypecode"]).Value);
        }
        
        [Fact]
        public void Should_Execute_Rules_On_Update_When_Configured()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#triggers
            // "Business rules can be configured to run on specific events like Create, Update, or field changes"
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "UpdateRule",
                EntityLogicalName = "account",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnUpdate,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "telephone1",
                        Operator = ConditionOperator.Null
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "telephone1",
                        Value = "000-000-0000"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["telephone1"] = "555-1234"
            };
            
            // Act - Create should not trigger the rule (it's OnUpdate only)
            var service = context.GetOrganizationService();
            var createdId = service.Create(account);
            
            // Update to trigger the rule
            var updateEntity = new Entity("account")
            {
                Id = createdId,
                ["address1_city"] = "Seattle", // Some other field
                ["telephone1"] = null // Clear the phone
            };
            service.Update(updateEntity);
            
            // Assert
            var retrieved = service.Retrieve("account", createdId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("000-000-0000", retrieved["telephone1"]);
        }
        
        [Fact]
        public void Should_Use_AND_Logic_By_Default()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#condition-logic
            // "By default, all conditions must be true (AND). You can change to OR logic where any condition being true is sufficient."
            
            // Arrange
            var context = new XrmFakedContext();
            var executor = context.BusinessRuleExecutor;
            
            var rule = new BusinessRuleDefinition
            {
                Name = "ANDLogicRule",
                EntityLogicalName = "account",
                Scope = BusinessRuleScope.Entity,
                Trigger = BusinessRuleTrigger.OnCreate,
                UseAndLogic = true,
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "name",
                        Operator = ConditionOperator.NotNull
                    },
                    new BusinessRuleCondition
                    {
                        FieldName = "creditlimit",
                        Operator = ConditionOperator.GreaterThan,
                        Value = 0m
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "statecode",
                        Value = new OptionSetValue(0)
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            // Test 1: Both conditions met - action should execute
            var account1 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["creditlimit"] = new Money(1000m)
            };
            
            var service = context.GetOrganizationService();
            var id1 = service.Create(account1);
            var retrieved1 = service.Retrieve("account", id1, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.True(retrieved1.Contains("statecode"));
            
            // Test 2: Only one condition met - action should NOT execute
            var account2 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account 2",
                ["creditlimit"] = new Money(0m) // This fails the second condition
            };
            
            var id2 = service.Create(account2);
            var retrieved2 = service.Retrieve("account", id2, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            // statecode would only be set by the rule, so it should not exist if rule didn't run
            Assert.False(retrieved2.Contains("statecode") && retrieved2["statecode"] is OptionSetValue);
        }
    }
}
