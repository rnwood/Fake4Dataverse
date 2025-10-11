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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
        public void Should_ThrowException_ForUnsupportedActionType()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "unsupported_action_flow",
                Trigger = new DataverseTrigger(),
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "UploadFile",
                        DataverseActionType = DataverseActionType.UploadFile,
                        EntityLogicalName = "contact"
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var result = flowSimulator.SimulateTrigger("unsupported_action_flow", new Dictionary<string, object>());

            // Assert
            Assert.False(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.False(result.ActionResults[0].Succeeded);
            Assert.Contains("not yet implemented", result.ActionResults[0].ErrorMessage);
        }

        [Fact]
        public void Should_HandleMissingEntityId_ForRetrieve()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
