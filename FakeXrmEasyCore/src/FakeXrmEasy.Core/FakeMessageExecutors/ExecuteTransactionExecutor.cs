using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class ExecuteTransactionExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ExecuteTransactionRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var executeTransactionRequest = (ExecuteTransactionRequest)request;
            var response = new ExecuteTransactionResponse { ["Responses"] = new OrganizationResponseCollection() };

            var service = ctx.GetOrganizationService();

            foreach (var r in executeTransactionRequest.Requests)
            {
                var result = service.Execute(r);

                if (executeTransactionRequest.ReturnResponses.HasValue && executeTransactionRequest.ReturnResponses.Value)
                {
                    response.Responses.Add(result);
                }
            }
            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(ExecuteTransactionRequest);
        }
    }
}
