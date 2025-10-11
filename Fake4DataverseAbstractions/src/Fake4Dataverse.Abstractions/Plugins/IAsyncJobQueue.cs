using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fake4Dataverse.Abstractions.Plugins
{
    /// <summary>
    /// Interface for managing asynchronous job queue (simulating Dataverse's asyncoperation table).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
    /// 
    /// The async job queue simulates how Dataverse queues and executes asynchronous operations:
    /// - Async plugins are queued when triggered
    /// - Jobs can be executed on-demand or automatically
    /// - Test writers can monitor job status and wait for completion
    /// - Failed jobs can be retried or inspected for errors
    /// 
    /// This mirrors the behavior of the asyncoperation entity in Dataverse, allowing developers to:
    /// - Test async plugin behavior
    /// - Verify async operations are queued correctly
    /// - Wait for async operations to complete
    /// - Inspect async operation results and errors
    /// </summary>
    public interface IAsyncJobQueue
    {
        /// <summary>
        /// Enqueues an async operation for later execution.
        /// This simulates how Dataverse queues async plugins after the main transaction completes.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/asynchronous-service
        /// </summary>
        /// <param name="asyncOperation">The async operation to enqueue</param>
        void Enqueue(AsyncOperation asyncOperation);

        /// <summary>
        /// Executes all pending async operations in the queue.
        /// This simulates the async service processing queued jobs.
        /// Operations execute in FIFO order (first in, first out).
        /// </summary>
        /// <returns>Number of operations executed</returns>
        int ExecuteAll();

        /// <summary>
        /// Executes a specific async operation by its ID.
        /// </summary>
        /// <param name="asyncOperationId">ID of the async operation to execute</param>
        /// <returns>True if the operation was found and executed, false otherwise</returns>
        bool Execute(Guid asyncOperationId);

        /// <summary>
        /// Executes all pending async operations asynchronously.
        /// This provides async/await support for test scenarios that need it.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of operations executed</returns>
        Task<int> ExecuteAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for all pending async operations to complete.
        /// This is useful in tests to ensure all async work is done before asserting results.
        /// </summary>
        /// <param name="timeoutMilliseconds">Maximum time to wait in milliseconds (default: 30000 = 30 seconds)</param>
        /// <returns>True if all operations completed, false if timeout occurred</returns>
        bool WaitForAll(int timeoutMilliseconds = 30000);

        /// <summary>
        /// Waits for all pending async operations to complete asynchronously.
        /// </summary>
        /// <param name="timeoutMilliseconds">Maximum time to wait in milliseconds (default: 30000 = 30 seconds)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all operations completed, false if timeout occurred</returns>
        Task<bool> WaitForAllAsync(int timeoutMilliseconds = 30000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for a specific async operation to complete.
        /// </summary>
        /// <param name="asyncOperationId">ID of the async operation to wait for</param>
        /// <param name="timeoutMilliseconds">Maximum time to wait in milliseconds (default: 30000 = 30 seconds)</param>
        /// <returns>True if the operation completed, false if timeout occurred or operation not found</returns>
        bool WaitFor(Guid asyncOperationId, int timeoutMilliseconds = 30000);

        /// <summary>
        /// Gets all async operations in the queue (both pending and completed).
        /// This mirrors querying the asyncoperation entity in Dataverse.
        /// </summary>
        /// <returns>List of all async operations</returns>
        IReadOnlyList<AsyncOperation> GetAll();

        /// <summary>
        /// Gets all pending (not yet completed) async operations.
        /// </summary>
        /// <returns>List of pending async operations</returns>
        IReadOnlyList<AsyncOperation> GetPending();

        /// <summary>
        /// Gets all completed async operations.
        /// </summary>
        /// <returns>List of completed async operations</returns>
        IReadOnlyList<AsyncOperation> GetCompleted();

        /// <summary>
        /// Gets all failed async operations.
        /// </summary>
        /// <returns>List of failed async operations</returns>
        IReadOnlyList<AsyncOperation> GetFailed();

        /// <summary>
        /// Gets a specific async operation by its ID.
        /// </summary>
        /// <param name="asyncOperationId">ID of the async operation</param>
        /// <returns>The async operation, or null if not found</returns>
        AsyncOperation Get(Guid asyncOperationId);

        /// <summary>
        /// Gets the count of pending async operations.
        /// </summary>
        int PendingCount { get; }

        /// <summary>
        /// Gets the count of completed async operations.
        /// </summary>
        int CompletedCount { get; }

        /// <summary>
        /// Gets the count of failed async operations.
        /// </summary>
        int FailedCount { get; }

        /// <summary>
        /// Clears all async operations from the queue.
        /// This is useful for test cleanup.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes completed async operations from the queue.
        /// This simulates cleanup of old asyncoperation records in Dataverse.
        /// </summary>
        /// <returns>Number of operations removed</returns>
        int ClearCompleted();

        /// <summary>
        /// Gets or sets whether async operations should auto-execute immediately when enqueued.
        /// When true, async operations execute synchronously for easier testing.
        /// When false (default), operations remain queued until explicitly executed.
        /// This provides flexibility for different testing scenarios.
        /// </summary>
        bool AutoExecute { get; set; }
    }
}
