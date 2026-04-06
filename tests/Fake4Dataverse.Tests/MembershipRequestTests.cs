using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class MembershipRequestTests
    {
        [Fact]
        public void AddMembersTeamRequest_AddsTeamMemberships()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var teamId = service.Create(new Entity("team") { ["name"] = "TestTeam" });
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            service.Execute(new AddMembersTeamRequest
            {
                TeamId = teamId,
                MemberIds = new[] { userId1, userId2 }
            });

            var result = service.RetrieveMultiple(new QueryExpression("teammembership") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, result.Entities.Count);
        }

        [Fact]
        public void RemoveMembersTeamRequest_RemovesTeamMemberships()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var teamId = service.Create(new Entity("team") { ["name"] = "TestTeam" });
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            service.Execute(new AddMembersTeamRequest
            {
                TeamId = teamId,
                MemberIds = new[] { userId1, userId2 }
            });

            service.Execute(new RemoveMembersTeamRequest
            {
                TeamId = teamId,
                MemberIds = new[] { userId1 }
            });

            var result = service.RetrieveMultiple(new QueryExpression("teammembership") { ColumnSet = new ColumnSet(true) });
            Assert.Single(result.Entities);
        }

        [Fact]
        public void AddListMembersListRequest_AddsListMembers()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var listId = service.Create(new Entity("list") { ["listname"] = "TestList" });
            var memberId1 = Guid.NewGuid();
            var memberId2 = Guid.NewGuid();

            service.Execute(new AddListMembersListRequest
            {
                ListId = listId,
                MemberIds = new[] { memberId1, memberId2 }
            });

            var result = service.RetrieveMultiple(new QueryExpression("listmember") { ColumnSet = new ColumnSet(true) });
            Assert.Equal(2, result.Entities.Count);
        }

        [Fact]
        public void RemoveMemberListRequest_RemovesListMember()
        {
            var env = new FakeDataverseEnvironment();
            var service = env.CreateOrganizationService();
            var listId = service.Create(new Entity("list") { ["listname"] = "TestList" });
            var memberId = Guid.NewGuid();

            service.Execute(new AddListMembersListRequest
            {
                ListId = listId,
                MemberIds = new[] { memberId }
            });

            var members = service.RetrieveMultiple(new QueryExpression("listmember") { ColumnSet = new ColumnSet(true) });
            Assert.Single(members.Entities);

            service.Execute(new RemoveMemberListRequest
            {
                ListId = listId,
                EntityId = members.Entities[0].Id
            });

            var remaining = service.RetrieveMultiple(new QueryExpression("listmember") { ColumnSet = new ColumnSet(true) });
            Assert.Empty(remaining.Entities);
        }
    }
}
