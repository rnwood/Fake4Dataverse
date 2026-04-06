using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="ExecuteMultipleRequest"/> by executing each request in the collection
    /// sequentially. Supports ContinueOnError and ReturnResponses settings.
    /// </summary>
    internal sealed class ExecuteMultipleRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "ExecuteMultiple", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var emRequest = OrganizationRequestTypeAdapter.AsTyped<ExecuteMultipleRequest>(request);
            var settings = emRequest.Settings ?? new ExecuteMultipleSettings
            {
                ContinueOnError = false,
                ReturnResponses = true
            };
            var requests = emRequest.Requests;
            if (requests == null)
                throw DataverseFault.InvalidArgumentFault("ExecuteMultipleRequest.Requests must not be null.");

            var response = new ExecuteMultipleResponse();
            var responseItems = new ExecuteMultipleResponseItemCollection();

            for (int i = 0; i < requests.Count; i++)
            {
                var item = new ExecuteMultipleResponseItem { RequestIndex = i };
                try
                {
                    var subResponse = service.Execute(requests[i]);
                    if (settings.ReturnResponses)
                        item.Response = subResponse;
                }
                catch (Exception ex)
                {
                    var fault = new OrganizationServiceFault
                    {
                        Message = ex.Message
                    };
                    item.Fault = fault;

                    if (!settings.ContinueOnError)
                    {
                        responseItems.Add(item);
                        break;
                    }
                }
                responseItems.Add(item);
            }

            response.Results["Responses"] = responseItems;
            return response;
        }
    }
}
