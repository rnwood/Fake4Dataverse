using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveRelationshipRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveRelationshipRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = (RetrieveRelationshipRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.MetadataStore;

            if (string.IsNullOrEmpty(retrieveRequest.Name))
                throw DataverseFault.InvalidArgumentFault("Relationship name is required.");

            RelationshipMetadataBase? result = null;

            var otm = store.GetOneToManyRelationship(retrieveRequest.Name);
            if (otm != null)
            {
                var rel = new OneToManyRelationshipMetadata();
                rel.SchemaName = otm.SchemaName;
                rel.ReferencedEntity = otm.ReferencedEntity;
                rel.ReferencedAttribute = otm.ReferencedAttribute;
                rel.ReferencingEntity = otm.ReferencingEntity;
                rel.ReferencingAttribute = otm.ReferencingAttribute;
                result = rel;
            }

            if (result == null)
            {
                var mtm = store.GetManyToManyRelationship(retrieveRequest.Name);
                if (mtm != null)
                {
                    var rel = new ManyToManyRelationshipMetadata();
                    rel.SchemaName = mtm.SchemaName;
                    rel.Entity1LogicalName = mtm.Entity1LogicalName;
                    rel.Entity2LogicalName = mtm.Entity2LogicalName;
                    if (mtm.IntersectEntityName != null)
                        rel.IntersectEntityName = mtm.IntersectEntityName;
                    result = rel;
                }
            }

            if (result == null)
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Relationship '{retrieveRequest.Name}' does not exist.");
            }

            var response = new RetrieveRelationshipResponse();
            response.Results["RelationshipMetadata"] = result;
            return response;
        }
    }
}
