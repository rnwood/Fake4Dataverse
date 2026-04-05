using System;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="RemoveMemberListRequest"/> by removing listmember association records.
    /// </summary>
    internal sealed class RemoveMemberListRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RemoveMemberListRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var removeRequest = (RemoveMemberListRequest)request;
            var listId = removeRequest.ListId;
            var entityId = removeRequest.EntityId;

            var fakeService = service as FakeOrganizationService
                ?? throw new InvalidOperationException("RemoveMemberListRequestHandler requires FakeOrganizationService.");

            var allMembers = fakeService.Store.GetAll("listmember");
            var match = allMembers.FirstOrDefault(e =>
            {
                var list = e.GetAttributeValue<EntityReference>("listid");
                return list != null && list.Id == listId && e.Id == entityId;
            });
            if (match != null)
                service.Delete("listmember", match.Id);

            return new RemoveMemberListResponse();
        }
    }
}
