# Batch Operations and Transactions

Learn how to test batch operations using ExecuteMultiple and ExecuteTransaction in Fake4Dataverse.

## Table of Contents
- [ExecuteMultiple](#executemultiple)
- [ExecuteTransaction](#executetransaction)
- [Performance Considerations](#performance-considerations)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

## ExecuteMultiple

Fake4Dataverse supports `ExecuteMultiple` for testing bulk operations with batched requests.

### Basic ExecuteMultiple

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

[Fact]
public void Should_Execute_Multiple_Creates()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = false,
            ReturnResponses = true
        }
    };
    
    // Add multiple create requests
    for (int i = 0; i < 5; i++)
    {
        var account = new Entity("account")
        {
            ["name"] = $"Account {i}"
        };
        request.Requests.Add(new CreateRequest { Target = account });
    }
    
    // Act
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    // Assert
    Assert.Equal(5, response.Responses.Count);
    Assert.All(response.Responses, r => Assert.IsType<CreateResponse>(r.Response));
    
    // Verify all accounts were created
    var accounts = context.CreateQuery("account").ToList();
    Assert.Equal(5, accounts.Count);
}
```

### ExecuteMultiple with Mixed Operations

```csharp
[Fact]
public void Should_Execute_Multiple_Operations()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create initial account
    var existingAccountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = existingAccountId,
        ["name"] = "Existing Account"
    });
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = false,
            ReturnResponses = true
        }
    };
    
    // Create
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "New Account" }
    });
    
    // Update
    request.Requests.Add(new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = existingAccountId,
            ["name"] = "Updated Account"
        }
    });
    
    // Retrieve
    request.Requests.Add(new RetrieveRequest
    {
        Target = new EntityReference("account", existingAccountId),
        ColumnSet = new ColumnSet("name")
    });
    
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    Assert.Equal(3, response.Responses.Count);
    Assert.IsType<CreateResponse>(response.Responses[0].Response);
    Assert.IsType<UpdateResponse>(response.Responses[1].Response);
    Assert.IsType<RetrieveResponse>(response.Responses[2].Response);
}
```

### Continue On Error

```csharp
[Fact]
public void Should_Continue_On_Error_When_Configured()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = true, // Continue even if some fail
            ReturnResponses = true
        }
    };
    
    // Valid create
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Valid Account" }
    });
    
    // Invalid update (non-existent entity)
    request.Requests.Add(new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = Guid.NewGuid(), // Doesn't exist
            ["name"] = "Updated"
        }
    });
    
    // Another valid create
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Another Valid Account" }
    });
    
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    // Check responses
    Assert.Equal(3, response.Responses.Count);
    
    // First and third succeeded, second failed
    Assert.Null(response.Responses[0].Fault);
    Assert.NotNull(response.Responses[1].Fault);
    Assert.Null(response.Responses[2].Fault);
    
    // Two accounts created despite one error
    var accounts = context.CreateQuery("account").ToList();
    Assert.Equal(2, accounts.Count);
}
```

### Without Return Responses

For better performance when you don't need responses:

```csharp
[Fact]
public void Should_Execute_Without_Returning_Responses()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = false,
            ReturnResponses = false // Don't return responses
        }
    };
    
    for (int i = 0; i < 100; i++)
    {
        request.Requests.Add(new CreateRequest
        {
            Target = new Entity("account") { ["name"] = $"Account {i}" }
        });
    }
    
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    // No responses returned
    Assert.Empty(response.Responses);
    
    // But all accounts were created
    var accounts = context.CreateQuery("account").ToList();
    Assert.Equal(100, accounts.Count);
}
```

## ExecuteTransaction

`ExecuteTransaction` executes multiple requests as an atomic transaction. All requests succeed or all fail.

### Basic Transaction

```csharp
[Fact]
public void Should_Execute_Requests_In_Transaction()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteTransactionRequest
    {
        Requests = new OrganizationRequestCollection(),
        ReturnResponses = true
    };
    
    // Add multiple operations
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Account 1" }
    });
    
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Account 2" }
    });
    
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("contact") { ["firstname"] = "John" }
    });
    
    var response = (ExecuteTransactionResponse)service.Execute(request);
    
    Assert.Equal(3, response.Responses.Count);
    
    // All created successfully
    Assert.Equal(2, context.CreateQuery("account").Count());
    Assert.Single(context.CreateQuery("contact"));
}
```

### Transaction with Rollback

**Note**: In Fake4Dataverse, transaction rollback on error is simulated but not fully implemented like in real Dataverse.

```csharp
[Fact]
public void Should_Rollback_On_Error()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteTransactionRequest
    {
        Requests = new OrganizationRequestCollection(),
        ReturnResponses = true
    };
    
    // Valid create
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Account 1" }
    });
    
    // Invalid operation (will fail)
    request.Requests.Add(new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = Guid.NewGuid(), // Doesn't exist
            ["name"] = "Updated"
        }
    });
    
    // This request won't execute due to previous error
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Account 2" }
    });
    
    // Expect failure
    Assert.Throws<Exception>(() => service.Execute(request));
    
    // Note: In real Dataverse, all changes would rollback
    // In Fake4Dataverse, rollback behavior may be limited
}
```

### Mixed Operations in Transaction

```csharp
[Fact]
public void Should_Handle_Mixed_Operations_In_Transaction()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Setup existing data
    var existingAccountId = Guid.NewGuid();
    context.Initialize(new Entity("account")
    {
        Id = existingAccountId,
        ["name"] = "Existing Account",
        ["revenue"] = new Money(1000000)
    });
    
    var request = new ExecuteTransactionRequest
    {
        Requests = new OrganizationRequestCollection(),
        ReturnResponses = true
    };
    
    // Update existing
    request.Requests.Add(new UpdateRequest
    {
        Target = new Entity("account")
        {
            Id = existingAccountId,
            ["revenue"] = new Money(2000000)
        }
    });
    
    // Create new
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("contact")
        {
            ["firstname"] = "John",
            ["parentcustomerid"] = new EntityReference("account", existingAccountId)
        }
    });
    
    var response = (ExecuteTransactionResponse)service.Execute(request);
    
    Assert.Equal(2, response.Responses.Count);
    
    // Verify update
    var account = service.Retrieve("account", existingAccountId, new ColumnSet("revenue"));
    Assert.Equal(2000000m, ((Money)account["revenue"]).Value);
    
    // Verify create
    Assert.Single(context.CreateQuery("contact"));
}
```

## Performance Considerations

### In Real Dataverse

- ExecuteMultiple reduces network roundtrips
- Batch size limits apply (default: 1000 requests)
- Server-side processing is still sequential

### In Fake4Dataverse

- All operations are in-memory
- No network overhead
- Performance difference is minimal

```csharp
[Fact]
public void Performance_Comparison()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Individual creates
    var sw1 = Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        service.Create(new Entity("account") { ["name"] = $"Account {i}" });
    }
    sw1.Stop();
    
    // Clear data
    var accounts = context.CreateQuery("account").ToList();
    foreach (var account in accounts)
    {
        service.Delete("account", account.Id);
    }
    
    // ExecuteMultiple
    var sw2 = Stopwatch.StartNew();
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = false,
            ReturnResponses = false
        }
    };
    
    for (int i = 0; i < 100; i++)
    {
        request.Requests.Add(new CreateRequest
        {
            Target = new Entity("account") { ["name"] = $"Account {i}" }
        });
    }
    service.Execute(request);
    sw2.Stop();
    
    // In Fake4Dataverse, times are similar
    // In real Dataverse, ExecuteMultiple would be much faster
}
```

## Error Handling

### Handling Individual Failures

```csharp
[Fact]
public void Should_Handle_Individual_Failures()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = true,
            ReturnResponses = true
        }
    };
    
    // Mix of valid and invalid requests
    request.Requests.Add(new CreateRequest
    {
        Target = new Entity("account") { ["name"] = "Valid" }
    });
    
    request.Requests.Add(new DeleteRequest
    {
        Target = new EntityReference("account", Guid.NewGuid()) // Doesn't exist
    });
    
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    // Check each response
    foreach (var item in response.Responses)
    {
        if (item.Fault != null)
        {
            // Handle error
            Console.WriteLine($"Request {item.RequestIndex} failed: {item.Fault.Message}");
        }
        else
        {
            // Success
            Console.WriteLine($"Request {item.RequestIndex} succeeded");
        }
    }
}
```

### Collecting Errors

```csharp
[Fact]
public void Should_Collect_All_Errors()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var request = new ExecuteMultipleRequest
    {
        Requests = new OrganizationRequestCollection(),
        Settings = new ExecuteMultipleSettings
        {
            ContinueOnError = true,
            ReturnResponses = true
        }
    };
    
    // Add requests (some will fail)
    for (int i = 0; i < 10; i++)
    {
        request.Requests.Add(new DeleteRequest
        {
            Target = new EntityReference("account", Guid.NewGuid())
        });
    }
    
    var response = (ExecuteMultipleResponse)service.Execute(request);
    
    // Collect all errors
    var errors = response.Responses
        .Where(r => r.Fault != null)
        .Select(r => new
        {
            RequestIndex = r.RequestIndex,
            ErrorMessage = r.Fault.Message
        })
        .ToList();
    
    Assert.Equal(10, errors.Count); // All failed
}
```

## Best Practices

### ✅ Do

1. **Use ExecuteMultiple for bulk operations**
   ```csharp
   // ✅ Good - batch creates
   var request = new ExecuteMultipleRequest { /* ... */ };
   for (int i = 0; i < 1000; i++)
   {
       request.Requests.Add(new CreateRequest { /* ... */ });
   }
   ```

2. **Set appropriate batch size**
   ```csharp
   // ✅ Good - reasonable batch size
   const int batchSize = 100;
   ```

3. **Handle errors appropriately**
   ```csharp
   // ✅ Good - check for faults
   foreach (var item in response.Responses)
   {
       if (item.Fault != null)
       {
           // Handle error
       }
   }
   ```

### ❌ Don't

1. **Don't use ExecuteMultiple for single operations**
   ```csharp
   // ❌ Bad - overhead for single request
   var request = new ExecuteMultipleRequest();
   request.Requests.Add(new CreateRequest { /* ... */ });
   ```

2. **Don't ignore errors when ContinueOnError = true**
   ```csharp
   // ❌ Bad - not checking for failures
   var response = (ExecuteMultipleResponse)service.Execute(request);
   // Missing: check response.Responses for faults
   ```

## Testing Patterns

### Pattern: Bulk Data Setup

```csharp
public static class BulkDataHelper
{
    public static void CreateBulkAccounts(
        IOrganizationService service,
        int count,
        string namePrefix = "Account")
    {
        var request = new ExecuteMultipleRequest
        {
            Requests = new OrganizationRequestCollection(),
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = false,
                ReturnResponses = false
            }
        };
        
        for (int i = 0; i < count; i++)
        {
            request.Requests.Add(new CreateRequest
            {
                Target = new Entity("account")
                {
                    ["name"] = $"{namePrefix} {i}"
                }
            });
        }
        
        service.Execute(request);
    }
}

// Usage
[Fact]
public void Test_WithBulkData()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    BulkDataHelper.CreateBulkAccounts(service, 100);
    
    var accounts = context.CreateQuery("account").ToList();
    Assert.Equal(100, accounts.Count);
}
```

## Next Steps

- [CRUD Operations](./crud-operations.md) - Individual operations
- [Testing Plugins](./testing-plugins.md) - Test plugins with batch operations
- [Message Executors](../messages/README.md) - All supported messages

## See Also

- [ExecuteMultiple Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/execute-multiple-requests)
- [ExecuteTransaction Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/use-executetransaction)
