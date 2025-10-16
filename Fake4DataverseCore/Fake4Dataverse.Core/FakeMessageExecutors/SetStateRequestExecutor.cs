using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class SetStateRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is SetStateRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as SetStateRequest;

            //We are going to translate a SetStateRequest into an update message basically

            var entityName = req.EntityMoniker.LogicalName;
            var guid = req.EntityMoniker.Id;

            var entityToUpdate = new Entity(entityName) { Id = guid };
            entityToUpdate["statecode"] = req.State;
            entityToUpdate["statuscode"] = req.Status;

            var fakedService = ctx.GetOrganizationService();
            fakedService.Update(entityToUpdate);

            return new SetStateResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(SetStateRequest);
        }
    }
}