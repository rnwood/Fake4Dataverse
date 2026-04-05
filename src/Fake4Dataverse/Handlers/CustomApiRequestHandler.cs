using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="OrganizationRequest"/> messages by matching on <see cref="OrganizationRequest.RequestName"/>.
    /// Used to simulate custom API endpoints.
    /// </summary>
    internal sealed class CustomApiRequestHandler : IOrganizationRequestHandler
    {
        private readonly string _requestName;
        private readonly Func<OrganizationRequest, IOrganizationService, OrganizationResponse> _handler;

        public CustomApiRequestHandler(string requestName, Func<OrganizationRequest, IOrganizationService, OrganizationResponse> handler)
        {
            _requestName = requestName ?? throw new ArgumentNullException(nameof(requestName));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public bool CanHandle(OrganizationRequest request)
            => string.Equals(request.RequestName, _requestName, StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
            => _handler(request, service);
    }
}
