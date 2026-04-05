using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class GetValidManyToManyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is GetValidManyToManyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            // Return all registered entities as valid for N:N
            var allEntities = store.GetAllEntityMetadataInfo();
            var entityNames = allEntities
                .Select(e =>
                {
                    var em = new EntityMetadata();
                    em.LogicalName = e.LogicalName;
                    return em;
                })
                .ToArray();

            var response = new GetValidManyToManyResponse();
            response.Results["EntityMetadata"] = entityNames;
            return response;
        }
    }
}
