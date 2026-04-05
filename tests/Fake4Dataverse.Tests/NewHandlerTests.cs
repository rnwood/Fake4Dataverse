using System;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class NewHandlerTests
    {
        [Fact]
        public void AddMembersTeamRequest_AddsTeamMemberships()
        {
            var service = new FakeOrganizationService();
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
            var service = new FakeOrganizationService();
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
            var service = new FakeOrganizationService();
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
            var service = new FakeOrganizationService();
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

        [Fact]
        public void SendEmailRequest_MarksEmailAsSent()
        {
            var service = new FakeOrganizationService();
            var emailId = service.Create(new Entity("email")
            {
                ["subject"] = "Test Email",
                ["description"] = "Body"
            });

            service.Execute(new SendEmailRequest
            {
                EmailId = emailId,
                IssueSend = true
            });

            var email = service.Retrieve("email", emailId, new ColumnSet("statecode", "statuscode"));
            Assert.Equal(1, email.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(3, email.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void InitializeFromRequest_CopiesSourceAttributes()
        {
            var service = new FakeOrganizationService();
            var accountId = service.Create(new Entity("account")
            {
                ["name"] = "Contoso",
                ["telephone1"] = "555-1234",
                ["revenue"] = new Money(1000000m)
            });

            var response = (InitializeFromResponse)service.Execute(new InitializeFromRequest
            {
                EntityMoniker = new EntityReference("account", accountId),
                TargetEntityName = "account"
            });

            var newEntity = (Entity)response.Results["Entity"];
            Assert.Equal("account", newEntity.LogicalName);
            Assert.Equal("Contoso", newEntity.GetAttributeValue<string>("name"));
            Assert.Equal("555-1234", newEntity.GetAttributeValue<string>("telephone1"));
            // System fields should NOT be copied
            Assert.False(newEntity.Contains("createdon"));
            Assert.False(newEntity.Contains("modifiedon"));
        }

        [Fact]
        public void CalculateRollupFieldRequest_TriggersRollupCalculation()
        {
            var service = new FakeOrganizationService();

            service.CalculatedFields.RegisterRollupField(
                "account", "totalrevenue",
                "opportunity", "estimatedvalue", "parentaccountid",
                RollupType.Sum);

            var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });
            service.Create(new Entity("opportunity")
            {
                ["name"] = "Deal 1",
                ["estimatedvalue"] = 100m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            });
            service.Create(new Entity("opportunity")
            {
                ["name"] = "Deal 2",
                ["estimatedvalue"] = 200m,
                ["parentaccountid"] = new EntityReference("account", accountId)
            });

            var response = (CalculateRollupFieldResponse)service.Execute(new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "totalrevenue"
            });

            var entity = (Entity)response.Results["Entity"];
            Assert.Equal(300m, entity.GetAttributeValue<decimal>("totalrevenue"));
        }

        [Fact]
        public void RegisterCustomApi_MatchesByRequestName()
        {
            var service = new FakeOrganizationService();
            service.RegisterCustomApi("myorg_CustomAction", (req, svc) =>
            {
                var response = new OrganizationResponse();
                response.Results["Output"] = $"Hello, {req["Input"]}!";
                return response;
            });

            var request = new OrganizationRequest("myorg_CustomAction");
            request["Input"] = "World";

            var result = service.Execute(request);
            Assert.Equal("Hello, World!", result.Results["Output"]);
        }

        [Fact]
        public void RegisterCustomApi_CaseInsensitiveMatch()
        {
            var service = new FakeOrganizationService();
            service.RegisterCustomApi("myorg_Action", (req, svc) =>
            {
                return new OrganizationResponse();
            });

            var request = new OrganizationRequest("MYORG_ACTION");
            // Should not throw
            service.Execute(request);
        }

        [Fact]
        public void FileUpload_FullFlow_StoresBinaryData()
        {
            var service = new FakeOrganizationService();
            var recordId = service.Create(new Entity("annotation") { ["subject"] = "Test" });

            // Step 1: Initialize upload
            var initResponse = (InitializeFileBlocksUploadResponse)service.Execute(
                new InitializeFileBlocksUploadRequest
                {
                    Target = new EntityReference("annotation", recordId),
                    FileAttributeName = "documentbody"
                });

            var token = (string)initResponse.Results["FileContinuationToken"];
            Assert.False(string.IsNullOrEmpty(token));

            // Step 2: Upload blocks
            var block1 = new byte[] { 1, 2, 3, 4, 5 };
            var block2 = new byte[] { 6, 7, 8, 9, 10 };

            service.Execute(new UploadBlockRequest
            {
                FileContinuationToken = token,
                BlockData = block1,
                BlockId = "block1"
            });

            service.Execute(new UploadBlockRequest
            {
                FileContinuationToken = token,
                BlockData = block2,
                BlockId = "block2"
            });

            // Step 3: Commit
            var commitResponse = (CommitFileBlocksUploadResponse)service.Execute(
                new CommitFileBlocksUploadRequest
                {
                    FileContinuationToken = token,
                    FileName = "test.bin",
                    BlockList = new[] { "block1", "block2" }
                });

            // Verify the binary data was stored
            var stored = service.GetBinaryAttribute("annotation", recordId, "documentbody");
            Assert.NotNull(stored);
            Assert.Equal(10, stored!.Length);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, stored);
        }
    }
}
