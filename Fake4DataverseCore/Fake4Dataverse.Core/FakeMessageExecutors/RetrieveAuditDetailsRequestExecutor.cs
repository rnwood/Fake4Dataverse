using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Audit;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executor for RetrieveAuditDetailsRequest
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest
    /// 
    /// RetrieveAuditDetailsRequest retrieves the full audit details for a specific audit record.
    /// This includes the AttributeAuditDetail with old and new values for changed attributes.
    /// The request requires:
    /// - AuditId: The GUID of the audit record to retrieve details for
    /// 
    /// The response contains:
    /// - AuditDetail: The audit detail object (typically AttributeAuditDetail)
    /// </summary>
    public class RetrieveAuditDetailsRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveAuditDetailsRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var retrieveRequest = (RetrieveAuditDetailsRequest)request;

            if (retrieveRequest.AuditId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidArgument,
                    "AuditId cannot be empty");
            }

            var auditRepository = ctx.GetProperty<IAuditRepository>();
            
            if (auditRepository == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidOperation,
                    "Audit repository not initialized");
            }

            var auditDetail = auditRepository.GetAuditDetails(retrieveRequest.AuditId);

            if (auditDetail == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.ObjectDoesNotExist,
                    $"Audit record with ID {retrieveRequest.AuditId} not found");
            }

            var response = new RetrieveAuditDetailsResponse();
            response.Results["AuditDetail"] = auditDetail;

            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveAuditDetailsRequest);
        }
    }
}
