using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveMultipleRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveMultipleRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var rmRequest = (RetrieveMultipleRequest)request;
            var result = service.RetrieveMultiple(rmRequest.Query);
            return new RetrieveMultipleResponse { Results = { ["EntityCollection"] = result } };
        }
    }
}
