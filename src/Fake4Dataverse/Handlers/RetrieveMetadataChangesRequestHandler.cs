using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveMetadataChangesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveMetadataChangesRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            // Return all entities as the "changes" — simplified implementation
            var allEntities = store.GetAllEntityMetadataInfo();
            var sdkEntities = allEntities
                .Select(e => SdkMetadataConverter.ToSdkEntityMetadata(
                    e,
                    store.GetOneToManyRelationships(e.LogicalName),
                    store.GetManyToManyRelationships(e.LogicalName)))
                .ToList();

            var collection = new EntityMetadataCollection();
            foreach (var em in sdkEntities)
                collection.Add(em);

            var timestamp = store.GetMetadataTimestamp().ToString();

            var response = new RetrieveMetadataChangesResponse();
            response.Results["EntityMetadata"] = collection;
            response.Results["ServerVersionStamp"] = timestamp;
            response.Results["DeletedMetadata"] = null;
            return response;
        }
    }
}
