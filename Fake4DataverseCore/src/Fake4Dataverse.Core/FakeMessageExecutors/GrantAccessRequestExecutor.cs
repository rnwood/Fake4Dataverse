using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Permissions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class GrantAccessRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is GrantAccessRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            GrantAccessRequest req = (GrantAccessRequest)request;
            ctx.GetProperty<IAccessRightsRepository>().GrantAccessTo(req.Target, req.PrincipalAccess);
            return new GrantAccessResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(GrantAccessRequest);
        }
    }
}