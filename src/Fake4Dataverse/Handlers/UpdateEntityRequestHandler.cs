using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class UpdateEntityRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is UpdateEntityRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var updateRequest = (UpdateEntityRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var sdkEntity = updateRequest.Entity;
            if (sdkEntity == null || string.IsNullOrEmpty(sdkEntity.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Entity metadata with a valid LogicalName is required.");

            var entityInfo = new EntityMetadataInfo(sdkEntity.LogicalName)
            {
                SchemaName = sdkEntity.SchemaName,
                PrimaryIdAttribute = sdkEntity.PrimaryIdAttribute,
                PrimaryNameAttribute = sdkEntity.PrimaryNameAttribute,
                ObjectTypeCode = sdkEntity.ObjectTypeCode
            };

            store.UpdateEntityMetadata(entityInfo);
            store.IncrementMetadataTimestamp();

            return new UpdateEntityResponse();
        }
    }
}
