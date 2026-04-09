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
                && service is FakeOrganizationService fakeService1)
            {
                id = fakeService1.Environment.Store.FindByAlternateKey(target.LogicalName, target.KeyAttributes, fakeService1.Environment.MetadataStore);
            }

            if (deleteRequest.ConcurrencyBehavior == ConcurrencyBehavior.IfRowVersionMatches
                && service is FakeOrganizationService fakeService2)
            {
                if (string.IsNullOrEmpty(target.RowVersion))
                    throw DataverseFault.ConcurrencyVersionNotProvidedFault();

                if (!long.TryParse(target.RowVersion, out var expectedVersion))
                    throw DataverseFault.ConcurrencyVersionNotProvidedFault();

                fakeService2.DeleteWithConcurrencyCheck(target.LogicalName, id, expectedVersion);
            }
            else
            {
                service.Delete(target.LogicalName, id);
            }

            return new DeleteResponse();
        }
    }
}
