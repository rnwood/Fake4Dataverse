#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Fake4Dataverse.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.RemoveFromQueueRequestTests
{
    public class RemoveFromQueueRequestTests: Fake4DataverseTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new RemoveFromQueueRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_a_request_is_called_Queueitem_Is_Removed()
        {
            var queueItem = new Entity
            {
                LogicalName = Crm.QueueItem.EntityLogicalName,
                Id = Guid.NewGuid()
            };

            _context.Initialize(new[]
            {
                queueItem
            });

            var executor = new RemoveFromQueueRequestExecutor();

            var req = new RemoveFromQueueRequest
            {
                QueueItemId = queueItem.Id
            };

            executor.Execute(req, _context);

            var retrievedQueueItemQuery = _context.CreateQuery(Crm.QueueItem.EntityLogicalName);

            Assert.True(!retrievedQueueItemQuery.Any());
        }

        [Fact]
        public void When_a_request_without_QueueItem_is_called_exception_is_raised()
        {
            
            var executor = new RemoveFromQueueRequestExecutor();
            var req = new RemoveFromQueueRequest();

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(req, _context));
        }
    }
}
#endif