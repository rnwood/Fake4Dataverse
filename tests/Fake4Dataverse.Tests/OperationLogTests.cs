using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class OperationLogTests
    {
        [Fact]
        public void Create_IsLogged()
        {
            var service = new FakeOrganizationService();
            var entity = new Entity("account") { ["name"] = "Contoso" };

            var id = service.Create(entity);

            Assert.True(service.OperationLog.HasCreated("account"));
            Assert.True(service.OperationLog.HasCreated("account", id));
            Assert.False(service.OperationLog.HasCreated("contact"));
        }

        [Fact]
        public void Update_IsLogged()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.OperationLog.Clear();

            service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

            Assert.True(service.OperationLog.HasUpdated("account", id));
        }

        [Fact]
        public void Delete_IsLogged()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.OperationLog.Clear();

            service.Delete("account", id);

            Assert.True(service.OperationLog.HasDeleted("account", id));
        }

        [Fact]
        public void Retrieve_IsLogged()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.OperationLog.Clear();

            service.Retrieve("account", id, new ColumnSet(true));

            var ops = service.OperationLog.GetOperations("Retrieve");
            Assert.Single(ops);
            Assert.Equal("account", ops[0].EntityName);
            Assert.Equal(id, ops[0].EntityId);
        }

        [Fact]
        public void RetrieveMultiple_IsLogged()
        {
            var service = new FakeOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.OperationLog.Clear();

            service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });

            var ops = service.OperationLog.GetOperations("RetrieveMultiple");
            Assert.Single(ops);
            Assert.Equal("account", ops[0].EntityName);
        }

        [Fact]
        public void Execute_IsLogged_ByType()
        {
            var service = new FakeOrganizationService();

            service.Execute(new WhoAmIRequest());

            Assert.True(service.OperationLog.HasExecuted<WhoAmIRequest>());
            Assert.False(service.OperationLog.HasExecuted<AssignRequest>());
        }

        [Fact]
        public void Execute_IsLogged_ByName()
        {
            var service = new FakeOrganizationService();

            service.Execute(new WhoAmIRequest());

            Assert.True(service.OperationLog.HasExecuted("WhoAmI"));
            Assert.False(service.OperationLog.HasExecuted("Assign"));
        }

        [Fact]
        public void Associate_IsLogged()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var contactId = service.Create(new Entity("contact") { ["lastname"] = "Doe" });
            service.OperationLog.Clear();

            service.Associate("account", id, new Relationship("account_contacts"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            var ops = service.OperationLog.GetOperations("Associate");
            Assert.Single(ops);
            Assert.Equal("account", ops[0].EntityName);
            Assert.Equal(id, ops[0].EntityId);
        }

        [Fact]
        public void Disassociate_IsLogged()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var contactId = service.Create(new Entity("contact") { ["lastname"] = "Doe" });
            service.Associate("account", id, new Relationship("account_contacts"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });
            service.OperationLog.Clear();

            service.Disassociate("account", id, new Relationship("account_contacts"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            var ops = service.OperationLog.GetOperations("Disassociate");
            Assert.Single(ops);
        }

        [Fact]
        public void Clear_RemovesAllRecords()
        {
            var service = new FakeOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Contoso" });
            Assert.NotEmpty(service.OperationLog.Records);

            service.OperationLog.Clear();

            Assert.Empty(service.OperationLog.Records);
        }

        [Fact]
        public void GetOperations_FiltersByTypeAndEntity()
        {
            var service = new FakeOrganizationService();
            service.Create(new Entity("account") { ["name"] = "A" });
            service.Create(new Entity("contact") { ["lastname"] = "B" });
            service.Create(new Entity("account") { ["name"] = "C" });

            var accountCreates = service.OperationLog.GetOperations("Create", "account");
            Assert.Equal(2, accountCreates.Count);

            var contactCreates = service.OperationLog.GetOperations("Create", "contact");
            Assert.Single(contactCreates);
        }

        [Fact]
        public void Create_RecordContainsEntitySnapshot()
        {
            var service = new FakeOrganizationService();
            var entity = new Entity("account") { ["name"] = "Contoso" };

            service.Create(entity);

            var record = service.OperationLog.Records[0];
            Assert.NotNull(record.Entity);
            Assert.Equal("Contoso", record.Entity!.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Records_HaveTimestamps()
        {
            var clock = new FakeClock(new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc));
            var service = new FakeOrganizationService { Clock = clock };

            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.Equal(clock.UtcNow, service.OperationLog.Records[0].Timestamp);
        }

        [Fact]
        public void Execute_RecordContainsRequest()
        {
            var service = new FakeOrganizationService();
            var request = new WhoAmIRequest();

            service.Execute(request);

            var ops = service.OperationLog.GetOperations("Execute");
            Assert.Single(ops);
            Assert.Same(request, ops[0].Request);
        }

        [Fact]
        public void HasCreated_IsCaseInsensitive()
        {
            var service = new FakeOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            Assert.True(service.OperationLog.HasCreated("Account"));
            Assert.True(service.OperationLog.HasCreated("ACCOUNT"));
        }
    }
}
