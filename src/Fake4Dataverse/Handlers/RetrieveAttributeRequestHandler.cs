using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveAttributeRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveAttributeRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = (RetrieveAttributeRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var entityInfo = store.GetEntityMetadataInfo(retrieveRequest.EntityLogicalName);
            if (entityInfo == null)
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Entity '{retrieveRequest.EntityLogicalName}' metadata does not exist.");
            }

            if (!entityInfo.Attributes.TryGetValue(retrieveRequest.LogicalName, out var attrInfo))
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Attribute '{retrieveRequest.LogicalName}' does not exist on entity '{retrieveRequest.EntityLogicalName}'.");
            }

            var sdkAttribute = SdkMetadataConverter.ToSdkAttributeMetadata(attrInfo);

            var response = new RetrieveAttributeResponse();
            response.Results["AttributeMetadata"] = sdkAttribute;
            return response;
        }
    }
}
