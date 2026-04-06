using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="CommitFileBlocksUploadRequest"/> by assembling uploaded blocks
    /// and storing them as binary attribute data.
    /// </summary>
    internal sealed class CommitFileBlocksUploadRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CommitFileBlocksUploadRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var commitRequest = (CommitFileBlocksUploadRequest)request;
            var fakeService = service as FakeOrganizationService
                ?? throw new InvalidOperationException("CommitFileBlocksUploadRequestHandler requires FakeOrganizationService.");

            var token = commitRequest.FileContinuationToken;
            var fileName = commitRequest.FileName;

            fakeService.Environment.CommitUploadSession(token, fileName);

            var response = new CommitFileBlocksUploadResponse();
            response.Results["FileSizeInBytes"] = fakeService.Environment.GetCommittedFileSize(token);
            return response;
        }
    }
}
