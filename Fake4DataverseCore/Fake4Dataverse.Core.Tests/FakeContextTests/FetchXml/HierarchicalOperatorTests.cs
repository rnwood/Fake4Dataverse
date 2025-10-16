using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Query;
using Fake4Dataverse.Middleware;

namespace Fake4Dataverse.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for hierarchical query operators (Above, Under, ChildOf, etc.)
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
    /// </summary>
    public class HierarchicalOperatorTests : Fake4DataverseTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public HierarchicalOperatorTests()
        {
            // Use context and service from base class

            _context = base._context;

            _service = base._service;
        }

        /// <summary>
        /// Sets up a test hierarchy of accounts:
        ///        Root (A)
        ///       /    \
        ///      B      C
        ///     / \      \
        ///    D   E      F
        ///   /
        ///  G
        /// </summary>
        private Dictionary<string, Guid> SetupAccountHierarchy()
        {
            _context.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

            var accountIds = new Dictionary<string, Guid>
            {
                { "A", Guid.NewGuid() },
                { "B", Guid.NewGuid() },
                { "C", Guid.NewGuid() },
                { "D", Guid.NewGuid() },
                { "E", Guid.NewGuid() },
                { "F", Guid.NewGuid() },
                { "G", Guid.NewGuid() }
            };

            var accountA = new Account { Id = accountIds["A"], Name = "Account A" };
            var accountB = new Account { Id = accountIds["B"], Name = "Account B", ParentAccountId = new EntityReference("account", accountIds["A"]) };
            var accountC = new Account { Id = accountIds["C"], Name = "Account C", ParentAccountId = new EntityReference("account", accountIds["A"]) };
            var accountD = new Account { Id = accountIds["D"], Name = "Account D", ParentAccountId = new EntityReference("account", accountIds["B"]) };
            var accountE = new Account { Id = accountIds["E"], Name = "Account E", ParentAccountId = new EntityReference("account", accountIds["B"]) };
            var accountF = new Account { Id = accountIds["F"], Name = "Account F", ParentAccountId = new EntityReference("account", accountIds["C"]) };
            var accountG = new Account { Id = accountIds["G"], Name = "Account G", ParentAccountId = new EntityReference("account", accountIds["D"]) };

            _context.Initialize(new[] { accountA, accountB, accountC, accountD, accountE, accountF, accountG });

            return accountIds;
        }

        #region FetchXML Translation Tests

        [Fact]
        public void FetchXml_Operator_Under_Translation()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            var testGuid = Guid.NewGuid();
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='under' value='{testGuid}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var query = fetchXml.ToQueryExpression(_context);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("parentaccountid", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.Under, query.Criteria.Conditions[0].Operator);
            Assert.Equal(testGuid, query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_UnderOrEqual_Translation()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            var testGuid = Guid.NewGuid();
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='eq-or-under' value='{testGuid}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var query = fetchXml.ToQueryExpression(_context);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("parentaccountid", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.UnderOrEqual, query.Criteria.Conditions[0].Operator);
            Assert.Equal(testGuid, query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_Above_Translation()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            var testGuid = Guid.NewGuid();
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='above' value='{testGuid}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var query = fetchXml.ToQueryExpression(_context);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("parentaccountid", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.Above, query.Criteria.Conditions[0].Operator);
            Assert.Equal(testGuid, query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_AboveOrEqual_Translation()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            var testGuid = Guid.NewGuid();
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='eq-or-above' value='{testGuid}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var query = fetchXml.ToQueryExpression(_context);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("parentaccountid", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.AboveOrEqual, query.Criteria.Conditions[0].Operator);
            Assert.Equal(testGuid, query.Criteria.Conditions[0].Values[0]);
        }

        [Fact]
        public void FetchXml_Operator_NotUnder_Translation()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.conditionoperator
            var testGuid = Guid.NewGuid();
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='not-under' value='{testGuid}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var query = fetchXml.ToQueryExpression(_context);

            Assert.True(query.Criteria != null);
            Assert.Equal(1, query.Criteria.Conditions.Count);
            Assert.Equal("parentaccountid", query.Criteria.Conditions[0].AttributeName);
            Assert.Equal(ConditionOperator.NotUnder, query.Criteria.Conditions[0].Operator);
            Assert.Equal(testGuid, query.Criteria.Conditions[0].Values[0]);
        }

        #endregion

        #region Query Execution Tests

        [Fact]
        public void QueryExpression_Operator_Under_Returns_All_Descendants()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.Under, accountIds["A"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return B, C, D, E, F, G (all descendants of A, but not A itself)
            Assert.Equal(6, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds["B"], resultIds);
            Assert.Contains(accountIds["C"], resultIds);
            Assert.Contains(accountIds["D"], resultIds);
            Assert.Contains(accountIds["E"], resultIds);
            Assert.Contains(accountIds["F"], resultIds);
            Assert.Contains(accountIds["G"], resultIds);
            Assert.DoesNotContain(accountIds["A"], resultIds);
        }

        [Fact]
        public void QueryExpression_Operator_UnderOrEqual_Returns_Record_And_Descendants()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.UnderOrEqual, accountIds["B"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return B, D, E, G (B and all its descendants)
            Assert.Equal(4, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds["B"], resultIds);
            Assert.Contains(accountIds["D"], resultIds);
            Assert.Contains(accountIds["E"], resultIds);
            Assert.Contains(accountIds["G"], resultIds);
        }

        [Fact]
        public void QueryExpression_Operator_Above_Returns_All_Ancestors()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.Above, accountIds["G"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return D, B, A (all ancestors of G, but not G itself)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds["D"], resultIds);
            Assert.Contains(accountIds["B"], resultIds);
            Assert.Contains(accountIds["A"], resultIds);
            Assert.DoesNotContain(accountIds["G"], resultIds);
        }

        [Fact]
        public void QueryExpression_Operator_AboveOrEqual_Returns_Record_And_Ancestors()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.AboveOrEqual, accountIds["D"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return D, B, A (D and all its ancestors)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds["D"], resultIds);
            Assert.Contains(accountIds["B"], resultIds);
            Assert.Contains(accountIds["A"], resultIds);
        }

        [Fact]
        public void QueryExpression_Operator_NotUnder_Returns_Records_Not_In_Hierarchy()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.NotUnder, accountIds["B"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return A, C, F (all records that are NOT under B in the hierarchy)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds["A"], resultIds);
            Assert.Contains(accountIds["C"], resultIds);
            Assert.Contains(accountIds["F"], resultIds);
            Assert.DoesNotContain(accountIds["B"], resultIds);
            Assert.DoesNotContain(accountIds["D"], resultIds);
            Assert.DoesNotContain(accountIds["E"], resultIds);
            Assert.DoesNotContain(accountIds["G"], resultIds);
        }

        [Fact]
        public void FetchXml_Operator_Under_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='under' value='{accountIds["A"]}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var results = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Should return B, C, D, E, F, G (all descendants of A)
            Assert.Equal(6, results.Entities.Count);
        }

        [Fact]
        public void FetchXml_Operator_Above_Execution()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='accountid' />
                                    <filter type='and'>
                                        <condition attribute='parentaccountid' operator='above' value='{accountIds["G"]}' />
                                    </filter>
                              </entity>
                            </fetch>";

            var results = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Should return D, B, A (all ancestors of G)
            Assert.Equal(3, results.Entities.Count);
        }

        [Fact]
        public void QueryExpression_Operator_Under_With_Leaf_Node_Returns_Empty()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // G is a leaf node with no children
                        new ConditionExpression("parentaccountid", ConditionOperator.Under, accountIds["G"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return empty since G has no descendants
            Assert.Empty(results.Entities);
        }

        [Fact]
        public void QueryExpression_Operator_Above_With_Root_Node_Returns_Empty()
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/query-hierarchical-data
            var accountIds = SetupAccountHierarchy();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // A is the root node with no parent
                        new ConditionExpression("parentaccountid", ConditionOperator.Above, accountIds["A"])
                    }
                }
            };

            var results = _service.RetrieveMultiple(query);

            // Should return empty since A has no ancestors
            Assert.Empty(results.Entities);
        }

        #endregion
    }
}
