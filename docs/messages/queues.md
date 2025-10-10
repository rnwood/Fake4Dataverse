# Queue Messages

> **📝 Note**: This documentation is currently under development.

## Overview

Queue messages manage queue operations in Dataverse, including adding items to queues, removing items, and picking items.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| AddToQueue | `AddToQueueRequest` | Add item to queue |
| RemoveFromQueue | `RemoveFromQueueRequest` | Remove item from queue |
| PickFromQueue | `PickFromQueueRequest` | Pick item from queue |

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Add_Item_To_Queue()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queueId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("account") { Id = accountId, ["name"] = "Test Account" }
    });
    
    var request = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("account", accountId)
    };
    
    var response = (AddToQueueResponse)service.Execute(request);
}
```

## Detailed Examples

### RemoveFromQueue

Remove an item from a queue.

**Reference:** [RemoveFromQueueRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.removefromqueuerequest) - Removes a queue item from a queue, typically after it has been processed or routed to another queue.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Remove_Item_From_Queue()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queueId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("account") { Id = accountId, ["name"] = "Test Account" }
    });
    
    // Add to queue first
    var addRequest = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("account", accountId)
    };
    var addResponse = (AddToQueueResponse)service.Execute(addRequest);
    
    // Remove from queue
    var removeRequest = new RemoveFromQueueRequest
    {
        QueueItemId = addResponse.QueueItemId
    };
    
    var response = (RemoveFromQueueResponse)service.Execute(removeRequest);
    Assert.NotNull(response);
}
```

### PickFromQueue

Pick (assign) a queue item to a user.

**Reference:** [PickFromQueueRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.pickfromqueuerequest) - Assigns a queue item to a user for processing, removing it from the available pool and marking it as being worked on.

```csharp
[Fact]
public void Should_Pick_Item_From_Queue()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queueId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("account") { Id = accountId, ["name"] = "Test Account" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "Agent" }
    });
    
    // Add to queue
    var addRequest = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("account", accountId)
    };
    var addResponse = (AddToQueueResponse)service.Execute(addRequest);
    
    // Pick from queue
    var pickRequest = new PickFromQueueRequest
    {
        QueueItemId = addResponse.QueueItemId,
        WorkerId = userId
    };
    
    var response = (PickFromQueueResponse)service.Execute(pickRequest);
    Assert.NotNull(response);
}
```

## Complete Queue Workflow

### End-to-End Queue Testing

```csharp
[Fact]
public void Should_Process_Complete_Queue_Workflow()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Setup
    var queueId = Guid.NewGuid();
    var caseId = Guid.NewGuid();
    var agent1Id = Guid.NewGuid();
    var agent2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("incident") { Id = caseId, ["title"] = "Customer Issue" },
        new Entity("systemuser") { Id = agent1Id, ["fullname"] = "Agent 1" },
        new Entity("systemuser") { Id = agent2Id, ["fullname"] = "Agent 2" }
    });
    
    // Step 1: Add case to queue
    var addRequest = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("incident", caseId)
    };
    var addResponse = (AddToQueueResponse)service.Execute(addRequest);
    var queueItemId = addResponse.QueueItemId;
    
    Assert.NotEqual(Guid.Empty, queueItemId);
    
    // Step 2: Agent 1 picks the item
    var pickRequest = new PickFromQueueRequest
    {
        QueueItemId = queueItemId,
        WorkerId = agent1Id
    };
    service.Execute(pickRequest);
    
    // Step 3: Verify queue item is assigned
    var queueItem = service.Retrieve("queueitem", queueItemId, new ColumnSet("workerid"));
    Assert.Equal(agent1Id, queueItem.GetAttributeValue<EntityReference>("workerid").Id);
    
    // Step 4: Remove from queue (work completed)
    var removeRequest = new RemoveFromQueueRequest
    {
        QueueItemId = queueItemId
    };
    service.Execute(removeRequest);
}
```

### Testing Queue Routing

```csharp
[Fact]
public void Should_Route_Between_Queues()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queue1Id = Guid.NewGuid();
    var queue2Id = Guid.NewGuid();
    var caseId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queue1Id, ["name"] = "Tier 1 Support" },
        new Entity("queue") { Id = queue2Id, ["name"] = "Tier 2 Support" },
        new Entity("incident") { Id = caseId, ["title"] = "Complex Issue" }
    });
    
    // Add to Tier 1
    var addToTier1 = new AddToQueueRequest
    {
        DestinationQueueId = queue1Id,
        Target = new EntityReference("incident", caseId)
    };
    var tier1Response = (AddToQueueResponse)service.Execute(addToTier1);
    
    // Escalate to Tier 2 (remove from Tier 1, add to Tier 2)
    service.Execute(new RemoveFromQueueRequest { QueueItemId = tier1Response.QueueItemId });
    
    var addToTier2 = new AddToQueueRequest
    {
        DestinationQueueId = queue2Id,
        Target = new EntityReference("incident", caseId)
    };
    var tier2Response = (AddToQueueResponse)service.Execute(addToTier2);
    
    Assert.NotEqual(Guid.Empty, tier2Response.QueueItemId);
}
```

### Testing Multiple Items in Queue

```csharp
[Fact]
public void Should_Handle_Multiple_Queue_Items()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queueId = Guid.NewGuid();
    var case1Id = Guid.NewGuid();
    var case2Id = Guid.NewGuid();
    var case3Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("incident") { Id = case1Id, ["title"] = "Case 1" },
        new Entity("incident") { Id = case2Id, ["title"] = "Case 2" },
        new Entity("incident") { Id = case3Id, ["title"] = "Case 3" }
    });
    
    // Add multiple cases to queue
    var queueItems = new List<Guid>();
    
    foreach (var caseId in new[] { case1Id, case2Id, case3Id })
    {
        var request = new AddToQueueRequest
        {
            DestinationQueueId = queueId,
            Target = new EntityReference("incident", caseId)
        };
        var response = (AddToQueueResponse)service.Execute(request);
        queueItems.Add(response.QueueItemId);
    }
    
    Assert.Equal(3, queueItems.Count);
    Assert.All(queueItems, id => Assert.NotEqual(Guid.Empty, id));
}
```

### Testing Priority Handling

```csharp
[Fact]
public void Should_Add_Items_With_Priority()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var queueId = Guid.NewGuid();
    var urgentCaseId = Guid.NewGuid();
    var normalCaseId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("queue") { Id = queueId, ["name"] = "Support Queue" },
        new Entity("incident") 
        { 
            Id = urgentCaseId, 
            ["title"] = "Urgent Case",
            ["prioritycode"] = new OptionSetValue(1) // High
        },
        new Entity("incident") 
        { 
            Id = normalCaseId, 
            ["title"] = "Normal Case",
            ["prioritycode"] = new OptionSetValue(2) // Normal
        }
    });
    
    // Add urgent case
    var urgentRequest = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("incident", urgentCaseId)
    };
    service.Execute(urgentRequest);
    
    // Add normal case
    var normalRequest = new AddToQueueRequest
    {
        DestinationQueueId = queueId,
        Target = new EntityReference("incident", normalCaseId)
    };
    service.Execute(normalRequest);
    
    // Query queue items (in practice, you'd sort by priority)
    var queueItems = context.CreateQuery("queueitem")
        .Where(qi => qi.GetAttributeValue<EntityReference>("queueid").Id == queueId)
        .ToList();
    
    Assert.Equal(2, queueItems.Count);
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Queue Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/queue-entities)
