using System;
using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles RetrieveOptionSet requests by returning global option set metadata from the store.
    /// </summary>
    internal sealed class RetrieveOptionSetRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveOptionSetRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = (RetrieveOptionSetRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var name = retrieveRequest.Name;
            OptionSetMetadataBase? sdkOptionSet = null;

            if (!string.IsNullOrEmpty(name))
            {
                var info = store.GetGlobalOptionSet(name);
                if (info != null)
                {
                    sdkOptionSet = ConvertToSdkOptionSet(info);
                }
            }

            var response = new RetrieveOptionSetResponse();
            response.Results["OptionSetMetadata"] = sdkOptionSet;
            return response;
        }

        internal static OptionSetMetadata ConvertToSdkOptionSet(GlobalOptionSetInfo info)
        {
            var osm = new OptionSetMetadata();
            osm.Name = info.Name;
            osm.IsGlobal = info.IsGlobal;
            foreach (var opt in info.Options)
            {
                osm.Options.Add(new OptionMetadata(
                    new Label(opt.Label ?? opt.Value.ToString(), 1033), opt.Value));
            }
            return osm;
        }
    }
}
