using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class FormattedValuesTests
    {
        [Fact]
        public void Retrieve_OptionSetValue_HasFormattedValue()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var id = service.Create(new Entity("account") { ["industrycode"] = new OptionSetValue(3) });

            var retrieved = service.Retrieve("account", id, new ColumnSet("industrycode"));

            Assert.True(retrieved.FormattedValues.ContainsKey("industrycode"));
            Assert.Equal("3", retrieved.FormattedValues["industrycode"]);
        }

        [Fact]
        public void Retrieve_MoneyValue_HasFormattedValue()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var id = service.Create(new Entity("account") { ["revenue"] = new Money(1234567.89m) });

            var retrieved = service.Retrieve("account", id, new ColumnSet("revenue"));

            Assert.True(retrieved.FormattedValues.ContainsKey("revenue"));
            Assert.Equal("1,234,567.89", retrieved.FormattedValues["revenue"]);
        }

        [Fact]
        public void Retrieve_BooleanValue_HasFormattedValue()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var id = service.Create(new Entity("account") { ["donotphone"] = true });

            var retrieved = service.Retrieve("account", id, new ColumnSet("donotphone"));

            Assert.True(retrieved.FormattedValues.ContainsKey("donotphone"));
            Assert.Equal("Yes", retrieved.FormattedValues["donotphone"]);
        }

        [Fact]
        public void RetrieveMultiple_FormattedValues_PopulatedOnResults()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            service.Create(new Entity("account") { ["industrycode"] = new OptionSetValue(1), ["revenue"] = new Money(100m) });

            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("industrycode", "revenue") };
            var result = service.RetrieveMultiple(query);

            Assert.True(result.Entities[0].FormattedValues.ContainsKey("industrycode"));
            Assert.True(result.Entities[0].FormattedValues.ContainsKey("revenue"));
        }

        [Fact]
        public void Retrieve_ExistingFormattedValue_NotOverridden()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var entity = new Entity("account") { ["industrycode"] = new OptionSetValue(3) };
            entity.FormattedValues["industrycode"] = "Manufacturing";
            var id = service.Create(entity);

            var retrieved = service.Retrieve("account", id, new ColumnSet("industrycode"));

            Assert.Equal("Manufacturing", retrieved.FormattedValues["industrycode"]);
        }

        [Fact]
        public void Money_CRUD_WorksCorrectly()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var id = service.Create(new Entity("account") { ["revenue"] = new Money(1000m) });

            // Retrieve
            var retrieved = service.Retrieve("account", id, new ColumnSet("revenue"));
            Assert.Equal(1000m, retrieved.GetAttributeValue<Money>("revenue").Value);

            // Update
            service.Update(new Entity("account", id) { ["revenue"] = new Money(2000m) });
            retrieved = service.Retrieve("account", id, new ColumnSet("revenue"));
            Assert.Equal(2000m, retrieved.GetAttributeValue<Money>("revenue").Value);

            // Query
            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("revenue") };
            query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 1500m);
            var result = service.RetrieveMultiple(query);
            Assert.Single(result.Entities);
        }
    }
}
