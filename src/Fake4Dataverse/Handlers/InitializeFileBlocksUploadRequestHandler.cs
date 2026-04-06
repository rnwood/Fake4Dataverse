using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="InitializeFileBlocksUploadRequest"/> by creating an in-memory upload session.
    /// </summary>
    internal sealed class InitializeFileBlocksUploadRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is InitializeFileBlocksUploadRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var initRequest = (InitializeFileBlocksUploadRequest)request;
            var fakeService = service as FakeOrganizationService
                ?? throw new InvalidOperationException("InitializeFileBlocksUploadRequestHandler requires FakeOrganizationService.");

            var target = initRequest.Target;
            var attributeName = initRequest.FileAttributeName;

            var token = Guid.NewGuid().ToString("N");
            fakeService.Environment.CreateUploadSession(token, target.LogicalName, target.Id, attributeName);

            var response = new InitializeFileBlocksUploadResponse();
            response.Results["FileContinuationToken"] = token;
            return response;
        }
    }
}
