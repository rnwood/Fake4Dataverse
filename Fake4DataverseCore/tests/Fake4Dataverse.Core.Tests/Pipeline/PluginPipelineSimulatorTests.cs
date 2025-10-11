using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Fake4Dataverse.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin pipeline simulation (Issues #16 and #17)
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
    /// </summary>
    public class PluginPipelineSimulatorTests
    {
        [Fact]
        public void Should_RegisterAndExecute_SinglePlugin()
        {
            // Arrange - Create context and register a plugin
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute the pipeline stage
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                userId: Guid.NewGuid(),
                organizationId: Guid.NewGuid());

            // Assert - Plugin should have executed and added accountnumber
            Assert.NotNull(account["accountnumber"]);
        }

        [Fact]
        public void Should_Execute_MultiplePlugins_InCorrectOrder()
        {
            // Arrange - Register multiple plugins with different execution orders
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
            // Multiple plugins on the same message/entity/stage execute in order based on ExecutionOrder (rank)
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            TestExecutionOrderPlugin.ExecutionLog.Clear();

            // Register plugins in non-sequential order to test sorting
            var registration3 = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 3,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "Plugin3",
                SecureConfiguration = "3"
            };

            var registration1 = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "Plugin1",
                SecureConfiguration = "1"
            };

            var registration2 = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 2,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "Plugin2",
                SecureConfiguration = "2"
            };

            // Register in reverse order to ensure sorting works
            simulator.RegisterPluginStep(registration3);
            simulator.RegisterPluginStep(registration1);
            simulator.RegisterPluginStep(registration2);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Preoperation,
                account);

            // Assert - Plugins should execute in order 1, 2, 3
            Assert.Equal(3, TestExecutionOrderPlugin.ExecutionLog.Count);
            Assert.Contains("|1|", TestExecutionOrderPlugin.ExecutionLog[0]);
            Assert.Contains("|2|", TestExecutionOrderPlugin.ExecutionLog[1]);
            Assert.Contains("|3|", TestExecutionOrderPlugin.ExecutionLog[2]);
        }

        [Fact]
        public void Should_Execute_PluginsInDifferentStages()
        {
            // Arrange - Register plugins in different pipeline stages
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
            // Pipeline stages: PreValidation (10), PreOperation (20), PostOperation (40)
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            TestExecutionOrderPlugin.ExecutionLog.Clear();

            var preValidation = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Prevalidation,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "PreVal",
                SecureConfiguration = "1"
            };

            var preOperation = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "PreOp",
                SecureConfiguration = "1"
            };

            var postOperation = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "PostOp",
                SecureConfiguration = "1"
            };

            simulator.RegisterPluginSteps(new[] { preValidation, preOperation, postOperation });

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute each stage separately (as would happen in the real pipeline)
            simulator.ExecutePipelineStage("Create", "account", ProcessingStepStage.Prevalidation, account);
            simulator.ExecutePipelineStage("Create", "account", ProcessingStepStage.Preoperation, account);
            simulator.ExecutePipelineStage("Create", "account", ProcessingStepStage.Postoperation, account);

            // Assert - All three stages should have executed
            Assert.Equal(3, TestExecutionOrderPlugin.ExecutionLog.Count);
            Assert.Contains("Stage10", TestExecutionOrderPlugin.ExecutionLog[0]); // PreValidation
            Assert.Contains("Stage20", TestExecutionOrderPlugin.ExecutionLog[1]); // PreOperation
            Assert.Contains("Stage40", TestExecutionOrderPlugin.ExecutionLog[2]); // PostOperation
        }

        [Fact]
        public void Should_FilterByAttributes_OnUpdateMessage()
        {
            // Arrange - Register plugin with filtering attributes
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in#filtering-attributes
            // Filtering attributes ensure plugin only executes when specific attributes are modified
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            FilteredAttributePlugin.ExecutionCount = 0;

            var registration = new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(FilteredAttributePlugin),
                FilteringAttributes = new HashSet<string> { "name", "revenue" }
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid()
            };

            // Act 1 - Update with matching attribute (name)
            var modifiedAttributes1 = new HashSet<string> { "name" };
            simulator.ExecutePipelineStage(
                "Update",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                modifiedAttributes1);

            // Act 2 - Update with non-matching attribute (telephone)
            var modifiedAttributes2 = new HashSet<string> { "telephone" };
            simulator.ExecutePipelineStage(
                "Update",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                modifiedAttributes2);

            // Act 3 - Update with one matching attribute (revenue)
            var modifiedAttributes3 = new HashSet<string> { "revenue", "telephone" };
            simulator.ExecutePipelineStage(
                "Update",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                modifiedAttributes3);

            // Assert - Plugin should have executed twice (for name and revenue), but not for telephone alone
            Assert.Equal(2, FilteredAttributePlugin.ExecutionCount);
        }

        [Fact]
        public void Should_TrackDepth_AndPreventInfiniteLoops()
        {
            // Arrange - Configure maximum depth
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-recursive-loops
            // Depth tracking prevents infinite loops when plugins trigger other operations
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;
            
            // Set a low max depth for testing
            simulator.MaxDepth = 3;

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act & Assert - Exceeding max depth should throw exception
            var exception = Assert.Throws<InvalidPluginExecutionException>(() =>
            {
                simulator.ExecutePipelineStage(
                    "Create",
                    "account",
                    ProcessingStepStage.Preoperation,
                    account,
                    currentDepth: 4); // Exceeds max depth of 3
            });

            Assert.Contains("Maximum plugin execution depth", exception.Message);
            Assert.Contains("3", exception.Message);
        }

        [Fact]
        public void Should_PassConfiguration_ToPlugin()
        {
            // Arrange - Register plugin with configuration
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
            // Plugins can receive configuration data (secure and unsecure)
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ConfigurationPlugin),
                UnsecureConfiguration = "UnsecureValue",
                SecureConfiguration = "SecureValue"
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Preoperation,
                account);

            // Assert - Configuration values should be passed to plugin
            Assert.Equal("UnsecureValue", account["unsecure"]);
            Assert.Equal("SecureValue", account["secure"]);
        }

        [Fact]
        public void Should_UnregisterPluginStep()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            // Verify registration worked
            var registeredSteps = simulator.GetRegisteredPluginSteps("Create", "account", ProcessingStepStage.Preoperation);
            Assert.Single(registeredSteps);

            // Act - Unregister the plugin
            simulator.UnregisterPluginStep(registration);

            // Assert - Plugin should no longer be registered
            registeredSteps = simulator.GetRegisteredPluginSteps("Create", "account", ProcessingStepStage.Preoperation);
            Assert.Empty(registeredSteps);
        }

        [Fact]
        public void Should_ClearAllPluginSteps()
        {
            // Arrange - Register multiple plugins
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration1 = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(AccountNumberPlugin)
            };

            var registration2 = new PluginStepRegistration
            {
                MessageName = "Update",
                PrimaryEntityName = "contact",
                Stage = ProcessingStepStage.Postoperation,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginSteps(new[] { registration1, registration2 });

            // Act - Clear all registrations
            simulator.ClearAllPluginSteps();

            // Assert - No plugins should be registered
            var steps1 = simulator.GetRegisteredPluginSteps("Create", "account", ProcessingStepStage.Preoperation);
            var steps2 = simulator.GetRegisteredPluginSteps("Update", "contact", ProcessingStepStage.Postoperation);
            
            Assert.Empty(steps1);
            Assert.Empty(steps2);
        }

        [Fact]
        public void Should_ThrowException_WhenPluginFails()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(ThrowExceptionPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act & Assert - Plugin exception should bubble up
            var exception = Assert.Throws<InvalidPluginExecutionException>(() =>
            {
                simulator.ExecutePipelineStage(
                    "Create",
                    "account",
                    ProcessingStepStage.Preoperation,
                    account);
            });

            Assert.Contains("Test exception from plugin", exception.Message);
        }

        [Fact]
        public void Should_OnlyExecute_ForMatchingEntityAndMessage()
        {
            // Arrange - Register plugin for account/Create
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            FilteredAttributePlugin.ExecutionCount = 0;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(FilteredAttributePlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account") { Id = Guid.NewGuid() };
            var contact = new Entity("contact") { Id = Guid.NewGuid() };

            // Act - Execute for matching entity
            simulator.ExecutePipelineStage("Create", "account", ProcessingStepStage.Preoperation, account);

            // Act - Execute for non-matching entity
            simulator.ExecutePipelineStage("Create", "contact", ProcessingStepStage.Preoperation, contact);

            // Act - Execute for matching entity but different message
            simulator.ExecutePipelineStage("Update", "account", ProcessingStepStage.Preoperation, account);

            // Assert - Plugin should only execute once (for account/Create)
            Assert.Equal(1, FilteredAttributePlugin.ExecutionCount);
        }

        [Fact]
        public void Should_SetDepthProperty_InPluginContext()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(TestPropertiesPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute with specific depth
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                currentDepth: 2);

            // Assert - Depth should be set correctly in context
            // TestPropertiesPlugin sets depth attribute on the entity
            Assert.True(account.Contains("depth"));
            Assert.Equal(2, account.GetAttributeValue<int>("depth"));
        }

        [Fact]
        public void Should_ReturnEmpty_WhenNoPluginsRegistered()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            // Act
            var steps = simulator.GetRegisteredPluginSteps("Create", "account", ProcessingStepStage.Preoperation);

            // Assert
            Assert.Empty(steps);
        }

        [Fact]
        public void Should_HandleNullModifiedAttributes_ForNonUpdateMessages()
        {
            // Arrange - Plugin with filtering attributes on non-Update message
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            FilteredAttributePlugin.ExecutionCount = 0;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",  // Not Update, so filtering attributes are ignored
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Preoperation,
                PluginType = typeof(FilteredAttributePlugin),
                FilteringAttributes = new HashSet<string> { "name" }  // Should be ignored for Create
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute without specifying modified attributes
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Preoperation,
                account,
                modifiedAttributes: null);  // null is valid for non-Update messages

            // Assert - Plugin should execute (filtering attributes ignored for non-Update messages)
            Assert.Equal(1, FilteredAttributePlugin.ExecutionCount);
        }
    }
}
