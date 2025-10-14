using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for control flow actions in Cloud Flows: Condition, Switch, Parallel, Do Until.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-switch-action
    /// Reference: https://learn.microsoft.com/en-us/power-automate/use-parallel-branches
    /// Reference: https://learn.microsoft.com/en-us/power-automate/do-until-loop
    /// 
    /// These tests verify that conditional logic, branching, and loop constructs work correctly.
    /// </summary>
    public class ControlFlowActionTests
    {
        #region Condition Action Tests

        [Fact]
        public void Should_Execute_Condition_True_Branch()
        {
            // Reference: Condition action executes TrueActions when expression evaluates to true
            // https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["amount"] = 1500
            };

            var trueAction = new ComposeAction
            {
                Name = "High_Value",
                Inputs = "High value opportunity"
            };

            var falseAction = new ComposeAction
            {
                Name = "Low_Value",
                Inputs = "Low value opportunity"
            };

            var condition = new ConditionAction
            {
                Name = "Check_Amount",
                Expression = "@greater(triggerBody()['amount'], 1000)",
                TrueActions = new List<IFlowAction> { trueAction },
                FalseActions = new List<IFlowAction> { falseAction }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Condition_True",
                Trigger = new DataverseTrigger { EntityLogicalName = "opportunity", Message = "Create" },
                Actions = new List<IFlowAction> { condition }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            Assert.Single(result.ActionResults);
            var conditionResult = result.ActionResults[0];
            Assert.Equal("Check_Amount", conditionResult.ActionName);
            Assert.True(conditionResult.Succeeded);
            Assert.True((bool)conditionResult.Outputs["conditionResult"]);
            Assert.Equal("true", conditionResult.Outputs["branchExecuted"]);
        }

        [Fact]
        public void Should_Execute_Condition_False_Branch()
        {
            // Reference: Condition action executes FalseActions when expression evaluates to false
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["amount"] = 500
            };

            var trueAction = new ComposeAction
            {
                Name = "High_Value",
                Inputs = "High value"
            };

            var falseAction = new ComposeAction
            {
                Name = "Low_Value",
                Inputs = "Low value"
            };

            var condition = new ConditionAction
            {
                Name = "Check_Amount",
                Expression = "@greater(triggerBody()['amount'], 1000)",
                TrueActions = new List<IFlowAction> { trueAction },
                FalseActions = new List<IFlowAction> { falseAction }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Condition_False",
                Trigger = new DataverseTrigger { EntityLogicalName = "opportunity", Message = "Create" },
                Actions = new List<IFlowAction> { condition }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var conditionResult = result.ActionResults[0];
            Assert.False((bool)conditionResult.Outputs["conditionResult"]);
            Assert.Equal("false", conditionResult.Outputs["branchExecuted"]);
        }

        [Fact]
        public void Should_Execute_Multiple_Actions_In_Condition_Branch()
        {
            // Reference: Each branch can contain multiple actions that execute sequentially
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["status"] = "active"
            };

            var action1 = new ComposeAction
            {
                Name = "Step1",
                Inputs = "First step"
            };

            var action2 = new ComposeAction
            {
                Name = "Step2",
                Inputs = "Second step"
            };

            var condition = new ConditionAction
            {
                Name = "Check_Status",
                Expression = "@equals(triggerBody()['status'], 'active')",
                TrueActions = new List<IFlowAction> { action1, action2 },
                FalseActions = new List<IFlowAction>()
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Multiple_Actions_In_Branch",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { condition }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var conditionResult = result.ActionResults[0];
            var branchResults = conditionResult.Outputs["branchResults"] as List<Dictionary<string, object>>;
            Assert.NotNull(branchResults);
            Assert.Equal(2, branchResults.Count);
        }

        [Fact]
        public void Should_Handle_Nested_Conditions()
        {
            // Reference: Conditions can be nested within other conditions
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["value"] = 2000
            };

            var veryHighAction = new ComposeAction
            {
                Name = "Very_High",
                Inputs = "Very high value"
            };

            var highAction = new ComposeAction
            {
                Name = "High",
                Inputs = "High value"
            };

            var innerCondition = new ConditionAction
            {
                Name = "Check_Very_High",
                Expression = "@greater(triggerBody()['value'], 5000)",
                TrueActions = new List<IFlowAction> { veryHighAction },
                FalseActions = new List<IFlowAction> { highAction }
            };

            var lowAction = new ComposeAction
            {
                Name = "Low",
                Inputs = "Low value"
            };

            var outerCondition = new ConditionAction
            {
                Name = "Check_High",
                Expression = "@greater(triggerBody()['value'], 1000)",
                TrueActions = new List<IFlowAction> { innerCondition },
                FalseActions = new List<IFlowAction> { lowAction }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Nested_Conditions",
                Trigger = new DataverseTrigger { EntityLogicalName = "opportunity", Message = "Create" },
                Actions = new List<IFlowAction> { outerCondition }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
        }

        #endregion

        #region Switch Action Tests

        [Fact]
        public void Should_Execute_Matching_Switch_Case()
        {
            // Reference: Switch action executes actions for the matching case
            // https://learn.microsoft.com/en-us/power-automate/use-switch-action
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["priority"] = "high"
            };

            var highAction = new ComposeAction
            {
                Name = "High_Priority_Action",
                Inputs = "Processing high priority"
            };

            var mediumAction = new ComposeAction
            {
                Name = "Medium_Priority_Action",
                Inputs = "Processing medium priority"
            };

            var lowAction = new ComposeAction
            {
                Name = "Low_Priority_Action",
                Inputs = "Processing low priority"
            };

            var switchAction = new SwitchAction
            {
                Name = "Route_By_Priority",
                Expression = "@triggerBody()['priority']",
                Cases = new Dictionary<string, IList<IFlowAction>>
                {
                    ["high"] = new List<IFlowAction> { highAction },
                    ["medium"] = new List<IFlowAction> { mediumAction },
                    ["low"] = new List<IFlowAction> { lowAction }
                }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Switch_Matching_Case",
                Trigger = new DataverseTrigger { EntityLogicalName = "task", Message = "Create" },
                Actions = new List<IFlowAction> { switchAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            Assert.Single(result.ActionResults);
            var switchResult = result.ActionResults[0];
            Assert.Equal("Route_By_Priority", switchResult.ActionName);
            Assert.Equal("high", switchResult.Outputs["switchValue"]);
            Assert.Equal("high", switchResult.Outputs["matchedCase"]);
        }

        [Fact]
        public void Should_Execute_Default_Case_When_No_Match()
        {
            // Reference: Switch action executes default actions when no case matches
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["status"] = "unknown"
            };

            var activeAction = new ComposeAction
            {
                Name = "Active_Action",
                Inputs = "Active processing"
            };

            var defaultAction = new ComposeAction
            {
                Name = "Default_Action",
                Inputs = "Default processing"
            };

            var switchAction = new SwitchAction
            {
                Name = "Route_By_Status",
                Expression = "@triggerBody()['status']",
                Cases = new Dictionary<string, IList<IFlowAction>>
                {
                    ["active"] = new List<IFlowAction> { activeAction }
                },
                DefaultActions = new List<IFlowAction> { defaultAction }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Switch_Default_Case",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { switchAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var switchResult = result.ActionResults[0];
            Assert.Equal("unknown", switchResult.Outputs["switchValue"]);
            Assert.Equal("default", switchResult.Outputs["matchedCase"]);
        }

        [Fact]
        public void Should_Execute_Multiple_Actions_In_Switch_Case()
        {
            // Reference: Each case can contain multiple sequential actions
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["type"] = "premium"
            };

            var action1 = new ComposeAction
            {
                Name = "Premium_Step1",
                Inputs = "Step 1"
            };

            var action2 = new ComposeAction
            {
                Name = "Premium_Step2",
                Inputs = "Step 2"
            };

            var switchAction = new SwitchAction
            {
                Name = "Process_By_Type",
                Expression = "@triggerBody()['type']",
                Cases = new Dictionary<string, IList<IFlowAction>>
                {
                    ["premium"] = new List<IFlowAction> { action1, action2 }
                }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Switch_Multiple_Actions",
                Trigger = new DataverseTrigger { EntityLogicalName = "contact", Message = "Create" },
                Actions = new List<IFlowAction> { switchAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var switchResult = result.ActionResults[0];
            var caseResults = switchResult.Outputs["caseResults"] as List<Dictionary<string, object>>;
            Assert.NotNull(caseResults);
            Assert.Equal(2, caseResults.Count);
        }

        #endregion

        #region Parallel Branch Action Tests

        [Fact]
        public void Should_Execute_All_Parallel_Branches()
        {
            // Reference: Parallel branches execute multiple independent action sequences
            // https://learn.microsoft.com/en-us/power-automate/use-parallel-branches
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["name"] = "Test"
            };

            var branch1Action = new ComposeAction
            {
                Name = "Branch1_Action",
                Inputs = "Branch 1 processing"
            };

            var branch2Action = new ComposeAction
            {
                Name = "Branch2_Action",
                Inputs = "Branch 2 processing"
            };

            var parallelAction = new ParallelBranchAction
            {
                Name = "Parallel_Processing",
                Branches = new List<ParallelBranch>
                {
                    new ParallelBranch
                    {
                        Name = "Branch1",
                        Actions = new List<IFlowAction> { branch1Action }
                    },
                    new ParallelBranch
                    {
                        Name = "Branch2",
                        Actions = new List<IFlowAction> { branch2Action }
                    }
                }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Parallel_Branches",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { parallelAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            Assert.Single(result.ActionResults);
            var parallelResult = result.ActionResults[0];
            Assert.Equal("Parallel_Processing", parallelResult.ActionName);
            var branchResults = parallelResult.Outputs["branchResults"] as List<Dictionary<string, object>>;
            Assert.NotNull(branchResults);
            Assert.Equal(2, branchResults.Count);
        }

        [Fact]
        public void Should_Execute_Multiple_Actions_In_Each_Branch()
        {
            // Reference: Each parallel branch can contain multiple sequential actions
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["value"] = 100
            };

            var parallelAction = new ParallelBranchAction
            {
                Name = "Parallel_Multi_Action",
                Branches = new List<ParallelBranch>
                {
                    new ParallelBranch
                    {
                        Name = "Branch1",
                        Actions = new List<IFlowAction>
                        {
                            new ComposeAction { Name = "B1_Step1", Inputs = "B1 Step 1" },
                            new ComposeAction { Name = "B1_Step2", Inputs = "B1 Step 2" }
                        }
                    },
                    new ParallelBranch
                    {
                        Name = "Branch2",
                        Actions = new List<IFlowAction>
                        {
                            new ComposeAction { Name = "B2_Step1", Inputs = "B2 Step 1" },
                            new ComposeAction { Name = "B2_Step2", Inputs = "B2 Step 2" }
                        }
                    }
                }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Parallel_Multiple_Actions",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { parallelAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var parallelResult = result.ActionResults[0];
            var branchResults = parallelResult.Outputs["branchResults"] as List<Dictionary<string, object>>;
            Assert.Equal(2, branchResults.Count);

            // Check first branch has 2 actions
            var branch1 = branchResults[0];
            var branch1Actions = branch1["actions"] as List<Dictionary<string, object>>;
            Assert.Equal(2, branch1Actions.Count);

            // Check second branch has 2 actions
            var branch2 = branchResults[1];
            var branch2Actions = branch2["actions"] as List<Dictionary<string, object>>;
            Assert.Equal(2, branch2Actions.Count);
        }

        #endregion

        #region Do Until Action Tests

        [Fact]
        public void Should_Execute_Do_Until_Loop_Until_Condition_Met()
        {
            // Reference: Do Until loops execute actions repeatedly until condition becomes true
            // https://learn.microsoft.com/en-us/power-automate/do-until-loop
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            int counter = 0;
            var triggerInputs = new Dictionary<string, object>
            {
                ["target"] = 3
            };

            // Create a compose action that will track iterations
            var incrementAction = new ComposeAction
            {
                Name = "Increment",
                Inputs = "Incrementing"
            };

            var doUntil = new DoUntilAction
            {
                Name = "Loop_Until_Target",
                Expression = "@greater(outputs('Increment')['value'], 'Inc')", // Will be true after iterations
                Actions = new List<IFlowAction> { incrementAction },
                MaxIterations = 10
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Do_Until",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { doUntil }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            Assert.Single(result.ActionResults);
            var loopResult = result.ActionResults[0];
            Assert.Equal("Loop_Until_Target", loopResult.ActionName);
            Assert.True((bool)loopResult.Outputs["conditionMet"]);
        }

        [Fact]
        public void Should_Fail_Do_Until_When_Max_Iterations_Exceeded()
        {
            // Reference: Do Until loops have a maximum iteration limit to prevent infinite loops
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>();

            var action = new ComposeAction
            {
                Name = "Action",
                Inputs = "Processing"
            };

            var doUntil = new DoUntilAction
            {
                Name = "Infinite_Loop",
                Expression = "@equals(1, 0)", // Always false
                Actions = new List<IFlowAction> { action },
                MaxIterations = 5 // Low limit for testing
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Do_Until_Max_Iterations",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { doUntil }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("maximum iterations", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Should_Execute_Multiple_Actions_In_Do_Until_Loop()
        {
            // Reference: Do Until loops can contain multiple actions per iteration
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>();

            var action1 = new ComposeAction
            {
                Name = "Step1",
                Inputs = "Step 1"
            };

            var action2 = new ComposeAction
            {
                Name = "Step2",
                Inputs = "Step 2"
            };

            var doUntil = new DoUntilAction
            {
                Name = "Loop_With_Multiple_Actions",
                Expression = "@greater(1, 0)", // True after first iteration
                Actions = new List<IFlowAction> { action1, action2 },
                MaxIterations = 10
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Do_Until_Multiple_Actions",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { doUntil }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            var loopResult = result.ActionResults[0];
            Assert.Equal(1, loopResult.Outputs["iterations"]); // Should iterate once
            var iterationResults = loopResult.Outputs["iterationResults"] as List<Dictionary<string, object>>;
            Assert.NotNull(iterationResults);
            Assert.Single(iterationResults);
            
            var firstIteration = iterationResults[0];
            var actions = firstIteration["actions"] as List<Dictionary<string, object>>;
            Assert.Equal(2, actions.Count); // Both actions executed
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Should_Combine_Control_Actions_In_Complex_Flow()
        {
            // Reference: Control actions can be combined to create complex workflows
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["type"] = "premium",
                ["amount"] = 5000
            };

            // Create complex flow with nested control actions
            var highValueAction = new ComposeAction
            {
                Name = "High_Value_Processing",
                Inputs = "High value premium customer"
            };

            var lowValueAction = new ComposeAction
            {
                Name = "Low_Value_Processing",
                Inputs = "Low value premium customer"
            };

            var amountCondition = new ConditionAction
            {
                Name = "Check_Amount",
                Expression = "@greater(triggerBody()['amount'], 1000)",
                TrueActions = new List<IFlowAction> { highValueAction },
                FalseActions = new List<IFlowAction> { lowValueAction }
            };

            var standardAction = new ComposeAction
            {
                Name = "Standard_Processing",
                Inputs = "Standard customer"
            };

            var typeSwitch = new SwitchAction
            {
                Name = "Route_By_Type",
                Expression = "@triggerBody()['type']",
                Cases = new Dictionary<string, IList<IFlowAction>>
                {
                    ["premium"] = new List<IFlowAction> { amountCondition }
                },
                DefaultActions = new List<IFlowAction> { standardAction }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Complex_Control_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "opportunity", Message = "Create" },
                Actions = new List<IFlowAction> { typeSwitch }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
        }

        #endregion
    }
}
