using System;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="RemoveMembersTeamRequest"/> by removing teammembership association records.
    /// </summary>
    internal sealed class RemoveMembersTeamRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RemoveMembersTeamRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var removeRequest = (RemoveMembersTeamRequest)request;
            var teamId = removeRequest.TeamId;
            var memberIds = removeRequest.MemberIds;

            var fakeService = service as FakeOrganizationService
                ?? throw new InvalidOperationException("RemoveMembersTeamRequestHandler requires FakeOrganizationService.");

            var allMemberships = fakeService.Store.GetAll("teammembership");
            foreach (var memberId in memberIds)
            {
                var match = allMemberships.FirstOrDefault(e =>
                {
                    var team = e.GetAttributeValue<EntityReference>("teamid");
                    var user = e.GetAttributeValue<EntityReference>("systemuserid");
                    return team != null && team.Id == teamId && user != null && user.Id == memberId;
                });
                if (match != null)
                    service.Delete("teammembership", match.Id);
            }

            return new RemoveMembersTeamResponse();
        }
    }
}
