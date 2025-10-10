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

## Coming Soon

Detailed documentation for each queue message with examples.

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Queue Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/queue-entities)
