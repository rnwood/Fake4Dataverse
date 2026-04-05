using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="AddListMembersListRequest"/> by creating listmember association records.
    /// </summary>
    internal sealed class AddListMembersListRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is AddListMembersListRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var addRequest = (AddListMembersListRequest)request;
            var listId = addRequest.ListId;
            var memberIds = addRequest.MemberIds;

            foreach (var memberId in memberIds)
            {
                var membership = new Entity("listmember")
                {
                    ["listid"] = new EntityReference("list", listId),
                    ["entityid"] = memberId
                };
                service.Create(membership);
            }

            return new AddListMembersListResponse();
        }
    }
}
