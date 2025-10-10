using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class AssignRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AssignRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var assignRequest = (AssignRequest)request;

            var target = assignRequest.Target;
            var assignee = assignRequest.Assignee;

            if (target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not assign without target");
            }

            if (assignee == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Can not assign without assignee");
            }

            var service = ctx.GetOrganizationService();

            KeyValuePair<string, object> owningX = new KeyValuePair<string, object>();
            if (assignee.LogicalName == "systemuser")
                owningX = new KeyValuePair<string, object>("owninguser", assignee);
            else if (assignee.LogicalName == "team")
                owningX = new KeyValuePair<string, object>("owningteam", assignee);

            var assignment = new Entity
            {
                LogicalName = target.LogicalName,
                Id = target.Id,
                Attributes = new AttributeCollection
                {
                    { "ownerid", assignee },
                    owningX
                }
            };

            service.Update(assignment);

            return new AssignResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(AssignRequest);
        }
    }
}