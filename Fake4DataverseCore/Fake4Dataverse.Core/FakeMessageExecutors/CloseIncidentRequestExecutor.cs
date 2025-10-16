using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class CloseIncidentRequestExecutor : IFakeMessageExecutor
    {
        private const string AttributeIncidentId = "incidentid";
        private const string AttributeSubject = "subject";
        private const string IncidentLogicalName = "incident";
        private const string IncidentResolutionLogicalName = "incidentresolution";
        private const int StateResolved = 1;

        public bool CanExecute(OrganizationRequest request)
        {
            return request is CloseIncidentRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var service = ctx.GetOrganizationService();
            var closeIncidentRequest = (CloseIncidentRequest)request;

            var incidentResolution = closeIncidentRequest.IncidentResolution;
            if (incidentResolution == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Cannot close incident without incident resolution.");
            }

            var status = closeIncidentRequest.Status;
            if (status == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Cannot close incident without status.");
            }

            var incidentId = (EntityReference)incidentResolution[AttributeIncidentId];
            if (!ctx.ContainsEntity(IncidentLogicalName,incidentId.Id))
            {
                throw FakeOrganizationServiceFaultFactory.New(string.Format("Incident with id {0} not found.", incidentId.Id));
            }

            var newIncidentResolution = new Entity
            {
                LogicalName = IncidentResolutionLogicalName,
                Attributes = new AttributeCollection
                {
                    { "description", incidentResolution[AttributeSubject] },
                    { AttributeSubject, incidentResolution[AttributeSubject] },
                    { AttributeIncidentId, incidentId }
                }
            };
            service.Create(newIncidentResolution);

            var setState = new SetStateRequest
            {
                EntityMoniker = incidentId,
                Status = status,
                State = new OptionSetValue(StateResolved)
            };

            service.Execute(setState);

            return new CloseIncidentResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(CloseIncidentResponse);
        }
    }
}