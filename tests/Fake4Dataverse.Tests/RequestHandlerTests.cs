using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class RequestHandlerTests
    {
        [Fact]
        public void Merge_CopiesAttributesAndDeactivatesSubordinate()
        {
            var service = new FakeOrganizationService();
            var targetId = service.Create(new Entity("account") { ["name"] = "Target", ["city"] = "Seattle" });
            var subordinateId = service.Create(new Entity("account") { ["name"] = "Subordinate", ["revenue"] = new Money(500m) });

            var request = new OrganizationRequest("Merge");
            request["Target"] = new EntityReference("account", targetId);
            request["SubordinateId"] = subordinateId;
            request["UpdateContent"] = new Entity("account") { ["revenue"] = new Money(500m) };
            request["PerformParentingChecks"] = false;

            service.Execute(request);

            // Target should have merged attributes
            var target = service.Retrieve("account", targetId, new ColumnSet("revenue"));
            Assert.Equal(500m, target.GetAttributeValue<Money>("revenue").Value);

            // Subordinate should be deactivated
            var sub = service.Retrieve("account", subordinateId, new ColumnSet("statecode"));
            Assert.Equal(1, sub.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void UpsertMultiple_CreatesAndUpdatesRecords()
        {
            var service = new FakeOrganizationService();
            var existingId = service.Create(new Entity("account") { ["name"] = "Existing" });

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("account", existingId) { ["name"] = "Updated" });
            targets.Entities.Add(new Entity("account") { ["name"] = "New" });

            var request = new OrganizationRequest("UpsertMultiple");
            request["Targets"] = targets;

            var response = service.Execute(request);

            var existing = service.Retrieve("account", existingId, new ColumnSet("name"));
            Assert.Equal("Updated", existing.GetAttributeValue<string>("name"));

            var all = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, all.Entities.Count);
        }

        [Fact]
        public void BulkDelete_DeletesMatchingRecords()
        {
            var service = new FakeOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Delete1" });
            service.Create(new Entity("account") { ["name"] = "Delete2" });
            service.Create(new Entity("account") { ["name"] = "Keep" });

            var query = new QueryExpression("account");
            query.Criteria.AddCondition("name", ConditionOperator.BeginsWith, "Delete");

            var request = new OrganizationRequest("BulkDelete");
            request["QuerySet"] = new[] { query };
            request["JobName"] = "TestBulkDelete";
            request["StartDateTime"] = DateTime.UtcNow;
            request["RecurrencePattern"] = string.Empty;
            request["SendEmailNotification"] = false;
            request["ToRecipients"] = Array.Empty<Guid>();
            request["CCRecipients"] = Array.Empty<Guid>();

            var response = service.Execute(request);
            Assert.NotEqual(Guid.Empty, (Guid)response["JobId"]);

            var remaining = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Single(remaining.Entities);
            Assert.Equal("Keep", remaining.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void RetrieveCurrentOrganization_ReturnsOrgDetails()
        {
            var service = new FakeOrganizationService();

            var response = service.Execute(new OrganizationRequest("RetrieveCurrentOrganization"));
            var detail = (Entity)response["Detail"];

            Assert.NotNull(detail);
            Assert.Equal(service.OrganizationId, detail.GetAttributeValue<Guid>("organizationid"));
        }

        [Fact]
        public void RetrieveOptionSet_ReturnsResponse()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("RetrieveOptionSet");
            request["Name"] = "account_category";
            request["MetadataId"] = Guid.Empty;

            var response = service.Execute(request);
            Assert.NotNull(response);
        }

        [Fact]
        public void InsertOptionValue_ReturnsNewValue()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("InsertOptionValue");
            request["OptionSetName"] = "account_category";
            request["Label"] = new Label("Test Option", 1033);
            request["Value"] = 999;

            var response = service.Execute(request);
            Assert.Equal(999, (int)response["NewOptionValue"]);
        }

        [Fact]
        public void InsertStatusValue_ReturnsNewValue()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("InsertStatusValue");
            request["EntityLogicalName"] = "incident";
            request["AttributeLogicalName"] = "statuscode";
            request["Label"] = new Label("Custom Status", 1033);
            request["Value"] = 100000;
            request["StateCode"] = 0;

            var response = service.Execute(request);
            Assert.Equal(100000, (int)response["NewOptionValue"]);
        }

        [Fact]
        public void DeleteFile_ExecutesWithoutError()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("DeleteFile");
            request["FileId"] = Guid.NewGuid();

            var response = service.Execute(request);
            Assert.NotNull(response);
        }

        [Fact]
        public void DownloadBlock_ReturnsEmptyData()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("DownloadBlock");
            request["FileContinuationToken"] = "token123";

            var response = service.Execute(request);
            Assert.NotNull(response["Data"]);
        }

        [Fact]
        public void QualifyLead_CreatesAccountContactOpportunity()
        {
            var service = new FakeOrganizationService();
            var leadId = service.Create(new Entity("lead")
            {
                ["firstname"] = "John",
                ["lastname"] = "Doe",
                ["companyname"] = "Contoso",
                ["subject"] = "Hot Lead"
            });

            var request = new OrganizationRequest("QualifyLead");
            request["LeadId"] = new EntityReference("lead", leadId);
            request["CreateAccount"] = true;
            request["CreateContact"] = true;
            request["CreateOpportunity"] = true;
            request["Status"] = new OptionSetValue(3);

            var response = service.Execute(request);
            var created = (EntityReferenceCollection)response["CreatedEntities"];

            Assert.Equal(3, created.Count);

            // Lead should be deactivated
            var lead = service.Retrieve("lead", leadId, new ColumnSet("statecode"));
            Assert.Equal(1, lead.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void CloseIncident_ResolvesCase()
        {
            var service = new FakeOrganizationService();
            var incidentId = service.Create(new Entity("incident") { ["title"] = "Test Case" });

            var resolution = new Entity("incidentresolution");
            resolution["incidentid"] = new EntityReference("incident", incidentId);
            resolution["subject"] = "Resolved";

            var request = new OrganizationRequest("CloseIncident");
            request["IncidentResolution"] = resolution;
            request["Status"] = new OptionSetValue(5);

            service.Execute(request);

            var incident = service.Retrieve("incident", incidentId, new ColumnSet("statecode", "statuscode"));
            Assert.Equal(1, incident.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(5, incident.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void WinOpportunity_ClosesAsWon()
        {
            var service = new FakeOrganizationService();
            var oppId = service.Create(new Entity("opportunity") { ["name"] = "Big Deal" });

            var oppClose = new Entity("opportunityclose");
            oppClose["opportunityid"] = new EntityReference("opportunity", oppId);
            oppClose["subject"] = "Won!";

            var request = new OrganizationRequest("WinOpportunity");
            request["OpportunityClose"] = oppClose;
            request["Status"] = new OptionSetValue(3);

            service.Execute(request);

            var opp = service.Retrieve("opportunity", oppId, new ColumnSet("statecode"));
            Assert.Equal(1, opp.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void LoseOpportunity_ClosesAsLost()
        {
            var service = new FakeOrganizationService();
            var oppId = service.Create(new Entity("opportunity") { ["name"] = "Lost Deal" });

            var oppClose = new Entity("opportunityclose");
            oppClose["opportunityid"] = new EntityReference("opportunity", oppId);
            oppClose["subject"] = "Lost";

            var request = new OrganizationRequest("LoseOpportunity");
            request["OpportunityClose"] = oppClose;
            request["Status"] = new OptionSetValue(4);

            service.Execute(request);

            var opp = service.Retrieve("opportunity", oppId, new ColumnSet("statecode"));
            Assert.Equal(2, opp.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void CloseQuote_ClosesQuote()
        {
            var service = new FakeOrganizationService();
            var quoteId = service.Create(new Entity("quote") { ["name"] = "Test Quote" });

            var quoteClose = new Entity("quoteclose");
            quoteClose["quoteid"] = new EntityReference("quote", quoteId);
            quoteClose["subject"] = "Closed";

            var request = new OrganizationRequest("CloseQuote");
            request["QuoteClose"] = quoteClose;
            request["Status"] = new OptionSetValue(5);

            service.Execute(request);

            var quote = service.Retrieve("quote", quoteId, new ColumnSet("statecode"));
            Assert.Equal(2, quote.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void ReviseQuote_CreatesNewDraftCopy()
        {
            var service = new FakeOrganizationService();
            var quoteId = service.Create(new Entity("quote") { ["name"] = "Original Quote", ["description"] = "Test" });

            var request = new OrganizationRequest("ReviseQuote");
            request["QuoteId"] = new EntityReference("quote", quoteId);
            request["ColumnSet"] = new ColumnSet(true);

            var response = service.Execute(request);
            var revised = (Entity)response["Entity"];

            Assert.NotEqual(quoteId, revised.Id);
            Assert.Equal("Original Quote", revised.GetAttributeValue<string>("name"));
            Assert.Equal(0, revised.GetAttributeValue<OptionSetValue>("statecode").Value); // Draft
        }

        [Fact]
        public void PublishXml_ExecutesWithoutError()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("PublishXml");
            request["ParameterXml"] = "<importexportxml></importexportxml>";

            var response = service.Execute(request);
            Assert.NotNull(response);
        }

        [Fact]
        public void AddToQueue_CreatesQueueItem()
        {
            var service = new FakeOrganizationService();
            var caseId = service.Create(new Entity("incident") { ["title"] = "Queue Case" });
            var queueId = service.Create(new Entity("queue") { ["name"] = "Support Queue" });

            var request = new OrganizationRequest("AddToQueue");
            request["Target"] = new EntityReference("incident", caseId);
            request["DestinationQueueId"] = queueId;

            var response = service.Execute(request);
            var queueItemId = (Guid)response["QueueItemId"];
            Assert.NotEqual(Guid.Empty, queueItemId);

            // Verify queue item exists
            var queueItem = service.Retrieve("queueitem", queueItemId, new ColumnSet(true));
            Assert.Equal(caseId, queueItem.GetAttributeValue<EntityReference>("objectid").Id);
        }

        [Fact]
        public void RemoveFromQueue_DeletesQueueItem()
        {
            var service = new FakeOrganizationService();
            var caseId = service.Create(new Entity("incident") { ["title"] = "Queue Case" });
            var queueId = service.Create(new Entity("queue") { ["name"] = "Support Queue" });

            // Add to queue first
            var addRequest = new OrganizationRequest("AddToQueue");
            addRequest["Target"] = new EntityReference("incident", caseId);
            addRequest["DestinationQueueId"] = queueId;
            var addResponse = service.Execute(addRequest);
            var queueItemId = (Guid)addResponse["QueueItemId"];

            // Remove from queue
            var removeRequest = new OrganizationRequest("RemoveFromQueue");
            removeRequest["QueueItemId"] = queueItemId;
            service.Execute(removeRequest);

            // Verify removed
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve("queueitem", queueItemId, new ColumnSet(true)));
        }

        [Fact]
        public void InstantiateTemplate_CreatesEmailFromTemplate()
        {
            var service = new FakeOrganizationService();
            var templateId = service.Create(new Entity("template") { ["subject"] = "Welcome {!contact:fullname;}", ["body"] = "Hello!" });
            var contactId = service.Create(new Entity("contact") { ["fullname"] = "John Doe" });

            var request = new OrganizationRequest("InstantiateTemplate");
            request["TemplateId"] = templateId;
            request["ObjectId"] = contactId;
            request["ObjectType"] = "contact";

            var response = service.Execute(request);
            var collection = (EntityCollection)response["EntityCollection"];
            Assert.Single(collection.Entities);
            Assert.NotNull(collection.Entities[0].GetAttributeValue<string>("subject"));
        }

        [Fact]
        public void SendEmailFromTemplate_CreatesEmail()
        {
            var service = new FakeOrganizationService();
            var templateId = service.Create(new Entity("template") { ["subject"] = "Welcome" });
            var contactId = service.Create(new Entity("contact") { ["fullname"] = "Test" });

            var request = new OrganizationRequest("SendEmailFromTemplate");
            request["TemplateId"] = templateId;
            request["RegardingId"] = contactId;
            request["RegardingType"] = "contact";

            var response = service.Execute(request);
            var emailId = (Guid)response["Id"];
            Assert.NotEqual(Guid.Empty, emailId);
        }

        [Fact]
        public void SendFax_ExecutesWithoutError()
        {
            var service = new FakeOrganizationService();
            var response = service.Execute(new OrganizationRequest("SendFax"));
            Assert.NotNull(response);
        }

        [Fact]
        public void ExportPdfDocument_ReturnsPdfBytes()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("ExportPdfDocument");
            request["EntityTypeCode"] = 1;
            request["SelectedRecords"] = "[]";

            var response = service.Execute(request);
            Assert.NotNull(response["PdfFile"]);
        }
    }
}
