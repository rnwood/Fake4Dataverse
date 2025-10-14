using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin registration and execution with Custom Actions and Custom APIs
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
    /// </summary>
    public class CustomActionPluginTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_RegisterPlugin_ForCustomAction()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            
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
            // Use context from base class
            var context = _context;
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

            var service = _service;

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
            // Use context from base class
            var context = _context;
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

            var service = _service;

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
            // Use context from base class
            var context = _context;
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

            var service = _service;

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
            // Use context from base class
            var context = _context;
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

        [Fact]
        public void Should_ExecuteCustomAction_WithMetadata()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Custom Actions can be executed through Custom API infrastructure
            // Use context from base class
            var context = _context;
            context.UsePipelineSimulation = true;

            // Setup custom action metadata using Custom API entity
            var customAction = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_CalculateDiscount",
                ["isenabled"] = true,
                ["boundentitylogicalname"] = string.Empty // Global action
            };
            context.Initialize(customAction);

            // Register plugin for custom action
            CustomActionTestPlugin.WasExecuted = false;
            CustomActionTestPlugin.ExecutedMessageName = null;

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_CalculateDiscount",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(CustomActionTestPlugin)
            });

            var service = _service;

            // Act - Execute custom action
            var request = new OrganizationRequest("new_CalculateDiscount");
            request.Parameters["Amount"] = 1000m;
            request.Parameters["DiscountPercent"] = 10m;

            var response = service.Execute(request);

            // Assert
            Assert.True(CustomActionTestPlugin.WasExecuted);
            Assert.Equal("new_CalculateDiscount", CustomActionTestPlugin.ExecutedMessageName);
        }

        [Fact]
        public void Should_ThrowException_WhenCustomActionNotEnabled()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Custom Actions must be enabled before they can be executed
            // Use context from base class
            var context = _context;

            // Setup custom action in disabled state
            var customAction = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_DisabledAction",
                ["isenabled"] = false, // Not enabled
                ["boundentitylogicalname"] = string.Empty
            };
            context.Initialize(customAction);

            var service = _service;

            // Act & Assert
            var request = new OrganizationRequest("new_DisabledAction");
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            Assert.Contains("not enabled", exception.Message);
        }

        [Fact]
        public void Should_ExecuteEntityBoundCustomAction()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Custom Actions can be bound to a specific entity
            // Use context from base class
            var context = _context;
            context.UsePipelineSimulation = true;

            // Create test account
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            // Setup entity-bound custom action
            var customAction = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_AccountCustomAction",
                ["isenabled"] = true,
                ["boundentitylogicalname"] = "account" // Bound to account entity
            };
            context.Initialize(new List<Entity> { customAction, account });

            // Register plugin
            CustomActionTestPlugin.WasExecuted = false;
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_AccountCustomAction",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(CustomActionTestPlugin)
            });

            var service = _service;

            // Act - Execute entity-bound custom action
            var request = new OrganizationRequest("new_AccountCustomAction");
            request.Parameters["Target"] = new EntityReference("account", accountId);

            service.Execute(request);

            // Assert
            Assert.True(CustomActionTestPlugin.WasExecuted);
            Assert.Equal("new_AccountCustomAction", CustomActionTestPlugin.ExecutedMessageName);
        }

        [Fact]
        public void Should_ExecutePlugins_AtAllStages_ForProcessBasedCustomAction()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Plugins can be registered on custom action messages at any stage
            // Use context from base class
            var context = _context;
            context.UsePipelineSimulation = true;

            // Setup custom action
            var customAction = new Entity("customapi")
            {
                Id = Guid.NewGuid(),
                ["uniquename"] = "new_ProcessAction",
                ["isenabled"] = true,
                ["boundentitylogicalname"] = string.Empty
            };
            context.Initialize(customAction);

            // Register plugins at different stages
            StageTrackingPlugin.ExecutedStages.Clear();

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessAction",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Prevalidation,
                PluginType = typeof(StageTrackingPlugin)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessAction",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(StageTrackingPlugin)
            });

            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "new_ProcessAction",
                PrimaryEntityName = string.Empty,
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(StageTrackingPlugin)
            });

            var service = _service;

            // Act - Execute custom action
            var request = new OrganizationRequest("new_ProcessAction");
            service.Execute(request);

            // Assert - All stages executed
            Assert.Equal(3, StageTrackingPlugin.ExecutedStages.Count);
            Assert.Contains(ProcessingStepStage.Prevalidation, StageTrackingPlugin.ExecutedStages);
            Assert.Contains(ProcessingStepStage.Preoperation, StageTrackingPlugin.ExecutedStages);
            Assert.Contains(ProcessingStepStage.Postoperation, StageTrackingPlugin.ExecutedStages);
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
