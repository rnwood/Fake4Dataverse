using System;
using System.Collections.Generic;
using System.Linq;
using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.FakeMessageExecutors;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.MergeRequestTests
{
    public class MergeRequestTests : Fake4DataverseTests
    {
        [Fact]
        public void When_Merge_Request_Is_Executed_Subordinate_Entity_Is_Deleted()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };

            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id
            };

            // Act
            var response = _service.Execute(mergeRequest);

            // Assert
            Assert.NotNull(response);
            Assert.IsType<MergeResponse>(response);
            
            // Target should still exist
            Assert.True(_context.ContainsEntity(targetAccount.LogicalName, targetAccount.Id));
            
            // Subordinate should be deleted
            Assert.False(_context.ContainsEntity(subordinateAccount.LogicalName, subordinateAccount.Id));
        }

        [Fact]
        public void When_Merge_Request_Is_Executed_With_UpdateContent_Target_Is_Updated()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };

            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount });

            var updateContent = new Entity(targetAccount.LogicalName)
            {
                ["name"] = "Merged Account Name",
                ["telephone1"] = "555-1234"
            };

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = updateContent
            };

            // Act
            _service.Execute(mergeRequest);

            // Assert
            var mergedAccount = _service.Retrieve(targetAccount.LogicalName, targetAccount.Id, new ColumnSet(true));
            Assert.Equal("Merged Account Name", mergedAccount.GetAttributeValue<string>("name"));
            Assert.Equal("555-1234", mergedAccount.GetAttributeValue<string>("telephone1"));
        }

        [Fact]
        public void When_Merge_Request_Is_Executed_References_Are_Updated()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            var contact = new Contact 
            { 
                Id = Guid.NewGuid(), 
                FirstName = "John",
                ParentCustomerId = subordinateAccount.ToEntityReference()
            };

            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount, contact });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id
            };

            // Act
            _service.Execute(mergeRequest);

            // Assert
            var updatedContact = _service.Retrieve(contact.LogicalName, contact.Id, new ColumnSet(true));
            var parentRef = updatedContact.GetAttributeValue<EntityReference>("parentcustomerid");
            Assert.NotNull(parentRef);
            Assert.Equal(targetAccount.Id, parentRef.Id);
            Assert.Equal(targetAccount.LogicalName, parentRef.LogicalName);
        }

        [Fact]
        public void When_Merge_Request_Is_Executed_Multiple_References_Are_Updated()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            var contact1 = new Contact 
            { 
                Id = Guid.NewGuid(), 
                FirstName = "John",
                ParentCustomerId = subordinateAccount.ToEntityReference()
            };
            var contact2 = new Contact 
            { 
                Id = Guid.NewGuid(), 
                FirstName = "Jane",
                ParentCustomerId = subordinateAccount.ToEntityReference()
            };

            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount, contact1, contact2 });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id
            };

            // Act
            _service.Execute(mergeRequest);

            // Assert
            var updatedContact1 = _service.Retrieve(contact1.LogicalName, contact1.Id, new ColumnSet(true));
            var parentRef1 = updatedContact1.GetAttributeValue<EntityReference>("parentcustomerid");
            Assert.Equal(targetAccount.Id, parentRef1.Id);

            var updatedContact2 = _service.Retrieve(contact2.LogicalName, contact2.Id, new ColumnSet(true));
            var parentRef2 = updatedContact2.GetAttributeValue<EntityReference>("parentcustomerid");
            Assert.Equal(targetAccount.Id, parentRef2.Id);
        }

        [Fact]
        public void When_Merge_Request_Has_Null_Target_Exception_Is_Thrown()
        {
            // Arrange
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            _context.Initialize(new List<Entity> { subordinateAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = null,
                SubordinateId = subordinateAccount.Id
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Has_Empty_SubordinateId_Exception_Is_Thrown()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = Guid.Empty
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Target_Does_Not_Exist_Exception_Is_Thrown()
        {
            // Arrange
            var nonExistentTargetId = Guid.NewGuid();
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            _context.Initialize(new List<Entity> { subordinateAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = new EntityReference("account", nonExistentTargetId),
                SubordinateId = subordinateAccount.Id
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Subordinate_Does_Not_Exist_Exception_Is_Thrown()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var nonExistentSubordinateId = Guid.NewGuid();
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = nonExistentSubordinateId
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Attempts_To_Merge_Entity_With_Itself_Exception_Is_Thrown()
        {
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = targetAccount.Id
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Can_Execute_Is_Called_With_MergeRequest_Returns_True()
        {
            // Arrange
            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest();

            // Act
            var canExecute = executor.CanExecute(mergeRequest);

            // Assert
            Assert.True(canExecute);
        }

        [Fact]
        public void When_Can_Execute_Is_Called_With_Non_MergeRequest_Returns_False()
        {
            // Arrange
            var executor = new MergeRequestExecutor();
            var retrieveRequest = new RetrieveMultipleRequest();

            // Act
            var canExecute = executor.CanExecute(retrieveRequest);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void When_Get_Responsible_Request_Type_Is_Called_Returns_MergeRequest()
        {
            // Arrange
            var executor = new MergeRequestExecutor();

            // Act
            var requestType = executor.GetResponsibleRequestType();

            // Assert
            Assert.Equal(typeof(MergeRequest), requestType);
        }
    }
}
