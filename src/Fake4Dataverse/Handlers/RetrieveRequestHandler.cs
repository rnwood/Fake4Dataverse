using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "Retrieve", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = OrganizationRequestTypeAdapter.AsTyped<RetrieveRequest>(request);
            var target = retrieveRequest.Target;
            Entity entity;

            if (target.Id == Guid.Empty && target.KeyAttributes != null && target.KeyAttributes.Count > 0
                && service is FakeOrganizationService fakeService)
            {
                entity = fakeService.RetrieveByAlternateKey(target.LogicalName, target.KeyAttributes, retrieveRequest.ColumnSet);
            }
            else
            {
                entity = service.Retrieve(target.LogicalName, target.Id, retrieveRequest.ColumnSet);
            }

            return new RetrieveResponse { Results = { ["Entity"] = entity } };
        }
    }
}
