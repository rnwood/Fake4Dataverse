using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteEntityKeyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is DeleteEntityKeyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = (DeleteEntityKeyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            if (string.IsNullOrEmpty(deleteRequest.EntityLogicalName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");
            if (string.IsNullOrEmpty(deleteRequest.Name))
                throw DataverseFault.InvalidArgumentFault("Key name is required.");

            store.DeleteAlternateKey(deleteRequest.EntityLogicalName, deleteRequest.Name);
            store.IncrementMetadataTimestamp();

            return new DeleteEntityKeyResponse();
        }
    }
}
