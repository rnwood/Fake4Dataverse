using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for Cloud Flow simulator core functionality (Phase 2)
    /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
    /// </summary>
    public class CloudFlowSimulatorTests
    {
        [Fact]
        public void Should_RegisterFlow_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "test_flow",
                DisplayName = "Test Flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                }
            };

            // Act
            flowSimulator.RegisterFlow(flowDefinition);

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains("test_flow", registeredFlows);
        }

        [Fact]
        public void Should_ThrowException_WhenRegisteringFlowWithoutName()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = null,
                Trigger = new DataverseTrigger()
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlow(flowDefinition));
        }

        [Fact]
        public void Should_ThrowException_WhenRegisteringFlowWithoutTrigger()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "test_flow",
                Trigger = null
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlow(flowDefinition));
        }

        [Fact]
        public void Should_UnregisterFlow_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "test_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            flowSimulator.UnregisterFlow("test_flow");

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.DoesNotContain("test_flow", registeredFlows);
        }

        [Fact]
        public void Should_ClearAllFlows_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            flowSimulator.RegisterFlow(new CloudFlowDefinition
            {
                Name = "flow1",
                Trigger = new DataverseTrigger()
            });

            flowSimulator.RegisterFlow(new CloudFlowDefinition
            {
                Name = "flow2",
                Trigger = new DataverseTrigger()
            });

            // Act
            flowSimulator.ClearAllFlows();

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Empty(registeredFlows);
        }

        [Fact]
        public void Should_SimulateTrigger_WithEmptyActions()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "test_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>() // No actions
            };

            flowSimulator.RegisterFlow(flowDefinition);

            var triggerInputs = new Dictionary<string, object>
            {
                ["contactid"] = Guid.NewGuid(),
                ["firstname"] = "John"
            };

            // Act
            var result = flowSimulator.SimulateTrigger("test_flow", triggerInputs);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_flow", result.FlowName);
            Assert.True(result.Succeeded);
            Assert.Empty(result.ActionResults);
            Assert.Empty(result.Errors);
            Assert.Equal("John", result.TriggerInputs["firstname"]);
        }

        [Fact]
        public void Should_ThrowException_WhenSimulatingUnregisteredFlow()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                flowSimulator.SimulateTrigger("nonexistent_flow", new Dictionary<string, object>()));
        }

        [Fact]
        public void Should_ThrowException_WhenSimulatingDisabledFlow()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "disabled_flow",
                IsEnabled = false,
                Trigger = new DataverseTrigger()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                flowSimulator.SimulateTrigger("disabled_flow", new Dictionary<string, object>()));
        }

        [Fact]
        public void Should_ExecuteMultipleActions_InSequence()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Register a test handler
            var testHandler = new TestConnectorHandler();
            flowSimulator.RegisterConnectorActionHandler("TestConnector", testHandler);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "multi_action_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new TestFlowAction { Name = "Action1", ActionType = "TestConnector" },
                    new TestFlowAction { Name = "Action2", ActionType = "TestConnector" },
                    new TestFlowAction { Name = "Action3", ActionType = "TestConnector" }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("multi_action_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(3, result.ActionResults.Count);
            Assert.All(result.ActionResults, ar => Assert.True(ar.Succeeded));
            Assert.Equal(3, testHandler.ExecutionCount);
        }

        [Fact]
        public void Should_StopExecution_OnFirstActionFailure()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var testHandler = new TestConnectorHandler { ShouldFail = true };
            flowSimulator.RegisterConnectorActionHandler("TestConnector", testHandler);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "failing_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new TestFlowAction { Name = "Action1", ActionType = "TestConnector" },
                    new TestFlowAction { Name = "Action2", ActionType = "TestConnector" }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("failing_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Single(result.ActionResults); // Only first action executed
            Assert.False(result.ActionResults[0].Succeeded);
            Assert.NotEmpty(result.Errors);
            Assert.Equal(1, testHandler.ExecutionCount); // Second action not executed
        }

        [Fact]
        public void Should_TrackExecutionHistory()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "history_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act - Execute flow multiple times
            flowSimulator.SimulateTrigger("history_flow", new Dictionary<string, object> { ["attempt"] = 1 });
            flowSimulator.SimulateTrigger("history_flow", new Dictionary<string, object> { ["attempt"] = 2 });
            flowSimulator.SimulateTrigger("history_flow", new Dictionary<string, object> { ["attempt"] = 3 });

            // Assert
            var count = flowSimulator.GetFlowExecutionCount("history_flow");
            Assert.Equal(3, count);

            var results = flowSimulator.GetFlowExecutionResults("history_flow");
            Assert.Equal(3, results.Count);
            Assert.Equal(1, results[0].TriggerInputs["attempt"]);
            Assert.Equal(2, results[1].TriggerInputs["attempt"]);
            Assert.Equal(3, results[2].TriggerInputs["attempt"]);
        }

        [Fact]
        public void Should_AssertFlowTriggered_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "assert_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);
            flowSimulator.SimulateTrigger("assert_flow", new Dictionary<string, object>());

            // Act & Assert - Should not throw
            flowSimulator.AssertFlowTriggered("assert_flow");
        }

        [Fact]
        public void Should_ThrowException_WhenAssertingUntriggeredFlow()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "untriggered_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                flowSimulator.AssertFlowTriggered("untriggered_flow"));
        }

        [Fact]
        public void Should_AssertFlowNotTriggered_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "not_triggered_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act & Assert - Should not throw
            flowSimulator.AssertFlowNotTriggered("not_triggered_flow");
        }

        [Fact]
        public void Should_ThrowException_WhenAssertingFlowNotTriggered_ButItWas()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "triggered_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);
            flowSimulator.SimulateTrigger("triggered_flow", new Dictionary<string, object>());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                flowSimulator.AssertFlowNotTriggered("triggered_flow"));
        }

        [Fact]
        public void Should_ClearExecutionHistory_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "clear_history_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);
            flowSimulator.SimulateTrigger("clear_history_flow", new Dictionary<string, object>());

            // Act
            flowSimulator.ClearExecutionHistory();

            // Assert
            var count = flowSimulator.GetFlowExecutionCount("clear_history_flow");
            Assert.Equal(0, count);
        }

        [Fact]
        public void Should_RegisterConnectorHandler_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var handler = new TestConnectorHandler();

            // Act
            flowSimulator.RegisterConnectorActionHandler("TestConnector", handler);

            // Assert
            var retrievedHandler = flowSimulator.GetConnectorHandler("TestConnector");
            Assert.Same(handler, retrievedHandler);
        }

        [Fact]
        public void Should_ReturnNull_WhenGettingUnregisteredConnectorHandler()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Act
            var handler = flowSimulator.GetConnectorHandler("NonExistentConnector");

            // Assert
            Assert.Null(handler);
        }

        [Fact]
        public void Should_FailAction_WhenNoHandlerRegistered()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "no_handler_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new TestFlowAction { Name = "UnhandledAction", ActionType = "UnregisteredConnector" }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("no_handler_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.False(result.ActionResults[0].Succeeded);
            Assert.Contains("No connector handler registered", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_ThrowNotImplementedException_ForJsonImport()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert - JSON import not yet implemented (Phase 4)
            Assert.Throws<NotImplementedException>(() =>
                flowSimulator.RegisterFlowFromJson("{}"));
        }

        [Fact]
        public void Should_PassExecutionContext_ToActionHandler()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var testHandler = new ContextCapturingHandler();
            flowSimulator.RegisterConnectorActionHandler("ContextTest", testHandler);

            var triggerInputs = new Dictionary<string, object>
            {
                ["triggerData"] = "test_value"
            };

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "context_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new TestFlowAction { Name = "TestAction", ActionType = "ContextTest" }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("context_flow", triggerInputs);

            // Assert
            Assert.NotNull(testHandler.CapturedContext);
            Assert.Equal("test_value", testHandler.CapturedContext.TriggerInputs["triggerData"]);
        }

        [Fact]
        public void Should_PassActionOutputs_ToSubsequentActions()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var handler = new OutputProducingHandler();
            flowSimulator.RegisterConnectorActionHandler("OutputTest", handler);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "output_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new TestFlowAction { Name = "Action1", ActionType = "OutputTest" },
                    new TestFlowAction { Name = "Action2", ActionType = "OutputTest" }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("output_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.ActionResults.Count);
            
            // Verify Action2 received Action1's outputs in its context
            Assert.NotNull(handler.LastContext);
            var action1Outputs = handler.LastContext.GetActionOutputs("Action1");
            Assert.NotNull(action1Outputs);
            Assert.Equal("output_from_Action1", action1Outputs["result"]);
        }
    }

    #region Test Helper Classes

    /// <summary>
    /// Test flow action for unit tests
    /// </summary>
    public class TestFlowAction : IFlowAction
    {
        public string ActionType { get; set; }
        public string Name { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Test connector handler for unit tests
    /// </summary>
    public class TestConnectorHandler : IConnectorActionHandler
    {
        public string ConnectorType => "TestConnector";
        public int ExecutionCount { get; private set; }
        public bool ShouldFail { get; set; }

        public bool CanHandle(IFlowAction action)
        {
            return action.ActionType == "TestConnector";
        }

        public IDictionary<string, object> Execute(IFlowAction action, IXrmFakedContext context, IFlowExecutionContext flowContext)
        {
            ExecutionCount++;

            if (ShouldFail)
            {
                throw new Exception("Test handler failure");
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["executionCount"] = ExecutionCount
            };
        }
    }

    /// <summary>
    /// Handler that captures execution context for verification
    /// </summary>
    public class ContextCapturingHandler : IConnectorActionHandler
    {
        public string ConnectorType => "ContextTest";
        public IFlowExecutionContext CapturedContext { get; private set; }

        public bool CanHandle(IFlowAction action)
        {
            return action.ActionType == "ContextTest";
        }

        public IDictionary<string, object> Execute(IFlowAction action, IXrmFakedContext context, IFlowExecutionContext flowContext)
        {
            CapturedContext = flowContext;
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Handler that produces outputs for subsequent actions
    /// </summary>
    public class OutputProducingHandler : IConnectorActionHandler
    {
        public string ConnectorType => "OutputTest";
        public IFlowExecutionContext LastContext { get; private set; }

        public bool CanHandle(IFlowAction action)
        {
            return action.ActionType == "OutputTest";
        }

        public IDictionary<string, object> Execute(IFlowAction action, IXrmFakedContext context, IFlowExecutionContext flowContext)
        {
            LastContext = flowContext;

            return new Dictionary<string, object>
            {
                ["result"] = $"output_from_{action.Name}"
            };
        }
    }

    #endregion
}
