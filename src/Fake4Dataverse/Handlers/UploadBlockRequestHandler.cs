using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="UploadBlockRequest"/> by appending data to an in-memory upload session.
    /// </summary>
    internal sealed class UploadBlockRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is UploadBlockRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var uploadRequest = (UploadBlockRequest)request;
            var fakeService = service as FakeOrganizationService
                ?? throw new InvalidOperationException("UploadBlockRequestHandler requires FakeOrganizationService.");

            var token = uploadRequest.FileContinuationToken;
            var blockData = uploadRequest.BlockData;

            fakeService.Environment.AppendUploadBlock(token, blockData);

            return new UploadBlockResponse();
        }
    }
}
