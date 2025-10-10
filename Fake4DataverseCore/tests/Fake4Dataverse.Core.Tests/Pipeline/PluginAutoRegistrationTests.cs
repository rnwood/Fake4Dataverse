using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Fake4Dataverse.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for automatic plugin execution during CRUD operations (Issue #16 & #17 - Auto Registration)
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
    /// </summary>
    public class PluginAutoRegistrationTests
    {
        [Fact]
        public void Should_AutoExecutePlugins_When_UsePipelineSimulationIsTrue_OnCreate()
        {
            // Arrange - Create context with pipeline simulation enabled
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            
            // Register a plugin for Create message
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(AccountNumberPlugin)
            });

            var service = context.GetOrganizationService();
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };

            // Act - Create the entity (should auto-execute the plugin)
            var id = service.Create(account);

            // Assert - Plugin should have executed and added accountnumber
            var createdAccount = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.NotNull(createdAccount["accountnumber"]);
        }

        [Fact]
        public void Should_AutoExecutePlugins_When_UsePipelineSimulationIsTrue_OnUpdate()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            
            // Use a simple tracking plugin instead of one that causes recursion
            UpdateTrackingPlugin.WasExecuted = false;
            
            // Register a plugin for Update message
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(UpdateTrackingPlugin)
            });

            var service = context.GetOrganizationService();
            
            // Create initial entity
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            context.Initialize(account);

            // Act - Update the entity (should auto-execute the plugin)
            var updateAccount = new Entity("account")
            {
                Id = account.Id,
                ["name"] = "Updated Account"
            };
            service.Update(updateAccount);

            // Assert - Plugin should have executed
            Assert.True(UpdateTrackingPlugin.WasExecuted);
        }

        [Fact]
        public void Should_AutoExecutePlugins_When_UsePipelineSimulationIsTrue_OnDelete()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            
            // Track plugin execution using a static flag
            DeleteTrackingPlugin.WasExecuted = false;
            
            // Register a plugin for Delete message
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Delete",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(DeleteTrackingPlugin)
            });

            var service = context.GetOrganizationService();
            
            // Create initial entity
            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            context.Initialize(account);

            // Act - Delete the entity (should auto-execute the plugin)
            service.Delete("account", account.Id);

            // Assert - Plugin should have executed
            Assert.True(DeleteTrackingPlugin.WasExecuted);
        }

        [Fact]
        public void Should_NotAutoExecutePlugins_When_UsePipelineSimulationIsFalse()
        {
            // Arrange - Create context with pipeline simulation disabled (default)
            var context = XrmFakedContextFactory.New();
            Assert.False(context.UsePipelineSimulation); // Verify default is false
            
            // Register a plugin
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(AccountNumberPlugin)
            });

            var service = context.GetOrganizationService();
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };

            // Act - Create the entity (should NOT auto-execute the plugin)
            var id = service.Create(account);

            // Assert - Plugin should NOT have executed
            var createdAccount = service.Retrieve("account", id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.False(createdAccount.Contains("accountnumber"));
        }

        [Fact]
        public void Should_ExecuteMultiplePlugins_InOrder_OnCreate()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            
            TestExecutionOrderPlugin.ExecutionLog.Clear();
            
            // Register multiple plugins with different execution orders
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 2,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "Plugin2",
                SecureConfiguration = "2"
            });
            
            context.PluginPipelineSimulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "Plugin1",
                SecureConfiguration = "1"
            });

            var service = context.GetOrganizationService();
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };

            // Act - Create entity (should auto-execute both plugins in order)
            service.Create(account);

            // Assert - Plugins should have executed in correct order
            Assert.Equal(2, TestExecutionOrderPlugin.ExecutionLog.Count);
            Assert.Contains("Plugin1", TestExecutionOrderPlugin.ExecutionLog[0]);
            Assert.Contains("Plugin2", TestExecutionOrderPlugin.ExecutionLog[1]);
        }
    }

    /// <summary>
    /// Simple plugin to track if Delete was executed
    /// </summary>
    public class DeleteTrackingPlugin : IPlugin
    {
        public static bool WasExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            WasExecuted = true;
        }
    }

    /// <summary>
    /// Simple plugin to track if Update was executed
    /// </summary>
    public class UpdateTrackingPlugin : IPlugin
    {
        public static bool WasExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            WasExecuted = true;
        }
    }
}
