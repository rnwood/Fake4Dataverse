using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class GetValidReferencedEntitiesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is GetValidReferencedEntitiesRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            // Return all registered entities as valid referenced entities
            var allEntities = store.GetAllEntityMetadataInfo();
            var entityNames = allEntities.Select(e => e.LogicalName).ToArray();

            var response = new GetValidReferencedEntitiesResponse();
            response.Results["EntityNames"] = entityNames;
            return response;
        }
    }
}
