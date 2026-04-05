using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles the UpdateMultiple request by updating each entity in the Targets collection.
    /// </summary>
    internal sealed class UpdateMultipleRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "UpdateMultiple", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var targets = request.Parameters.ContainsKey("Targets")
                ? request.Parameters["Targets"] as EntityCollection
                : null;

            if (targets == null)
                throw DataverseFault.InvalidArgumentFault("UpdateMultiple requires a 'Targets' parameter of type EntityCollection.");

            foreach (var entity in targets.Entities)
            {
                service.Update(entity);
            }

            return new OrganizationResponse { ResponseName = "UpdateMultiple" };
        }
    }
}
