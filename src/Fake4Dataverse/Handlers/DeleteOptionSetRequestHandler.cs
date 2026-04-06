using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteOptionSetRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is DeleteOptionSetRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = (DeleteOptionSetRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            if (string.IsNullOrEmpty(deleteRequest.Name))
                throw DataverseFault.InvalidArgumentFault("Option set name is required.");

            store.DeleteGlobalOptionSet(deleteRequest.Name);
            store.IncrementMetadataTimestamp();

            return new DeleteOptionSetResponse();
        }
    }
}
