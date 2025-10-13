# Cloud Flow Simulation

## Overview

Cloud Flows (Power Automate flows) are an increasingly common integration pattern for Dataverse applications. The Cloud Flow simulation feature in Fake4Dataverse enables developers to test Dataverse-triggered flows, verify flow execution, and validate flow actions/outputs in unit tests.

**Status:** ✅ **Implemented** (October 12, 2025) - Phases 1-7 Complete, JSON Import Extended

**Test Coverage:** 157 unit tests, all passing ✅ (includes control flow actions and JSON import)

## Microsoft Documentation

Official references:
- [Cloud Flows Overview](https://learn.microsoft.com/en-us/power-automate/overview-cloud)
- [Dataverse Connector for Power Automate](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)
- [Trigger Conditions](https://learn.microsoft.com/en-us/power-automate/triggers-introduction)
- [Flow Definition Schema](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language)

## What are Cloud Flows?

Cloud Flows allow you to:
- Automate business processes triggered by Dataverse events
- Integrate Dataverse with external systems (Outlook, Teams, SharePoint, custom APIs)
- Execute complex multi-step workflows with branching logic
- Transform and manipulate data across systems

## Capabilities

The Cloud Flow simulation feature provides:
1. **Flow Registration** - Register flow definitions programmatically or from JSON
2. **JSON Import** - Import real Cloud Flow definitions exported from Power Automate ✅ **NEW**
3. **Expression Language** - Full Power Automate expression evaluation using Jint.net ✅ **NEW**
4. **Automatic Triggering** - Flows automatically trigger on Create/Update/Delete operations when `UsePipelineSimulation = true`
5. **Manual Triggering** - Manually simulate flow execution with `SimulateTrigger`
6. **Built-in Dataverse Connector** - Full CRUD support (Create, Retrieve, Update, Delete, ListRecords, Relate, Unrelate, ExecuteAction)
7. **Extensible Connector System** - Register custom handlers for Office 365, SharePoint, Teams, HTTP, and any custom connectors
8. **Comprehensive Verification** - Assert flows triggered, inspect execution results, examine action outputs
9. **Filtered Attributes** - Update triggers support filtered attributes (trigger only when specific fields change)
10. **Asynchronous Behavior** - Flow failures don't fail CRUD operations, matching real Dataverse behavior

## API Usage

### Expression Language Support ✅ **NEW**

Fake4Dataverse now supports Power Automate expression language for dynamic values in flow actions:

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "dynamic_task_creation",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "Create_Task",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "task",
            Attributes = new Dictionary<string, object>
            {
                // Expressions are evaluated automatically
                ["subject"] = "@concat('Follow up with ', triggerBody()['firstname'], ' ', triggerBody()['lastname'])",
                ["description"] = "@concat('Email: ', triggerBody()['emailaddress1'], ' | Phone: ', triggerBody()['telephone1'])",
                ["scheduledend"] = "@addDays(utcNow(), 7)"  // Due in 7 days
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

**Supported Functions:**
- Reference: `triggerOutputs()`, `triggerBody()`, `outputs('ActionName')`, `body('ActionName')`, `variables('varName')` ✅
- String: `concat()`, `substring()`, `slice()`, `replace()`, `toLower()`, `toUpper()`, `split()`, `trim()`, `guid()`, `nthIndexOf()`
- Logical: `equals()`, `greater()`, `less()`, `not()`, `empty()`, `xor()`
- Conversion: `string()`, `int()`, `float()`, `bool()`, `base64()`
- Collection: `first()`, `last()`, `take()`, `skip()`, `join()`, `reverse()`, `createArray()`, `flatten()`
- Date/Time: `utcNow()`, `addDays()`, `addHours()`, `formatDateTime()`, `startOfDay()`, `getPastTime()`, `getFutureTime()`
- Math: `add()`, `sub()`, `mul()`, `div()`, `min()`, `max()`
- Type Checking: `isInt()`, `isFloat()`, `isString()`, `isArray()`, `isObject()` ✅ **NEW**
- URI: `uriComponent()`, `uriHost()`, `uriPath()`, `uriQuery()`, `uriScheme()` ✅ **NEW**

**Total: 80+ functions implemented**

**See:** [Expression Language Reference](../expression-language.md) for complete documentation and examples.

### Flow Registration

#### Register a Flow Programmatically
```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Enable automatic triggering
var flowSimulator = context.CloudFlowSimulator;

// Register a flow definition programmatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "notify_on_contact_create",
    DisplayName = "Notify on New Contact",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Create",
        Scope = TriggerScope.Organization
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
                ["subject"] = "Follow up with new contact",
                ["description"] = "Contact the new lead"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

#### Register Multiple Flows
```csharp
var flows = new List<ICloudFlowDefinition>
{
    contactCreateFlow,
    accountUpdateFlow,
    opportunityDeleteFlow
};

flowSimulator.RegisterFlows(flows);
```

#### Unregister Flows
```csharp
// Unregister a specific flow
flowSimulator.UnregisterFlow("notify_on_contact_create");

// Clear all flows
flowSimulator.ClearAllFlows();
```

#### Register a Flow from JSON ✅ **NEW**

You can import real Cloud Flow definitions exported from Power Automate:

```csharp
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true;
var flowSimulator = context.CloudFlowSimulator;

// Export your flow from Power Automate as JSON
// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
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

// Import and register the flow
flowSimulator.RegisterFlowFromJson(flowJson);

// The flow works exactly like a programmatically registered flow
var service = context.GetOrganizationService();
service.Create(new Entity("contact") { ["firstname"] = "Jane" });

flowSimulator.AssertFlowTriggered("notify_on_contact_create");
```

**Supported JSON Features:**
- **Triggers:** Dataverse triggers (Create, Update, Delete, CreateOrUpdate)
- **Trigger Scopes:** Organization, BusinessUnit, ParentChildBusinessUnits, User
- **Filtered Attributes:** Update triggers with specific attribute filtering
- **Actions:** Dataverse actions (CreateRecord, UpdateRecord, DeleteRecord, GetItem, ListRecords)
- **Control Flow:** Condition (If), Switch, Foreach (Apply to Each), Until (Do Until) ✅ **NEW**
- **Data Operations:** Compose ✅ **NEW**
- **Action Parameters:** Entity names, attributes, filters, ordering, top
- **Expression Language:** Full Power Automate expression evaluation ✅ **NEW**
- **OData Type Conversion:** Automatic conversion of OData/REST API types to SDK types ✅ **NEW**

**Limitations:**
- Non-Dataverse connectors require custom handlers via `RegisterConnectorActionHandler`
- Scope actions (Try/Catch/Finally) not yet supported
- Some advanced connector-specific features may require custom handlers

**OData Conventions (Automatically Handled):**

When importing flows from JSON, the Dataverse connector uses OData/Web API conventions:
- **OptionSet values** are integers in JSON but automatically converted to `OptionSetValue` objects
- **Money values** are decimals in JSON but automatically converted to `Money` objects  
- **EntityReferences** use `@odata.bind` notation (e.g., `"accounts(guid)"`) and are converted to `EntityReference` objects
- **DateTime values** are ISO 8601 strings and converted to `DateTime` objects

These conversions happen automatically, so you can use real Power Automate JSON exports without modification.
Expressions also automatically unwrap SDK types (e.g., `@triggerBody()['prioritycode']` returns the integer value from an OptionSetValue).

**How to Export a Flow from Power Automate:**
1. Open your Cloud Flow in Power Automate
2. Click the flow menu (three dots) → Export → Package (.zip)
3. Extract the zip file
4. Find the `definition.json` file inside
5. Use that JSON with `RegisterFlowFromJson`



### Automatic Flow Triggering

Flows automatically trigger when matching CRUD operations occur:

```csharp
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Required for automatic triggering
var flowSimulator = context.CloudFlowSimulator;

// Register flow
flowSimulator.RegisterFlow(new CloudFlowDefinition
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
            Attributes = { ["subject"] = "Follow up" }
        }
    }
});

