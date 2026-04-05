using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles LoseOpportunity requests by creating an opportunity close activity and marking the opportunity as lost.
    /// </summary>
    internal sealed class LoseOpportunityRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "LoseOpportunity", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var opportunityClose = (Entity)request["OpportunityClose"];
            var status = ((OptionSetValue)request["Status"]).Value;

            var oppRef = opportunityClose.GetAttributeValue<EntityReference>("opportunityid");
            if (oppRef == null)
                throw new InvalidOperationException("OpportunityClose must contain an opportunityid reference.");

            service.Create(opportunityClose);

            var update = new Entity("opportunity", oppRef.Id);
            update["statecode"] = new OptionSetValue(2); // Lost
            update["statuscode"] = new OptionSetValue(status);
            service.Update(update);

            return new OrganizationResponse { ResponseName = "LoseOpportunity" };
        }
    }
}
