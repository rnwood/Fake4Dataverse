using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveEntityRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveEntity", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = OrganizationRequestTypeAdapter.AsTyped<RetrieveEntityRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var entityInfo = store.GetEntityMetadataInfo(retrieveRequest.LogicalName);
            if (entityInfo == null)
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Entity '{retrieveRequest.LogicalName}' metadata does not exist.");
            }

            var oneToMany = store.GetOneToManyRelationships(retrieveRequest.LogicalName);
            var manyToMany = store.GetManyToManyRelationships(retrieveRequest.LogicalName);
            var sdkEntity = SdkMetadataConverter.ToSdkEntityMetadata(entityInfo, oneToMany, manyToMany);

            var response = new RetrieveEntityResponse();
            response.Results["EntityMetadata"] = sdkEntity;
            return response;
        }
    }
}
