using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveAllOptionSetsRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveAllOptionSets", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var allOptionSets = store.GetAllGlobalOptionSets();
            var sdkOptionSets = allOptionSets
                .Select(os => (OptionSetMetadataBase)RetrieveOptionSetRequestHandler.ConvertToSdkOptionSet(os))
                .ToArray();

            var response = new RetrieveAllOptionSetsResponse();
            response.Results["OptionSetMetadata"] = sdkOptionSets;
            return response;
        }
    }
}
