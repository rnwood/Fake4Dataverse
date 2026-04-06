using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class MessagingAndFileRequestTests
    {
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
        public void InstantiateTemplate_CreatesEmailFromTemplate()
        {
            var service = new FakeOrganizationService();
            var templateId = service.Create(new Entity("template") { ["subject"] = "Welcome {!contact:fullname;}", ["body"] = "Hello!" });
            var contactId = service.Create(new Entity("contact") { ["fullname"] = "John Doe" });

            var request = new OrganizationRequest("InstantiateTemplate");
            request["TemplateId"] = templateId;
            request["ObjectId"] = contactId;
            request["ObjectType"] = "contact";

            var response = service.Execute(request);
            var collection = (EntityCollection)response["EntityCollection"];
            Assert.Single(collection.Entities);
            Assert.NotNull(collection.Entities[0].GetAttributeValue<string>("subject"));
        }

        [Fact]
        public void SendEmailFromTemplate_CreatesEmail()
        {
            var service = new FakeOrganizationService();
            var templateId = service.Create(new Entity("template") { ["subject"] = "Welcome" });
            var contactId = service.Create(new Entity("contact") { ["fullname"] = "Test" });

            var request = new OrganizationRequest("SendEmailFromTemplate");
            request["TemplateId"] = templateId;
            request["RegardingId"] = contactId;
            request["RegardingType"] = "contact";

            var response = service.Execute(request);
            var emailId = (Guid)response["Id"];
            Assert.NotEqual(Guid.Empty, emailId);
        }

        [Fact]
        public void SendFax_ExecutesWithoutError()
        {
            var service = new FakeOrganizationService();
            var response = service.Execute(new OrganizationRequest("SendFax"));
            Assert.NotNull(response);
        }

        [Fact]
        public void DeleteFile_ExecutesWithoutError()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("DeleteFile");
            request["FileId"] = Guid.NewGuid();

            var response = service.Execute(request);
            Assert.NotNull(response);
        }

        [Fact]
        public void DownloadBlock_ReturnsEmptyData()
        {
            var service = new FakeOrganizationService();
            var request = new OrganizationRequest("DownloadBlock");
            request["FileContinuationToken"] = "token123";

            var response = service.Execute(request);
            Assert.NotNull(response["Data"]);
        }

        [Fact]
        public void FileUpload_FullFlow_StoresBinaryData()
        {
            var service = new FakeOrganizationService();
            var recordId = service.Create(new Entity("annotation") { ["subject"] = "Test" });

            var initResponse = (InitializeFileBlocksUploadResponse)service.Execute(
                new InitializeFileBlocksUploadRequest
                {
                    Target = new EntityReference("annotation", recordId),
                    FileAttributeName = "documentbody"
                });

            var token = (string)initResponse.Results["FileContinuationToken"];
            Assert.False(string.IsNullOrEmpty(token));

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

            service.Execute(new CommitFileBlocksUploadRequest
            {
                FileContinuationToken = token,
                FileName = "test.bin",
                BlockList = new[] { "block1", "block2" }
            });

            var stored = service.GetBinaryAttribute("annotation", recordId, "documentbody");
            Assert.NotNull(stored);
            Assert.Equal(10, stored!.Length);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, stored);
        }
    }
}
