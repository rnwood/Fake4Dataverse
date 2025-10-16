using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests
{
    public class FakeContextTestExecute : Fake4DataverseTests
    {        public FakeContextTestExecute()
        {
            // Use context from base class (validation disabled)
        }

        [Fact]
        public void When_Executing_Assign_Request_New_Owner_Should_Be_Assigned()
        {
            var oldOwner = new EntityReference("systemuser", Guid.NewGuid());
            var newOwner = new EntityReference("systemuser", Guid.NewGuid());

            var account = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = oldOwner
            };

            _context.Initialize(new[] { account });

            var assignRequest = new AssignRequest
            {
                Target = account.ToEntityReference(),
                Assignee = newOwner
            };
            _service.Execute(assignRequest);

            //retrieve account updated
            var updatedAccount = _context.CreateQuery<Account>().FirstOrDefault();
            Assert.Equal(newOwner.Id, updatedAccount.OwnerId.Id);
        }
    }
}