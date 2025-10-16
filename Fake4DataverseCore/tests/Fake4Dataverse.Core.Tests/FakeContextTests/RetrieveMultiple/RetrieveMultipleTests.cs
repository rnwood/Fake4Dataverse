using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.RetrieveMultiple
{
    public class RetrieveMultipleTests : Fake4DataverseTests
    {        public RetrieveMultipleTests()
        {
            // Use context from base class (validation disabled)
        }

        /// <summary>
        /// Tests that paging works correctly
        /// </summary>
        [Fact]
        public void TestPaging()
        {
            List<Entity> initialEntities = new List<Entity>();
            int excessNumberOfRecords = 50;

            (_context as XrmFakedContext).MaxRetrieveCount = 1000;
            for (int i = 0; i < (_context as XrmFakedContext).MaxRetrieveCount + excessNumberOfRecords; i++)
            {
                Entity e = new Entity("testentity");
                e.Id = Guid.NewGuid();
                initialEntities.Add(e);
            }
            _context.Initialize(initialEntities);

            List<Entity> allRecords = new List<Entity>();
            QueryExpression query = new QueryExpression("testentity");
            EntityCollection result = _service.RetrieveMultiple(query);
            allRecords.AddRange(result.Entities);
            Assert.Equal((_context as XrmFakedContext).MaxRetrieveCount, result.Entities.Count);
            Assert.True(result.MoreRecords);
            Assert.NotNull(result.PagingCookie);

            query.PageInfo = new PagingInfo()
            {
                PagingCookie = result.PagingCookie,
                PageNumber = 2,
            };
            result = _service.RetrieveMultiple(query);
            allRecords.AddRange(result.Entities);
            Assert.Equal(excessNumberOfRecords, result.Entities.Count);
            Assert.False(result.MoreRecords);

            foreach (Entity e in initialEntities)
            {
                Assert.True(allRecords.Any(r => r.Id == e.Id));
            }
        }

        /// <summary>
        /// Tests that paging works correctly
        /// </summary>
        [Fact]
        public void TestDistinct()
        {
            Entity e1 = new Entity("testentity");
            e1.Id = Guid.NewGuid();
            e1["name"] = "Fake4Dataverse";

            Entity e2 = new Entity("testentity");
            e2.Id = Guid.NewGuid();
            e2["name"] = "Fake4Dataverse";

            _context.Initialize(new Entity[] { e1, e2 });

            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' returntotalrecordcount='true'>
                              <entity name='testentity'>
                                    <attribute name='name' />                                    
                              </entity>
                            </fetch>";
            var query = new FetchExpression(fetchXml);
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(1, result.Entities.Count);
            Assert.False(result.MoreRecords);
        }

        /// <summary>
        /// Tests that top count works correctly
        /// </summary>
        [Fact]
        public void TestTop()
        {
            List<Entity> initialEntities = new List<Entity>();

            for (int i = 0; i < 10; i++)
            {
                Entity e = new Entity("testentity");
                e.Id = Guid.NewGuid();
                initialEntities.Add(e);
            }
            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.TopCount = 5;
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(query.TopCount, result.Entities.Count);
            Assert.False(result.MoreRecords);
        }

        /// <summary>
        /// Tests that an empty result set doesn't cause an error and that more records is correctly set to false
        /// </summary>
        [Fact]
        public void TestEmptyResultSet()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity e = new Entity("testentity");
            e.Id = Guid.NewGuid();
            e["retrieve"] = false;
            initialEntities.Add(e);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.Criteria.AddCondition("retrieve", ConditionOperator.Equal, true);
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(0, result.Entities.Count);
            Assert.False(result.MoreRecords);
        }

        /// <summary>
        /// Tests that a query with a filter on a link entity works the second time it is used (this was due to a shallow copy issue) 
        /// </summary>
        [Fact]
        public void TestMultiplePagesWithLinkedEntity()
        {
            List<Entity> initialEntities = new List<Entity>();
            int excessNumberOfRecords = 50;

            (_context as XrmFakedContext).MaxRetrieveCount = 1000;
            for (int i = 0; i < (_context as XrmFakedContext).MaxRetrieveCount + excessNumberOfRecords; i++)
            {
                Entity second = new Entity("second");
                second.Id = Guid.NewGuid();
                second["filter"] = true;
                initialEntities.Add(second);
                Entity first = new Entity("testentity");
                first.Id = Guid.NewGuid();
                first["secondid"] = second.ToEntityReference();
                initialEntities.Add(first);
            }
            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            LinkEntity link = new LinkEntity("testentity", "second", "secondid", "secondid", JoinOperator.Inner);
            link.EntityAlias = "second";
            link.LinkCriteria.AddCondition("filter", ConditionOperator.Equal, true);
            query.LinkEntities.Add(link);
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal((_context as XrmFakedContext).MaxRetrieveCount, result.Entities.Count);
            Assert.True(result.MoreRecords);
            Assert.NotNull(result.PagingCookie);

            query.PageInfo = new PagingInfo()
            {
                PagingCookie = result.PagingCookie,
                PageNumber = 2,
            };
            result = _service.RetrieveMultiple(query);
            Assert.Equal(excessNumberOfRecords, result.Entities.Count);
            Assert.False(result.MoreRecords);
        }

        /// <summary>
        /// Tests that a link's criteria aren't changed by the query (this was a buggy behavior due to a shallow copy)
        /// </summary>
        [Fact]
        public void TestLinkCriteriaAreNotChanged()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity second = new Entity("second");
            second.Id = Guid.NewGuid();
            second["filter"] = true;
            Entity first = new Entity("testentity");
            first.Id = Guid.NewGuid();
            first["secondid"] = second.ToEntityReference();
            initialEntities.Add(first);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            LinkEntity link = new LinkEntity("testentity", "second", "secondid", "secondid", JoinOperator.Inner);
            link.EntityAlias = "second";
            link.LinkCriteria.AddCondition("filter", ConditionOperator.Equal, true);
            query.LinkEntities.Add(link);
            _service.RetrieveMultiple(query);

            Assert.Equal("filter", query.LinkEntities[0].LinkCriteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.Equal, query.LinkEntities[0].LinkCriteria.Conditions[0].Operator);
            Assert.Equal(true, query.LinkEntities[0].LinkCriteria.Conditions[0].Values[0]);
        }

        /// <summary>
        /// Tests that if we ask for a non-existant page we don't get anything back and an error doesn't occur
        /// </summary>
        [Fact]
        public void TestAskingForEmptyPage()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity first = new Entity("testentity");
            first.Id = Guid.NewGuid();
            initialEntities.Add(first);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.PageInfo = new PagingInfo() { PageNumber = 2, Count = 20 };
            Assert.Equal(0, _service.RetrieveMultiple(query).Entities.Count);
        }

        /// <summary>
        /// Tests that if distinct is asked for that a distinct number of entities is returned
        /// </summary>
        [Fact]
        public void TestThatDistinctWorks()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity first = new Entity("testentity");
            first.Id = Guid.NewGuid();
            first["field"] = "value";
            initialEntities.Add(first);

            Entity related = new Entity("related");
            related.Id = Guid.NewGuid();
            related["entityid"] = first.ToEntityReference();
            related["include"] = true;
            initialEntities.Add(related);

            Entity secondRelated = new Entity("related");
            secondRelated.Id = Guid.NewGuid();
            secondRelated["entityid"] = first.ToEntityReference();
            secondRelated["include"] = true;
            initialEntities.Add(secondRelated);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.ColumnSet = new ColumnSet("field");
            query.Distinct = true;

            LinkEntity link = new LinkEntity("testentity", "related", "entityid", "entityid", JoinOperator.Inner);
            link.LinkCriteria.AddCondition("include", ConditionOperator.Equal, true);

            query.LinkEntities.Add(link);

            Assert.Equal(1, _service.RetrieveMultiple(query).Entities.Count);
        }

        /// <summary>
        /// Tests that if distinct is asked for and fields are pulled in from the link entities that the correct 
        /// records are returned
        /// </summary>
        [Fact]
        public void TestThatDistinctWorksWithLinkEntityFields()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity first = new Entity("testentity");
            first.Id = Guid.NewGuid();
            first["field"] = "value";
            initialEntities.Add(first);

            Entity related = new Entity("related");
            related.Id = Guid.NewGuid();
            related["entityid"] = first.ToEntityReference();
            related["include"] = true;
            related["linkfield"] = "value";
            initialEntities.Add(related);

            Entity secondRelated = new Entity("related");
            secondRelated.Id = Guid.NewGuid();
            secondRelated["entityid"] = first.ToEntityReference();
            secondRelated["include"] = true;
            secondRelated["linkfield"] = "other value";
            initialEntities.Add(secondRelated);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.ColumnSet = new ColumnSet("field");
            query.Distinct = true;

            LinkEntity link = new LinkEntity("testentity", "related", "entityid", "entityid", JoinOperator.Inner);
            link.LinkCriteria.AddCondition("include", ConditionOperator.Equal, true);
            link.Columns = new ColumnSet("linkfield");

            query.LinkEntities.Add(link);

            Assert.Equal(2, _service.RetrieveMultiple(query).Entities.Count);
        }

        /// <summary>
        /// Tests that if PageInfo's ReturnTotalRecordCount sets total record count.
        /// </summary>
        [Fact]
        public void TestThatPageInfoTotalRecordCountWorks()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity e = new Entity("testentity");
            e.Id = Guid.NewGuid();
            e["retrieve"] = true;
            initialEntities.Add(e);

            Entity e2 = new Entity("testentity");
            e2.Id = Guid.NewGuid();
            e2["retrieve"] = true;
            initialEntities.Add(e2);

            Entity e3 = new Entity("testentity");
            e3.Id = Guid.NewGuid();
            e3["retrieve"] = false;
            initialEntities.Add(e3);

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.PageInfo.ReturnTotalRecordCount = true;
            query.Criteria.AddCondition("retrieve", ConditionOperator.Equal, true);

            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(2, result.Entities.Count);
            Assert.Equal(2, result.TotalRecordCount);
            Assert.False(result.MoreRecords);
        }

        /// <summary>
        /// Tests that if PageInfo's ReturnTotalRecordCount works correctly with paging 
        /// </summary>
        [Fact]
        public void TestThatPageInfoTotalRecordCountWorksWithPaging()
        {
            List<Entity> initialEntities = new List<Entity>();

            for (int i = 0; i < 100; i++)
            {
                Entity e = new Entity("testentity");
                e.Id = Guid.NewGuid();
                initialEntities.Add(e);
            }

            _context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("testentity");
            query.PageInfo.ReturnTotalRecordCount = true;
            query.PageInfo.PageNumber = 1;
            query.PageInfo.Count = 10;

            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(10, result.Entities.Count);
            Assert.Equal(100, result.TotalRecordCount);
            Assert.True(result.MoreRecords);

            query.PageInfo.PageNumber++;
            query.PageInfo.Count = 20;
            query.PageInfo.PagingCookie = result.PagingCookie;

            result = _service.RetrieveMultiple(query);
            Assert.Equal(20, result.Entities.Count);
            Assert.Equal(100, result.TotalRecordCount);
            Assert.True(result.MoreRecords);
        }

        [Fact]
        public void TestNestedFiltersWithLateBoundEntities()
        {
            Entity account = new Entity("account") { Id = Guid.NewGuid() };
            account["name"] = "test";

            Entity contact = new Entity("contact") { Id = Guid.NewGuid() };
            contact["accountid"] = account.ToEntityReference();
            contact["birthdate"] = null;
            contact["territorycode"] = null;

            _context.Initialize(new List<Entity>
            {
                account,
                contact
            });

            var query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("name");
            query.Criteria.AddCondition(new ConditionExpression("name", ConditionOperator.Like, "test"));

            var linkEntity = query.AddLink("contact", "accountid", "accountid", JoinOperator.Inner);
            linkEntity.LinkCriteria.AddFilter(new FilterExpression(LogicalOperator.Or)
            {
                Filters = {
                    new FilterExpression(LogicalOperator.And) {
                        Conditions = {
                            new ConditionExpression("birthdate",ConditionOperator.Null),
                            new ConditionExpression("territorycode",ConditionOperator.Null)
                        }
                    },
                    new FilterExpression(LogicalOperator.And) {
                        Conditions = {
                            new ConditionExpression("birthdate",ConditionOperator.NotNull),
                            new ConditionExpression("territorycode",ConditionOperator.NotNull)
                        }
                    }
                }
            });

            var results = _service.RetrieveMultiple(query).Entities;
            Assert.Single(results);
        }

        [Fact]
        public void TestNestedFiltersWithEarlyBoundEntities()
        {
            Crm.Account account = new Crm.Account() { Id = Guid.NewGuid() };
            account.Name = "test";

            Crm.Contact contact = new Crm.Contact() { Id = Guid.NewGuid() };
            contact["accountid"] = account.ToEntityReference();
            contact.BirthDate = null;
            contact.TerritoryCode = null;

            _context.Initialize(new List<Entity>
            {
                account,
                contact
            });

            var query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("name");
            query.Criteria.AddCondition(new ConditionExpression("name", ConditionOperator.Like, "test"));

            var linkEntity = query.AddLink("contact", "accountid", "accountid", JoinOperator.Inner);
            linkEntity.LinkCriteria.AddFilter(new FilterExpression(LogicalOperator.Or)
            {
                Filters = {
                    new FilterExpression(LogicalOperator.And) {
                        Conditions = {
                            new ConditionExpression("birthdate",ConditionOperator.Null),
                            new ConditionExpression("territorycode",ConditionOperator.Null)
                        }
                    },
                    new FilterExpression(LogicalOperator.And) {
                        Conditions = {
                            new ConditionExpression("birthdate",ConditionOperator.NotNull),
                            new ConditionExpression("territorycode",ConditionOperator.NotNull)
                        }
                    }
                }
            });

            var results = _service.RetrieveMultiple(query).Entities.Cast<Crm.Account>().ToList();
            Assert.Single(results);
        }

        [Fact]
        public void Should_Populate_EntityReference_Name_When_Metadata_Is_Provided()
        {
            var userMetadata = new EntityMetadata() { LogicalName = "systemuser" };
            userMetadata.SetSealedPropertyValue("PrimaryNameAttribute", "fullname");

            var user = new Entity() { LogicalName = "systemuser", Id = Guid.NewGuid() };
            user["fullname"] = "Fake XrmEasy";

            _context.InitializeMetadata(userMetadata);
            _context.Initialize(user);
            (_context as XrmFakedContext).CallerId = user.ToEntityReference();

            var account = new Entity() { LogicalName = "account" };
            var accountId = _service.Create(account);

            QueryExpression query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet(true);

            var accounts = _service.RetrieveMultiple(query);

            Assert.Equal("Fake XrmEasy", accounts.Entities[0].GetAttributeValue<EntityReference>("ownerid").Name);
        }


