using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="RetrieveVersionRequest"/> returning a configurable fake version string.
    /// Also matches untyped <c>OrganizationRequest("RetrieveVersion")</c>.
    /// </summary>
    internal sealed class RetrieveVersionRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RetrieveVersion", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new RetrieveVersionResponse();
            response.Results["Version"] = "9.2.0.0";
            return response;
        }
    }
}
