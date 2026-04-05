using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles the RetrieveVersion request returning a configurable fake version string.
    /// </summary>
    internal sealed class RetrieveVersionRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveVersion", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new OrganizationResponse { ResponseName = "RetrieveVersion" };
            response.Results["Version"] = "9.2.0.0";
            return response;
        }
    }
}
