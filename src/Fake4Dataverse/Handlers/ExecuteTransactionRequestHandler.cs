using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="ExecuteTransactionRequest"/> by executing all requests atomically.
    /// On first error, aborts and throws. No real rollback in the fake — simply fail fast.
    /// </summary>
    internal sealed class ExecuteTransactionRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is ExecuteTransactionRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var txRequest = (ExecuteTransactionRequest)request;
            var requests = txRequest.Requests;
            if (requests == null)
                throw DataverseFault.InvalidArgumentFault("ExecuteTransactionRequest.Requests must not be null.");

            var response = new ExecuteTransactionResponse();
            var responses = new OrganizationResponseCollection();

            for (int i = 0; i < requests.Count; i++)
            {
                try
                {
                    var subResponse = service.Execute(requests[i]);
                    responses.Add(subResponse);
                }
                catch (Exception ex)
                {
                    var fault = new OrganizationServiceFault
                    {
                        Message = ex.Message
                    };

                    response.Results["FaultedRequestIndex"] = i;
                    throw DataverseFault.Create(DataverseFault.Unspecified,
                        $"ExecuteTransaction failed at request index {i}: {ex.Message}");
                }
            }

            response.Results["Responses"] = responses;
            return response;
        }
    }
}
