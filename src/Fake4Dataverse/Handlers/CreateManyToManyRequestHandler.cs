using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateManyToManyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CreateManyToManyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var createRequest = (CreateManyToManyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            var rel = createRequest.ManyToManyRelationship;
            if (rel == null || string.IsNullOrEmpty(rel.SchemaName))
                throw DataverseFault.InvalidArgumentFault("ManyToManyRelationship with a valid SchemaName is required.");

            var info = new ManyToManyRelationshipInfo(
                rel.SchemaName,
                rel.Entity1LogicalName ?? string.Empty,
                rel.Entity2LogicalName ?? string.Empty,
                rel.IntersectEntityName);

            store.CreateManyToManyRelationshipInternal(info);
            store.IncrementMetadataTimestamp();

            var response = new CreateManyToManyResponse();
            response.Results["ManyToManyRelationshipId"] = Guid.NewGuid();
            return response;
        }
    }
}
