using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace Fake4Dataverse.Middleware.Crud.FakeMessageExecutors
{
    public class CreateRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var createRequest = (CreateRequest)request;

            var guid = ctx.CreateEntity(createRequest.Target);

            return new CreateResponse()
            {
                ResponseName = "Create",
                Results = new ParameterCollection { { "id", guid } }
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(CreateRequest);
        }
    }
}