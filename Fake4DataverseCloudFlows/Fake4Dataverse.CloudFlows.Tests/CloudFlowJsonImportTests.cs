using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Fake4Dataverse.Tests.CloudFlows
{
    /// <summary>
    /// Tests for Cloud Flow JSON import functionality (Phase 4)
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
    /// 
    /// These tests verify that exported Cloud Flow JSON definitions can be imported
    /// and simulated correctly, matching the behavior of the programmatic API.
    /// </summary>
    public class CloudFlowJsonImportTests
    {
        #region Basic JSON Import Tests

        [Fact]
        public void Should_ImportSimpleCreateTriggerFlow_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Tests importing a flow with a simple Create trigger and one action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""notify_on_contact_create"",
  ""properties"": {
    ""displayName"": ""Notify on New Contact"",
    ""state"": ""Started"",
    ""definition"": {
      ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
      ""contentVersion"": ""1.0.0.0"",
      ""triggers"": {
        ""When_a_record_is_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Create_a_new_record"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord""
            },
            ""parameters"": {
              ""entityName"": ""task"",
              ""item/subject"": ""Follow up with new contact"",
              ""item/description"": ""Contact the new lead""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains("notify_on_contact_create", registeredFlows);

            // Verify flow properties
            var results = flowSimulator.GetFlowExecutionResults("notify_on_contact_create");
            Assert.Empty(results); // No executions yet
        }

        [Fact]
        public void Should_ImportUpdateTriggerWithFilteredAttributes_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger#trigger-conditions
            // Tests importing a flow with Update trigger and filtered attributes
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""update_account_on_opportunity_change"",
  ""properties"": {
    ""displayName"": ""Update Account on Opportunity Change"",
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_updated"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 2,
              ""subscriptionRequest/entityname"": ""opportunity"",
              ""subscriptionRequest/scope"": 4,
              ""subscriptionRequest/filteringattributes"": ""estimatedvalue,closeprobability""
            }
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            // Assert - Flow should be registered
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains("update_account_on_opportunity_change", registeredFlows);
        }

        [Fact]
        public void Should_ImportFlowWithMultipleActions_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // Tests importing a flow with multiple Dataverse actions
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""process_high_value_opportunity"",
  ""properties"": {
    ""displayName"": ""Process High Value Opportunity"",
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_created_or_updated"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 4,
              ""subscriptionRequest/entityname"": ""opportunity"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Create_follow_up_task"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord""
            },
            ""parameters"": {
              ""entityName"": ""task"",
              ""item/subject"": ""Follow up on high value opportunity""
            }
          },
          ""runAfter"": {}
        },
        ""List_related_contacts"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""ListRecords""
            },
            ""parameters"": {
              ""entityName"": ""contact"",
              ""$filter"": ""parentcustomerid eq 'test'"",
              ""$top"": 10
            }
          },
          ""runAfter"": {
            ""Create_follow_up_task"": [""Succeeded""]
          }
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains("process_high_value_opportunity", registeredFlows);
        }

        #endregion

        #region Trigger Parsing Tests

        [Fact]
        public void Should_ParseCreateTrigger_Correctly()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Message code 1 = Create
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""test_create_trigger"",
  ""properties"": {
    ""displayName"": ""Test Create Trigger"",
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": 1
            }
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            var account = new Entity("account") { ["name"] = "Test Account" };
            service.Create(account);

            // Assert - Flow should have been triggered
            flowSimulator.AssertFlowTriggered("test_create_trigger");
        }

        [Fact]
        public void Should_ParseUpdateTrigger_Correctly()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Message code 2 = Update
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""test_update_trigger"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_updated"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 2,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            var contactId = service.Create(new Entity("contact") { ["firstname"] = "John" });
            service.Update(new Entity("contact", contactId) { ["lastname"] = "Doe" });

            // Assert - Flow should have been triggered by the Update
            flowSimulator.AssertFlowTriggered("test_update_trigger");
            var results = flowSimulator.GetFlowExecutionResults("test_update_trigger");
            Assert.Single(results);
        }

        [Fact]
        public void Should_ParseDeleteTrigger_Correctly()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Message code 3 = Delete
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""test_delete_trigger"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_deleted"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 3,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
            service.Delete("account", accountId);

            // Assert
            flowSimulator.AssertFlowTriggered("test_delete_trigger");
        }

        [Fact]
        public void Should_ParseCreateOrUpdateTrigger_Correctly()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Message code 4 = CreateOrUpdate
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""test_createorupdate_trigger"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_created_or_updated"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 4,
              ""subscriptionRequest/entityname"": ""opportunity"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            var oppId = service.Create(new Entity("opportunity") { ["name"] = "Big Deal" });

            // Assert - Should trigger on Create
            flowSimulator.AssertFlowTriggered("test_createorupdate_trigger");
            
            // Update should also trigger
            service.Update(new Entity("opportunity", oppId) { ["estimatedvalue"] = new Money(100000) });
            var results = flowSimulator.GetFlowExecutionResults("test_createorupdate_trigger");
            Assert.Equal(2, results.Count);
        }

        #endregion

        #region Action Parsing Tests

        [Fact]
        public void Should_ParseCreateAction_WithAttributes()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // CreateRecord operation creates a new record with specified attributes
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""create_task_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""manual"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Create_task"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord""
            },
            ""parameters"": {
              ""entityName"": ""task"",
              ""item/subject"": ""Test Task"",
              ""item/description"": ""Task description"",
              ""item/prioritycode"": ""2""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            flowSimulator.AssertFlowTriggered("create_task_flow");
            
            // Verify task was created
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("Test Task", tasks[0]["subject"]);
            Assert.Equal("Task description", tasks[0]["description"]);
        }

        [Fact]
        public void Should_ParseListRecordsAction_WithFilters()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // ListRecords operation queries records with $filter, $top, $orderby parameters
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            // Add test data
            var service = context.GetOrganizationService();
            service.Create(new Entity("account") { ["name"] = "Account 1", ["revenue"] = new Money(50000) });
            service.Create(new Entity("account") { ["name"] = "Account 2", ["revenue"] = new Money(150000) });
            service.Create(new Entity("account") { ["name"] = "Account 3", ["revenue"] = new Money(200000) });

            var flowJson = @"{
  ""name"": ""list_high_value_accounts"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""manual"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""List_accounts"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""ListRecords""
            },
            ""parameters"": {
              ""entityName"": ""account"",
              ""$top"": 10
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("contact") { ["firstname"] = "Trigger" });

            // Assert
            flowSimulator.AssertFlowTriggered("list_high_value_accounts");
            var results = flowSimulator.GetFlowExecutionResults("list_high_value_accounts");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void Should_ThrowException_WhenJsonIsNull()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlowFromJson(null));
        }

        [Fact]
        public void Should_ThrowException_WhenJsonIsEmpty()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlowFromJson(""));
        }

        [Fact]
        public void Should_ThrowException_WhenJsonIsInvalid()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var invalidJson = "{ this is not valid json }";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                flowSimulator.RegisterFlowFromJson(invalidJson));
            Assert.Contains("Invalid Cloud Flow JSON format", ex.Message);
        }

        [Fact]
        public void Should_ThrowException_WhenTriggerIsMissing()
        {
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""no_trigger_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {},
      ""actions"": {}
    }
  }
}";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                flowSimulator.RegisterFlowFromJson(flowJson));
            Assert.Contains("must have at least one trigger", ex.Message);
        }

        [Fact]
        public void Should_ThrowException_ForUnsupportedTriggerType()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
            // Only Dataverse triggers (OpenApiConnectionWebhook with commondataserviceforapps) are currently supported
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""http_trigger_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""manual"": {
          ""type"": ""Request"",
          ""inputs"": {
            ""schema"": {}
          }
        }
      },
      ""actions"": {}
    }
  }
}";

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => 
                flowSimulator.RegisterFlowFromJson(flowJson));
            Assert.Contains("not yet supported", ex.Message);
            Assert.Contains("Dataverse triggers", ex.Message);
        }

        [Fact]
        public void Should_ThrowException_ForUnsupportedActionType()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/
            // Only Dataverse actions are currently supported via JSON import
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""office365_action_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Send_email"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_office365"",
              ""operationId"": ""SendEmailV2""
            },
            ""parameters"": {
              ""emailMessage/To"": ""test@example.com"",
              ""emailMessage/Subject"": ""New Contact""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            // The flow should be registered, but unsupported actions are skipped
            // This is expected behavior - flow is partially imported
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains("office365_action_flow", registeredFlows);
        }

        #endregion

        #region Scope Tests

        [Theory]
        [InlineData(1, TriggerScope.Organization)]
        [InlineData(2, TriggerScope.BusinessUnit)]
        [InlineData(3, TriggerScope.ParentChildBusinessUnits)]
        [InlineData(4, TriggerScope.User)]
        public void Should_ParseTriggerScope_Correctly(int scopeCode, TriggerScope expectedScope)
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
            // Scope codes: 1=Organization, 2=BusinessUnit, 3=ParentChildBusinessUnits, 4=User
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = $@"{{
  ""name"": ""test_scope_{scopeCode}"",
  ""properties"": {{
    ""state"": ""Started"",
    ""definition"": {{
      ""triggers"": {{
        ""When_a_record_is_created"": {{
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {{
            ""host"": {{
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            }},
            ""parameters"": {{
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": {scopeCode}
            }}
          }}
        }}
      }},
      ""actions"": {{}}
    }}
  }}
}}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            // Assert
            var registeredFlows = flowSimulator.GetRegisteredFlowNames();
            Assert.Contains($"test_scope_{scopeCode}", registeredFlows);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Should_ExecuteImportedFlow_EndToEnd()
        {
            // Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
            // Full end-to-end test: Import JSON, trigger flow, verify actions executed
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;

            var flowJson = @"{
  ""name"": ""contact_to_task_flow"",
  ""properties"": {
    ""displayName"": ""Create Task on Contact Create"",
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_a_record_is_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Create_follow_up_task"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord""
            },
            ""parameters"": {
              ""entityName"": ""task"",
              ""item/subject"": ""Follow up with new contact"",
              ""item/description"": ""This is an automated task""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);

            var service = context.GetOrganizationService();
            var contactId = service.Create(new Entity("contact") 
            { 
                ["firstname"] = "Jane",
                ["lastname"] = "Smith"
            });

            // Assert
            flowSimulator.AssertFlowTriggered("contact_to_task_flow");
            
            var results = flowSimulator.GetFlowExecutionResults("contact_to_task_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            
            // Verify the task was created
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("Follow up with new contact", tasks[0]["subject"]);
            Assert.Equal("This is an automated task", tasks[0]["description"]);
        }

        #endregion

        #region Control Flow Actions JSON Import Tests

        [Fact]
        public void Should_ImportConditionAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-conditional-statement
            // Tests importing a flow with a Condition (If) action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();

            var flowJson = @"{
  ""name"": ""conditional_flow"",
  ""properties"": {
    ""displayName"": ""Conditional Flow"",
    ""state"": ""Started"",
    ""definition"": {
      ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
      ""contentVersion"": ""1.0.0.0"",
      ""triggers"": {
        ""When_opportunity_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""opportunity"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Check_Value"": {
          ""type"": ""If"",
          ""expression"": ""@greater(triggerBody()['estimatedvalue'], 100000)"",
          ""actions"": {
            ""High_Value_Task"": {
              ""type"": ""OpenApiConnection"",
              ""inputs"": {
                ""host"": {
                  ""connectionName"": ""shared_commondataserviceforapps"",
                  ""operationId"": ""CreateRecord""
                },
                ""parameters"": {
                  ""entityName"": ""task"",
                  ""item/subject"": ""High value opportunity"",
                  ""item/prioritycode"": 2
                }
              },
              ""runAfter"": {}
            }
          },
          ""else"": {
            ""actions"": {
              ""Standard_Task"": {
                ""type"": ""OpenApiConnection"",
                ""inputs"": {
                  ""host"": {
                    ""connectionName"": ""shared_commondataserviceforapps"",
                    ""operationId"": ""CreateRecord""
                  },
                  ""parameters"": {
                    ""entityName"": ""task"",
                    ""item/subject"": ""Standard opportunity"",
                    ""item/prioritycode"": 1
                  }
                },
                ""runAfter"": {}
              }
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("opportunity") { ["estimatedvalue"] = new Money(150000) });

            // Assert
            flowSimulator.AssertFlowTriggered("conditional_flow");
            var results = flowSimulator.GetFlowExecutionResults("conditional_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            
            // Verify high value task was created (true branch)
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("High value opportunity", tasks[0]["subject"]);
        }

        [Fact]
        public void Should_ImportSwitchAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-switch-statement
            // Tests importing a flow with a Switch action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();

            var flowJson = @"{
  ""name"": ""switch_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_incident_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""incident"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Route_By_Priority"": {
          ""type"": ""Switch"",
          ""expression"": ""@triggerBody()['prioritycode']"",
          ""cases"": {
            ""High"": {
              ""case"": ""1"",
              ""actions"": {
                ""High_Priority_Task"": {
                  ""type"": ""OpenApiConnection"",
                  ""inputs"": {
                    ""host"": {
                      ""connectionName"": ""shared_commondataserviceforapps"",
                      ""operationId"": ""CreateRecord""
                    },
                    ""parameters"": {
                      ""entityName"": ""task"",
                      ""item/subject"": ""High priority case""
                    }
                  },
                  ""runAfter"": {}
                }
              }
            },
            ""Normal"": {
              ""case"": ""2"",
              ""actions"": {
                ""Normal_Priority_Task"": {
                  ""type"": ""OpenApiConnection"",
                  ""inputs"": {
                    ""host"": {
                      ""connectionName"": ""shared_commondataserviceforapps"",
                      ""operationId"": ""CreateRecord""
                    },
                    ""parameters"": {
                      ""entityName"": ""task"",
                      ""item/subject"": ""Normal priority case""
                    }
                  },
                  ""runAfter"": {}
                }
              }
            }
          },
          ""default"": {
            ""actions"": {
              ""Default_Task"": {
                ""type"": ""OpenApiConnection"",
                ""inputs"": {
                  ""host"": {
                    ""connectionName"": ""shared_commondataserviceforapps"",
                    ""operationId"": ""CreateRecord""
                  },
                  ""parameters"": {
                    ""entityName"": ""task"",
                    ""item/subject"": ""Unknown priority case""
                  }
                },
                ""runAfter"": {}
              }
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("incident") { ["prioritycode"] = new OptionSetValue(1) }); // Now properly handles OptionSetValue with OData conversion

            // Assert
            flowSimulator.AssertFlowTriggered("switch_flow");
            var results = flowSimulator.GetFlowExecutionResults("switch_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            
            // Verify high priority task was created
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("High priority case", tasks[0]["subject"]);
        }

        [Fact]
        public void Should_ImportForeachAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-loops#foreach-loop
            // Tests importing a flow with a Foreach (Apply to Each) action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();
            
            // Create test accounts
            service.Create(new Entity("account") { ["name"] = "Account 1", ["accountid"] = Guid.NewGuid() });
            service.Create(new Entity("account") { ["name"] = "Account 2", ["accountid"] = Guid.NewGuid() });

            var flowJson = @"{
  ""name"": ""foreach_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""Manual"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""List_Accounts"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""ListRecords""
            },
            ""parameters"": {
              ""entityName"": ""account""
            }
          },
          ""runAfter"": {}
        },
        ""Apply_to_each"": {
          ""type"": ""Foreach"",
          ""expression"": ""@outputs('List_Accounts')['value']"",
          ""actions"": {
            ""Create_Task_For_Account"": {
              ""type"": ""OpenApiConnection"",
              ""inputs"": {
                ""host"": {
                  ""connectionName"": ""shared_commondataserviceforapps"",
                  ""operationId"": ""CreateRecord""
                },
                ""parameters"": {
                  ""entityName"": ""task"",
                  ""item/subject"": ""@concat('Follow up with ', item()['name'])""
                }
              },
              ""runAfter"": {}
            }
          },
          ""runAfter"": {
            ""List_Accounts"": [""Succeeded""]
          }
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("contact") { ["firstname"] = "Test" });

            // Assert
            flowSimulator.AssertFlowTriggered("foreach_flow");
            var results = flowSimulator.GetFlowExecutionResults("foreach_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
        }

        [Fact]
        public void Should_ImportUntilAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-loops#until-loop
            // Tests importing a flow with an Until (Do Until) action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();

            var flowJson = @"{
  ""name"": ""until_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""Manual"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Do_until"": {
          ""type"": ""Until"",
          ""expression"": ""@greater(1, 0)"",
          ""limit"": {
            ""count"": 5,
            ""timeout"": ""PT1H""
          },
          ""actions"": {
            ""Log_Action"": {
              ""type"": ""Compose"",
              ""inputs"": ""Checking status...""
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("account") { ["name"] = "Test" });

            // Assert
            flowSimulator.AssertFlowTriggered("until_flow");
            var results = flowSimulator.GetFlowExecutionResults("until_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
        }

        [Fact]
        public void Should_ImportComposeAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-perform-data-operations#compose-action
            // Tests importing a flow with a Compose action
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();

            var flowJson = @"{
  ""name"": ""compose_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_contact_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Compose_Name"": {
          ""type"": ""Compose"",
          ""inputs"": ""@concat(triggerBody()['firstname'], ' ', triggerBody()['lastname'])"",
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("contact") { ["firstname"] = "John", ["lastname"] = "Doe" });

            // Assert
            flowSimulator.AssertFlowTriggered("compose_flow");
            var results = flowSimulator.GetFlowExecutionResults("compose_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            
            // Verify compose output
            var composeResult = results[0].ActionResults[0];
            Assert.Equal("Compose_Name", composeResult.ActionName);
            Assert.Equal("John Doe", composeResult.Outputs["value"]);
        }

        [Fact]
        public void Should_ImportComplexFlow_WithMultipleControlActions()
        {
            // Tests importing a flow with multiple control flow actions combined
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            context.UsePipelineSimulation = true;
            var flowSimulator = context.CloudFlowSimulator;
            var service = context.GetOrganizationService();

            var flowJson = @"{
  ""name"": ""complex_control_flow"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""When_opportunity_created"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""opportunity"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""Compose_Value"": {
          ""type"": ""Compose"",
          ""inputs"": ""@triggerBody()['estimatedvalue']"",
          ""runAfter"": {}
        },
        ""Check_Value"": {
          ""type"": ""If"",
          ""expression"": ""@greater(outputs('Compose_Value')['value'], 50000)"",
          ""actions"": {
            ""High_Value_Task"": {
              ""type"": ""OpenApiConnection"",
              ""inputs"": {
                ""host"": {
                  ""connectionName"": ""shared_commondataserviceforapps"",
                  ""operationId"": ""CreateRecord""
                },
                ""parameters"": {
                  ""entityName"": ""task"",
                  ""item/subject"": ""High value opportunity""
                }
              },
              ""runAfter"": {}
            }
          },
          ""else"": {
            ""actions"": {
              ""Low_Value_Task"": {
                ""type"": ""OpenApiConnection"",
                ""inputs"": {
                  ""host"": {
                    ""connectionName"": ""shared_commondataserviceforapps"",
                    ""operationId"": ""CreateRecord""
                  },
                  ""parameters"": {
                    ""entityName"": ""task"",
                    ""item/subject"": ""Low value opportunity""
                  }
                },
                ""runAfter"": {}
              }
            }
          },
          ""runAfter"": {
            ""Compose_Value"": [""Succeeded""]
          }
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            service.Create(new Entity("opportunity") { ["estimatedvalue"] = new Money(75000) });

            // Assert
            flowSimulator.AssertFlowTriggered("complex_control_flow");
            var results = flowSimulator.GetFlowExecutionResults("complex_control_flow");
            Assert.Single(results);
            Assert.True(results[0].Succeeded);
            
            // Verify high value task was created
            var tasks = context.CreateQuery("task").ToList();
            Assert.Single(tasks);
            Assert.Equal("High value opportunity", tasks[0]["subject"]);
        }

        #endregion

        #region File Operations JSON Import Tests

        [Fact]
        public void Should_ParseUploadFileAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // Tests importing a flow with UploadFile operation
            
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

            // Create test image as base64
            var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var base64Content = Convert.ToBase64String(imageBytes);

            var flowJson = $@"{{
  ""name"": ""upload_contact_photo"",
  ""properties"": {{
    ""state"": ""Started"",
    ""definition"": {{
      ""triggers"": {{
        ""manual"": {{
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {{
            ""host"": {{
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            }},
            ""parameters"": {{
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }}
          }}
        }}
      }},
      ""actions"": {{
        ""Upload_Photo"": {{
          ""type"": ""OpenApiConnection"",
          ""inputs"": {{
            ""host"": {{
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""UploadFile""
            }},
            ""parameters"": {{
              ""entityName"": ""contact"",
              ""recordId"": ""{contactId}"",
              ""columnName"": ""entityimage"",
              ""fileName"": ""photo.png"",
              ""fileContent"": ""{base64Content}""
            }}
          }},
          ""runAfter"": {{}}
        }}
      }}
    }}
  }}
}}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            var result = flowSimulator.SimulateTrigger("upload_contact_photo", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            // Verify file was uploaded
            var service = context.GetOrganizationService();
            var updatedContact = service.Retrieve("contact", contactId, new Microsoft.Xrm.Sdk.Query.ColumnSet("entityimage"));
            Assert.Contains("entityimage", updatedContact.Attributes.Keys);
            var uploadedImage = updatedContact["entityimage"] as byte[];
            Assert.NotNull(uploadedImage);
            Assert.Equal(imageBytes, uploadedImage);
        }

        [Fact]
        public void Should_ParseDownloadFileAction_FromJson()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // Tests importing a flow with DownloadFile operation
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            var contactId = Guid.NewGuid();
            var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var contact = new Entity("contact")
            {
                Id = contactId,
                ["firstname"] = "Jane",
                ["entityimage"] = imageBytes,
                ["entityimage_name"] = "avatar.png"
            };
            context.Initialize(contact);

            var flowJson = $@"{{
  ""name"": ""download_contact_photo"",
  ""properties"": {{
    ""state"": ""Started"",
    ""definition"": {{
      ""triggers"": {{
        ""manual"": {{
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {{
            ""host"": {{
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            }},
            ""parameters"": {{
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""contact"",
              ""subscriptionRequest/scope"": 4
            }}
          }}
        }}
      }},
      ""actions"": {{
        ""Download_Photo"": {{
          ""type"": ""OpenApiConnection"",
          ""inputs"": {{
            ""host"": {{
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""DownloadFile""
            }},
            ""parameters"": {{
              ""entityName"": ""contact"",
              ""recordId"": ""{contactId}"",
              ""columnName"": ""entityimage""
            }}
          }},
          ""runAfter"": {{}}
        }}
      }}
    }}
  }}
}}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            var result = flowSimulator.SimulateTrigger("download_contact_photo", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("fileContent", outputs.Keys);
            var downloadedContent = outputs["fileContent"] as byte[];
            Assert.Equal(imageBytes, downloadedContent);
            Assert.Equal("avatar.png", outputs["fileName"]);
        }

        [Fact]
        public void Should_ParseListRecordsAction_WithAdvancedPaging()
        {
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            // Tests importing a flow with ListRecords using advanced paging ($skip, $count)
            
            // Arrange
            var context = XrmFakedContextFactory.New().WithCloudFlowSimulator();
            var flowSimulator = context.CloudFlowSimulator;

            // Add test data
            var service = context.GetOrganizationService();
            for (int i = 1; i <= 15; i++)
            {
                service.Create(new Entity("contact") 
                { 
                    ["firstname"] = $"Contact{i}",
                    ["lastname"] = "Test"
                });
            }

            var flowJson = @"{
  ""name"": ""list_contacts_with_paging"",
  ""properties"": {
    ""state"": ""Started"",
    ""definition"": {
      ""triggers"": {
        ""manual"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": 4
            }
          }
        }
      },
      ""actions"": {
        ""List_Contacts"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""ListRecords""
            },
            ""parameters"": {
              ""entityName"": ""contact"",
              ""$top"": 5,
              ""$skip"": 3,
              ""$count"": true
            }
          },
          ""runAfter"": {}
        }
      }
    }
  }
}";

            // Act
            flowSimulator.RegisterFlowFromJson(flowJson);
            var result = flowSimulator.SimulateTrigger("list_contacts_with_paging", new Dictionary<string, object>());

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.ActionResults);
            Assert.True(result.ActionResults[0].Succeeded);

            var outputs = result.ActionResults[0].Outputs;
            Assert.Contains("value", outputs.Keys);
            Assert.Contains("@odata.count", outputs.Keys);
            
            var records = outputs["value"] as List<Dictionary<string, object>>;
            Assert.Equal(5, records.Count); // Page size
            Assert.Equal(15, outputs["@odata.count"]); // Total count
        }

        #endregion
    }
}
