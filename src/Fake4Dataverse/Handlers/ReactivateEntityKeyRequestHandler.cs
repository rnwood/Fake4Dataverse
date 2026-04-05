using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class ReactivateEntityKeyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is ReactivateEntityKeyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // In the fake, keys are always active. This is a no-op that validates the key exists.
            var reactivateRequest = (ReactivateEntityKeyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var entityName = reactivateRequest.Parameters.ContainsKey("EntityLogicalName") ? (string)reactivateRequest.Parameters["EntityLogicalName"] : null;
            var keyName = reactivateRequest.Parameters.ContainsKey("EntityKeyLogicalName") ? (string)reactivateRequest.Parameters["EntityKeyLogicalName"] : null;
            if (string.IsNullOrEmpty(entityName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");
            if (string.IsNullOrEmpty(keyName))
                throw DataverseFault.InvalidArgumentFault("Key name is required.");

            var key = store.GetAlternateKey(entityName!, keyName!);
            if (key == null)
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Alternate key '{keyName}' does not exist on entity '{entityName}'.");
            }

            return new ReactivateEntityKeyResponse();
        }
    }
}
