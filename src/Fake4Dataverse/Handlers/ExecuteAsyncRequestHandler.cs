using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class ExecuteAsyncRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is ExecuteAsyncRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var asyncRequest = (ExecuteAsyncRequest)request;

            // Execute the inner request synchronously in the fake
            if (asyncRequest.Request != null)
            {
                service.Execute(asyncRequest.Request);
            }

            var response = new ExecuteAsyncResponse();
            response.Results["AsyncJobId"] = Guid.NewGuid();
            return response;
        }
    }
}
