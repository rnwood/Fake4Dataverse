using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles DeleteFile requests. This is a no-op in the fake since file storage
    /// is handled by the binary attribute system.
    /// </summary>
    internal sealed class DeleteFileRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "DeleteFile", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            return new OrganizationResponse { ResponseName = "DeleteFile" };
        }
    }
}
