using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Permissions;
using Fake4Dataverse.Permissions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class RetrieveSharedPrincipalsAndAccessRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveSharedPrincipalsAndAccessRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            RetrieveSharedPrincipalsAndAccessRequest req = (RetrieveSharedPrincipalsAndAccessRequest)request;
            return ctx.GetProperty<IAccessRightsRepository>().RetrieveSharedPrincipalsAndAccess(req.Target);
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveSharedPrincipalsAndAccessRequest);
        }
    }
}