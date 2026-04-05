using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles InsertOptionValue and InsertStatusValue requests by returning the specified or generated option value.
    /// </summary>
    internal sealed class InsertOptionValueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "InsertOptionValue", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.RequestName, "InsertStatusValue", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var newValue = request.Parameters.ContainsKey("Value") ? (int)request["Value"] : new Random().Next(100000, 999999);

            var response = new OrganizationResponse { ResponseName = request.RequestName };
            response["NewOptionValue"] = newValue;
            return response;
        }
    }
}
