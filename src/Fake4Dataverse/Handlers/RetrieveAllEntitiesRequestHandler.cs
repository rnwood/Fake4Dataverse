using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveAllEntitiesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveAllEntitiesRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var allEntities = store.GetAllEntityMetadataInfo();
            var sdkEntities = allEntities
                .Select(e => SdkMetadataConverter.ToSdkEntityMetadata(
                    e,
                    store.GetOneToManyRelationships(e.LogicalName),
                    store.GetManyToManyRelationships(e.LogicalName)))
                .ToArray();

            var response = new RetrieveAllEntitiesResponse();
            response.Results["EntityMetadata"] = sdkEntities;
            return response;
        }
    }
}
