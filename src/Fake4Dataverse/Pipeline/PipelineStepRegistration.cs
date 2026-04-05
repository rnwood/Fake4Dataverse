using System;

namespace Fake4Dataverse.Pipeline
{
    /// <summary>
    /// Represents a registered pipeline step. Dispose to unregister.
    /// </summary>
    public sealed class PipelineStepRegistration : IDisposable
    {
        private readonly Action _unregister;
        private bool _disposed;

        internal PipelineStepRegistration(Action unregister)
        {
            _unregister = unregister;
        }

        /// <summary>
        /// Unregisters this pipeline step.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _unregister();
            }
        }
    }
}
