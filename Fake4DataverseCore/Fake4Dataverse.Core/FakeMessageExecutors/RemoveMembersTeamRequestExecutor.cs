using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class RemoveMembersTeamRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RemoveMembersTeamRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = (RemoveMembersTeamRequest)request;

            if (req.TeamId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("TeamId parameter is required");
            }

            if (req.MemberIds == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("MemberIds parameter is required");
            }

            var service = ctx.GetOrganizationService();

            // Find the list
            var team = ctx.CreateQuery("team").FirstOrDefault(e => e.Id == req.TeamId);

            if (team == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(string.Format("Team with Id {0} wasn't found", req.TeamId.ToString()));
            }

            foreach (var memberId in req.MemberIds)
            {
                var user = ctx.CreateQuery("systemuser").FirstOrDefault(e => e.Id == memberId);
                if (user == null)
                {
                    throw FakeOrganizationServiceFaultFactory.New(string.Format("SystemUser with Id {0} wasn't found", memberId.ToString()));
                }

                var queryTeamMember = new QueryExpression("teammembership")
                {
                    TopCount = 1,
                    ColumnSet = new ColumnSet("teammembershipid"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("teamid", ConditionOperator.Equal, req.TeamId),
                            new ConditionExpression("systemuserid", ConditionOperator.Equal, user.Id)
                        }
                    }
                };

                var teamMember = service.RetrieveMultiple(queryTeamMember).Entities.FirstOrDefault();

                if (teamMember != null)
                {
                    service.Delete("teammembership", teamMember.Id);
                }
            }

            return new RemoveMembersTeamResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RemoveMembersTeamRequest);
        }
    }
}