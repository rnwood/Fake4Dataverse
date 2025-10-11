using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Plugins.Enums;
using Fake4Dataverse.Abstractions.Enums;
using Fake4Dataverse.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fake4Dataverse.Tests.Pipeline
{
    /// <summary>
    /// Tests for asynchronous plugin execution with system job queue (Issue #18).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// 
    /// These tests validate that:
    /// - Async plugins are queued instead of executed immediately
    /// - Test writers can monitor pending async jobs
    /// - Test writers can wait for async job completion
    /// - Async jobs can be executed on-demand
    /// - Failed async jobs can be inspected
    /// </summary>
    public class AsyncPluginExecutionTests
    {
        [Fact]
        public void Should_QueueAsyncPlugin_WhenRegisteredAsAsync()
        {
            // Arrange - Register an async plugin
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
            // Async plugins (Mode = 1) are queued as asyncoperation records and don't execute in the main transaction
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous, // Async mode
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
                ProcessingStepStage.Postoperation,
                account);

            // Assert - Plugin should be queued, not executed immediately
            Assert.Equal(1, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(0, simulator.AsyncJobQueue.CompletedCount);
            Assert.Null(account.GetAttributeValue<string>("accountnumber")); // Not executed yet
        }

        [Fact]
        public void Should_ExecuteQueuedAsyncPlugin_WhenExecuteAllCalled()
        {
            // Arrange - Queue an async plugin
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Act - Execute all queued async operations
            var executedCount = simulator.AsyncJobQueue.ExecuteAll();

            // Assert
            Assert.Equal(1, executedCount);
            Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(1, simulator.AsyncJobQueue.CompletedCount);
            Assert.NotNull(account.GetAttributeValue<string>("accountnumber")); // Plugin executed
        }

        [Fact]
        public void Should_TrackAsyncOperationStatus_ThroughoutExecution()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Get the queued operation
            var pendingOps = simulator.AsyncJobQueue.GetPending();
            Assert.Single(pendingOps);
            
            var asyncOp = pendingOps[0];

            // Assert initial state
            Assert.Equal(AsyncOperationState.Ready, asyncOp.StateCode);
            Assert.Equal(AsyncOperationStatus.WaitingForResources, asyncOp.StatusCode);
            Assert.Equal(AsyncOperationType.ExecutePlugin, asyncOp.OperationType);
            Assert.Null(asyncOp.StartedOn);
            Assert.Null(asyncOp.CompletedOn);

            // Act - Execute the operation
            simulator.AsyncJobQueue.Execute(asyncOp.AsyncOperationId);

            // Assert final state
            Assert.Equal(AsyncOperationState.Completed, asyncOp.StateCode);
            Assert.Equal(AsyncOperationStatus.Succeeded, asyncOp.StatusCode);
            Assert.NotNull(asyncOp.StartedOn);
            Assert.NotNull(asyncOp.CompletedOn);
            Assert.True(asyncOp.IsCompleted);
            Assert.True(asyncOp.IsSuccessful);
            Assert.False(asyncOp.IsFailed);
        }

        [Fact]
        public void Should_CapturePluginExecutionError_InAsyncOperation()
        {
            // Arrange - Register a plugin that throws an exception
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(ExceptionLoverPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Act - Execute the failing operation
            var asyncOp = simulator.AsyncJobQueue.GetPending().First();
            simulator.AsyncJobQueue.Execute(asyncOp.AsyncOperationId);

            // Assert - Operation should be marked as failed
            Assert.Equal(AsyncOperationState.Completed, asyncOp.StateCode);
            Assert.Equal(AsyncOperationStatus.Failed, asyncOp.StatusCode);
            Assert.True(asyncOp.IsFailed);
            Assert.False(asyncOp.IsSuccessful);
            Assert.NotNull(asyncOp.ErrorMessage);
            Assert.NotNull(asyncOp.Exception);
            Assert.Contains("amazing exception", asyncOp.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Should_WaitForAllAsyncOperations_ToComplete()
        {
            // Arrange - Queue multiple async operations
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            // Queue 3 operations
            for (int i = 0; i < 3; i++)
            {
                var account = new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = $"Test Account {i}"
                };

                simulator.ExecutePipelineStage(
                    "Create",
                    "account",
                    ProcessingStepStage.Postoperation,
                    account);
            }

            Assert.Equal(3, simulator.AsyncJobQueue.PendingCount);

            // Act - Wait for all to complete (with auto-execution via WaitForAll)
            var completed = simulator.AsyncJobQueue.WaitForAll(timeoutMilliseconds: 5000);

            // Assert
            Assert.True(completed);
            Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(3, simulator.AsyncJobQueue.CompletedCount);
        }

        [Fact]
        public void Should_WaitForSpecificAsyncOperation_ToComplete()
        {
            // Arrange - Queue an async operation
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            var asyncOp = simulator.AsyncJobQueue.GetPending().First();

            // Act - Wait for specific operation
            var completed = simulator.AsyncJobQueue.WaitFor(asyncOp.AsyncOperationId, timeoutMilliseconds: 5000);

            // Assert
            Assert.True(completed);
            Assert.True(asyncOp.IsCompleted);
        }

        [Fact]
        public async Task Should_ExecuteAsyncOperations_Asynchronously()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Act - Execute asynchronously
            var executedCount = await simulator.AsyncJobQueue.ExecuteAllAsync();

            // Assert
            Assert.Equal(1, executedCount);
            Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(1, simulator.AsyncJobQueue.CompletedCount);
        }

        [Fact]
        public async Task Should_WaitForAllAsyncOperations_Asynchronously()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            // Queue 2 operations
            for (int i = 0; i < 2; i++)
            {
                var account = new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = $"Test Account {i}"
                };

                simulator.ExecutePipelineStage(
                    "Create",
                    "account",
                    ProcessingStepStage.Postoperation,
                    account);
            }

            // Act - Wait asynchronously
            var completed = await simulator.AsyncJobQueue.WaitForAllAsync(timeoutMilliseconds: 5000);

            // Assert
            Assert.True(completed);
            Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(2, simulator.AsyncJobQueue.CompletedCount);
        }

        [Fact]
        public void Should_GetAllAsyncOperations_ByStatus()
        {
            // Arrange - Create a mix of successful and failed operations
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            // Register successful plugin
            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            });

            // Register failing plugin
            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "contact",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(ExceptionLoverPlugin)
            });

            // Queue successful operation
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });

            // Queue failing operation
            simulator.ExecutePipelineStage(
                "Create",
                "contact",
                ProcessingStepStage.Postoperation,
                new Entity("contact") { Id = Guid.NewGuid(), ["firstname"] = "Test" });

            // Act - Execute all
            simulator.AsyncJobQueue.ExecuteAll();

            // Assert
            var allOps = simulator.AsyncJobQueue.GetAll();
            var completedOps = simulator.AsyncJobQueue.GetCompleted();
            var failedOps = simulator.AsyncJobQueue.GetFailed();

            Assert.Equal(2, allOps.Count);
            Assert.Equal(2, completedOps.Count);
            Assert.Equal(1, failedOps.Count);
            Assert.Equal(1, simulator.AsyncJobQueue.FailedCount);
        }

        [Fact]
        public void Should_ClearCompletedAsyncOperations()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            });

            // Queue and execute 2 operations
            for (int i = 0; i < 2; i++)
            {
                simulator.ExecutePipelineStage(
                    "Create",
                    "account",
                    ProcessingStepStage.Postoperation,
                    new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Test" });
            }

            simulator.AsyncJobQueue.ExecuteAll();
            Assert.Equal(2, simulator.AsyncJobQueue.CompletedCount);

            // Act - Clear completed
            var clearedCount = simulator.AsyncJobQueue.ClearCompleted();

            // Assert
            Assert.Equal(2, clearedCount);
            Assert.Equal(0, simulator.AsyncJobQueue.GetAll().Count);
        }

        [Fact]
        public void Should_AutoExecuteAsyncPlugin_WhenAutoExecuteEnabled()
        {
            // Arrange - Enable auto-execute mode
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;
            simulator.AsyncJobQueue.AutoExecute = true; // Enable auto-execute

            var registration = new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            };

            simulator.RegisterPluginStep(registration);

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute pipeline (async plugin should auto-execute)
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Assert - Operation executed immediately
            Assert.Equal(0, simulator.AsyncJobQueue.PendingCount);
            Assert.Equal(1, simulator.AsyncJobQueue.CompletedCount);
            Assert.NotNull(account.GetAttributeValue<string>("accountnumber")); // Plugin executed
        }

        [Fact]
        public void Should_ConvertAsyncOperation_ToEntity()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(AccountNumberPlugin)
            });

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            var asyncOp = simulator.AsyncJobQueue.GetPending().First();

            // Act - Convert to entity (simulating asyncoperation entity in Dataverse)
            var entity = asyncOp.ToEntity();

            // Assert
            Assert.Equal("asyncoperation", entity.LogicalName);
            Assert.Equal(asyncOp.AsyncOperationId, entity.Id);
            Assert.Equal(asyncOp.Name, entity.GetAttributeValue<string>("name"));
            Assert.Equal((int)AsyncOperationType.ExecutePlugin, entity.GetAttributeValue<OptionSetValue>("operationtype").Value);
            Assert.Equal((int)AsyncOperationState.Ready, entity.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal((int)AsyncOperationStatus.WaitingForResources, entity.GetAttributeValue<OptionSetValue>("statuscode").Value);
            Assert.Equal("Create", entity.GetAttributeValue<string>("messagename"));
            Assert.Equal("account", entity.GetAttributeValue<string>("primaryentitytype"));
        }

        [Fact]
        public void Should_PreservePluginContext_InAsyncExecution()
        {
            // Arrange - Register a plugin that validates context properties
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
            // Async plugins receive a modified execution context with Mode = 1 (Asynchronous)
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                PluginType = typeof(TestPropertiesPlugin)
            });

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Queue and execute
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            var asyncOp = simulator.AsyncJobQueue.GetPending().First();
            simulator.AsyncJobQueue.Execute(asyncOp.AsyncOperationId);

            // Assert - Plugin executed successfully with correct context
            Assert.True(asyncOp.IsSuccessful);
        }

        [Fact]
        public void Should_ExecuteMultipleSyncAndAsyncPlugins_InCorrectOrder()
        {
            // Arrange - Register both sync and async plugins
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/event-framework
            // Sync plugins execute immediately in order, async plugins are queued for later
            var context = XrmFakedContextFactory.New();
            var simulator = context.PluginPipelineSimulator;

            TestExecutionOrderPlugin.ExecutionLog.Clear();

            // Register sync plugin (executes immediately)
            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Synchronous,
                ExecutionOrder = 1,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "SyncPlugin"
            });

            // Register async plugin (queued)
            simulator.RegisterPluginStep(new PluginStepRegistration
            {
                MessageName = "Create",
                PrimaryEntityName = "account",
                Stage = ProcessingStepStage.Postoperation,
                Mode = ProcessingStepMode.Asynchronous,
                ExecutionOrder = 2,
                PluginType = typeof(TestExecutionOrderPlugin),
                UnsecureConfiguration = "AsyncPlugin"
            });

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            // Act - Execute pipeline
            simulator.ExecutePipelineStage(
                "Create",
                "account",
                ProcessingStepStage.Postoperation,
                account);

            // Assert - Only sync plugin executed so far
            Assert.Single(TestExecutionOrderPlugin.ExecutionLog);
            Assert.Contains("SyncPlugin", TestExecutionOrderPlugin.ExecutionLog[0]);
            Assert.Equal(1, simulator.AsyncJobQueue.PendingCount);

            // Execute async jobs
            simulator.AsyncJobQueue.ExecuteAll();

            // Assert - Now async plugin also executed
            Assert.Equal(2, TestExecutionOrderPlugin.ExecutionLog.Count);
            Assert.Contains("AsyncPlugin", TestExecutionOrderPlugin.ExecutionLog[1]);
        }
    }
}
