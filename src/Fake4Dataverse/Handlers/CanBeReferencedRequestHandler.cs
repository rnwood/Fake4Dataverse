using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CanBeReferencedRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "CanBeReferenced", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var canRequest = OrganizationRequestTypeAdapter.AsTyped<CanBeReferencedRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            // In the fake, any registered entity can be a referenced entity
            var entityInfo = store.GetEntityMetadataInfo(canRequest.EntityName);
            var canBeReferenced = entityInfo != null;

            var response = new CanBeReferencedResponse();
            response.Results["CanBeReferenced"] = canBeReferenced;
            return response;
        }
    }
}
