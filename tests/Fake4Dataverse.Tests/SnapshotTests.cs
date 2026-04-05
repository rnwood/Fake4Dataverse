using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class SnapshotTests
    {
        [Fact]
        public void TakeSnapshot_RestoreSnapshot_RestoresEntities()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });

            var snapshot = service.TakeSnapshot();
            service.Update(new Entity("account", id) { ["name"] = "Modified" });

            service.RestoreSnapshot(snapshot);

            var retrieved = service.Retrieve("account", id, new ColumnSet("name"));
            Assert.Equal("Contoso", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void RestoreSnapshot_RemovesEntitiesAddedAfterSnapshot()
        {
            var service = new FakeOrganizationService();
            var snapshot = service.TakeSnapshot();

            service.Create(new Entity("account") { ["name"] = "Contoso" });

            service.RestoreSnapshot(snapshot);

            var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Empty(result.Entities);
        }

        [Fact]
        public void RestoreSnapshot_RestoresDeletedEntities()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            var snapshot = service.TakeSnapshot();

            service.Delete("account", id);

            service.RestoreSnapshot(snapshot);

            var retrieved = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal("Contoso", retrieved.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Scope_AutoRestoresOnDispose()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Original" });

            using (service.Scope())
            {
                service.Update(new Entity("account", id) { ["name"] = "Modified" });
                var modified = service.Retrieve("account", id, new ColumnSet("name"));
                Assert.Equal("Modified", modified.GetAttributeValue<string>("name"));
            }

            var restored = service.Retrieve("account", id, new ColumnSet("name"));
            Assert.Equal("Original", restored.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Scope_RemovesCreatedEntitiesOnDispose()
        {
            var service = new FakeOrganizationService();

            using (service.Scope())
            {
                service.Create(new Entity("account") { ["name"] = "Temporary" });
                var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
                Assert.Single(result.Entities);
            }

            var afterScope = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Empty(afterScope.Entities);
        }

        [Fact]
        public void MultipleSnapshots_IndependentRestore()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "V1" });

            var snapshot1 = service.TakeSnapshot();
            service.Update(new Entity("account", id) { ["name"] = "V2" });

            var snapshot2 = service.TakeSnapshot();
            service.Update(new Entity("account", id) { ["name"] = "V3" });

            service.RestoreSnapshot(snapshot2);
            Assert.Equal("V2", service.Retrieve("account", id, new ColumnSet("name")).GetAttributeValue<string>("name"));

            service.RestoreSnapshot(snapshot1);
            Assert.Equal("V1", service.Retrieve("account", id, new ColumnSet("name")).GetAttributeValue<string>("name"));
        }

        [Fact]
        public void Snapshot_RestoresBinaryData()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.SetBinaryAttribute("account", id, "logo", new byte[] { 1, 2, 3 });

            var snapshot = service.TakeSnapshot();
            service.SetBinaryAttribute("account", id, "logo", new byte[] { 4, 5, 6 });

            service.RestoreSnapshot(snapshot);

            var data = service.GetBinaryAttribute("account", id, "logo");
            Assert.NotNull(data);
            Assert.Equal(new byte[] { 1, 2, 3 }, data);
        }

        [Fact]
        public void RestoreSnapshot_ThrowsForInvalidSnapshot()
        {
            var service = new FakeOrganizationService();
            Assert.Throws<ArgumentException>(() => service.RestoreSnapshot("not a snapshot"));
        }

        [Fact]
        public void RestoreSnapshot_ThrowsForNull()
        {
            var service = new FakeOrganizationService();
            Assert.Throws<ArgumentNullException>(() => service.RestoreSnapshot(null!));
        }

        [Fact]
        public void NestedScopes_RestoreCorrectly()
        {
            var service = new FakeOrganizationService();
            var id = service.Create(new Entity("account") { ["name"] = "Original" });

            using (service.Scope())
            {
                service.Update(new Entity("account", id) { ["name"] = "Level1" });

                using (service.Scope())
                {
                    service.Update(new Entity("account", id) { ["name"] = "Level2" });
                    Assert.Equal("Level2", service.Retrieve("account", id, new ColumnSet("name")).GetAttributeValue<string>("name"));
                }

                Assert.Equal("Level1", service.Retrieve("account", id, new ColumnSet("name")).GetAttributeValue<string>("name"));
            }

            Assert.Equal("Original", service.Retrieve("account", id, new ColumnSet("name")).GetAttributeValue<string>("name"));
        }
    }
}
