using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class SeedFromCsvTests
    {
        [Fact]
        public void SeedFromCsv_BasicData_CreatesEntities()
        {
            var service = new FakeOrganizationService();
            var csv = "logicalName,name,telephone1\naccount,Contoso,555-1234\naccount,Fabrikam,555-5678";

            service.SeedFromCsv(csv);

            var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, result.Entities.Count);
        }

        [Fact]
        public void SeedFromCsv_WithIdColumn_SetsEntityId()
        {
            var service = new FakeOrganizationService();
            var id = Guid.NewGuid();
            var csv = $"logicalName,id,name\naccount,{id},Contoso";

            service.SeedFromCsv(csv);

            var entity = service.Retrieve("account", id, new ColumnSet("name"));
            Assert.Equal("Contoso", entity.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void SeedFromCsv_QuotedFields_HandlesCommasInValues()
        {
            var service = new FakeOrganizationService();
            var csv = "logicalName,name,description\naccount,\"Contoso, Ltd.\",\"A large company\"";

            service.SeedFromCsv(csv);

            var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Single(result.Entities);
            Assert.Equal("Contoso, Ltd.", result.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void SeedFromCsv_EmptyLines_SkipsBlankRows()
        {
            var service = new FakeOrganizationService();
            var csv = "logicalName,name\naccount,Contoso\n\naccount,Fabrikam\n";

            service.SeedFromCsv(csv);

            var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, result.Entities.Count);
        }

        [Fact]
        public void SeedFromCsv_NullInput_ThrowsArgumentNullException()
        {
            var service = new FakeOrganizationService();
            Assert.Throws<ArgumentNullException>(() => service.SeedFromCsv(null!));
        }

        [Fact]
        public void SeedFromCsv_HeaderOnly_NoError()
        {
            var service = new FakeOrganizationService();
            Assert.Throws<ArgumentException>(() => service.SeedFromCsv("logicalName,name"));
        }

        [Fact]
        public void SeedFromCsv_MissingLogicalNameHeader_ThrowsArgumentException()
        {
            var service = new FakeOrganizationService();
            Assert.Throws<ArgumentException>(() => service.SeedFromCsv("name,phone\nContoso,555"));
        }

        [Fact]
        public void SeedFromCsv_EmptyFieldsSkipped()
        {
            var service = new FakeOrganizationService();
            var csv = "logicalName,name,phone\naccount,Contoso,\naccount,,555";

            service.SeedFromCsv(csv);

            var result = service.RetrieveMultiple(new QueryExpression("account") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, result.Entities.Count);

            // First record: name=Contoso, phone not set (empty)
            // Second record: name not set, phone=555
        }
    }
}
