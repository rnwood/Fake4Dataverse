using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class DeleteRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "Delete", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var deleteRequest = OrganizationRequestTypeAdapter.AsTyped<DeleteRequest>(request);
            var target = deleteRequest.Target;

            Guid id = target.Id;
            if (id == Guid.Empty && target.KeyAttributes != null && target.KeyAttributes.Count > 0
                && service is FakeOrganizationService fakeService)
            {
                id = fakeService.Environment.Store.FindByAlternateKey(target.LogicalName, target.KeyAttributes, fakeService.Environment.MetadataStore);
            }

            service.Delete(target.LogicalName, id);
            return new DeleteResponse();
        }
    }
}
