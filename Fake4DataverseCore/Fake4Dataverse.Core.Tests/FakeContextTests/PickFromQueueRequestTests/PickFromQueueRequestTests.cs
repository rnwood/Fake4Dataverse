#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Fake4Dataverse.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.PickFromQueueRequestTests
{
    public class PickFromQueueRequestTests: Fake4DataverseTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new PickFromQueueRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_a_request_is_called_worker_is_set()
        {
            var email = new Entity
            {
                LogicalName = Crm.Email.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var queue = new Entity
            {
                LogicalName = Crm.Queue.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var user = new Entity
            {
                LogicalName = Crm.SystemUser.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var queueItem = new Entity
            {
                LogicalName = Crm.QueueItem.EntityLogicalName,
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "queueid", queue.ToEntityReference() },
                    { "objectid", email.ToEntityReference() }
                }
            };

            _context.Initialize(new[]
            {
                queue, email, user, queueItem
            });

            var executor = new PickFromQueueRequestExecutor();

            var req = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = user.Id,
            };

            var before = DateTime.Now.Ticks;
            executor.Execute(req, _context);
            var after = DateTime.Now.Ticks;

            var queueItemUpdated = _service.Retrieve(Crm.QueueItem.EntityLogicalName, queueItem.Id, new ColumnSet(true));

            Assert.Equal(user.ToEntityReference(), queueItemUpdated.GetAttributeValue<EntityReference>("workerid"));
            Assert.True(before <= queueItemUpdated.GetAttributeValue<DateTime>("workeridmodifiedon").Ticks);
            Assert.True(after >= queueItemUpdated.GetAttributeValue<DateTime>("workeridmodifiedon").Ticks);
        }

        [Fact]
        public void When_a_request_is_called_with_removal_queueitem_is_deleted()
        {
            
            

            var email = new Entity
            {
                LogicalName = Crm.Email.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var queue = new Entity
            {
                LogicalName = Crm.Queue.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var user = new Entity
            {
                LogicalName = Crm.SystemUser.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var queueItem = new Entity
            {
                LogicalName = Crm.QueueItem.EntityLogicalName,
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "queueid", queue.ToEntityReference() },
                    { "objectid", email.ToEntityReference() }
                }
            };

            _context.Initialize(new[]
            {
                queue, email, user, queueItem
            });

            var executor = new PickFromQueueRequestExecutor();

            var req = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = user.Id,
                RemoveQueueItem = true
            };

            executor.Execute(req, _context);

            Assert.Empty(_context.CreateQuery(Crm.QueueItem.EntityLogicalName));
        }

        [Fact]
        public void When_a_request_is_for_non_existing_woker_an_exception_is_thrown()
        {
            
            

            var queue = new Entity
            {
                LogicalName = Crm.Queue.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            var queueItem = new Entity
            {
                LogicalName = Crm.QueueItem.EntityLogicalName,
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "queueid", queue.ToEntityReference() },
                    { "objectid", Guid.NewGuid() }
                }
            };

            _context.Initialize(new[]
            {
                queue, queueItem
            });

            var executor = new PickFromQueueRequestExecutor();

            var req = new PickFromQueueRequest
            {
                QueueItemId = queueItem.Id,
                WorkerId = Guid.NewGuid(),
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(req, _context));
        }

        [Fact]
        public void When_a_request_is_for_non_existing_queueitem_an_exception_is_thrown()
        {
            
            

            var user = new Entity
            {
                LogicalName = Crm.SystemUser.EntityLogicalName,
                Id = Guid.NewGuid(),
            };

            _context.Initialize(new[]
            {
                user
            });

            var executor = new PickFromQueueRequestExecutor();

            var req = new PickFromQueueRequest
            {
                QueueItemId = Guid.NewGuid(),
                WorkerId = user.Id,
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(req, _context));
        }
    }
}

#endif