using Fake4Dataverse.BusinessRules;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.BusinessRules
{
    /// <summary>
    /// Direct tests for the BusinessRuleExecutor without going through the CRUD pipeline.
    /// These tests verify that the core business rule logic works correctly.
    /// </summary>
    public class BusinessRuleExecutorDirectTests
    {
        [Fact]
        public void Executor_Should_Execute_Simple_Rule_With_Action()
        {
            // Arrange
            var executor = new BusinessRuleExecutor(null);
            var rule = new BusinessRuleDefinition
            {
                Name = "TestRule",
                EntityLogicalName = "account",
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>(),
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "description",
                        Value = "Test Description"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            
            // Act
            var result = executor.ExecuteRules(entity, BusinessRuleTrigger.OnCreate, isServerSide: true);
            
            // Assert
            Assert.True(result.IsValid);
            Assert.False(result.HasErrors);
            Assert.True(entity.Contains("description"));
            Assert.Equal("Test Description", entity["description"]);
        }
        
        [Fact]
        public void Executor_Should_Execute_Rule_Only_When_Condition_Is_Met()
        {
            // Arrange
            var executor = new BusinessRuleExecutor(null);
            var rule = new BusinessRuleDefinition
            {
                Name = "ConditionalRule",
                EntityLogicalName = "account",
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
                        Value = "On Hold"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            // Test 1: Condition is met
            var entity1 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["creditonhold"] = true
            };
            
            var result1 = executor.ExecuteRules(entity1, BusinessRuleTrigger.OnCreate, isServerSide: true);
            Assert.True(entity1.Contains("description"));
            Assert.Equal("On Hold", entity1["description"]);
            
            // Test 2: Condition is NOT met
            var entity2 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["creditonhold"] = false
            };
            
            var result2 = executor.ExecuteRules(entity2, BusinessRuleTrigger.OnCreate, isServerSide: true);
            Assert.False(entity2.Contains("description"));
        }
        
        [Fact]
        public void Executor_Should_Generate_Error_When_ShowErrorMessage_Action_Executes()
        {
            // Arrange
            var executor = new BusinessRuleExecutor(null);
            var rule = new BusinessRuleDefinition
            {
                Name = "ValidationRule",
                EntityLogicalName = "account",
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
                        Message = "Name is required"
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid()
                // name not set
            };
            
            // Act
            var result = executor.ExecuteRules(entity, BusinessRuleTrigger.OnCreate, isServerSide: true);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Single(result.Errors);
            Assert.Contains("Name is required", result.Errors[0].Message);
        }
        
        [Fact]
        public void Executor_Should_Execute_ElseActions_When_Conditions_Not_Met()
        {
            // Arrange
            var executor = new BusinessRuleExecutor(null);
            var rule = new BusinessRuleDefinition
            {
                Name = "ElseRule",
                EntityLogicalName = "account",
                Conditions = new System.Collections.Generic.List<BusinessRuleCondition>
                {
                    new BusinessRuleCondition
                    {
                        FieldName = "revenue",
                        Operator = ConditionOperator.GreaterThan,
                        Value = 1000000
                    }
                },
                Actions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "accountcategorycode",
                        Value = new OptionSetValue(1) // Enterprise
                    }
                },
                ElseActions = new System.Collections.Generic.List<BusinessRuleAction>
                {
                    new BusinessRuleAction
                    {
                        ActionType = BusinessRuleActionType.SetFieldValue,
                        FieldName = "accountcategorycode",
                        Value = new OptionSetValue(2) // Small Business
                    }
                }
            };
            
            executor.RegisterRule(rule);
            
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["revenue"] = 50000 // Less than 1000000
            };
            
            // Act
            var result = executor.ExecuteRules(entity, BusinessRuleTrigger.OnCreate, isServerSide: true);
            
            // Assert
            Assert.True(entity.Contains("accountcategorycode"));
            Assert.Equal(2, ((OptionSetValue)entity["accountcategorycode"]).Value);
        }
    }
}
