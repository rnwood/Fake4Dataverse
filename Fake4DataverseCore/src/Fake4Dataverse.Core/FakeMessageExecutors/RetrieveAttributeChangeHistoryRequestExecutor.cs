using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executor for RetrieveAttributeChangeHistoryRequest
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveattributechangehistoryrequest
    /// 
    /// RetrieveAttributeChangeHistoryRequest retrieves the audit history for a specific attribute of a record.
    /// This returns a collection of audit records showing all changes to that specific attribute over time.
    /// The request requires:
    /// - Target: EntityReference to the record
    /// - AttributeLogicalName: The logical name of the attribute to retrieve history for
    /// 
    /// Optional parameters:
    /// - PagingInfo: PagingInfo for pagination support
    /// 
    /// The response contains:
    /// - AuditDetailCollection: Collection of audit detail records filtered to the specific attribute
    /// </summary>
    public class RetrieveAttributeChangeHistoryRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveAttributeChangeHistoryRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var historyRequest = (RetrieveAttributeChangeHistoryRequest)request;

            if (historyRequest.Target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidArgument,
                    "Target cannot be null");
            }

            if (string.IsNullOrEmpty(historyRequest.AttributeLogicalName))
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidArgument,
                    "AttributeLogicalName cannot be null or empty");
            }

            var auditRepository = ctx.GetProperty<IAuditRepository>();
            
            if (auditRepository == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidOperation,
                    "Audit repository not initialized");
            }

            var auditRecords = auditRepository.GetAuditRecordsForAttribute(
                historyRequest.Target, 
                historyRequest.AttributeLogicalName);

            // Create audit detail collection
            var auditDetailCollection = new AuditDetailCollection();
            
            foreach (var auditRecord in auditRecords)
            {
                var auditId = auditRecord.GetAttributeValue<Guid>("auditid");
                var detail = auditRepository.GetAuditDetails(auditId);
                
                if (detail != null)
                {
                    auditDetailCollection.AuditDetails.Add((AuditDetail)detail);
                }
            }

            var response = new RetrieveAttributeChangeHistoryResponse();
            response.Results["AuditDetailCollection"] = auditDetailCollection;

            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveAttributeChangeHistoryRequest);
        }
    }
}
