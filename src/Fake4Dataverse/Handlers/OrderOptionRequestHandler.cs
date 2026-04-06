using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class OrderOptionRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is OrderOptionRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var orderRequest = (OrderOptionRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            if (!string.IsNullOrEmpty(orderRequest.OptionSetName) && orderRequest.Values != null)
            {
                var optionSet = store.GetGlobalOptionSet(orderRequest.OptionSetName);
                if (optionSet != null)
                {
                    var ordered = new List<OptionInfo>();
                    foreach (var val in orderRequest.Values)
                    {
                        var opt = optionSet.Options.FirstOrDefault(o => o.Value == val);
                        if (opt != null)
                            ordered.Add(opt);
                    }
                    // Add any remaining options not in the order list
                    foreach (var opt in optionSet.Options)
                    {
                        if (!ordered.Contains(opt))
                            ordered.Add(opt);
                    }
                    optionSet.Options.Clear();
                    optionSet.Options.AddRange(ordered);
                }
            }

            store.IncrementMetadataTimestamp();

            return new OrderOptionResponse();
        }
    }
}
