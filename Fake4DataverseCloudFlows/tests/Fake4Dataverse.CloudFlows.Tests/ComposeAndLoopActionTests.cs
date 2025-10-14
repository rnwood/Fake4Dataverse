#if !NET462
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
    /// Tests for Compose and Apply to Each action types in Cloud Flows.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/data-operations#use-the-compose-action
    /// Reference: https://learn.microsoft.com/en-us/power-automate/apply-to-each
    /// 
    /// These tests verify that data transformation and loop constructs work correctly.
    /// </summary>
    public class ComposeAndLoopActionTests
    {
        #region Compose Action Tests

        [Fact]
        public void Should_Execute_Simple_Compose_Action()
        {
            // Reference: Compose action evaluates an expression and returns the result
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };

            var composeAction = new ComposeAction
            {
                Name = "Compose_FullName",
                Inputs = "@concat(triggerBody()['firstname'], ' ', triggerBody()['lastname'])"
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Compose_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "contact", Message = "Create" },
                Actions = new List<IFlowAction> { composeAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            var actionResult = result.ActionResults[0];
            Assert.Equal("Compose_FullName", actionResult.ActionName);
            Assert.True(actionResult.Succeeded);
            Assert.Equal("John Doe", actionResult.Outputs["value"]);
        }

        [Fact]
        public void Should_Execute_Compose_Action_With_Object_Output()
        {
            // Reference: Compose can create structured objects
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["firstname"] = "Jane",
                ["lastname"] = "Smith",
                ["email"] = "jane.smith@example.com"
            };

            // Note: Creating objects requires the object to be passed as a dictionary
            var contactObject = new Dictionary<string, object>
            {
                ["fullname"] = "@concat(triggerBody()['firstname'], ' ', triggerBody()['lastname'])",
                ["email"] = "@triggerBody()['email']",
                ["displayname"] = "@toUpper(triggerBody()['lastname'])"
            };

            var composeAction = new ComposeAction
            {
                Name = "Compose_ContactObject",
                Inputs = contactObject
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Compose_Object_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "contact", Message = "Create" },
                Actions = new List<IFlowAction> { composeAction }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded);
            var actionResult = result.ActionResults[0];
            Assert.True(actionResult.Succeeded);
            
            var output = actionResult.Outputs["value"] as IDictionary<string, object>;
            Assert.NotNull(output);
            Assert.Equal("Jane Smith", output["fullname"]);
            Assert.Equal("jane.smith@example.com", output["email"]);
            Assert.Equal("SMITH", output["displayname"]);
        }

        [Fact]
        public void Should_Reference_Compose_Output_In_Subsequent_Action()
        {
            // Reference: Compose action outputs can be referenced with @outputs('ActionName')['value']
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["firstname"] = "Bob"
            };

            var compose1 = new ComposeAction
            {
                Name = "Compose_1",
                Inputs = "@concat('Hello ', triggerBody()['firstname'])"
            };

            var compose2 = new ComposeAction
            {
                Name = "Compose_2",
                Inputs = "@concat(outputs('Compose_1')['value'], '!')"
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Chained_Compose_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "contact", Message = "Create" },
                Actions = new List<IFlowAction> { compose1, compose2 }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.ActionResults.Count);
            Assert.Equal("Hello Bob", result.ActionResults[0].Outputs["value"]);
            Assert.Equal("Hello Bob!", result.ActionResults[1].Outputs["value"]);
        }

        #endregion

        #region Apply to Each Tests

        [Fact]
        public void Should_Execute_Apply_To_Each_Action()
        {
            // Reference: Apply to Each iterates over a collection
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var contacts = new[]
            {
                new Dictionary<string, object> { ["name"] = "Contact 1", ["email"] = "c1@example.com" },
                new Dictionary<string, object> { ["name"] = "Contact 2", ["email"] = "c2@example.com" },
                new Dictionary<string, object> { ["name"] = "Contact 3", ["email"] = "c3@example.com" }
            };

            var triggerInputs = new Dictionary<string, object>
            {
                ["contacts"] = contacts
            };

            var composeInLoop = new ComposeAction
            {
                Name = "Compose_Email",
                Inputs = "@item()['email']"
            };

            var applyToEach = new ApplyToEachAction
            {
                Name = "Apply_to_each_contact",
                Collection = "@triggerBody()['contacts']",
                Actions = new List<IFlowAction> { composeInLoop }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Loop_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { applyToEach }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
            Assert.Single(result.ActionResults);
            var loopResult = result.ActionResults[0];
            Assert.True(loopResult.Succeeded);
            Assert.Equal("Apply_to_each_contact", loopResult.ActionName);
        }

        [Fact]
        public void Should_Access_Loop_Item_Properties()
        {
            // Reference: @item() function returns the current item in the loop
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var items = new[]
            {
                new Dictionary<string, object> { ["firstname"] = "Alice", ["lastname"] = "Anderson" },
                new Dictionary<string, object> { ["firstname"] = "Bob", ["lastname"] = "Brown" }
            };

            var triggerInputs = new Dictionary<string, object>
            {
                ["items"] = items
            };

            var composeFullName = new ComposeAction
            {
                Name = "Compose_FullName",
                Inputs = "@concat(item()['firstname'], ' ', item()['lastname'])"
            };

            var applyToEach = new ApplyToEachAction
            {
                Name = "Process_Items",
                Collection = "@triggerBody()['items']",
                Actions = new List<IFlowAction> { composeFullName }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Item_Access_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { applyToEach }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault());
        }

        [Fact]
        public void Should_Handle_Empty_Collection_In_Apply_To_Each()
        {
            // Reference: Loop should handle empty collections gracefully
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var triggerInputs = new Dictionary<string, object>
            {
                ["items"] = new object[0]
            };

            var composeInLoop = new ComposeAction
            {
                Name = "Compose_Item",
                Inputs = "@item()"
            };

            var applyToEach = new ApplyToEachAction
            {
                Name = "Process_Items",
                Collection = "@triggerBody()['items']",
                Actions = new List<IFlowAction> { composeInLoop }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Empty_Loop_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { applyToEach }
            };

            simulator.RegisterFlow(flowDef);

            // Act
            var result = simulator.SimulateTrigger(flowDef.Name, triggerInputs);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Should_Execute_Multiple_Actions_In_Loop()
        {
            // Reference: Multiple actions can be executed for each iteration
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var simulator = new CloudFlowSimulator(context);

            var numbers = new[] { 5, 10, 15 };

            var triggerInputs = new Dictionary<string, object>
            {
                ["numbers"] = numbers
            };

            var doubleValue = new ComposeAction
            {
                Name = "Double_Value",
                Inputs = "@mul(item(), 2)"
            };

            var addTen = new ComposeAction
            {
                Name = "Add_Ten",
                Inputs = "@add(outputs('Double_Value')['value'], 10)"
            };

            var applyToEach = new ApplyToEachAction
            {
                Name = "Process_Numbers",
                Collection = "@triggerBody()['numbers']",
                Actions = new List<IFlowAction> { doubleValue, addTen }
            };

            var flowDef = new CloudFlowDefinition
            {
                Name = "Test_Multiple_Actions_Loop_Flow",
                Trigger = new DataverseTrigger { EntityLogicalName = "account", Message = "Create" },
                Actions = new List<IFlowAction> { applyToEach }
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
#endif
