#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Fake4Dataverse.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.ExecuteTransationTests
{
    public class ExecuteTransactionTests: Fake4DataverseTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new ExecuteTransactionExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_execute_is_called_all_requests_are_executed()
        {
            
            var executor = new ExecuteTransactionExecutor();
            var req = new ExecuteTransactionRequest()
            {
                Requests = new OrganizationRequestCollection()
                {
                    new CreateRequest() { Target = new Entity("contact") },
                    new CreateRequest() { Target = new Entity("contact") },
                    new CreateRequest() { Target = new Entity("contact") }
                }
            };

            var response = executor.Execute(req, _context) as ExecuteTransactionResponse;
            var contacts = _context.CreateQuery("contact").ToList();
            Assert.Empty(response.Responses);
            Assert.Equal(3, contacts.Count);
        }

        [Fact]
        public void When_execute_is_called_all_requests_are_executed_with_responses()
        {
            
            var executor = new ExecuteTransactionExecutor();
            var req = new ExecuteTransactionRequest()
            {
                ReturnResponses = true,
                Requests = new OrganizationRequestCollection()
                {
                    new CreateRequest() { Target = new Entity("contact") },
                    new CreateRequest() { Target = new Entity("contact") },
                    new CreateRequest() { Target = new Entity("contact") }
                }
            };

            var response = executor.Execute(req, _context) as ExecuteTransactionResponse;
            var contacts = _context.CreateQuery("contact").ToList();
            Assert.Equal(3, response.Responses.Count);
            Assert.Equal(3, contacts.Count);
        }
    }
}

#endif