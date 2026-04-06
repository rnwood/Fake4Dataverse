using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateEntityKeyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "CreateEntityKey", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var createRequest = OrganizationRequestTypeAdapter.AsTyped<CreateEntityKeyRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var entityName = createRequest.Parameters.ContainsKey("EntityName") ? (string)createRequest.Parameters["EntityName"] : null;
            if (string.IsNullOrEmpty(entityName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");

            var sdkKey = createRequest.Parameters.ContainsKey("EntityKey") ? (EntityKeyMetadata)createRequest.Parameters["EntityKey"] : null;
            if (sdkKey == null || string.IsNullOrEmpty(sdkKey.LogicalName))
                throw DataverseFault.InvalidArgumentFault("EntityKey with a valid LogicalName is required.");

            var keyAttributes = sdkKey.KeyAttributes ?? Array.Empty<string>();
            var keyInfo = new AlternateKeyInfo(sdkKey.LogicalName, keyAttributes);

            store.CreateAlternateKey(entityName!, keyInfo);
            store.IncrementMetadataTimestamp();

            var response = new CreateEntityKeyResponse();
            response.Results["EntityKeyId"] = Guid.NewGuid();
            return response;
        }
    }
}
