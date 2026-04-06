using System;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateOneToManyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CreateOneToManyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var createRequest = (CreateOneToManyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var rel = createRequest.OneToManyRelationship;
            if (rel == null || string.IsNullOrEmpty(rel.SchemaName))
                throw DataverseFault.InvalidArgumentFault("OneToManyRelationship with a valid SchemaName is required.");

            var info = new OneToManyRelationshipInfo(
                rel.SchemaName,
                rel.ReferencedEntity ?? string.Empty,
                rel.ReferencedAttribute ?? string.Empty,
                rel.ReferencingEntity ?? string.Empty,
                rel.ReferencingAttribute ?? string.Empty);

            store.CreateOneToManyRelationshipInternal(info);
            store.IncrementMetadataTimestamp();

            var response = new CreateOneToManyResponse();
            response.Results["RelationshipId"] = Guid.NewGuid();
            response.Results["AttributeId"] = Guid.NewGuid();
            return response;
        }
    }
}
