using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class IsDataEncryptionActiveRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is IsDataEncryptionActiveRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new IsDataEncryptionActiveResponse();
            response.Results["IsActive"] = false;
            return response;
        }
    }
}
