using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class PublishXmlRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is PublishXmlRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as PublishXmlRequest;

            if (string.IsNullOrWhiteSpace(req.ParameterXml))
            {
                throw new Exception(string.Format("ParameterXml property must not be blank."));
            }
            return new PublishXmlResponse()
            {
            };
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(PublishXmlRequest);
        }
    }
}