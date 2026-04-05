using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class InsertStatusValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "InsertStatusValue", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var insertRequest = OrganizationRequestTypeAdapter.AsTyped<InsertStatusValueRequest>(request);
            var newValue = insertRequest.Value ?? new Random().Next(100000, 999999);

            var fakeService = (FakeOrganizationService)service;
            fakeService.Environment.MetadataStore.IncrementMetadataTimestamp();

            var response = new InsertStatusValueResponse();
            response.Results["NewOptionValue"] = newValue;
            return response;
        }
    }
}
