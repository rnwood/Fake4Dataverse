using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.QueryLookupTests
{
    public class Tests : Fake4DataverseTests
    {                [Fact]
        public void When_a_query_on_lookup_is_executed_with_a_guid_right_result_is_returned()
        {
            var user = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "User" };
            var user2 = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "Other user!" };
            var account = new Account() { Id = Guid.NewGuid(), Name = "Test", ["createdby"] = user.ToEntityReference() };
            var account2 = new Account() { Id = Guid.NewGuid(), Name = "Other account!", ["createdby"] = user2.ToEntityReference() };
            _context.Initialize(new List<Entity>()
            {
                user, user2, account, account2
            });

            QueryExpression query = new QueryExpression();
            query.ColumnSet = new ColumnSet(new string[] { "name" });
            query.EntityName = Account.EntityLogicalName;
            query.Criteria = new FilterExpression { Conditions = { new ConditionExpression("createdby", ConditionOperator.Equal, user.Id) } };

            //Execute using a request to test the OOB (XRM) message contracts
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;
            Collection<Entity> entityList = ((RetrieveMultipleResponse)_service.Execute(request)).EntityCollection.Entities;

            Assert.True(entityList.Count == 1);
            Assert.Equal("Test", entityList[0]["name"].ToString());
        }

        [Fact]
        public void When_a_query_on_lookup_is_executed_with_a_guid_as_string_right_result_is_returned()
        {
            var user = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "User" };
            var user2 = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "Other user!" };
            var account = new Account() { Id = Guid.NewGuid(), Name = "Test", ["createdby"] = user.ToEntityReference() };
            var account2 = new Account() { Id = Guid.NewGuid(), Name = "Other account!", ["createdby"] = user2.ToEntityReference() };
            _context.Initialize(new List<Entity>()
            {
                user, user2, account, account2
            });

            QueryExpression query = new QueryExpression();
            query.ColumnSet = new ColumnSet(new string[] { "name" });
            query.EntityName = Account.EntityLogicalName;
            query.Criteria = new FilterExpression { Conditions = { new ConditionExpression("createdby", ConditionOperator.Equal, user.Id.ToString("N").ToUpperInvariant()) } };

            //Execute using a request to test the OOB (XRM) message contracts
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;
            Collection<Entity> entityList = ((RetrieveMultipleResponse)_service.Execute(request)).EntityCollection.Entities;

            Assert.True(entityList.Count == 1);
            Assert.Equal("Test", entityList[0]["name"].ToString());
        }

        [Fact]
        public void When_a_query_on_lookup_is_executed_with_name_suffixed_right_result_is_returned()
        {
            var user = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "User" };
            var user2 = new SystemUser { Id = Guid.NewGuid(), ["fullname"] = "Other user!" };
            var account = new Account() { Id = Guid.NewGuid(), Name = "Test", ["createdby"] = new EntityReference(user.LogicalName, user.Id) { Name = user.FullName } };
            var account2 = new Account() { Id = Guid.NewGuid(), Name = "Other account!", ["createdby"] = new EntityReference(user.LogicalName, user2.Id) { Name = user2.FullName } };
            
            _context.Initialize(new List<Entity>()
            {
                user, user2, account, account2
            });

            QueryExpression query = new QueryExpression();
            query.ColumnSet = new ColumnSet(new string[] { "name" });
            query.EntityName = Account.EntityLogicalName;
            query.Criteria = new FilterExpression { Conditions = { new ConditionExpression("createdbyname", ConditionOperator.Equal, "User") } };

            //Execute using a request to test the OOB (XRM) message contracts
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;
            Collection<Entity> entityList = ((RetrieveMultipleResponse)_service.Execute(request)).EntityCollection.Entities;

            Assert.True(entityList.Count == 1);
            Assert.Equal("Test", entityList[0]["name"].ToString());
        }
    }
}