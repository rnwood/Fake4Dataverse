using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="ModifyAccessRequest"/> by replacing shared access rights on a record.
    /// </summary>
    internal sealed class ModifyAccessRequestHandler : IOrganizationRequestHandler
    {
        private readonly Security.SecurityManager _security;

        public ModifyAccessRequestHandler(Security.SecurityManager security)
        {
            _security = security;
        }

        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "ModifyAccess", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var modifyRequest = OrganizationRequestTypeAdapter.AsTyped<ModifyAccessRequest>(request);
            var target = modifyRequest.Target;
            var principalAccess = modifyRequest.PrincipalAccess;

            _security.ModifyAccess(
                target.LogicalName,
                target.Id,
                principalAccess.Principal.Id,
                principalAccess.AccessMask);

            return new ModifyAccessResponse();
        }
    }
}
