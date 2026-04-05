using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class GetValidReferencingEntitiesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "GetValidReferencingEntities", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            // Return all registered entities as valid referencing entities
            var allEntities = store.GetAllEntityMetadataInfo();
            var entityNames = allEntities.Select(e => e.LogicalName).ToArray();

            var response = new GetValidReferencingEntitiesResponse();
            response.Results["EntityNames"] = entityNames;
            return response;
        }
    }
}