// Flow triggers automatically on Create!
var service = context.GetOrganizationService();
var contactId = service.Create(new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
});

// Verify flow was triggered
flowSimulator.AssertFlowTriggered("on_contact_create");

// Verify task was created by the flow
var tasks = context.CreateQuery("task").ToList();
Assert.Single(tasks);
Assert.Equal("Follow up", tasks[0]["subject"]);
```

#### Filtered Attributes (Update Triggers Only)
```csharp
// Flow only triggers when specific attributes are modified
var flowDefinition = new CloudFlowDefinition
{
    Name = "on_email_change",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "Update",
        FilteredAttributes = new List<string> { "emailaddress1", "emailaddress2" }
    },
    Actions = { /* ... */ }
};

flowSimulator.RegisterFlow(flowDefinition);

// This update triggers the flow (email changed)
contact["emailaddress1"] = "newemail@example.com";
service.Update(contact);
flowSimulator.AssertFlowTriggered("on_email_change");

// This update does NOT trigger the flow (name changed, not email)
contact["firstname"] = "Jane";
service.Update(contact);
flowSimulator.AssertFlowExecutionCount("on_email_change", 1); // Still 1
```

#### CreateOrUpdate Message
```csharp
// Flow triggers on both Create AND Update operations
var flowDefinition = new CloudFlowDefinition
{
    Name = "on_contact_create_or_update",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "contact",
        Message = "CreateOrUpdate"
    },
    Actions = { /* ... */ }
};

flowSimulator.RegisterFlow(flowDefinition);

// Both operations trigger the flow
service.Create(contact);  // Triggers flow
service.Update(contact);  // Also triggers flow
Assert.Equal(2, flowSimulator.GetFlowExecutionCount("on_contact_create_or_update"));
```

### Manual Trigger Simulation

You can also manually trigger a flow without performing a CRUD operation:

```csharp
// Manually trigger a flow with specific inputs
var triggerInputs = new Dictionary<string, object>
{
    ["contactid"] = Guid.NewGuid(),
    ["firstname"] = "Jane",
    ["lastname"] = "Smith",
    ["emailaddress1"] = "jane.smith@example.com"
};

var result = flowSimulator.SimulateTrigger("on_contact_create", triggerInputs);

