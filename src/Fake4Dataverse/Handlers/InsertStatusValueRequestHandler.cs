using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class InsertStatusValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is InsertStatusValueRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var insertRequest = (InsertStatusValueRequest)request;
            var newValue = insertRequest.Value ?? new Random().Next(100000, 999999);

            var fakeService = (FakeOrganizationService)service;
            fakeService.MetadataStore.IncrementMetadataTimestamp();

            var response = new InsertStatusValueResponse();
            response.Results["NewOptionValue"] = newValue;
            return response;
        }
    }
}
