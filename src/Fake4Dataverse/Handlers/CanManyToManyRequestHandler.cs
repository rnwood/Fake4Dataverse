using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CanManyToManyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CanManyToManyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var canRequest = (CanManyToManyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            // In the fake, any registered entity can participate in N:N
            var entityInfo = store.GetEntityMetadataInfo(canRequest.EntityName);
            var canManyToMany = entityInfo != null;

            var response = new CanManyToManyResponse();
            response.Results["CanManyToMany"] = canManyToMany;
            return response;
        }
    }
}