// Check result
Assert.True(result.Succeeded);
Assert.Empty(result.Errors);
Assert.NotEmpty(result.ActionResults);
```

### Connector Action Handling

#### Dataverse Connector Actions (Built-in)
```csharp
// Dataverse connector actions execute against the fake context automatically
var flowDefinition = new CloudFlowDefinition
{
    Name = "create_related_records",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        // This action will execute against the fake context
        new DataverseAction
        {
            Name = "CreateContact",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "contact",
            Attributes = new Dictionary<string, object>
            {
                ["firstname"] = "Default"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Create account triggers flow, which creates contact
var service = context.GetOrganizationService();
var account = new Entity("account") { ["name"] = "Contoso" };
var accountId = service.Create(account);

// Verify contact was created by the flow
var contacts = context.CreateQuery("contact")
    .Where(c => c.GetAttributeValue<string>("firstname") == "Default")
    .ToList();

Assert.Single(contacts);
```

**Supported Dataverse Action Types:**
- `Create` - Create new records
- `Retrieve` - Retrieve a single record by ID
- `Update` - Update existing records
- `Delete` - Delete records
- `ListRecords` - Query multiple records with filtering, ordering, paging, and total count ✅ **ENHANCED**
- `Relate` - Associate records (many-to-many or one-to-many)
- `Unrelate` - Disassociate records
- `ExecuteAction` - Execute custom actions or custom APIs
- `UploadFile` - Upload files or images to entity columns ✅ **NEW**
- `DownloadFile` - Download files or images from entity columns ✅ **NEW**

#### File Operations (UploadFile & DownloadFile) ✅ **NEW**

The `UploadFile` and `DownloadFile` actions simulate file and image column operations in Dataverse. These are commonly used for uploading contact photos, document attachments, or any binary data.

**Reference:** [File Attributes in Dataverse](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes)

##### UploadFile Example
```csharp
// Upload a contact photo
var contactId = Guid.NewGuid();
var contact = new Entity("contact")
{
    Id = contactId,
    ["firstname"] = "John",
    ["lastname"] = "Doe"
};
context.Initialize(contact);

// Read file content (e.g., from disk)
byte[] imageBytes = File.ReadAllBytes("profile_photo.jpg");

var flowDefinition = new CloudFlowDefinition
{
    Name = "upload_contact_photo",
    Trigger = new DataverseTrigger(),
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "UploadPhoto",
            DataverseActionType = DataverseActionType.UploadFile,
            EntityLogicalName = "contact",
            EntityId = contactId,
            ColumnName = "entityimage",  // Image column name
            FileContent = imageBytes,     // Byte array
            FileName = "profile_photo.jpg" // Optional filename
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
var result = flowSimulator.SimulateTrigger("upload_contact_photo", new Dictionary<string, object>());

// Verify upload succeeded
Assert.True(result.Succeeded);

// Verify file was stored in entity
var updatedContact = context.CreateQuery("contact").First(c => c.Id == contactId);
Assert.Contains("entityimage", updatedContact.Attributes.Keys);
var uploadedImage = updatedContact["entityimage"] as byte[];
Assert.NotNull(uploadedImage);
Assert.Equal(imageBytes.Length, uploadedImage.Length);
```

##### DownloadFile Example
```csharp
// Download a contact photo
var contactId = Guid.NewGuid();
byte[] originalImageBytes = File.ReadAllBytes("avatar.png");

var contact = new Entity("contact")
{
    Id = contactId,
    ["firstname"] = "Jane",
    ["entityimage"] = originalImageBytes,
    ["entityimage_name"] = "avatar.png"
};
context.Initialize(contact);

var flowDefinition = new CloudFlowDefinition
{
    Name = "download_contact_photo",
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
var result = flowSimulator.SimulateTrigger("download_contact_photo", new Dictionary<string, object>());

// Verify download succeeded
Assert.True(result.Succeeded);
var outputs = result.ActionResults[0].Outputs;

// Get file content (raw byte array for testing)
var downloadedContent = outputs["fileContent"] as byte[];
Assert.Equal(originalImageBytes, downloadedContent);

// Get base64-encoded content (as returned by real Power Automate)
var base64Content = outputs["$content"] as string;
var decodedBytes = Convert.FromBase64String(base64Content);
Assert.Equal(originalImageBytes, decodedBytes);

// Get metadata
Assert.Equal("avatar.png", outputs["fileName"]);
Assert.Equal(originalImageBytes.Length, outputs["fileSize"]);
```

**Supported File Column Types:**
- Image columns (e.g., `entityimage` on contact, account, etc.)
- File columns (custom file columns created in Dataverse)

**Notes:**
- File content is stored as byte arrays in entity attributes
- Filename is stored in a separate `{columnname}_name` attribute if provided
- DownloadFile returns both raw byte array and base64-encoded content for flexibility
- In real Power Automate, file content is always base64-encoded in the `$content` property

#### Advanced ListRecords Features ✅ **ENHANCED**

The `ListRecords` action now supports advanced paging and total count features for working with large datasets.

**Reference:** [Query Data with Web API](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api)

##### Paging with Skip and Top
```csharp
// Create 20 contacts
var contacts = Enumerable.Range(1, 20).Select(i => new Entity("contact")
{
    Id = Guid.NewGuid(),
    ["firstname"] = $"Contact{i}",
    ["lastname"] = "Test"
}).ToArray();
context.Initialize(contacts);

// Get page 2 (skip 10, take 5)
var flowDefinition = new CloudFlowDefinition
{
    Name = "list_contacts_page_2",
    Trigger = new DataverseTrigger(),
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "ListContacts",
            DataverseActionType = DataverseActionType.ListRecords,
            EntityLogicalName = "contact",
            Top = 5,      // Page size
            Skip = 10     // Skip first 10 records
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
var result = flowSimulator.SimulateTrigger("list_contacts_page_2", new Dictionary<string, object>());

var outputs = result.ActionResults[0].Outputs;
var records = outputs["value"] as List<Dictionary<string, object>>;
Assert.Equal(5, records.Count);  // Got 5 records
```

##### Total Count with @odata.count
```csharp
// Get total count of records across all pages
var flowDefinition = new CloudFlowDefinition
{
    Name = "list_with_count",
    Trigger = new DataverseTrigger(),
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "ListContacts",
            DataverseActionType = DataverseActionType.ListRecords,
            EntityLogicalName = "contact",
            Top = 5,
            IncludeTotalCount = true  // Request total count
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
var result = flowSimulator.SimulateTrigger("list_with_count", new Dictionary<string, object>());

var outputs = result.ActionResults[0].Outputs;
Assert.Equal(5, outputs["count"]);        // Records in this page
Assert.Equal(20, outputs["@odata.count"]); // Total records across all pages
```

##### Continuation with @odata.nextLink
```csharp
// When there are more records, @odata.nextLink indicates pagination
var outputs = result.ActionResults[0].Outputs;

if (outputs.ContainsKey("@odata.nextLink"))
{
    var nextLink = outputs["@odata.nextLink"] as string;
    // Contains "?$skip=5" to get next page
    // In production, you'd parse this and make another request
}
```

**ListRecords Capabilities:**
- ✅ `Filter` - Basic OData filter expressions (simple equality: `field eq 'value'`)
- ✅ `OrderBy` - Sort by attribute (e.g., "createdon desc")
- ✅ `Top` - Maximum records to return per page
- ✅ `Skip` - Skip records for paging (offset-based)
- ✅ `IncludeTotalCount` - Include total count with @odata.count
- ✅ `@odata.nextLink` - Automatic generation when more records available
- ⚠️ `Expand` - Navigation property expansion (placeholder for future enhancement)

**Advanced OData Filter Support:**

For complex OData filter expressions with operators and functions (e.g., `revenue gt 100000 and contains(name, 'Corp')`), 
use the **REST API endpoints** (`/api/data/v9.2`) which leverage Microsoft.AspNetCore.OData v9.4.0 for full OData v4.0 compliance.

The REST API provides automatic parsing of complex filter expressions including:
- Comparison operators: `eq`, `ne`, `gt`, `lt`, `ge`, `le`
- Logical operators: `and`, `or`, `not`
- String functions: `contains()`, `startswith()`, `endswith()`
- Date/time functions and arithmetic operations

**Reference:** [REST/OData API Documentation](../rest-api.md)

Cloud flows using complex filters can:
1. Call the REST API endpoints directly from the flow
2. Use the Fake4DataverseService REST endpoints for testing
3. Implement custom filtering logic using QueryExpression conditions

**Shared OData Implementation:**

Both the Cloud Flows connector and REST API endpoints share the same `ODataEntityConverter` 
and `ODataValueConverter` classes for consistent type conversion between OData JSON and SDK types.
This ensures that OptionSet values, Money fields, EntityReferences, and DateTime values are 
handled identically in both contexts.

**Notes:**
- Basic filter parsing (simple equality checks) is implemented for cloud flows
- Complex OData filter parsing requires the Microsoft.AspNetCore.OData library (REST API only)
- For cloud flows needing advanced filtering, consider using the REST API or QueryExpression

#### Custom Connector Actions (Extensibility)
```csharp
// Implement a custom connector handler
public class Office365EmailHandler : IConnectorActionHandler
{
    public string ConnectorType => "Office365";
    public List<EmailCapture> EmailsSent { get; } = new List<EmailCapture>();

    public IDictionary<string, object> Execute(
        IFlowAction action,
        IXrmFakedContext context,
        IFlowExecutionContext flowContext)
    {
        // Extract email parameters from action
        var emailAction = action as CustomFlowAction;
        var to = emailAction?.Parameters.GetValueOrDefault("To") as string;
        var subject = emailAction?.Parameters.GetValueOrDefault("Subject") as string;
        var body = emailAction?.Parameters.GetValueOrDefault("Body") as string;

        // Mock sending email - capture for verification
        EmailsSent.Add(new EmailCapture
        {
            To = to,
            Subject = subject,
            Body = body
        });

        // Return success with outputs
        return new Dictionary<string, object>
        {
            ["StatusCode"] = 200,
            ["MessageId"] = Guid.NewGuid().ToString()
        };
    }
}

public class EmailCapture
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}

// Register the handler
var emailHandler = new Office365EmailHandler();
flowSimulator.RegisterConnectorActionHandler("Office365", emailHandler);

// Define a flow that uses the custom connector
var flowDefinition = new CloudFlowDefinition
{
    Name = "send_email_on_account_create",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new CustomFlowAction
        {
            Name = "SendEmail",
            ConnectorType = "Office365",
            Parameters = new Dictionary<string, object>
            {
                ["To"] = "sales@example.com",
                ["Subject"] = "New Account Created",
                ["Body"] = "A new account was created"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Create account - flow triggers and "sends" email
service.Create(new Entity("account") { ["name"] = "Contoso" });

// Verify email was "sent"
Assert.Single(emailHandler.EmailsSent);
Assert.Equal("sales@example.com", emailHandler.EmailsSent[0].To);
Assert.Contains("New Account", emailHandler.EmailsSent[0].Subject);
```

**Note:** You'll need to create a `CustomFlowAction` class that implements `IFlowAction` for non-Dataverse connectors. The framework provides `DataverseAction` built-in, but custom connectors require your own action implementation.

### Verification APIs

```csharp
// Assert a flow was triggered
flowSimulator.AssertFlowTriggered("on_contact_create");

// Assert a flow was NOT triggered
flowSimulator.AssertFlowNotTriggered("on_account_update");

// Get execution count
int count = flowSimulator.GetFlowExecutionCount("on_contact_create");
Assert.Equal(3, count);

// Get flow execution results
var results = flowSimulator.GetFlowExecutionResults("on_contact_create");
Assert.Equal(3, results.Count);

foreach (var result in results)
{
    Assert.True(result.Succeeded);
    Assert.Empty(result.Errors);
    
    // Inspect trigger inputs
    var contactId = result.TriggerInputs["contactid"];
    var firstname = result.TriggerInputs["firstname"];
    
    // Inspect action outputs
    var taskCreated = result.ActionResults
        .FirstOrDefault(a => a.ActionName == "CreateTask");
    
    if (taskCreated != null)
    {
        Assert.True(taskCreated.Succeeded);
        Assert.NotNull(taskCreated.Outputs);
    }
}
```

### Control Flow Actions ✅ **NEW**

The Cloud Flow simulator now supports all major control flow actions for conditional logic, branching, and loops.

#### Condition Actions (If/Then/Else)

Condition actions evaluate an expression and execute different branches based on the result.

**Reference:** [Use expressions in conditions](https://learn.microsoft.com/en-us/power-automate/use-expressions-in-conditions)

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "conditional_processing",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "opportunity",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new ConditionAction
        {
            Name = "Check_Value",
            Expression = "@greater(triggerBody()['estimatedvalue'], 100000)",
            TrueActions = new List<IFlowAction>
            {
                new DataverseAction
                {
                    Name = "Create_High_Value_Task",
                    DataverseActionType = DataverseActionType.Create,
                    EntityLogicalName = "task",
                    Attributes = new Dictionary<string, object>
                    {
                        ["subject"] = "High value opportunity - immediate follow-up required",
                        ["prioritycode"] = 2 // High priority
                    }
                }
            },
            FalseActions = new List<IFlowAction>
            {
                new DataverseAction
                {
                    Name = "Create_Standard_Task",
                    DataverseActionType = DataverseActionType.Create,
                    EntityLogicalName = "task",
                    Attributes = new Dictionary<string, object>
                    {
                        ["subject"] = "Standard opportunity follow-up",
                        ["prioritycode"] = 1 // Normal priority
                    }
                }
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Test high value scenario
var highValueOpp = new Entity("opportunity") 
{ 
    ["estimatedvalue"] = 150000.0m 
};
service.Create(highValueOpp);

// Verify the high value branch executed
var results = flowSimulator.GetFlowExecutionResults("conditional_processing");
Assert.True(results[0].Succeeded);
var conditionResult = results[0].ActionResults[0];
Assert.True((bool)conditionResult.Outputs["conditionResult"]);
Assert.Equal("true", conditionResult.Outputs["branchExecuted"]);
```

**Key Features:**
- Supports all expression functions for condition evaluation
- Nested conditions supported (conditions within conditions)
- Multiple actions can execute in each branch
- Branch results tracked in outputs

#### Switch Actions (Multi-Case Branching)

Switch actions evaluate an expression and execute the matching case's actions, with a default case for unmatched values.

**Reference:** [Use the Switch action](https://learn.microsoft.com/en-us/power-automate/use-switch-action)

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "route_by_priority",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "incident",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new SwitchAction
        {
            Name = "Route_By_Priority",
            Expression = "@triggerBody()['prioritycode']",
            Cases = new Dictionary<string, IList<IFlowAction>>
            {
                ["1"] = new List<IFlowAction> // High
                {
                    new ComposeAction
                    {
                        Name = "High_Priority_Processing",
                        Inputs = "Escalate to senior support immediately"
                    }
                },
                ["2"] = new List<IFlowAction> // Normal
                {
                    new ComposeAction
                    {
                        Name = "Normal_Priority_Processing",
                        Inputs = "Assign to support queue"
                    }
                },
                ["3"] = new List<IFlowAction> // Low
                {
                    new ComposeAction
                    {
                        Name = "Low_Priority_Processing",
                        Inputs = "Schedule for next available agent"
                    }
                }
            },
            DefaultActions = new List<IFlowAction>
            {
                new ComposeAction
                {
                    Name = "Default_Processing",
                    Inputs = "Unknown priority - assign to triage"
                }
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Test high priority case
var highPriorityCase = new Entity("incident") { ["prioritycode"] = 1 };
service.Create(highPriorityCase);

// Verify correct case executed
var results = flowSimulator.GetFlowExecutionResults("route_by_priority");
var switchResult = results[0].ActionResults[0];
Assert.Equal("1", switchResult.Outputs["switchValue"]);
Assert.Equal("1", switchResult.Outputs["matchedCase"]);
```

**Key Features:**
- String-based case matching (case-insensitive)
- Default case for unmatched values
- Multiple actions per case
- Case execution tracked in outputs

#### Parallel Branch Actions

Parallel branch actions execute multiple independent action sequences. In simulation, branches execute sequentially but are logically independent.

**Reference:** [Add parallel branches](https://learn.microsoft.com/en-us/power-automate/use-parallel-branches)

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "parallel_notifications",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new ParallelBranchAction
        {
            Name = "Send_Notifications",
            Branches = new List<ParallelBranch>
            {
                new ParallelBranch
                {
                    Name = "Email_Branch",
                    Actions = new List<IFlowAction>
                    {
                        new ComposeAction
                        {
                            Name = "Compose_Email",
                            Inputs = "@concat('New account: ', triggerBody()['name'])"
                        },
                        // In real flow, this would send email
                        new ComposeAction
                        {
                            Name = "Send_Email",
                            Inputs = "Email sent"
                        }
                    }
                },
                new ParallelBranch
                {
                    Name = "Teams_Branch",
                    Actions = new List<IFlowAction>
                    {
                        new ComposeAction
                        {
                            Name = "Compose_Teams_Message",
                            Inputs = "@concat('🎉 New account created: ', triggerBody()['name'])"
                        },
                        // In real flow, this would post to Teams
                        new ComposeAction
                        {
                            Name = "Post_To_Teams",
                            Inputs = "Teams notification sent"
                        }
                    }
                },
                new ParallelBranch
                {
                    Name = "Database_Branch",
                    Actions = new List<IFlowAction>
                    {
                        new DataverseAction
                        {
                            Name = "Create_Log_Entry",
                            DataverseActionType = DataverseActionType.Create,
                            EntityLogicalName = "audit",
                            Attributes = new Dictionary<string, object>
                            {
                                ["message"] = "Account created notification sent"
                            }
                        }
                    }
                }
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Test parallel execution
var account = new Entity("account") { ["name"] = "Contoso Ltd" };
service.Create(account);

// Verify all branches executed
var results = flowSimulator.GetFlowExecutionResults("parallel_notifications");
var parallelResult = results[0].ActionResults[0];
var branchResults = parallelResult.Outputs["branchResults"] as List<Dictionary<string, object>>;
Assert.Equal(3, branchResults.Count); // All 3 branches executed
```

**Key Features:**
- Multiple independent branches
- Each branch can contain multiple sequential actions
- All branches must complete for success
- Branch results tracked in outputs
- Simulates parallel execution (actually sequential for deterministic testing)

#### Do Until Loops

Do Until loops repeatedly execute actions until a condition becomes true, with safeguards against infinite loops.

**Reference:** [Use Do Until loops](https://learn.microsoft.com/en-us/power-automate/do-until-loop)

```csharp
var flowDefinition = new CloudFlowDefinition
{
    Name = "poll_for_completion",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Create"
    },
    Actions = new List<IFlowAction>
    {
        new DoUntilAction
        {
            Name = "Wait_For_Approval",
            Expression = "@equals(outputs('Check_Status')['value'], 'Approved')",
            MaxIterations = 10,
            Timeout = "PT1H", // 1 hour (for documentation)
            Actions = new List<IFlowAction>
            {
                new DataverseAction
                {
                    Name = "Check_Status",
                    DataverseActionType = DataverseActionType.Retrieve,
                    EntityLogicalName = "approval",
                    EntityId = "@triggerBody()['approvalid']"
                },
                new ComposeAction
                {
                    Name = "Log_Check",
                    Inputs = "Checking approval status..."
                }
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);
```

**Key Features:**
- Condition checked AFTER each iteration (do-while pattern)
- Maximum iteration limit (default: 60, configurable)
- Timeout specification (for documentation)
- Multiple actions per iteration
- Iteration count and results tracked in outputs

**Important:** The condition is evaluated AFTER executing the actions, so the loop always runs at least once.

### Advanced Features (Future Enhancements)

The following features are planned for future releases:

#### Error Handling (Scope, Try/Catch)

Error handling with Scope actions and Try/Catch/Finally patterns is planned for a future release.

#### Error Simulation

You can simulate errors in custom connector handlers:

```csharp
// Configure a connector to fail
public class FailingConnectorHandler : IConnectorActionHandler
{
    public string ConnectorType => "CustomAPI";
    
    public IDictionary<string, object> Execute(
        IFlowAction action,
        IXrmFakedContext context,
        IFlowExecutionContext flowContext)
    {
        // Simulate a failure
        throw new InvalidPluginExecutionException("External service unavailable");
    }
}

flowSimulator.RegisterConnectorActionHandler("CustomAPI", new FailingConnectorHandler());

// Execute operation that triggers flow
service.Create(account);

// Verify flow handled error
var results = flowSimulator.GetFlowExecutionResults("send_email_on_account_create");
Assert.False(results[0].Succeeded);
Assert.Single(results[0].Errors);
Assert.Contains("External service unavailable", results[0].Errors[0]);
```

## Action Types

### Compose Action ✅ **NEW**

The Compose action allows you to create data transformations and compose new objects or values from expressions.

**Reference:** https://learn.microsoft.com/en-us/power-automate/data-operations#use-the-compose-action

**Use Cases:**
- Transform data from previous steps
- Create structured objects or arrays
- Perform calculations or string manipulations
- Format data before passing to subsequent actions

**Example: Simple Value Composition**
```csharp
var composeAction = new ComposeAction
{
    Name = "Compose_FullName",
    Inputs = "@concat(triggerBody()['firstname'], ' ', triggerBody()['lastname'])"
};

var flowDef = new CloudFlowDefinition
{
    Name = "Test_Compose",
    Trigger = new DataverseTrigger { EntityLogicalName = "contact", Message = "Create" },
    Actions = new List<IFlowAction> { composeAction }
};

simulator.RegisterFlow(flowDef);
var result = simulator.SimulateTrigger("Test_Compose", triggerInputs);

// Access composed value
var fullName = result.ActionResults[0].Outputs["value"];
```

**Example: Composing Objects**
```csharp
var contactObject = new Dictionary<string, object>
{
    ["fullname"] = "@concat(triggerBody()['firstname'], ' ', triggerBody()['lastname'])",
    ["email"] = "@triggerBody()['email']",
    ["displayname"] = "@toUpper(triggerBody()['lastname'])"
};

var composeAction = new ComposeAction
{
    Name = "Compose_ContactData",
    Inputs = contactObject
};

// All expressions in the dictionary are evaluated recursively
```

**Referencing Compose Outputs:**
```csharp
// Use @outputs('ActionName')['value'] to reference composed data
var compose1 = new ComposeAction
{
    Name = "Compose_Greeting",
    Inputs = "@concat('Hello ', triggerBody()['firstname'])"
};

var compose2 = new ComposeAction
{
    Name = "Compose_Message",
    Inputs = "@concat(outputs('Compose_Greeting')['value'], '!')"
};
```

### Apply to Each Action ✅ **NEW**

The Apply to Each action iterates over a collection and executes a set of actions for each item.

**Reference:** https://learn.microsoft.com/en-us/power-automate/apply-to-each

**Use Cases:**
- Process each record from a list query
- Send emails to multiple recipients
- Create or update multiple records
- Transform each item in an array

**Example: Basic Loop**
```csharp
var contacts = new[]
{
    new Dictionary<string, object> { ["name"] = "Contact 1", ["email"] = "c1@example.com" },
    new Dictionary<string, object> { ["name"] = "Contact 2", ["email"] = "c2@example.com" }
};

var triggerInputs = new Dictionary<string, object>
{
    ["contacts"] = contacts
};

var composeInLoop = new ComposeAction
{
    Name = "Compose_Email",
    Inputs = "@item()['email']"  // @item() returns current loop item
};

var applyToEach = new ApplyToEachAction
{
    Name = "Process_Contacts",
    Collection = "@triggerBody()['contacts']",  // Expression that returns collection
    Actions = new List<IFlowAction> { composeInLoop }
};
```

**Example: Multiple Actions Per Item**
```csharp
var doubleValue = new ComposeAction
{
    Name = "Double_Value",
    Inputs = "@mul(item(), 2)"
};

var addTen = new ComposeAction
{
    Name = "Add_Ten",
    Inputs = "@add(outputs('Double_Value')['value'], 10)"
};

var applyToEach = new ApplyToEachAction
{
    Name = "Process_Numbers",
    Collection = "@triggerBody()['numbers']",
    Actions = new List<IFlowAction> { doubleValue, addTen }
};
```

**Using @item() Function:**
```csharp
// @item() returns the current item in the loop
// For object items, access properties: @item()['propertyName']
// For primitive items, use directly: @item()

var composeFullName = new ComposeAction
{
    Name = "Compose_FullName",
    Inputs = "@concat(item()['firstname'], ' ', item()['lastname'])"
};
```

**Loop with Dataverse Actions:**
```csharp
var createTask = new DataverseAction
{
    Name = "Create_Task_For_Contact",
    DataverseActionType = DataverseActionType.Create,
    EntityLogicalName = "task",
    Attributes = new Dictionary<string, object>
    {
        ["subject"] = "@concat('Follow up with ', item()['name'])",
        ["description"] = "@item()['email']"
    }
};

var applyToEach = new ApplyToEachAction
{
    Name = "Create_Tasks",
    Collection = "@outputs('List_Contacts')['value']",
    Actions = new List<IFlowAction> { createTask }
};
```

**Nested Loops:**
Nested loops are supported through the stack-based item tracking:
```csharp
var innerLoop = new ApplyToEachAction
{
    Name = "Inner_Loop",
    Collection = "@item()['children']",
    Actions = new List<IFlowAction> 
    { 
        new ComposeAction 
        { 
            Name = "Process_Child", 
            Inputs = "@item()['name']"  // Refers to inner loop item
        } 
    }
};

var outerLoop = new ApplyToEachAction
{
    Name = "Outer_Loop",
    Collection = "@triggerBody()['parents']",
    Actions = new List<IFlowAction> { innerLoop }
};
```

## Trigger Types

### Dataverse Triggers

| Trigger Type | Description | When It Fires |
|-------------|-------------|---------------|
| **Create** | When a record is created | After successful Create operation (PostOperation stage) |
| **Update** | When a record is updated | After successful Update operation (PostOperation stage) |
| **Delete** | When a record is deleted | After successful Delete operation (PostOperation stage) |
| **CreateOrUpdate** | When a record is created or updated | After Create or Update |

**Note:** Automatic triggering requires `context.UsePipelineSimulation = true`.

### Trigger Scope

| Scope | Description | Status |
|-------|-------------|--------|
| **Organization** | All records in the organization | ✅ Supported |
| **BusinessUnit** | Records owned by users in the same business unit | ⏳ Planned |
| **ParentChildBusinessUnits** | Records in parent/child business units | ⏳ Planned |
| **User** | Records owned by the triggering user | ⏳ Planned |

Currently, only Organization scope is fully implemented. Other scopes are planned for future releases.

### Filtered Attributes (Update Trigger Only)
```csharp
var trigger = new DataverseTrigger
{
    EntityLogicalName = "account",
    Message = "Update",
    FilteredAttributes = new List<string> { "name", "revenue" } // Only trigger on these fields
};
```

## Connector Types

### Built-in Connectors
- **Dataverse** - Full support for CRUD operations (Create, Retrieve, Update, Delete, ListRecords, Relate, Unrelate, ExecuteAction)

### Extensible Connectors
Test writers can register handlers for:
- Office 365 Outlook
- SharePoint
- Microsoft Teams
- OneDrive
- Azure services
- Custom connectors and APIs

## Best Practices

1. **Test Flow Logic Separately**: Test the flow definition and business logic independently from integration points
2. **Mock External Services**: Use connector action handlers to mock external dependencies
3. **Verify Dataverse Changes**: Assert that flows make expected changes to Dataverse records
4. **Use Real Flow Definitions**: Import actual flow JSON to ensure tests match production behavior
5. **Test Error Scenarios**: Simulate connector failures to verify error handling
6. **Validate Trigger Conditions**: Ensure flows only trigger under the right conditions

## Common Use Cases

### Create Follow-up Task on Contact Creation
```csharp
// Flow: Create a task when a new contact is created
var flowDefinition = new CloudFlowDefinition
{
    Name = "create_followup_task",
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
                ["subject"] = "Follow up with new contact",
                ["description"] = "Reach out to the new lead",
                ["scheduledstart"] = DateTime.Now.AddDays(1)
            }
        }
    }
};

// Test
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true;
var flowSimulator = context.CloudFlowSimulator;
flowSimulator.RegisterFlow(flowDefinition);

var service = context.GetOrganizationService();
var contactId = service.Create(new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
});

// Verify task was created
flowSimulator.AssertFlowTriggered("create_followup_task");
var tasks = context.CreateQuery("task").ToList();
Assert.Single(tasks);
Assert.Equal("Follow up with new contact", tasks[0]["subject"]);
```

### Update Related Records
```csharp
// Flow: When account is updated, create a note
var flowDefinition = new CloudFlowDefinition
{
    Name = "log_account_update",
    Trigger = new DataverseTrigger
    {
        EntityLogicalName = "account",
        Message = "Update",
        FilteredAttributes = new List<string> { "address1_city" }
    },
    Actions = new List<IFlowAction>
    {
        new DataverseAction
        {
            Name = "CreateNote",
            DataverseActionType = DataverseActionType.Create,
            EntityLogicalName = "annotation",
            Attributes = new Dictionary<string, object>
            {
                ["subject"] = "Account address updated",
                ["notetext"] = "City was changed"
            }
        }
    }
};

flowSimulator.RegisterFlow(flowDefinition);

// Update account - triggers flow only when city changes
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso",
    ["address1_city"] = "Seattle"
};
context.Initialize(account);

// This triggers the flow (city changed)
account["address1_city"] = "Portland";
service.Update(account);

flowSimulator.AssertFlowTriggered("log_account_update");
var notes = context.CreateQuery("annotation").ToList();
Assert.Single(notes);

// This doesn't trigger the flow (other field changed)
account["phone"] = "555-1234";
service.Update(account);
Assert.Equal(1, flowSimulator.GetFlowExecutionCount("log_account_update")); // Still 1
```

## Implementation Status

**Current Status:** ✅ **Completed** (October 12, 2025)

All core phases have been implemented and tested. The Cloud Flow simulation feature is fully functional for testing Dataverse-triggered flows with comprehensive expression language support, safe navigation, path separators, Compose actions, and Apply to Each loops.

### Phase 1: Core Infrastructure ✅ **COMPLETED**
- ✅ `ICloudFlowSimulator` interface
- ✅ `CloudFlowDefinition` class
- ✅ `IFlowTrigger` interface and implementations
- ✅ `IFlowAction` interface and base implementations
- ✅ Flow registration and storage
- ✅ Basic trigger simulation

### Phase 2: Dataverse Integration ✅ **COMPLETED**
- ✅ `DataverseTrigger` implementation
- ✅ `DataverseAction` implementation with full CRUD support
- ✅ Integration with CRUD operations
- ✅ Trigger condition evaluation
- ✅ Filtered attributes support

### Phase 3: Connector Extensibility ✅ **COMPLETED**
- ✅ `IConnectorActionHandler` interface
- ✅ Connector action handler registration
- ✅ Built-in Dataverse connector
- ✅ Built-in Compose action handler ✅ **NEW**
- ✅ Extensibility for custom connectors

### Phase 4: Verification APIs ✅ **COMPLETED**
- ✅ Flow execution tracking
- ✅ `AssertFlowTriggered` / `AssertFlowNotTriggered`
- ✅ `GetFlowExecutionResults`
- ✅ Action result inspection
- ✅ Execution history

### Phase 5: Expression Language ✅ **COMPLETED**
- ✅ Full expression language implementation using Jint 4.2.0
- ✅ 80+ Power Automate functions
- ✅ Safe navigation operator (?) for null-safe access ✅ **NEW**
- ✅ Path separator (/) for nested property access ✅ **NEW**
- ✅ All reference functions (triggerBody, outputs, body, variables, item)
- ✅ String, math, logical, date/time, collection, and conversion functions
- ✅ Type preservation (int, double, string, bool)

### Phase 6: Advanced Action Types ✅ **COMPLETED**
- ✅ Compose actions for data transformation ✅ **NEW**
- ✅ Apply to Each (loops) with `@item()` function ✅ **NEW**
- ✅ Nested loop support via stack-based item tracking ✅ **NEW**
- ✅ Recursive expression evaluation in composed objects ✅ **NEW**

### Phase 7: Control Flow Actions ✅ **COMPLETED** (October 12, 2025)
- ✅ Condition actions (if/then/else branching) ✅ **NEW**
- ✅ Switch actions (multi-case branching) ✅ **NEW**
- ✅ Parallel branches (parallel execution paths) ✅ **NEW**
- ✅ Do Until loops (loop with exit condition) ✅ **NEW**

### Phase 8: Future Enhancements
- ⏳ Error handling and retry logic (Scope, Try/Catch)
- ⏳ Additional connector types (Office365, SharePoint, HTTP, etc.)
- ⏳ Schedule triggers and recurrence
- ⏳ Manual triggers with input schemas

**Test Coverage:** 157 unit tests, all passing ✅
- 57 tests for expression evaluator
- 7 tests for safe navigation and path separators ✅ **NEW**
- 7 tests for Compose and Apply to Each actions ✅ **NEW**
- 13 tests for control flow actions (Condition, Switch, Parallel, Do Until) ✅ **NEW**
- 6 tests for JSON import of control flow actions ✅ **NEW**
- 67 tests for simulator, Dataverse actions, and triggering

## Key Differences from FakeXrmEasy v2

**Important**: The Cloud Flow simulation feature in Fake4Dataverse differs from FakeXrmEasy v2+ in several ways:

### API Design Differences

| Feature | FakeXrmEasy v2+ | Fake4Dataverse |
|---------|----------------|----------------|
| **Flow Registration** | Unknown | Explicit registration with `RegisterFlow` |
| **Automatic Triggering** | Unknown | Requires `UsePipelineSimulation = true` |
| **Connector Handlers** | Unknown | Extensibility-first with `IConnectorActionHandler` |
| **JSON Import** | Unknown | Planned for future release |
| **Expression Engine** | Unknown | Basic support, full expressions planned |

### Setup Differences

**Fake4Dataverse:**
```csharp
// Explicit registration with automatic triggering
var context = XrmFakedContextFactory.New();
context.UsePipelineSimulation = true; // Required for automatic triggering
var flowSimulator = context.CloudFlowSimulator;

flowSimulator.RegisterFlow(new CloudFlowDefinition
{
    Name = "my_flow",
    Trigger = new DataverseTrigger { /* ... */ },
    Actions = new List<IFlowAction> { /* ... */ }
});

// Flows trigger automatically on CRUD operations
service.Create(entity);
```

**FakeXrmEasy v2+ (reference):**
Specific implementation details for FakeXrmEasy v2+ are not publicly documented. Consult FakeXrmEasy v2+ documentation for comparison.

## Related Documentation

- [Testing Plugins](testing-plugins.md) - Similar patterns for testing business logic
- [Custom API Support](custom-api.md) - Custom APIs can be triggered by flows
- [CRUD Operations](crud-operations.md) - Dataverse operations that trigger flows
- [Message Executors](../messages/README.md) - Integration points for flow triggers

## Contributing

This feature is now implemented and functional! We welcome:
- Bug reports and issue submissions
- Feature requests for Phase 5 enhancements
- Pull requests with improvements
- Test case contributions
- Documentation improvements

See [FEATURE_PARITY_ISSUES.md](../../FEATURE_PARITY_ISSUES.md) for tracking and [GitHub Issues](https://github.com/rnwood/Fake4Dataverse/issues) to report bugs or request features.
