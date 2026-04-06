using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateEntityRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CreateEntityRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var createRequest = (CreateEntityRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var sdkEntity = createRequest.Entity;
            if (sdkEntity == null || string.IsNullOrEmpty(sdkEntity.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Entity metadata with a valid LogicalName is required.");

            var entityInfo = new EntityMetadataInfo(sdkEntity.LogicalName)
            {
                SchemaName = sdkEntity.SchemaName,
                PrimaryIdAttribute = sdkEntity.PrimaryIdAttribute,
                PrimaryNameAttribute = sdkEntity.PrimaryNameAttribute,
                ObjectTypeCode = sdkEntity.ObjectTypeCode
            };

            store.CreateEntityMetadata(entityInfo);
            store.IncrementMetadataTimestamp();

            var entityId = Guid.NewGuid();
            var response = new CreateEntityResponse();
            response.Results["EntityId"] = entityId;
            response.Results["AttributeId"] = Guid.NewGuid();
            return response;
        }
    }
}
