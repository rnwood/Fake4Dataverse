using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class SalesAndQueueRequestTests
    {
        [Fact]
        public void QualifyLead_CreatesAccountContactOpportunity()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
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

            var lead = service.Retrieve("lead", leadId, new ColumnSet("statecode"));
            Assert.Equal(1, lead.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void CloseIncident_ResolvesCase()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
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
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
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
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
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
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
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
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var quoteId = service.Create(new Entity("quote") { ["name"] = "Original Quote", ["description"] = "Test" });

            var request = new OrganizationRequest("ReviseQuote");
            request["QuoteId"] = new EntityReference("quote", quoteId);
            request["ColumnSet"] = new ColumnSet(true);

            var response = service.Execute(request);
            var revised = (Entity)response["Entity"];

            Assert.NotEqual(quoteId, revised.Id);
            Assert.Equal("Original Quote", revised.GetAttributeValue<string>("name"));
            Assert.Equal(0, revised.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void AddToQueue_CreatesQueueItem()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var caseId = service.Create(new Entity("incident") { ["title"] = "Queue Case" });
            var queueId = service.Create(new Entity("queue") { ["name"] = "Support Queue" });

            var request = new OrganizationRequest("AddToQueue");
            request["Target"] = new EntityReference("incident", caseId);
            request["DestinationQueueId"] = queueId;

            var response = service.Execute(request);
            var queueItemId = (Guid)response["QueueItemId"];
            Assert.NotEqual(Guid.Empty, queueItemId);

            var queueItem = service.Retrieve("queueitem", queueItemId, new ColumnSet(true));
            Assert.Equal(caseId, queueItem.GetAttributeValue<EntityReference>("objectid").Id);
        }

        [Fact]
        public void RemoveFromQueue_DeletesQueueItem()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var caseId = service.Create(new Entity("incident") { ["title"] = "Queue Case" });
            var queueId = service.Create(new Entity("queue") { ["name"] = "Support Queue" });

            var addRequest = new OrganizationRequest("AddToQueue");
            addRequest["Target"] = new EntityReference("incident", caseId);
            addRequest["DestinationQueueId"] = queueId;
            var addResponse = service.Execute(addRequest);
            var queueItemId = (Guid)addResponse["QueueItemId"];

            var removeRequest = new OrganizationRequest("RemoveFromQueue");
            removeRequest["QueueItemId"] = queueItemId;
            service.Execute(removeRequest);

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve("queueitem", queueItemId, new ColumnSet(true)));
        }
    }
}
