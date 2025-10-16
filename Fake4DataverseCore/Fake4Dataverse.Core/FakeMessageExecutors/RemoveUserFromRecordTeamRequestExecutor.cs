using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Permissions;
using Fake4Dataverse.Abstractions.Permissions;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class RemoveUserFromRecordTeamRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RemoveUserFromRecordTeamRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            RemoveUserFromRecordTeamRequest remReq = (RemoveUserFromRecordTeamRequest)request;

            EntityReference target = remReq.Record;
            Guid systemuserId = remReq.SystemUserId;
            Guid teamTemplateId = remReq.TeamTemplateId;

            if (target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New( "Can not remove from team without target");
            }

            if (systemuserId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not remove from team without user");
            }

            if (teamTemplateId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not remove from team without team");
            }

            Entity teamTemplate = ctx.CreateQuery("teamtemplate").FirstOrDefault(p => p.Id == teamTemplateId);
            if (teamTemplate == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Team template with id=" + teamTemplateId + " does not exist");
            }

            Entity user = ctx.CreateQuery("systemuser").FirstOrDefault(p => p.Id == systemuserId);
            if (user == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("User with id=" + teamTemplateId + " does not exist");
            }

            IOrganizationService service = ctx.GetOrganizationService();

            ctx.GetProperty<IAccessRightsRepository>().RevokeAccessTo(target, user.ToEntityReference());
            Entity team = ctx.CreateQuery("team").FirstOrDefault(p => ((EntityReference)p["teamtemplateid"]).Id == teamTemplateId);
            if (team == null)
            {
                return new RemoveUserFromRecordTeamResponse
                {
                    ResponseName = "RemoveUserFromRecordTeam"
                };
            }
                
            Entity tm = ctx.CreateQuery("teammembership").FirstOrDefault(p => (Guid)p["teamid"] == team.Id);
            if (tm != null)
            {
                service.Delete(tm.LogicalName, tm.Id);
            }

            return new RemoveUserFromRecordTeamResponse
            {
                ResponseName = "RemoveUserFromRecordTeam"
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RemoveUserFromRecordTeamRequestExecutor);
        }
    }
}
