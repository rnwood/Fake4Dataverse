using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace Fake4Dataverse.Middleware.Crud.FakeMessageExecutors
{
    public class UpdateRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var updateRequest = (UpdateRequest)request;

            var target = (Entity)request.Parameters["Target"];

            ctx.UpdateEntity(target);

            return new UpdateResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateRequest);
        }
    }
}