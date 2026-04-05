using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles PublishXml and PublishAllXml requests as no-ops in the test context.
    /// </summary>
    internal sealed class PublishXmlRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "PublishXml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.RequestName, "PublishAllXml", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // No-op in test context
            return new OrganizationResponse { ResponseName = request.RequestName };
        }
    }
}
