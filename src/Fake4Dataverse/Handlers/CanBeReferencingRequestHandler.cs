using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CanBeReferencingRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "CanBeReferencing", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var canRequest = OrganizationRequestTypeAdapter.AsTyped<CanBeReferencingRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            // In the fake, any registered entity can be a referencing entity
            var entityInfo = store.GetEntityMetadataInfo(canRequest.EntityName);
            var canBeReferencing = entityInfo != null;

            var response = new CanBeReferencingResponse();
            response.Results["CanBeReferencing"] = canBeReferencing;
            return response;
        }
    }
}
