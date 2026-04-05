using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="RevokeAccessRequest"/> by removing shared access rights on a record.
    /// </summary>
    internal sealed class RevokeAccessRequestHandler : IOrganizationRequestHandler
    {
        private readonly Security.SecurityManager _security;

        public RevokeAccessRequestHandler(Security.SecurityManager security)
        {
            _security = security;
        }

        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RevokeAccess", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var revokeRequest = OrganizationRequestTypeAdapter.AsTyped<RevokeAccessRequest>(request);
            var target = revokeRequest.Target;
            var revokee = revokeRequest.Revokee;

            _security.RevokeAccess(target.LogicalName, target.Id, revokee.Id);

            return new RevokeAccessResponse();
        }
    }
}
