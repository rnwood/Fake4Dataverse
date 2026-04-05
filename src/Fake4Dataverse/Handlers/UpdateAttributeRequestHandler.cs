using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class UpdateAttributeRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is UpdateAttributeRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var updateRequest = (UpdateAttributeRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var entityName = updateRequest.Parameters.ContainsKey("EntityName") ? (string)updateRequest.Parameters["EntityName"] : null;
            if (string.IsNullOrEmpty(entityName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");

            var sdkAttr = updateRequest.Parameters.ContainsKey("Attribute") ? (AttributeMetadata)updateRequest.Parameters["Attribute"] : null;
            if (sdkAttr == null || string.IsNullOrEmpty(sdkAttr.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Attribute metadata with a valid LogicalName is required.");

            var attrType = sdkAttr.AttributeType ?? AttributeTypeCode.String;
            var attrInfo = new AttributeMetadataInfo(sdkAttr.LogicalName, attrType);

            if (sdkAttr.RequiredLevel?.Value != null)
                attrInfo.RequiredLevel = sdkAttr.RequiredLevel.Value;

            store.UpdateAttributeMetadata(entityName!, attrInfo);
            store.IncrementMetadataTimestamp();

            return new UpdateAttributeResponse();
        }
    }
}
