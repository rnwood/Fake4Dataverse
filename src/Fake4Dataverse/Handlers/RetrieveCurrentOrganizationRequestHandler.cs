using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles RetrieveCurrentOrganization requests by returning the fake organization details.
    /// </summary>
    internal sealed class RetrieveCurrentOrganizationRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveCurrentOrganization", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var fakeService = service as FakeOrganizationService;
            var detail = new Entity("organization");
            detail["organizationid"] = fakeService?.OrganizationId ?? Guid.Empty;
            detail["name"] = fakeService?.OrganizationName ?? "FakeOrganization";
            detail["uniquename"] = fakeService?.OrganizationName ?? "FakeOrganization";

            var response = new OrganizationResponse { ResponseName = "RetrieveCurrentOrganization" };
            response["Detail"] = detail;
            return response;
        }
    }
}
