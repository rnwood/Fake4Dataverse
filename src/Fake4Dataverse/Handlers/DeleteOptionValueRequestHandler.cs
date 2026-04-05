using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteOptionValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is DeleteOptionValueRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = (DeleteOptionValueRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var value = deleteRequest.Value;

            if (!string.IsNullOrEmpty(deleteRequest.OptionSetName))
            {
                var optionSet = store.GetGlobalOptionSet(deleteRequest.OptionSetName);
                if (optionSet != null)
                {
                    optionSet.Options.RemoveAll(o => o.Value == value);
                }
            }

            store.IncrementMetadataTimestamp();

            return new DeleteOptionValueResponse();
        }
    }
}
