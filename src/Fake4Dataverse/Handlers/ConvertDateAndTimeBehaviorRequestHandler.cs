using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class ConvertDateAndTimeBehaviorRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is ConvertDateAndTimeBehaviorRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // In the fake, this is a no-op that acknowledges the conversion request
            var response = new ConvertDateAndTimeBehaviorResponse();
            response.Results["JobId"] = System.Guid.NewGuid();
            return response;
        }
    }
}
