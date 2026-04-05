using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveAllManagedPropertiesRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveAllManagedProperties", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // In the fake, return an empty collection of managed properties
            var response = new RetrieveAllManagedPropertiesResponse();
            response.Results["ManagedPropertyMetadata"] = new EntityMetadataCollection();
            return response;
        }
    }
}
