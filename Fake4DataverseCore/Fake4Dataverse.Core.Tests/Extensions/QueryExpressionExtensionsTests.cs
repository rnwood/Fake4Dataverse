using Xunit;
using Fake4Dataverse.Extensions;
using Microsoft.Xrm.Sdk.Query;
using Fake4Dataverse.Query;

namespace Fake4Dataverse.Tests.Extensions
{
    public class QueryExpressionExtensionsTests
    {
        [Fact]
        public void TestClone()
        {
            QueryExpression query = new QueryExpression("entity");
            LinkEntity link = new LinkEntity("entity", "second", "secondid", "secondid", JoinOperator.Inner);
            link.EntityAlias = "second";
            link.LinkCriteria.AddCondition("filter", ConditionOperator.Equal, true);
            query.LinkEntities.Add(link);

            QueryExpression cloned = query.Clone();
            cloned.LinkEntities[0].LinkCriteria.Conditions[0].AttributeName = "otherfield";

            cloned.LinkEntities[0].LinkCriteria.Conditions[0].AttributeName = "link.field";
            Assert.Equal("entity", query.EntityName);
            Assert.Equal("filter", query.LinkEntities[0].LinkCriteria.Conditions[0].AttributeName );
        }
    }
}
