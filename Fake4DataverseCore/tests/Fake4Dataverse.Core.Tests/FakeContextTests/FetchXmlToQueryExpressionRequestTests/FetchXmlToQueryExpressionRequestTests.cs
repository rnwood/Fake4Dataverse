using Crm;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.FetchXmlToQueryExpressionRequestTests
{
    public class FetchXmlToQueryExpressionRequestTests: Fake4DataverseTests
    {
        [Fact]
        public void Should_convert_fetchxml_query_into_queryexpression()
        {
            _context.EnableProxyTypes(Assembly.GetExecutingAssembly());
            var request = new FetchXmlToQueryExpressionRequest
            {
                FetchXml = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                            "   <entity name='activitypointer'>" +
                            "        <attribute name='activitytypecode' />" +
                            "        <attribute name='subject' />" +
                            "        <attribute name='description' />" +
                            "        <attribute name='regardingobjectid' />" +
                            "        <attribute name='activityid' />" +
                            "        <attribute name='ownerid' />" +
                            "        <attribute name='scheduledend' />" +
                            "        <attribute name='statecode' />" +
                            "    <order attribute='modifiedon' descending='false' />" +
                            "  </entity>" +
                            "</fetch>"
            };

            var response = _service.Execute(request) as FetchXmlToQueryExpressionResponse;
            Assert.NotNull(response.Query);
            Assert.Equal("activitypointer", response.Query.EntityName);
        }
    }
}
