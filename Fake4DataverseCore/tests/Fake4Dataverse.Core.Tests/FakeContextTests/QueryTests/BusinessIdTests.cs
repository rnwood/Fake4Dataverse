using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.QueryTests
{
    public class EqualBusinessIdTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public EqualBusinessIdTests()
        {
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }

        [Fact]
        public void FetchXml_Operator_EqualBusinessId_Translation()
        {
            string _fetchXml =
            @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                <entity name='resource'>
                    <attribute name='name'/>
                    <attribute name='isdisables'/>
                    <filter type = 'and'>
                        <condition attribute='businessunitid' operator='eq-businessid' />
                    </filter>
                </entity>
            </fetch>";

            QueryExpression _query = _fetchXml.ToQueryExpression(_context);

            Assert.True(_query != null);
            Assert.Single(_query.Criteria.Conditions);
            Assert.Equal("businessunitid", _query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.EqualBusinessId, _query.Criteria.Conditions[0].Operator);
        }

        [Fact]
        public void FetchXml_Operator_EqualBusinessId_Execution()
        {
            string _fetchXml =
            @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                <entity name='resource'>
                    <attribute name='name'/>
                    <filter type = 'and'>
                        <condition attribute='businessunitid' operator='eq-businessid' />
                    </filter>
                </entity>
            </fetch>";

            string _resource1Id = "8AE5C98B-46E6-41B8-8326-279B29951A73";
            string _resource2Id = "DF328B18-45D9-4C30-B86A-A54D5CA2F04D";
            string _business1Id = "262DAFB9-9FC9-4006-9881-A680120B40C3";
            string _business2Id = "8BFC5749-8630-4CA0-A80F-9BDADE5F2BDB";

            List<Resource> _entities = new List<Resource>()
            {
                new Resource() { Id = Guid.Parse(_resource1Id), BusinessUnitId = new EntityReference("resource", Guid.Parse(_business1Id)) },
                new Resource() { Id = Guid.Parse(_resource2Id), BusinessUnitId = new EntityReference("resource", Guid.Parse(_business2Id)) }
            };

            _context.CallerProperties.BusinessUnitId = new EntityReference("businessunit", Guid.Parse(_business2Id));

            _context.Initialize(_entities);

            EntityCollection _collection = _service.RetrieveMultiple(new FetchExpression(_fetchXml));

            Assert.NotNull(_collection);
            Assert.Single(_collection.Entities);
            Assert.Equal(Guid.Parse(_resource2Id), _collection.Entities[0].Id);
        }

    }
}
