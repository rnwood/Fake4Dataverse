using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveMultipleRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveMultiple", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var rmRequest = OrganizationRequestTypeAdapter.AsTyped<RetrieveMultipleRequest>(request);
            var result = service.RetrieveMultiple(rmRequest.Query);
            return new RetrieveMultipleResponse { Results = { ["EntityCollection"] = result } };
        }
    }
}
