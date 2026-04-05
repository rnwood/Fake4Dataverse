using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles CloseIncident requests by creating an incident resolution activity and resolving the case.
    /// </summary>
    internal sealed class CloseIncidentRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "CloseIncident", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var incidentResolution = (Entity)request["IncidentResolution"];
            var status = ((OptionSetValue)request["Status"]).Value;

            var incidentRef = incidentResolution.GetAttributeValue<EntityReference>("incidentid");
            if (incidentRef == null)
                throw new InvalidOperationException("IncidentResolution must contain an incidentid reference.");

            // Create the resolution activity
            service.Create(incidentResolution);

            // Close the incident
            var update = new Entity("incident", incidentRef.Id);
            update["statecode"] = new OptionSetValue(1); // Resolved
            update["statuscode"] = new OptionSetValue(status);
            service.Update(update);

            return new OrganizationResponse { ResponseName = "CloseIncident" };
        }
    }
}
