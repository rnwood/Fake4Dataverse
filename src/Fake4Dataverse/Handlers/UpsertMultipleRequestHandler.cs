using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles UpsertMultiple requests by delegating each entity to an individual UpsertRequest.
    /// </summary>
    internal sealed class UpsertMultipleRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "UpsertMultiple", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var targets = (EntityCollection)request["Targets"];
            var responses = new OrganizationResponseCollection();

            foreach (var target in targets.Entities)
            {
                var upsertReq = new UpsertRequest { Target = target };
                var resp = (UpsertResponse)service.Execute(upsertReq);
                responses.Add(resp);
            }

            var response = new OrganizationResponse { ResponseName = "UpsertMultiple" };
            response["Results"] = responses;
            return response;
        }
    }
}
