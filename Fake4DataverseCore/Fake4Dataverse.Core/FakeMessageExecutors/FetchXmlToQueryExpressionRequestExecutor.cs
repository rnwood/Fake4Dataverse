using System;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Query;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class FetchXmlToQueryExpressionRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is FetchXmlToQueryExpressionRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as FetchXmlToQueryExpressionRequest;
            var service = ctx.GetOrganizationService();
            FetchXmlToQueryExpressionResponse response = new FetchXmlToQueryExpressionResponse();
            response["Query"] = req.FetchXml.ToQueryExpression(ctx);
            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(FetchXmlToQueryExpressionRequest);
        }
    }
}
