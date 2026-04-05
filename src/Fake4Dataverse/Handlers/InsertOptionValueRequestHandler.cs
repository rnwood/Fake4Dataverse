using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles InsertOptionValue requests by adding an option to the global option set in the store.
    /// </summary>
    internal sealed class InsertOptionValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "InsertOptionValue", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var insertRequest = OrganizationRequestTypeAdapter.AsTyped<InsertOptionValueRequest>(request);
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var newValue = insertRequest.Value ?? new Random().Next(100000, 999999);
            var label = insertRequest.Label?.UserLocalizedLabel?.Label;

            // If OptionSetName is given, add to global option set
            if (!string.IsNullOrEmpty(insertRequest.OptionSetName))
            {
                var optionSet = store.GetGlobalOptionSet(insertRequest.OptionSetName);
                if (optionSet != null)
                {
                    optionSet.Options.Add(new OptionInfo(newValue, label));
                }
            }

            store.IncrementMetadataTimestamp();

            var response = new InsertOptionValueResponse();
            response.Results["NewOptionValue"] = newValue;
            return response;
        }
    }
}
