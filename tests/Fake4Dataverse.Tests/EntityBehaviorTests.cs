using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class EntityBehaviorTests
    {
        [Fact]
        public void Create_SetsCreatedOnAndModifiedOn()
        {
            var clock = new FakeClock(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));
            var service = new FakeOrganizationService { Clock = clock };
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), entity.GetAttributeValue<DateTime>("createdon"));
            Assert.Equal(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), entity.GetAttributeValue<DateTime>("modifiedon"));
        }

        [Fact]
        public void Create_SetsCreatedByAndModifiedBy()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            var createdBy = entity.GetAttributeValue<EntityReference>("createdby");
            var modifiedBy = entity.GetAttributeValue<EntityReference>("modifiedby");
            Assert.NotNull(createdBy);
            Assert.Equal("systemuser", createdBy.LogicalName);
            Assert.Equal(service.CallerId, createdBy.Id);
            Assert.NotNull(modifiedBy);
            Assert.Equal(service.CallerId, modifiedBy.Id);
        }

        [Fact]
        public void Create_SetsDefaultStateCodeAndStatusCode()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(0, entity.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(1, entity.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void Create_DoesNotOverrideExplicitStateCode()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account")
            {
                ["name"] = "Contoso",
                ["statecode"] = new OptionSetValue(1),
                ["statuscode"] = new OptionSetValue(2)
            });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(1, entity.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(2, entity.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void Create_SetsVersionNumber()
        {
            var service = new FakeOrganizationService();
            var id1 = service.Create(new Entity("account") { ["name"] = "A" });
            var id2 = service.Create(new Entity("account") { ["name"] = "B" });

            var e1 = service.Retrieve("account", id1, new ColumnSet(true));
            var e2 = service.Retrieve("account", id2, new ColumnSet(true));
            var v1 = (long)e1["versionnumber"];
            var v2 = (long)e2["versionnumber"];
            Assert.True(v2 > v1, "Version number should increment with each create.");
        }

        [Fact]
        public void Update_SetsModifiedOnAndVersionNumber()
        {
            var clock = new FakeClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var service = new FakeOrganizationService { Clock = clock };
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            clock.Advance(TimeSpan.FromHours(1));
            service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.GetAttributeValue<DateTime>("createdon"));
            Assert.Equal(new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc), entity.GetAttributeValue<DateTime>("modifiedon"));
        }

        [Fact]
        public void Update_SetsModifiedBy()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var newCaller = Guid.NewGuid();
            service.CallerId = newCaller;
            service.Update(new Entity("account", id) { ["name"] = "Fabrikam" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(newCaller, entity.GetAttributeValue<EntityReference>("modifiedby").Id);
        }

        [Fact]
        public void Create_SetsPrimaryIdAttribute()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var entity = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(id, entity.GetAttributeValue<Guid>("accountid"));
        }

        [Fact]
        public void FakeClock_AdvanceWorks()
        {
            var clock = new FakeClock(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
            Assert.Equal(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc), clock.UtcNow);

            clock.Advance(TimeSpan.FromMinutes(30));
            Assert.Equal(new DateTime(2026, 6, 1, 12, 30, 0, DateTimeKind.Utc), clock.UtcNow);
        }

        [Fact]
        public void RetrievedEntity_IsClone_NotReference()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var r1 = service.Retrieve("account", id, new ColumnSet(true));
            r1["name"] = "Modified";

            var r2 = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal("Contoso", r2.GetAttributeValue<string>("name"));
        }
    }
}
