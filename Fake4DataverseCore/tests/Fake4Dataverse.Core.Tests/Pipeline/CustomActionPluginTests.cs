using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin registration and execution with Custom Actions and Custom APIs
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
    /// </summary>
    public class CustomActionPluginTests
    {
        [Fact]
        public void Should_RegisterPlugin_ForCustomAction()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            
            // Act - Register a plugin for a custom action
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CustomAction",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(CustomActionTestPlugin)
            });

            // Assert
            var registeredSteps = context.PluginPipelineSimulator.GetRegisteredPluginSteps(
                "new_CustomAction", "account", ProcessingStepStage.Preoperation);
            Assert.NotEmpty(registeredSteps);
        }

        [Fact]
        public void Should_ExecutePlugin_ForCustomAction_WhenUsePipelineSimulation()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Setup custom API metadata
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_CustomAction",
                ["isenabled"] = true,
                ["boundentitylogicalname"] = "account"
            };
            context.Initialize(customApi);

            // Register plugin for custom action
            CustomActionTestPlugin.WasExecuted = false;
            CustomActionTestPlugin.ExecutedMessageName = null;
            
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CustomAction",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(CustomActionTestPlugin)
            });

            var service = context.GetOrganizationService();

            // Act - Execute custom action
            var request = new OrganizationRequest("new_CustomAction");
            request.Parameters["Target"] = new EntityReference("account", Guid.NewGuid());
            
            var response = service.Execute(request);

            // Assert
            Assert.True(CustomActionTestPlugin.WasExecuted);
            Assert.Equal("new_CustomAction", CustomActionTestPlugin.ExecutedMessageName);
        }

        [Fact]
        public void Should_ExecuteMultiplePlugins_ForCustomAction_InCorrectOrder()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Setup custom API metadata
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_CalculateScore",
                ["isenabled"] = true
            };
            context.Initialize(customApi);

            // Register multiple plugins with different execution orders
            OrderTrackingPlugin.ExecutionOrder.Clear();
            
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CalculateScore",
                PrimaryEntityName = string.Empty, // Global custom action
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 2,
                PluginType = typeof(OrderTrackingPlugin2)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CalculateScore",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 1,
                PluginType = typeof(OrderTrackingPlugin1)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CalculateScore",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 3,
                PluginType = typeof(OrderTrackingPlugin3)
            });

            var service = context.GetOrganizationService();

            // Act - Execute custom action
            var request = new OrganizationRequest("new_CalculateScore");
            request.Parameters["Score"] = 100;
            
            service.Execute(request);

            // Assert - Plugins executed in correct order
            Assert.Equal(3, OrderTrackingPlugin.ExecutionOrder.Count);
            Assert.Equal("Plugin1", OrderTrackingPlugin.ExecutionOrder[0]);
            Assert.Equal("Plugin2", OrderTrackingPlugin.ExecutionOrder[1]);
            Assert.Equal("Plugin3", OrderTrackingPlugin.ExecutionOrder[2]);
        }

        [Fact]
        public void Should_ExecutePlugins_AtAllStages_ForCustomAction()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Setup custom API metadata
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_ProcessData",
                ["isenabled"] = true
            };
            context.Initialize(customApi);

            // Register plugins at different stages
            StageTrackingPlugin.ExecutedStages.Clear();
            
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessData",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Prevalidation,
                PluginType = typeof(StageTrackingPlugin)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessData",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(StageTrackingPlugin)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessData",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(StageTrackingPlugin)
            });

            var service = context.GetOrganizationService();

            // Act - Execute custom action
            var request = new OrganizationRequest("new_ProcessData");
            service.Execute(request);

            // Assert - All stages executed
            Assert.Equal(3, StageTrackingPlugin.ExecutedStages.Count);
            Assert.Contains(ProcessingStepStage.Prevalidation, StageTrackingPlugin.ExecutedStages);
            Assert.Contains(ProcessingStepStage.Preoperation, StageTrackingPlugin.ExecutedStages);
            Assert.Contains(ProcessingStepStage.Postoperation, StageTrackingPlugin.ExecutedStages);
        }

        [Fact]
        public void Should_AutoDiscoverPlugins_WithCustomActionAttributes()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;

            // Setup custom API metadata
            var customApi = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_AutoDiscoveredAction",
                ["isenabled"] = true
            };
            context.Initialize(customApi);

            // Act - Discover plugins with custom action registration attributes
            var assemblies = new[] { typeof(CustomActionPluginTests).Assembly };
            var count = context.PluginPipelineSimulator.DiscoverAndRegisterPlugins(assemblies);

            // Assert
            Assert.True(count > 0);
            var registeredSteps = context.PluginPipelineSimulator.GetRegisteredPluginSteps(
                "new_AutoDiscoveredAction", string.Empty, ProcessingStepStage.Preoperation);
            Assert.NotEmpty(registeredSteps);
        }
    }

    #region Test Plugins

    /// <summary>
    /// Test plugin for custom action execution
    /// </summary>
    public class CustomActionTestPlugin : IPlugin
    {
        public static bool WasExecuted { get; set; }
        public static string ExecutedMessageName { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            WasExecuted = true;
            ExecutedMessageName = context.MessageName;
        }
    }

    /// <summary>
    /// Plugin for tracking execution order
    /// </summary>
    public class OrderTrackingPlugin1 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            OrderTrackingPlugin.ExecutionOrder.Add("Plugin1");
        }
    }

    public class OrderTrackingPlugin2 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            OrderTrackingPlugin.ExecutionOrder.Add("Plugin2");
        }
    }

    public class OrderTrackingPlugin3 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            OrderTrackingPlugin.ExecutionOrder.Add("Plugin3");
        }
    }

    public static class OrderTrackingPlugin
    {
        public static List<string> ExecutionOrder { get; } = new List<string>();
    }

    /// <summary>
    /// Plugin for tracking which pipeline stages are executed
    /// </summary>
    public class StageTrackingPlugin : IPlugin
    {
        public static List<ProcessingStepStage> ExecutedStages { get; } = new List<ProcessingStepStage>();

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ExecutedStages.Add((ProcessingStepStage)context.Stage);
        }
    }

    /// <summary>
    /// Test plugin with custom action registration attribute for auto-discovery
    /// </summary>
    [CrmPluginRegistration("new_AutoDiscoveredAction", "", ProcessingStepStage.Preoperation, 1)]
    public class AutoDiscoveredCustomActionPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Test implementation
        }
    }

    #endregion
}
