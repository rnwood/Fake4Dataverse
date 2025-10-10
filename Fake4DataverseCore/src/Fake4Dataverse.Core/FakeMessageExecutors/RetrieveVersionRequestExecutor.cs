using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class RetrieveVersionRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveVersionRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            string version = "9.0.0.0";

            return new RetrieveVersionResponse
            {
                Results = new ParameterCollection
                {
                    { "Version", version }
                }
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveVersionRequest);
        }
    }
}
