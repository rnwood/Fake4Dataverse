using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles DownloadBlock requests by returning empty data.
    /// </summary>
    internal sealed class DownloadBlockRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "DownloadBlock", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new OrganizationResponse { ResponseName = "DownloadBlock" };
            response["Data"] = Array.Empty<byte>();
            return response;
        }
    }
}
