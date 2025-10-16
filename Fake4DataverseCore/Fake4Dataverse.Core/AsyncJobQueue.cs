using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Enums;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse
{
    /// <summary>
    /// Implementation of async job queue for simulating Dataverse's asyncoperation table.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// 
    /// This class simulates how Dataverse queues and processes asynchronous operations:
    /// - Async plugins are queued after the main transaction
    /// - Jobs can be executed on-demand or automatically
    /// - Test writers can monitor and wait for job completion
    /// - Failed jobs can be inspected and retried
    /// </summary>
    public class AsyncJobQueue : IAsyncJobQueue
    {
        private readonly IXrmFakedContext _context;
        private readonly List<AsyncOperation> _operations;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets or sets whether async operations should auto-execute immediately when enqueued.
        /// </summary>
        public bool AutoExecute { get; set; }

        /// <summary>
        /// Gets the count of pending async operations.
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _operations.Count(op => !op.IsCompleted);
                }
            }
        }

        /// <summary>
        /// Gets the count of completed async operations.
        /// </summary>
        public int CompletedCount
        {
            get
            {
                lock (_lock)
                {
                    return _operations.Count(op => op.IsCompleted);
                }
            }
        }

        /// <summary>
        /// Gets the count of failed async operations.
        /// </summary>
        public int FailedCount
        {
            get
            {
                lock (_lock)
                {
                    return _operations.Count(op => op.IsFailed);
                }
            }
        }

        public AsyncJobQueue(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _operations = new List<AsyncOperation>();
            AutoExecute = false;
        }

        /// <summary>
        /// Enqueues an async operation for later execution.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
        /// 
        /// In Dataverse, async plugins are queued after the main transaction completes.
        /// The async service then picks up and executes these jobs.
        /// </summary>
        public void Enqueue(AsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            lock (_lock)
            {
                _operations.Add(asyncOperation);
            }

            // Auto-execute if enabled
            if (AutoExecute)
            {
                Execute(asyncOperation.AsyncOperationId);
            }
        }

        /// <summary>
        /// Executes all pending async operations in the queue.
        /// Operations execute in FIFO order (first in, first out).
        /// </summary>
        public int ExecuteAll()
        {
            List<AsyncOperation> pendingOps;
            
            lock (_lock)
            {
                pendingOps = _operations.Where(op => !op.IsCompleted).ToList();
            }

            int executedCount = 0;
            foreach (var op in pendingOps)
            {
                if (ExecuteOperation(op))
                {
                    executedCount++;
                }
            }

            return executedCount;
        }

        /// <summary>
        /// Executes a specific async operation by its ID.
        /// </summary>
        public bool Execute(Guid asyncOperationId)
        {
            AsyncOperation operation;
            
            lock (_lock)
            {
                operation = _operations.FirstOrDefault(op => op.AsyncOperationId == asyncOperationId);
            }

            if (operation == null || operation.IsCompleted)
            {
                return false;
            }

            return ExecuteOperation(operation);
        }

        /// <summary>
        /// Executes all pending async operations asynchronously.
        /// </summary>
        public async Task<int> ExecuteAllAsync(CancellationToken cancellationToken = default)
        {
            List<AsyncOperation> pendingOps;
            
            lock (_lock)
            {
                pendingOps = _operations.Where(op => !op.IsCompleted).ToList();
            }

            int executedCount = 0;
            foreach (var op in pendingOps)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (await ExecuteOperationAsync(op))
                {
                    executedCount++;
                }
            }

            return executedCount;
        }

        /// <summary>
        /// Waits for all pending async operations to complete.
        /// </summary>
        public bool WaitForAll(int timeoutMilliseconds = 30000)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            while (PendingCount > 0)
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    return false;
                }

                // Execute pending operations
                ExecuteAll();

                // Small delay to prevent tight loop
                if (PendingCount > 0)
                {
                    Thread.Sleep(10);
                }
            }

            return true;
        }

        /// <summary>
        /// Waits for all pending async operations to complete asynchronously.
        /// </summary>
        public async Task<bool> WaitForAllAsync(int timeoutMilliseconds = 30000, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            while (PendingCount > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    return false;
                }

                // Execute pending operations
                await ExecuteAllAsync(cancellationToken);

                // Small delay to prevent tight loop
                if (PendingCount > 0 && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(10, cancellationToken);
                }
            }

            return !cancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Waits for a specific async operation to complete.
        /// </summary>
        public bool WaitFor(Guid asyncOperationId, int timeoutMilliseconds = 30000)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            AsyncOperation operation;
            lock (_lock)
            {
                operation = _operations.FirstOrDefault(op => op.AsyncOperationId == asyncOperationId);
            }

            if (operation == null)
            {
                return false;
            }

            while (!operation.IsCompleted)
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    return false;
                }

                // Try to execute the operation
                Execute(asyncOperationId);

                // Small delay to prevent tight loop
                if (!operation.IsCompleted)
                {
                    Thread.Sleep(10);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all async operations in the queue (both pending and completed).
        /// </summary>
        public IReadOnlyList<AsyncOperation> GetAll()
        {
            lock (_lock)
            {
                return _operations.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all pending (not yet completed) async operations.
        /// </summary>
        public IReadOnlyList<AsyncOperation> GetPending()
        {
            lock (_lock)
            {
                return _operations.Where(op => !op.IsCompleted).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all completed async operations.
        /// </summary>
        public IReadOnlyList<AsyncOperation> GetCompleted()
        {
            lock (_lock)
            {
                return _operations.Where(op => op.IsCompleted).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all failed async operations.
        /// </summary>
        public IReadOnlyList<AsyncOperation> GetFailed()
        {
            lock (_lock)
            {
                return _operations.Where(op => op.IsFailed).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets a specific async operation by its ID.
        /// </summary>
        public AsyncOperation Get(Guid asyncOperationId)
        {
            lock (_lock)
            {
                return _operations.FirstOrDefault(op => op.AsyncOperationId == asyncOperationId);
            }
        }

        /// <summary>
        /// Clears all async operations from the queue.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _operations.Clear();
            }
        }

        /// <summary>
        /// Removes completed async operations from the queue.
        /// </summary>
        public int ClearCompleted()
        {
            lock (_lock)
            {
                var completed = _operations.Where(op => op.IsCompleted).ToList();
                foreach (var op in completed)
                {
                    _operations.Remove(op);
                }
                return completed.Count;
            }
        }

        /// <summary>
        /// Executes a single async operation.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
        /// </summary>
        private bool ExecuteOperation(AsyncOperation operation)
        {
            if (operation == null || operation.IsCompleted)
            {
                return false;
            }

            try
            {
                // Mark as in progress
                operation.StateCode = AsyncOperationState.Locked;
                operation.StatusCode = AsyncOperationStatus.InProgress;
                operation.StartedOn = DateTime.UtcNow;

                // Create plugin instance
                IPlugin plugin;
                var step = operation.PluginStepRegistration;

                try
                {
                    // Check if plugin has a constructor with configuration parameters
                    var constructorWithConfig = step.PluginType.GetConstructor(new[] { typeof(string), typeof(string) });
                    if (constructorWithConfig != null)
                    {
                        plugin = (IPlugin)Activator.CreateInstance(
                            step.PluginType,
                            step.UnsecureConfiguration,
                            step.SecureConfiguration);
                    }
                    else
                    {
                        // Use parameterless constructor
                        plugin = (IPlugin)Activator.CreateInstance(step.PluginType);
                    }
                }
                catch (Exception ex)
                {
                    operation.StateCode = AsyncOperationState.Completed;
                    operation.StatusCode = AsyncOperationStatus.Failed;
                    operation.CompletedOn = DateTime.UtcNow;
                    operation.ErrorMessage = $"Failed to create plugin instance: {ex.Message}";
                    operation.Exception = ex;
                    return false;
                }

                // Create plugin execution context
                var pluginContext = new XrmFakedPluginExecutionContext
                {
                    Depth = operation.Depth,
                    MessageName = operation.MessageName,
                    PrimaryEntityName = operation.PrimaryEntityName,
                    Stage = (int)Abstractions.Plugins.Enums.ProcessingStepStage.Postoperation, // Async plugins typically run post-operation
                    Mode = (int)Abstractions.Plugins.Enums.ProcessingStepMode.Asynchronous,
                    UserId = operation.OwnerId,
                    InitiatingUserId = operation.OwnerId,
                    OrganizationId = operation.OrganizationId,
                    BusinessUnitId = _context.CallerProperties.BusinessUnitId.Id,
                    CorrelationId = operation.CorrelationId,
                    OperationId = operation.AsyncOperationId,
                    OperationCreatedOn = operation.CreatedOn,
                    InputParameters = new ParameterCollection(),
                    OutputParameters = new ParameterCollection(),
                    PreEntityImages = operation.PreEntityImages ?? new EntityImageCollection(),
                    PostEntityImages = operation.PostEntityImages ?? new EntityImageCollection(),
                    SharedVariables = new ParameterCollection()
                };

                // Add target entity to input parameters
                if (operation.TargetEntity != null)
                {
                    pluginContext.InputParameters["Target"] = operation.TargetEntity;
                    pluginContext.PrimaryEntityId = operation.TargetEntity.Id;
                }

                // Create service provider
                var serviceProvider = _context.PluginContextProperties.GetServiceProvider(pluginContext);

                try
                {
                    // Execute the plugin
                    plugin.Execute(serviceProvider);

                    // Mark as succeeded
                    operation.StateCode = AsyncOperationState.Completed;
                    operation.StatusCode = AsyncOperationStatus.Succeeded;
                    operation.CompletedOn = DateTime.UtcNow;
                }
                catch (InvalidPluginExecutionException ex)
                {
                    // Plugin execution failed
                    operation.StateCode = AsyncOperationState.Completed;
                    operation.StatusCode = AsyncOperationStatus.Failed;
                    operation.CompletedOn = DateTime.UtcNow;
                    operation.ErrorMessage = ex.Message;
                    operation.Exception = ex;
                }
                catch (Exception ex)
                {
                    // Unexpected error
                    operation.StateCode = AsyncOperationState.Completed;
                    operation.StatusCode = AsyncOperationStatus.Failed;
                    operation.CompletedOn = DateTime.UtcNow;
                    operation.ErrorMessage = $"Plugin execution failed: {ex.Message}";
                    operation.Exception = ex;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Critical error executing operation
                operation.StateCode = AsyncOperationState.Completed;
                operation.StatusCode = AsyncOperationStatus.Failed;
                operation.CompletedOn = DateTime.UtcNow;
                operation.ErrorMessage = $"Critical error: {ex.Message}";
                operation.Exception = ex;
                return false;
            }
        }

        /// <summary>
        /// Executes a single async operation asynchronously.
        /// </summary>
        private Task<bool> ExecuteOperationAsync(AsyncOperation operation)
        {
            // For now, just wrap the synchronous execution
            // In the future, this could be enhanced for true async execution
            return Task.FromResult(ExecuteOperation(operation));
        }
    }
}
