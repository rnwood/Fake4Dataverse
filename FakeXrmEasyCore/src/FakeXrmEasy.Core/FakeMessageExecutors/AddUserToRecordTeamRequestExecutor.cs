using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using FakeXrmEasy.Abstractions.FakeMessageExecutors;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Permissions;
using FakeXrmEasy.Abstractions.Permissions;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class AddUserToRecordTeamRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AddUserToRecordTeamRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            AddUserToRecordTeamRequest addReq = (AddUserToRecordTeamRequest)request;

            EntityReference target = addReq.Record;
            Guid systemuserId = addReq.SystemUserId;
            Guid teamTemplateId = addReq.TeamTemplateId;

            if (target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not add to team without target");
            }

            if (systemuserId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not add to team without user");
            }

            if (teamTemplateId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not add to team without team");
            }

            IOrganizationService service = ctx.GetOrganizationService();

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


            Entity team = ctx.CreateQuery("team").FirstOrDefault(p => ((EntityReference)p["teamtemplateid"]).Id == teamTemplateId);
            if (team == null)
            {
                team = new Entity("team")
                {
                    ["teamtemplateid"] = new EntityReference("teamtemplate", teamTemplateId)
                };
                team.Id = service.Create(team);
            }

            Entity tm = new Entity("teammembership")
            {
                ["systemuserid"] = systemuserId,
                ["teamid"] = team.Id
            };
            tm.Id = service.Create(tm);

            Entity poa = new Entity("principalobjectaccess")
            {
                ["objectid"] = target.Id,
                ["principalid"] = team.Id,
                ["accessrightsmask"] = teamTemplate.Contains("defaultaccessrightsmask") ? teamTemplate["defaultaccessrightsmask"] : 0
            };
            poa.Id = service.Create(poa);

            ctx.GetProperty<IAccessRightsRepository>().GrantAccessTo(target, new PrincipalAccess
            {
                Principal = user.ToEntityReference(),
                AccessMask = (AccessRights)poa["accessrightsmask"]
            });
            
            return new AddUserToRecordTeamResponse
            {
                ResponseName = "AddUserToRecordTeam"
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(AddUserToRecordTeamRequest);
        }
    }
}