#if !FAKE_XRM_EASY
        [Fact]
        public void Can_Filter_Using_Entity_Name_Without_Alias()
        {
            Entity e = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["retrieve"] = true
            };

            Entity e2 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["contactid"] = e.ToEntityReference()
            };

            _context.Initialize(new Entity[] { e, e2 });

            QueryExpression query = new QueryExpression("account");
            query.Criteria.AddCondition("contact", "retrieve", ConditionOperator.Equal, true);
            query.AddLink("contact", "contactid", "contactid");
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(1, result.Entities.Count);
        }

        [Fact]
        public void Can_Filter_Using_Entity_Name_With_Alias()
        {
            Entity e = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["retrieve"] = true
            };

            Entity e2 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["contactid"] = e.ToEntityReference()
            };

            _context.Initialize(new Entity[] { e, e2 });

            QueryExpression query = new QueryExpression("account");
            query.Criteria.AddCondition("mycontact", "retrieve", ConditionOperator.Equal, true);
            query.AddLink("contact", "contactid", "contactid").EntityAlias="mycontact";
            EntityCollection result = _service.RetrieveMultiple(query);
            Assert.Equal(1, result.Entities.Count);
        }
#endif

        [Fact]
        public void Should_Allow_Using_Aliases_with_Dot()
        {
            var contact = new Entity("contact") { Id = Guid.NewGuid() };
            contact["firstname"] = "Jordi";

            var account = new Entity("account") { Id = Guid.NewGuid() };
            account["primarycontactid"] = contact.ToEntityReference();
            account["name"] = "Dynamics Value";

            _context.Initialize(new Entity[] { contact, account });

            QueryExpression query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("name");
            var link = query.AddLink("contact", "contactid", "primarycontactid");
            link.EntityAlias = "primary.contact";
            link.Columns = new ColumnSet("firstname");

            var accounts = _service.RetrieveMultiple(query);

            Assert.True(accounts.Entities.First().Contains("primary.contact.firstname"));
            Assert.Equal("Jordi", accounts.Entities.First().GetAttributeValue<AliasedValue>("primary.contact.firstname").Value);
        }

        [Fact]
        public void TheCorrectResultIsReturnedWhenUsingConditionOperatorInWithGuid()
        {
            var contact = new Crm.Contact()
            {
                Id = Guid.NewGuid()
            };
            _context.Initialize(contact);

            var Ids = new string[] { Guid.NewGuid().ToString(), contact.Id.ToString() };

            var query = new QueryExpression("contact");
            query.Criteria.AddCondition("contactid", ConditionOperator.In, Ids);

            var result = _service.RetrieveMultiple(query).Entities;
            Assert.True(result.Any());
            Assert.Equal(contact.Id, result[0].Id);
        }
    }
}
