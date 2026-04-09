using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class UpdateRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "Update", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var updateRequest = OrganizationRequestTypeAdapter.AsTyped<UpdateRequest>(request);

            if (updateRequest.ConcurrencyBehavior == ConcurrencyBehavior.IfRowVersionMatches
                && service is FakeOrganizationService fakeService)
            {
                var target = updateRequest.Target;
                if (string.IsNullOrEmpty(target.RowVersion))
                    throw DataverseFault.ConcurrencyVersionNotProvidedFault();

                if (!long.TryParse(target.RowVersion, out var expectedVersion))
                    throw DataverseFault.ConcurrencyVersionNotProvidedFault();

                fakeService.UpdateWithConcurrencyCheck(target, expectedVersion);
            }
            else
            {
                service.Update(updateRequest.Target);
            }

            return new UpdateResponse();
        }
    }
}
