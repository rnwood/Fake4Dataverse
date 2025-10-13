using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executor for RetrieveRecordChangeHistoryRequest
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieverecordchangehistoryrequest
    /// 
    /// RetrieveRecordChangeHistoryRequest retrieves the audit history for a specific record.
    /// This returns a collection of audit records showing all changes made to the record over time.
    /// The request requires:
    /// - Target: EntityReference to the record to retrieve audit history for
    /// 
    /// Optional parameters:
    /// - PagingInfo: PagingInfo for pagination support
    /// 
    /// The response contains:
    /// - AuditDetailCollection: Collection of audit detail records
    /// </summary>
    public class RetrieveRecordChangeHistoryRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveRecordChangeHistoryRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var historyRequest = (RetrieveRecordChangeHistoryRequest)request;

            if (historyRequest.Target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidArgument,
                    "Target cannot be null");
            }

            var auditRepository = ctx.GetProperty<IAuditRepository>();
            
            if (auditRepository == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(ErrorCodes.InvalidOperation,
                    "Audit repository not initialized");
            }

            var auditRecords = auditRepository.GetAuditRecordsForEntity(historyRequest.Target);

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

            var response = new RetrieveRecordChangeHistoryResponse();
            response.Results["AuditDetailCollection"] = auditDetailCollection;

            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveRecordChangeHistoryRequest);
        }
    }
}
