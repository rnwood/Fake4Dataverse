using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class GenericCreateRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            request is not CreateRequest &&
            string.Equals(request.RequestName, "Create", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var target = (Entity)request["Target"];
            var id = service.Create(target);
            return new OrganizationResponse { Results = { ["id"] = id } };
        }
    }
}
