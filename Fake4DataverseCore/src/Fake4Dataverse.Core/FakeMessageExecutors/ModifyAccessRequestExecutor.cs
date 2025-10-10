using System;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Permissions;
using Fake4Dataverse.Permissions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class ModifyAccessRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ModifyAccessRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            ModifyAccessRequest req = (ModifyAccessRequest)request;
            ctx.GetProperty<IAccessRightsRepository>().ModifyAccessOn(req.Target, req.PrincipalAccess);
            return new ModifyAccessResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(ModifyAccessRequest);
        }
    }
}
