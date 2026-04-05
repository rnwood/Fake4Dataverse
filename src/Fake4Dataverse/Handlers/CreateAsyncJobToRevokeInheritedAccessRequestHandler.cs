using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    internal sealed class CreateAsyncJobToRevokeInheritedAccessRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CreateAsyncJobToRevokeInheritedAccessRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            // In the fake, this is a no-op that returns a job ID
            var response = new CreateAsyncJobToRevokeInheritedAccessResponse();
            response.Results["AsyncJobId"] = Guid.NewGuid();
            return response;
        }
    }
}
