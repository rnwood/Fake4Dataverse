using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class WhoAmIRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is WhoAmIRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as WhoAmIRequest;

            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.whoamiresponse
            // WhoAmIResponse has three properties: UserId, BusinessUnitId, and OrganizationId
            // These are populated from the Results collection in the OrganizationResponse
            var response = new WhoAmIResponse
            {
                Results = new ParameterCollection
                {
                    { "UserId", ctx.CallerProperties.CallerId.Id },
                    { "BusinessUnitId", ctx.CallerProperties.BusinessUnitId.Id },
                    { "OrganizationId", Guid.NewGuid() } // Fake OrganizationId for testing
                }
            };
            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(WhoAmIRequest);
        }
    }
}