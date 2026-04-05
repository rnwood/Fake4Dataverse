using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteAttributeRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is DeleteAttributeRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = (DeleteAttributeRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            if (string.IsNullOrEmpty(deleteRequest.EntityLogicalName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");
            if (string.IsNullOrEmpty(deleteRequest.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Attribute logical name is required.");

            store.DeleteAttributeMetadata(deleteRequest.EntityLogicalName, deleteRequest.LogicalName);
            store.IncrementMetadataTimestamp();

            return new DeleteAttributeResponse();
        }
    }
}
