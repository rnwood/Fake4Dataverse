using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="GrantAccessRequest"/> by adding shared access rights on a record.
    /// </summary>
    internal sealed class GrantAccessRequestHandler : IOrganizationRequestHandler
    {
        private readonly Security.SecurityManager _security;

        public GrantAccessRequestHandler(Security.SecurityManager security)
        {
            _security = security;
        }

        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "GrantAccess", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var grantRequest = OrganizationRequestTypeAdapter.AsTyped<GrantAccessRequest>(request);
            var target = grantRequest.Target;
            var principalAccess = grantRequest.PrincipalAccess;

            _security.GrantAccess(
                target.LogicalName,
                target.Id,
                principalAccess.Principal.Id,
                principalAccess.AccessMask);

            return new GrantAccessResponse();
        }
    }
}
