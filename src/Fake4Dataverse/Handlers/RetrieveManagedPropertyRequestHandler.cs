using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveManagedPropertyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveManagedPropertyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // In the fake, return a stub response for managed property retrieval
            var response = new RetrieveManagedPropertyResponse();
            response.Results["ManagedPropertyMetadata"] = null;
            return response;
        }
    }
}
