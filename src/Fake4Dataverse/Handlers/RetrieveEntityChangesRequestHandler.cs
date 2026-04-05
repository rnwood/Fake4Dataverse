using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveEntityChangesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveEntityChangesRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // Return a stub response — the real response uses BusinessEntityChanges
            // which is complex. We return the key properties via Results dictionary.
            var response = new RetrieveEntityChangesResponse();
            response.Results["BusinessEntityChanges"] = new Microsoft.Xrm.Sdk.EntityCollection();
            return response;
        }
    }
}
