using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles ReviseQuote requests by creating a new draft copy of an existing quote.
    /// </summary>
    internal sealed class ReviseQuoteRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "ReviseQuote", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var quoteId = ((EntityReference)request["QuoteId"]).Id;
            var columnSet = request.Parameters.ContainsKey("ColumnSet") ? (ColumnSet)request["ColumnSet"] : new ColumnSet(true);

            // Retrieve original quote
            var original = service.Retrieve("quote", quoteId, columnSet);

            // Create a revised copy
            var revised = new Entity("quote");
            foreach (var attr in original.Attributes)
            {
                if (attr.Key != "quoteid" && attr.Key != "statecode" && attr.Key != "statuscode")
                    revised[attr.Key] = InMemoryEntityStore.CloneAttributeValue(attr.Value);
            }
            revised["statecode"] = new OptionSetValue(0); // Draft
            revised["statuscode"] = new OptionSetValue(1); // Draft
            var revisedId = service.Create(revised);

            var response = new OrganizationResponse { ResponseName = "ReviseQuote" };
            response["Entity"] = service.Retrieve("quote", revisedId, new ColumnSet(true));
            return response;
        }
    }
}
