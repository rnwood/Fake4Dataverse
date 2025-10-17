using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.QueryTranslationTests
{
    public class ProjectionTests : Fake4DataverseTests
    {
        private readonly Account _account;

        public ProjectionTests()
        {

            _account = new Account()
            {
                Id = Guid.NewGuid(),
                Name = "Some name"
            };
        }

        [Fact]
        public void Should_return_primary_key_attribute_even_if_not_specified_in_column_set()
        {
            base._context.Initialize(_account);
            var account = base._service.Retrieve(Account.EntityLogicalName, _account.Id, new ColumnSet(new string[] { "name" }));
            Assert.True(account.Attributes.ContainsKey("accountid"));
        }

        [Fact]
        public void Should_return_primary_key_attribute_when_retrieving_using_all_columns()
        {
            base._context.Initialize(_account);
            var account = base._service.Retrieve(Account.EntityLogicalName, _account.Id, new ColumnSet(true));
            Assert.True(account.Attributes.ContainsKey("accountid"));
        }
    }
}

