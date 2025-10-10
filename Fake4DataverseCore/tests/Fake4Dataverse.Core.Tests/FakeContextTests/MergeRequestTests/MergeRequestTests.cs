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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // The subordinate record is deactivated (not deleted)
            
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };

            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account") // UpdateContent is required
            };

            // Act
            var response = _service.Execute(mergeRequest);

            // Assert
            Assert.NotNull(response);
            Assert.IsType<MergeResponse>(response);
            
            // Target should still exist
            Assert.True(_context.ContainsEntity(targetAccount.LogicalName, targetAccount.Id));
            
            // Subordinate should still exist but be deactivated (statecode=1)
            Assert.True(_context.ContainsEntity(subordinateAccount.LogicalName, subordinateAccount.Id));
            var subordinateAfterMerge = _service.Retrieve(subordinateAccount.LogicalName, subordinateAccount.Id, new ColumnSet(true));
            Assert.Equal(1, subordinateAfterMerge.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(2, subordinateAfterMerge.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void When_Merge_Request_Is_Executed_With_UpdateContent_Target_Is_Updated()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // UpdateContent: Additional entity attributes to be set during the merge
            
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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // All related records are reassigned to the target
            
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
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account") // UpdateContent is required
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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // All related records are reassigned to the target
            
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
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account") // UpdateContent is required
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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            
            // Arrange
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            _context.Initialize(new List<Entity> { subordinateAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = null,
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account")
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Has_Null_UpdateContent_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // UpdateContent is required per Microsoft documentation
            
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            _context.Initialize(new List<Entity> { targetAccount, subordinateAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = null // This should throw error
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Has_Empty_SubordinateId_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // SubordinateId is required - the ID of the entity record from which to merge data
            
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = Guid.Empty,
                UpdateContent = new Entity("account")
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Target_Does_Not_Exist_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // Target is required - the target of the merge operation
            
            // Arrange
            var nonExistentTargetId = Guid.NewGuid();
            var subordinateAccount = new Account { Id = Guid.NewGuid(), Name = "Subordinate Account" };
            _context.Initialize(new List<Entity> { subordinateAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = new EntityReference("account", nonExistentTargetId),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account")
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Subordinate_Does_Not_Exist_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // SubordinateId must reference an existing entity record
            
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            var nonExistentSubordinateId = Guid.NewGuid();
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = nonExistentSubordinateId,
                UpdateContent = new Entity("account")
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Merge_Request_Attempts_To_Merge_Entity_With_Itself_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // Target and SubordinateId must reference different entity records
            
            // Arrange
            var targetAccount = new Account { Id = Guid.NewGuid(), Name = "Target Account" };
            _context.Initialize(new List<Entity> { targetAccount });

            var executor = new MergeRequestExecutor();
            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = targetAccount.Id,
                UpdateContent = new Entity("account")
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(mergeRequest, _context));
        }

        [Fact]
        public void When_Can_Execute_Is_Called_With_MergeRequest_Returns_True()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // Executor should handle MergeRequest message type
            
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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // Executor should only handle MergeRequest message type
            
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
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // Executor is responsible for MergeRequest message type
            
            // Arrange
            var executor = new MergeRequestExecutor();

            // Act
            var requestType = executor.GetResponsibleRequestType();

            // Assert
            Assert.Equal(typeof(MergeRequest), requestType);
        }

        [Fact]
        public void When_PerformParentingChecks_Is_True_And_Parents_Differ_Exception_Is_Thrown()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest.performparentingchecks
            // When PerformParentingChecks is true, validates that parent records match
            
            // Arrange
            var parentAccount1 = new Account { Id = Guid.NewGuid(), Name = "Parent Account 1" };
            var parentAccount2 = new Account { Id = Guid.NewGuid(), Name = "Parent Account 2" };
            var targetAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Target Account",
                ParentAccountId = parentAccount1.ToEntityReference()
            };
            var subordinateAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Subordinate Account",
                ParentAccountId = parentAccount2.ToEntityReference()
            };

            _context.Initialize(new List<Entity> { parentAccount1, parentAccount2, targetAccount, subordinateAccount });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account"),
                PerformParentingChecks = true
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(mergeRequest));
        }

        [Fact]
        public void When_PerformParentingChecks_Is_True_And_Parents_Match_Merge_Succeeds()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest.performparentingchecks
            // When PerformParentingChecks is true and parents match, merge should succeed
            
            // Arrange
            var parentAccount = new Account { Id = Guid.NewGuid(), Name = "Parent Account" };
            var targetAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Target Account",
                ParentAccountId = parentAccount.ToEntityReference()
            };
            var subordinateAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Subordinate Account",
                ParentAccountId = parentAccount.ToEntityReference()
            };

            _context.Initialize(new List<Entity> { parentAccount, targetAccount, subordinateAccount });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account"),
                PerformParentingChecks = true
            };

            // Act
            var response = _service.Execute(mergeRequest);

            // Assert
            Assert.NotNull(response);
            Assert.IsType<MergeResponse>(response);
        }

        [Fact]
        public void When_PerformParentingChecks_Is_False_And_Parents_Differ_Merge_Succeeds()
        {
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest.performparentingchecks
            // When PerformParentingChecks is false (default), merge should succeed regardless of parent differences
            
            // Arrange
            var parentAccount1 = new Account { Id = Guid.NewGuid(), Name = "Parent Account 1" };
            var parentAccount2 = new Account { Id = Guid.NewGuid(), Name = "Parent Account 2" };
            var targetAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Target Account",
                ParentAccountId = parentAccount1.ToEntityReference()
            };
            var subordinateAccount = new Account 
            { 
                Id = Guid.NewGuid(), 
                Name = "Subordinate Account",
                ParentAccountId = parentAccount2.ToEntityReference()
            };

            _context.Initialize(new List<Entity> { parentAccount1, parentAccount2, targetAccount, subordinateAccount });

            var mergeRequest = new MergeRequest
            {
                Target = targetAccount.ToEntityReference(),
                SubordinateId = subordinateAccount.Id,
                UpdateContent = new Entity("account"),
                PerformParentingChecks = false // Explicitly set to false
            };

            // Act
            var response = _service.Execute(mergeRequest);

            // Assert
            Assert.NotNull(response);
            Assert.IsType<MergeResponse>(response);
        }
    }
}
