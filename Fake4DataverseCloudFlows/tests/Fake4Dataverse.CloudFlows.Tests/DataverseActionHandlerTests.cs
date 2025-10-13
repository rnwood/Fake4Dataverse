using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for Dataverse connector action handler (Phase 3)
    /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
    /// </summary>
    public class DataverseActionHandlerTests
    {
        [Fact]
        public void Should_HandleCreateAction_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "create_contact_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "account",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "CreateContact",
                        DataverseActionType = DataverseActionType.Create,
                        EntityLogicalName = "contact",
                        Attributes = new Dictionary<string, object>
                        {
                            ["firstname"] = "John",
                            ["lastname"] = "Doe",
                            ["emailaddress1"] = "john.doe@example.com"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("create_contact_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);
            
            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("id", outputs.Keys);
            Assert.Contains("contactid", outputs.Keys);

            // Verify contact was created in context
            var contacts = context.CreateQuery("contact").ToList();
            Assert.Single(contacts);
            Assert.Equal("John", contacts[0]["firstname"]);
            Assert.Equal("Doe", contacts[0]["lastname"]);
        }

        [Fact]
        public void Should_HandleRetrieveAction_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane",
                ["lastname"] = "Smith",
                ["emailaddress1"] = "jane.smith@example.com"
            };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "retrieve_contact_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "RetrieveContact",
                        DataverseActionType = DataverseActionType.Retrieve,
                        EntityLogicalName = "contact",
                        EntityId = contactId
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("retrieve_contact_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            Assert.Equal("Jane", outputs["firstname"]);
            Assert.Equal("Smith", outputs["lastname"]);
            Assert.Equal("jane.smith@example.com", outputs["emailaddress1"]);
        }

        [Fact]
        public void Should_HandleUpdateAction_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "update_contact_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "UpdateContact",
                        DataverseActionType = DataverseActionType.Update,
                        EntityLogicalName = "contact",
                        EntityId = contactId,
                        Attributes = new Dictionary<string, object>
                        {
                            ["firstname"] = "Johnny",
                            ["emailaddress1"] = "johnny.doe@example.com"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("update_contact_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            
            // Verify contact was updated
            var updated = context.GetEntityById("contact", contactId);
            Assert.Equal("Johnny", updated["firstname"]);
            Assert.Equal("Doe", updated["lastname"]); // Unchanged
            Assert.Equal("johnny.doe@example.com", updated["emailaddress1"]);
        }

        [Fact]
        public void Should_HandleDeleteAction_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John"
            };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "delete_contact_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "DeleteContact",
                        DataverseActionType = DataverseActionType.Delete,
                        EntityLogicalName = "contact",
                        EntityId = contactId
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("delete_contact_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            
            // Verify contact was deleted
            Assert.False(context.ContainsEntity("contact", contactId));
        }

        [Fact]
        public void Should_HandleListRecordsAction_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Alice",
                ["lastname"] = "Adams"
            };
            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Bob",
                ["lastname"] = "Brown"
            };
            var contact3 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Charlie",
                ["lastname"] = "Clark"
            };
            context.Initialize(new[] { contact1, contact2, contact3 });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_contacts_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_contacts_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("value", outputs.Keys);
            Assert.Contains("count", outputs.Keys);
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(3, records.Count);
            Assert.Equal(3, outputs["count"]);
        }

        [Fact]
        public void Should_HandleListRecordsAction_WithOrdering()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Charlie",
                ["lastname"] = "Clark"
            };
            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Alice",
                ["lastname"] = "Adams"
            };
            var contact3 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Bob",
                ["lastname"] = "Brown"
            };
            context.Initialize(new[] { contact1, contact2, contact3 });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_ordered_contacts_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        OrderBy = "firstname asc"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_ordered_contacts_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var records = result.ActionResults[0].Outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal("Alice", records[0]["firstname"]);
            Assert.Equal("Bob", records[1]["firstname"]);
            Assert.Equal("Charlie", records[2]["firstname"]);
        }

        [Fact]
        public void Should_HandleListRecordsAction_WithTopLimit()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contacts = Enumerable.Range(1, 10).Select(i => new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact{i}"
            }).ToList();
            context.Initialize(contacts);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_top_contacts_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        Top = 3
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_top_contacts_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var records = result.ActionResults[0].Outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(3, records.Count);
        }

        [Fact]
        public void Should_ChainMultipleDataverseActions()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "chained_actions_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    // Create account
                    new DataverseAction
                    {
                        Name = "CreateAccount",
                        DataverseActionType = DataverseActionType.Create,
                        EntityLogicalName = "account",
                        Attributes = new Dictionary<string, object>
                        {
                            ["name"] = "Contoso Corp"
                        }
                    },
                    // Create related contact (would normally use CreateAccount output)
                    new DataverseAction
                    {
                        Name = "CreateContact",
                        DataverseActionType = DataverseActionType.Create,
                        EntityLogicalName = "contact",
                        Attributes = new Dictionary<string, object>
                        {
                            ["firstname"] = "John",
                            ["lastname"] = "Doe"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("chained_actions_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.ActionResults.Count);
            Assert.All(result.ActionResults, ar => Assert.True(ar.Succeeded));

            // Verify both records created
            var accounts = context.CreateQuery("account").ToList();
            var contacts = context.CreateQuery("contact").ToList();
            Assert.Single(accounts);
            Assert.Single(contacts);
        }

        [Fact]
        public void Should_HandleUploadFile_Successfully()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes
            // The UploadFile action uploads binary data to a file or image column.
            // Common use case: Uploading contact photos (entityimage column)
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create a contact to upload file to
            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            };
            context.Initialize(contact);

            // Create test file content
            var random = new Random();
            byte[] imageBytes = new byte[1024];
            random.NextBytes(imageBytes);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "upload_contact_photo_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "UploadPhoto",
                        DataverseActionType = DataverseActionType.UploadFile,
                        EntityLogicalName = "contact",
                        EntityId = contactId,
                        ColumnName = "entityimage",
                        FileContent = imageBytes,
                        FileName = "profile_photo.jpg"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("upload_contact_photo_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("success", outputs.Keys);
            Assert.True((bool)outputs["success"]);
            Assert.Contains("columnName", outputs.Keys);
            Assert.Equal("entityimage", outputs["columnName"]);
            Assert.Contains("fileName", outputs.Keys);
            Assert.Equal("profile_photo.jpg", outputs["fileName"]);
            Assert.Contains("fileSize", outputs.Keys);
            Assert.Equal(1024, outputs["fileSize"]);

            // Verify file was uploaded to entity
            var updatedContact = context.CreateQuery("contact").First(c => c.Id == contactId);
            Assert.Contains("entityimage", updatedContact.Attributes.Keys);
            var uploadedImage = updatedContact["entityimage"] as byte[];
            Assert.NotNull(uploadedImage);
            Assert.Equal(1024, uploadedImage.Length);
            Assert.Equal(imageBytes, uploadedImage);

            // Verify filename was stored
            Assert.Contains("entityimage_name", updatedContact.Attributes.Keys);
            Assert.Equal("profile_photo.jpg", updatedContact["entityimage_name"]);
        }

        [Fact]
        public void Should_HandleDownloadFile_Successfully()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes
            // The DownloadFile action retrieves binary data from a file or image column.
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create a contact with an image
            var contactId = Guid.NewGuid();
            var random = new Random();
            byte[] imageBytes = new byte[2048];
            random.NextBytes(imageBytes);

            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane",
                ["lastname"] = "Smith",
                ["entityimage"] = imageBytes,
                ["entityimage_name"] = "avatar.png"
            };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "download_contact_photo_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "DownloadPhoto",
                        DataverseActionType = DataverseActionType.DownloadFile,
                        EntityLogicalName = "contact",
                        EntityId = contactId,
                        ColumnName = "entityimage"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("download_contact_photo_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("success", outputs.Keys);
            Assert.True((bool)outputs["success"]);
            Assert.Contains("columnName", outputs.Keys);
            Assert.Equal("entityimage", outputs["columnName"]);
            Assert.Contains("fileName", outputs.Keys);
            Assert.Equal("avatar.png", outputs["fileName"]);
            Assert.Contains("fileSize", outputs.Keys);
            Assert.Equal(2048, outputs["fileSize"]);

            // Verify file content
            Assert.Contains("fileContent", outputs.Keys);
            var downloadedContent = outputs["fileContent"] as byte[];
            Assert.NotNull(downloadedContent);
            Assert.Equal(imageBytes, downloadedContent);

            // Verify base64 content (as returned by real Power Automate connector)
            Assert.Contains("$content", outputs.Keys);
            var base64Content = outputs["$content"] as string;
            Assert.NotNull(base64Content);
            var decodedBytes = Convert.FromBase64String(base64Content);
            Assert.Equal(imageBytes, decodedBytes);
        }

        [Fact]
        public void Should_ThrowException_WhenUploadFile_MissingEntityId()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "upload_without_id_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "UploadFile",
                        DataverseActionType = DataverseActionType.UploadFile,
                        EntityLogicalName = "contact",
                        ColumnName = "entityimage",
                        FileContent = new byte[100]
                        // Missing EntityId
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("upload_without_id_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("EntityId is required", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_ThrowException_WhenUploadFile_MissingColumnName()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact") { Id = contactId };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "upload_without_column_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "UploadFile",
                        DataverseActionType = DataverseActionType.UploadFile,
                        EntityLogicalName = "contact",
                        EntityId = contactId,
                        FileContent = new byte[100]
                        // Missing ColumnName
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("upload_without_column_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("ColumnName is required", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_ThrowException_WhenDownloadFile_ColumnNotFound()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John"
                // No entityimage column
            };
            context.Initialize(contact);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "download_missing_column_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "DownloadFile",
                        DataverseActionType = DataverseActionType.DownloadFile,
                        EntityLogicalName = "contact",
                        EntityId = contactId,
                        ColumnName = "entityimage"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("download_missing_column_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("does not exist or has no value", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_HandleListRecords_WithPaging()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#paging
            // The $skip query option enables paging through large result sets.
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create 10 contacts
            var contacts = Enumerable.Range(1, 10).Select(i => new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact{i}",
                ["lastname"] = "Test"
            }).ToArray();
            context.Initialize(contacts);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_contacts_with_paging_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        Top = 5,
                        Skip = 3  // Skip first 3 records
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_contacts_with_paging_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(5, records.Count);  // Top 5 after skipping 3
            Assert.Equal(5, outputs["count"]);
        }

        [Fact]
        public void Should_HandleListRecords_WithTotalCount()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#count
            // The $count query option returns the total count of records matching the filter.
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create 15 contacts
            var contacts = Enumerable.Range(1, 15).Select(i => new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact{i}"
            }).ToArray();
            context.Initialize(contacts);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_with_count_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        Top = 5,
                        IncludeTotalCount = true
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_with_count_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(5, records.Count);  // Page size
            Assert.Equal(5, outputs["count"]);  // Count in current page
            
            // Verify total count is included
            Assert.Contains("@odata.count", outputs.Keys);
            Assert.Equal(15, outputs["@odata.count"]);  // Total across all pages
        }

        [Fact]
        public void Should_HandleListRecords_WithNextLink()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#paging
            // When there are more records, @odata.nextLink is included for continuation.
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create 10 contacts
            var contacts = Enumerable.Range(1, 10).Select(i => new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact{i}"
            }).ToArray();
            context.Initialize(contacts);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_with_nextlink_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        Top = 4,
                        Skip = 0,
                        IncludeTotalCount = true
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_with_nextlink_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(4, records.Count);
            
            // Verify next link is present
            Assert.Contains("@odata.nextLink", outputs.Keys);
            var nextLink = outputs["@odata.nextLink"] as string;
            Assert.Contains("$skip=4", nextLink);  // Next page starts at record 4
        }

        [Fact]
        public void Should_HandleListRecords_WithoutNextLink_WhenNoMoreRecords()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Create exactly 5 contacts
            var contacts = Enumerable.Range(1, 5).Select(i => new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = $"Contact{i}"
            }).ToArray();
            context.Initialize(contacts);

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "list_last_page_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "ListContacts",
                        DataverseActionType = DataverseActionType.ListRecords,
                        EntityLogicalName = "contact",
                        Top = 5
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("list_last_page_flow", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            var outputs = result.ActionResults[0].Outputs;
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(5, records.Count);
            
            // Verify NO next link when all records fit in one page
            Assert.DoesNotContain("@odata.nextLink", outputs.Keys);
        }

        [Fact]
        public void Should_HandleMissingEntityId_ForRetrieve()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "retrieve_without_id_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "RetrieveContact",
                        DataverseActionType = DataverseActionType.Retrieve,
                        EntityLogicalName = "contact"
                        // Missing EntityId
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("retrieve_without_id_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("EntityId is required", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_BeRegisteredByDefault_InCloudFlowSimulator()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Act
            var handler = flowSimulator.GetConnectorHandler("Dataverse");

            // Assert
            Assert.NotNull(handler);
            Assert.IsType<DataverseActionHandler>(handler);
        }

        [Fact]
        public void Should_HandleDataverseAction_InFlowExecution()
        {
            // Arrange - Full integration test
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "full_integration_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "account",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "CreateTask",
                        DataverseActionType = DataverseActionType.Create,
                        EntityLogicalName = "task",
                        Attributes = new Dictionary<string, object>
                        {
                            ["subject"] = "Follow up on new account"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("full_integration_flow", 
                new Dictionary<string, object> { ["accountid"] = Guid.NewGuid() });

            // Assert
            Assert.True(result.Succeeded);
            Assert.Empty(result.Errors);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            // Verify task was created
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("Follow up on new account", tasks[0]["subject"]);
        }
    }
}
