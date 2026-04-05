using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles QualifyLead requests by creating Account/Contact/Opportunity records and deactivating the lead.
    /// </summary>
    internal sealed class QualifyLeadRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "QualifyLead", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var leadId = ((EntityReference)request["LeadId"]).Id;
            var createAccount = request.Parameters.ContainsKey("CreateAccount") && (bool)request["CreateAccount"];
            var createContact = request.Parameters.ContainsKey("CreateContact") && (bool)request["CreateContact"];
            var createOpportunity = request.Parameters.ContainsKey("CreateOpportunity") && (bool)request["CreateOpportunity"];
            var status = request.Parameters.ContainsKey("Status") ? ((OptionSetValue)request["Status"]).Value : 3;

            // Retrieve lead info
            var lead = service.Retrieve("lead", leadId, new ColumnSet(true));
            var createdEntities = new EntityReferenceCollection();

            if (createAccount)
            {
                var account = new Entity("account");
                account["name"] = lead.GetAttributeValue<string>("companyname") ?? lead.GetAttributeValue<string>("subject");
                var accountId = service.Create(account);
                createdEntities.Add(new EntityReference("account", accountId));
            }

            if (createContact)
            {
                var contact = new Entity("contact");
                contact["firstname"] = lead.GetAttributeValue<string>("firstname");
                contact["lastname"] = lead.GetAttributeValue<string>("lastname");
                var contactId = service.Create(contact);
                createdEntities.Add(new EntityReference("contact", contactId));
            }

            if (createOpportunity)
            {
                var opp = new Entity("opportunity");
                opp["name"] = lead.GetAttributeValue<string>("subject") ?? "Qualified Lead";
                var oppId = service.Create(opp);
                createdEntities.Add(new EntityReference("opportunity", oppId));
            }

            // Deactivate the lead
            var deactivate = new Entity("lead", leadId);
            deactivate["statecode"] = new OptionSetValue(1); // Qualified
            deactivate["statuscode"] = new OptionSetValue(status);
            service.Update(deactivate);

            var response = new OrganizationResponse { ResponseName = "QualifyLead" };
            response["CreatedEntities"] = createdEntities;
            return response;
        }
    }
}
