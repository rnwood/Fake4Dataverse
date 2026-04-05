using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="RetrievePrincipalAccessRequest"/> by returning the effective access rights
    /// a principal has on a record (from ownership + sharing + roles).
    /// </summary>
    internal sealed class RetrievePrincipalAccessRequestHandler : IOrganizationRequestHandler
    {
        private readonly Security.SecurityManager _security;
        private readonly InMemoryEntityStore _store;

        public RetrievePrincipalAccessRequestHandler(Security.SecurityManager security, InMemoryEntityStore store)
        {
            _security = security;
            _store = store;
        }

        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrievePrincipalAccess", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = OrganizationRequestTypeAdapter.AsTyped<RetrievePrincipalAccessRequest>(request);
            var target = retrieveRequest.Target;
            var principalId = retrieveRequest.Principal.Id;

            // Try to determine owner
            Guid? ownerId = null;
            try
            {
                var entity = _store.Retrieve(target.LogicalName, target.Id, new ColumnSet("ownerid"));
                var ownerRef = entity.GetAttributeValue<EntityReference>("ownerid");
                if (ownerRef != null)
                    ownerId = ownerRef.Id;
            }
            catch (System.ServiceModel.FaultException<OrganizationServiceFault>)
            {
                // Entity not found or no ownerid — proceed without owner context
            }

            var rights = _security.RetrievePrincipalAccess(target.LogicalName, target.Id, principalId, ownerId);

            var response = new RetrievePrincipalAccessResponse();
            response.Results["AccessRights"] = rights;
            return response;
        }
    }
}
