using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for automatic Cloud Flow triggering on CRUD operations (Phase 4)
    /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
    /// </summary>
    public class AutomaticFlowTriggeringTests
    {
        [Fact]
        public void Should_TriggerFlow_WhenEntityIsCreated()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true; // Enable pipeline simulation
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "on_contact_create",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
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
                            ["subject"] = "Follow up with new contact"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            var contactId = service.Create(new Entity("contact")
            {
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            });

            // Assert
            flowSimulator.AssertFlowTriggered("on_contact_create");
            var results = flowSimulator.GetFlowExecutionResults("on_contact_create");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);

            // Verify task was created by the flow
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("Follow up with new contact", tasks[0]["subject"]);
        }

        [Fact]
        public void Should_TriggerFlow_WhenEntityIsUpdated()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            context.Initialize(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John"
            });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "on_contact_update",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Update"
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
                            ["subject"] = "Contact was updated"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Update(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane"
            });

            // Assert
            flowSimulator.AssertFlowTriggered("on_contact_update");
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
        }

        [Fact]
        public void Should_TriggerFlow_WhenEntityIsDeleted()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            context.Initialize(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John"
            });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "on_contact_delete",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Delete"
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
                            ["subject"] = "Contact was deleted"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Delete("contact", contactId);

            // Assert
            flowSimulator.AssertFlowTriggered("on_contact_delete");
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
        }

        [Fact]
        public void Should_NotTriggerFlow_WhenPipelineSimulationDisabled()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = false; // Disabled
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "disabled_pipeline_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            flowSimulator.AssertFlowNotTriggered("disabled_pipeline_flow");
        }

        [Fact]
        public void Should_TriggerFlow_WithFilteredAttributes_WhenAttributeIsModified()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger#trigger-conditions
            // Filtered attributes allow triggering only when specific attributes are modified
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            context.Initialize(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John",
                ["lastname"] = "Doe",
                ["emailaddress1"] = "john@example.com"
            });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "on_email_change",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Update",
                    FilteredAttributes = new List<string> { "emailaddress1" }
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
                            ["subject"] = "Email changed"
                        }
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Update(new Entity("contact")
            {
                Id = contactId,
                ["emailaddress1"] = "newemail@example.com"
            });

            // Assert
            flowSimulator.AssertFlowTriggered("on_email_change");
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
        }

        [Fact]
        public void Should_NotTriggerFlow_WithFilteredAttributes_WhenOtherAttributeIsModified()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            context.Initialize(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John",
                ["emailaddress1"] = "john@example.com"
            });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "on_email_change_only",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Update",
                    FilteredAttributes = new List<string> { "emailaddress1" }
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act - Update firstname, not emailaddress1
            var service = context.GetOrganizationService();
            service.Update(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane"
            });

            // Assert - Flow should NOT trigger
            flowSimulator.AssertFlowNotTriggered("on_email_change_only");
        }

        [Fact]
        public void Should_TriggerMultipleFlows_ForSameOperation()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flow1 = new CloudFlowDefinition
            {
                Name = "flow1",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            var flow2 = new CloudFlowDefinition
            {
                Name = "flow2",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flow1);
            flowSimulator.RegisterFlow(flow2);

            // Act
            var service = context.GetOrganizationService();
            service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            flowSimulator.AssertFlowTriggered("flow1");
            flowSimulator.AssertFlowTriggered("flow2");
        }

        [Fact]
        public void Should_NotTriggerFlow_WhenEntityNameDoesNotMatch()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "contact_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act - Create an account, not a contact
            var service = context.GetOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Contoso" });

            // Assert
            flowSimulator.AssertFlowNotTriggered("contact_flow");
        }

        [Fact]
        public void Should_NotTriggerFlow_WhenFlowIsDisabled()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "disabled_flow",
                IsEnabled = false, // Disabled
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            flowSimulator.AssertFlowNotTriggered("disabled_flow");
        }

        [Fact]
        public void Should_IncludeTriggerInputs_FromCreatedEntity()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "capture_inputs_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            var contactId = service.Create(new Entity("contact")
            {
                ["firstname"] = "John",
                ["lastname"] = "Doe"
            });

            // Assert
            var results = flowSimulator.GetFlowExecutionResults("capture_inputs_flow");
            Assert.Single(results);
            Assert.Equal("John", results[0].TriggerInputs["firstname"]);
            Assert.Equal("Doe", results[0].TriggerInputs["lastname"]);
            Assert.Contains("contactid", results[0].TriggerInputs.Keys);
        }

        [Fact]
        public void Should_TriggerFlow_WithCreateOrUpdateMessage_OnCreate()
        {
            // Arrange
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // CreateOrUpdate message triggers on both Create and Update operations
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "create_or_update_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "CreateOrUpdate"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            flowSimulator.AssertFlowTriggered("create_or_update_flow");
        }

        [Fact]
        public void Should_TriggerFlow_WithCreateOrUpdateMessage_OnUpdate()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            context.Initialize(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "John"
            });

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "create_or_update_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "CreateOrUpdate"
                },
                Actions = new List<IFlowAction>()
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act
            var service = context.GetOrganizationService();
            service.Update(new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane"
            });

            // Assert
            flowSimulator.AssertFlowTriggered("create_or_update_flow");
        }

        [Fact]
        public void Should_NotFailCRUDOperation_WhenFlowExecutionFails()
        {
            // Arrange
            // Reference: In real Dataverse, flows run asynchronously and don't fail the triggering operation
            var context = XrmFakedContextFactory.New();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowDefinition = new CloudFlowDefinition
            {
                Name = "failing_flow",
                Trigger = new DataverseTrigger
                {
                    EntityLogicalName = "contact",
                    Message = "Create"
                },
                Actions = new List<IFlowAction>
                {
                    new DataverseAction
                    {
                        Name = "InvalidAction",
                        DataverseActionType = DataverseActionType.Retrieve,
                        EntityLogicalName = "contact"
                        // Missing EntityId - will fail
                    }
                }
            };

            flowSimulator.RegisterFlow(flowDefinition);

            // Act - Should not throw even though flow will fail
            var service = context.GetOrganizationService();
            var contactId = service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert - Contact was created successfully despite flow failure
            Assert.NotEqual(Guid.Empty, contactId);
            var contact = context.GetEntityById("contact", contactId);
            Assert.NotNull(contact);

            // Flow was triggered but failed
            var results = flowSimulator.GetFlowExecutionResults("failing_flow");
            Assert.Single(results);
            Assert.False(results[0].Succeeded);
        }
    }
}
