using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse
{
    /// <summary>
    /// Registry that maps <see cref="OrganizationRequest"/> types to handlers.
    /// Thread-safe: uses copy-on-write for the handler list.
    /// </summary>
    public sealed class OrganizationRequestHandlerRegistry
    {
        private volatile IOrganizationRequestHandler[] _handlers = Array.Empty<IOrganizationRequestHandler>();
        private readonly object _writeLock = new object();

        /// <summary>
        /// Registers a handler. Later registrations take priority (matched last-wins).
        /// </summary>
        public void Register(IOrganizationRequestHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (_writeLock)
            {
                var current = _handlers;
                var next = new IOrganizationRequestHandler[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = handler;
                _handlers = next;
            }
        }

        internal OrganizationResponse Execute(OrganizationRequest request, IOrganizationService service)
        {
            var handlers = _handlers; // snapshot
            // Walk backwards so later registrations override earlier ones.
            for (int i = handlers.Length - 1; i >= 0; i--)
            {
                if (handlers[i].CanHandle(request))
                    return handlers[i].Handle(request, service);
            }

            throw new NotSupportedException($"No handler registered for request type '{request.GetType().Name}' (RequestName: '{request.RequestName}').");
        }
    }
}
