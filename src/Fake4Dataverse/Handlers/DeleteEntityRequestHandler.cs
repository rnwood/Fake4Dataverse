using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteEntityRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is DeleteEntityRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = (DeleteEntityRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            if (string.IsNullOrEmpty(deleteRequest.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");

            store.DeleteEntityMetadata(deleteRequest.LogicalName);
            store.IncrementMetadataTimestamp();

            return new DeleteEntityResponse();
        }
    }
}
