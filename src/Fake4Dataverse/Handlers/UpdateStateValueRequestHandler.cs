using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class UpdateStateValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "UpdateStateValue", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // UpdateStateValue updates the label/description of a State option value.
            // In the fake, this is a no-op that acknowledges the request.
            var fakeService = (FakeOrganizationService)service;
            fakeService.Environment.MetadataStore.IncrementMetadataTimestamp();

            return new UpdateStateValueResponse();
        }
    }
}
