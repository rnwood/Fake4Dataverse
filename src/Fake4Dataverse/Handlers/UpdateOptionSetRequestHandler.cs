using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class UpdateOptionSetRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is UpdateOptionSetRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var updateRequest = (UpdateOptionSetRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var sdkOptionSet = updateRequest.OptionSet;
            if (sdkOptionSet == null || string.IsNullOrEmpty(sdkOptionSet.Name))
                throw DataverseFault.InvalidArgumentFault("OptionSet with a valid Name is required.");

            var info = new GlobalOptionSetInfo(sdkOptionSet.Name)
            {
                IsGlobal = sdkOptionSet.IsGlobal ?? true
            };

            if (sdkOptionSet is OptionSetMetadata osm)
            {
                foreach (var opt in osm.Options)
                {
                    if (opt.Value.HasValue)
                        info.Options.Add(new OptionInfo(opt.Value.Value, opt.Label?.UserLocalizedLabel?.Label));
                }
            }

            store.UpdateGlobalOptionSet(info);
            store.IncrementMetadataTimestamp();

            return new UpdateOptionSetResponse();
        }
    }
}
