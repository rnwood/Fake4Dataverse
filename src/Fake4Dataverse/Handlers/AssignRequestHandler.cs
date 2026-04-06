using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="AssignRequest"/> by updating the ownerid on the target entity.
    /// </summary>
    internal sealed class AssignRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "Assign", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var assignRequest = OrganizationRequestTypeAdapter.AsTyped<AssignRequest>(request);
            var target = assignRequest.Target;
            var update = new Entity(target.LogicalName, target.Id)
            {
                ["ownerid"] = assignRequest.Assignee
            };
            service.Update(update);
            return new AssignResponse();
        }
    }
}
