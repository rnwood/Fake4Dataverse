using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.AuditTests
{
    /// <summary>
    /// Tests for audit message executors
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/retrieve-audit-data
    /// </summary>
    public class AuditMessageExecutorTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_ExecuteRetrieveAuditDetailsRequest()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest
            // RetrieveAuditDetailsRequest retrieves the audit details for a specific audit record
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create and update an account
            var accountId = service.Create(new Entity("account") { ["name"] = "Original" });
            service.Update(new Entity("account", accountId) { ["name"] = "Updated" });

            // Get the update audit record
            var accountRef = new EntityReference("account", accountId);
            var auditRecords = auditRepository.GetAuditRecordsForEntity(accountRef).ToList();
            var updateAuditId = auditRecords.Last().GetAttributeValue<Guid>("auditid");

            // Act - Execute RetrieveAuditDetailsRequest
            var request = new RetrieveAuditDetailsRequest
            {
                AuditId = updateAuditId
            };

            var response = (RetrieveAuditDetailsResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.AuditDetail);
            
            // Verify it's an AttributeAuditDetail
            var auditDetail = response.AuditDetail;
            Assert.IsType<AttributeAuditDetail>(auditDetail);
        }

        [Fact]
        public void Should_ExecuteRetrieveRecordChangeHistoryRequest()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieverecordchangehistoryrequest
            // RetrieveRecordChangeHistoryRequest retrieves all audit changes for a record
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create and update an account multiple times
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            service.Update(new Entity("account", accountId) { ["name"] = "Test 1" });
            service.Update(new Entity("account", accountId) { ["name"] = "Test 2" });

            // Act - Execute RetrieveRecordChangeHistoryRequest
            var request = new RetrieveRecordChangeHistoryRequest
            {
                Target = new EntityReference("account", accountId)
            };

            var response = (RetrieveRecordChangeHistoryResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.AuditDetailCollection);
            Assert.Equal(3, response.AuditDetailCollection.AuditDetails.Count); // 1 create + 2 updates
        }

        [Fact]
        public void Should_ExecuteRetrieveAttributeChangeHistoryRequest()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveattributechangehistoryrequest
            // RetrieveAttributeChangeHistoryRequest retrieves audit history for a specific attribute
            // Use context from base class
            var context = _context;
            var service = _service;
            var auditRepository = context.GetProperty<IAuditRepository>();
            auditRepository.IsAuditEnabled = true;

            // Create account and update different attributes
            var accountId = service.Create(new Entity("account") 
            { 
                ["name"] = "Test",
                ["revenue"] = new Money(1000)
            });
            
            service.Update(new Entity("account", accountId) { ["name"] = "Updated Name" });
            service.Update(new Entity("account", accountId) { ["revenue"] = new Money(2000) });
            service.Update(new Entity("account", accountId) { ["name"] = "Final Name" });

            // Act - Execute RetrieveAttributeChangeHistoryRequest for "name" attribute
            var request = new RetrieveAttributeChangeHistoryRequest
            {
                Target = new EntityReference("account", accountId),
                AttributeLogicalName = "name"
            };

            var response = (RetrieveAttributeChangeHistoryResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.AuditDetailCollection);
            // Should have 3 audit records where name is present:
            // 1. Create (name was set)
            // 2. First Update (name changed)
            // 3. Second Update (name changed)
            // The revenue-only update should not be included
            Assert.Equal(3, response.AuditDetailCollection.AuditDetails.Count);
        }

        [Fact]
        public void Should_ThrowException_WhenRetrieveAuditDetailsRequest_HasInvalidAuditId()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;

            // Act & Assert
            var request = new RetrieveAuditDetailsRequest
            {
                AuditId = Guid.NewGuid() // Non-existent audit ID
            };

            Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(
                () => service.Execute(request));
        }

        [Fact]
        public void Should_ThrowException_WhenRetrieveRecordChangeHistoryRequest_HasNullTarget()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;

            // Act & Assert
            var request = new RetrieveRecordChangeHistoryRequest
            {
                Target = null
            };

            Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(
                () => service.Execute(request));
        }

        [Fact]
        public void Should_ThrowException_WhenRetrieveAttributeChangeHistoryRequest_HasEmptyAttributeName()
        {
            // Arrange
            // Use context from base class
            var context = _context;
            var service = _service;

            var accountId = Guid.NewGuid();

            // Act & Assert
            var request = new RetrieveAttributeChangeHistoryRequest
            {
                Target = new EntityReference("account", accountId),
                AttributeLogicalName = string.Empty
            };

            Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(
                () => service.Execute(request));
        }
    }
}
