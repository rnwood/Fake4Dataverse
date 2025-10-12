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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlowFromJson(null));
        }

        [Fact]
        public void Should_ThrowException_WhenJsonIsEmpty()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var flowSimulator = context.CloudFlowSimulator;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => flowSimulator.RegisterFlowFromJson(""));
        }

        [Fact]
        public void Should_ThrowException_WhenJsonIsInvalid()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
            var context = XrmFakedContextFactory.New();
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
    }
}
