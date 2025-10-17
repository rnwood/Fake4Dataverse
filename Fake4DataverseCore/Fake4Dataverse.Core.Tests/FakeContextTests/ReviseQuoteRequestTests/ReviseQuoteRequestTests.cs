using Fake4Dataverse.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.ReviseQuoteRequestTests
{
    public class ReviseQuoteRequestTests: Fake4DataverseTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new ReviseQuoteRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();

            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void Should_Create_New_Quote_With_Lines_When_Revisioning()
        {
            
            var quote = new Entity
            {
                LogicalName = "quote",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    {"statuscode", new OptionSetValue(1)},
                    {"name", "Adventure Quote"}
                }
            };

            var quoteDetail = new Entity
            {
                LogicalName = "quotedetail",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    {"quoteid", quote.ToEntityReference() },
                    {"extendedamount", new Money(1000m) }
                }
            };

            _context.Initialize(new[]
            {
                quote, quoteDetail
            });

            var executor = new ReviseQuoteRequestExecutor();

            var req = new ReviseQuoteRequest
            {
                ColumnSet = new ColumnSet(true),
                QuoteId = quote.Id
            };

            executor.Execute(req, _context);

            quote = _service.RetrieveMultiple(new QueryExpression("quote")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("quoteid", ConditionOperator.NotEqual, quote.Id)
                    }
                }
            }).Entities.SingleOrDefault();

            Assert.NotNull(quote);
            Assert.Equal("Adventure Quote", quote.GetAttributeValue<string>("name"));

            var quoteLines = _service.RetrieveMultiple(new QueryExpression("quotedetail")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = { new ConditionExpression("quoteid", ConditionOperator.Equal, quote.ToEntityReference()) }
                }
            }).Entities.ToList();

            Assert.Single(quoteLines);
            Assert.Equal(new Money(1000m), quoteLines.Single().GetAttributeValue<Money>("extendedamount"));
        }
    }
}