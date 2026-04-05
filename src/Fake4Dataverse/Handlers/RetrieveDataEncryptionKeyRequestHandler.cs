using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveDataEncryptionKeyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveDataEncryptionKeyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new RetrieveDataEncryptionKeyResponse();
            response.Results["EncryptionKey"] = string.Empty;
            return response;
        }
    }
}
