using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateAttributeRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "CreateAttribute", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var createRequest = OrganizationRequestTypeAdapter.AsTyped<CreateAttributeRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var entityName = createRequest.Parameters.ContainsKey("EntityName") ? (string)createRequest.Parameters["EntityName"] : null;
            if (string.IsNullOrEmpty(entityName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");

            var sdkAttr = createRequest.Parameters.ContainsKey("Attribute") ? (AttributeMetadata)createRequest.Parameters["Attribute"] : null;
            if (sdkAttr == null || string.IsNullOrEmpty(sdkAttr.LogicalName))
                throw DataverseFault.InvalidArgumentFault("Attribute metadata with a valid LogicalName is required.");

            var attrType = sdkAttr.AttributeType ?? AttributeTypeCode.String;
            var attrInfo = new AttributeMetadataInfo(sdkAttr.LogicalName, attrType);

            if (sdkAttr.RequiredLevel?.Value != null)
                attrInfo.RequiredLevel = sdkAttr.RequiredLevel.Value;

            store.CreateAttributeMetadata(entityName!, attrInfo);
            store.IncrementMetadataTimestamp();

            var attributeId = Guid.NewGuid();
            var response = new CreateAttributeResponse();
            response.Results["AttributeId"] = attributeId;
            return response;
        }
    }
}
