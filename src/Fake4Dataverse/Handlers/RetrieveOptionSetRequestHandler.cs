using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles RetrieveOptionSet requests by returning a minimal stub response.
    /// </summary>
    internal sealed class RetrieveOptionSetRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveOptionSet", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new OrganizationResponse { ResponseName = "RetrieveOptionSet" };
            response["OptionSetMetadata"] = null;
            return response;
        }
    }
}
