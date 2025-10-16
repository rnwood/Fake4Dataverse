using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Permissions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class RevokeAccessRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RevokeAccessRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            RevokeAccessRequest req = (RevokeAccessRequest)request;
            ctx.GetProperty<IAccessRightsRepository>().RevokeAccessTo(req.Target, req.Revokee);
            return new RevokeAccessResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RevokeAccessRequest);
        }
    }
}