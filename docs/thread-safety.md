# Thread Safety in Fake4Dataverse

## Overview

Starting from version 4.x, Fake4Dataverse provides built-in thread safety for CRUD operations with database-like serialization of transactions. This ensures that concurrent operations from multiple threads can safely access the same `IXrmFakedContext` instance without race conditions or data corruption.

**Implementation Date**: October 2025  
**Reference Issue**: Thread safety and database-like transaction serialization

## Why Thread Safety Matters

In real-world testing scenarios, you may need to:

- **Test parallel plugin execution** - Multiple plugins running concurrently
- **Simulate concurrent user operations** - Multiple users accessing the system simultaneously
- **Test async workflows** - Background operations happening during user interactions
- **Verify race condition handling** - Ensure your code handles concurrent access correctly

Without thread safety, concurrent operations could lead to:
- ❌ Data corruption
- ❌ Lost updates
- ❌ Inconsistent state
- ❌ Test flakiness

## How It Works

### Database-Like Transaction Serialization

Fake4Dataverse implements thread safety using a lock-based approach that serializes access to the in-memory data store, similar to how database transactions work:

```csharp
// All CRUD operations are automatically thread-safe
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// These operations can be safely called from multiple threads
Parallel.For(0, 100, i =>
{
    service.Create(new Entity("account") { ["name"] = $"Account {i}" });
});
```

### Protected Operations

The following operations are automatically thread-safe:

1. **Create** - `service.Create(entity)`
2. **Retrieve** - `service.Retrieve(entityName, id, columnSet)`
3. **Update** - `service.Update(entity)`
4. **Delete** - `service.Delete(entityName, id)`
5. **RetrieveMultiple** - `service.RetrieveMultiple(query)`

## Usage Examples

### Example 1: Concurrent Creates

```csharp
[Fact]
public void Should_Handle_Concurrent_Creates()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountIds = new ConcurrentBag<Guid>();
    
    // Act - Create 100 accounts concurrently
    Parallel.For(0, 100, i =>
    {
        var id = service.Create(new Entity("account")
        {
            ["name"] = $"Account {i}"
        });
        accountIds.Add(id);
    });
    
    // Assert
    Assert.Equal(100, accountIds.Count);
    Assert.Equal(100, accountIds.Distinct().Count()); // All IDs unique
}
```

### Example 2: Concurrent Updates

```csharp
[Fact]
public void Should_Handle_Concurrent_Updates()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Test Account",
        ["revenue"] = new Money(0)
    });
    
    // Act - Update the same account 100 times concurrently
    Parallel.For(0, 100, i =>
    {
        service.Update(new Entity("account", accountId)
        {
            ["revenue"] = new Money(i + 1)
        });
    });
    
    // Assert - Account still exists and is valid
    var account = service.Retrieve("account", accountId, 
        new ColumnSet("revenue"));
    Assert.NotNull(account);
    Assert.True(account.Contains("revenue"));
}
```

### Example 3: Mixed Concurrent Operations

```csharp
[Fact]
public void Should_Handle_Mixed_CRUD_Operations()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Act - Perform mixed operations concurrently
    Parallel.For(0, 200, i =>
    {
        switch (i % 4)
        {
            case 0: // Create
                service.Create(new Entity("account") 
                    { ["name"] = $"Account {i}" });
                break;
                
            case 1: // Read
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name")
                };
                service.RetrieveMultiple(query);
                break;
                
            case 2: // Update
                var updateQuery = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("accountid"),
                    TopCount = 1
                };
                var results = service.RetrieveMultiple(updateQuery);
                if (results.Entities.Count > 0)
                {
                    service.Update(new Entity("account", 
                        results.Entities[0].Id)
                    {
                        ["name"] = $"Updated {i}"
                    });
                }
                break;
                
            case 3: // Delete
                var deleteQuery = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("accountid"),
                    TopCount = 1
                };
                var deleteResults = service.RetrieveMultiple(deleteQuery);
                if (deleteResults.Entities.Count > 0)
                {
                    service.Delete("account", 
                        deleteResults.Entities[0].Id);
                }
                break;
        }
    });
    
    // Assert - No exceptions occurred
    Assert.True(true);
}
```

## Performance Considerations

### Lock Granularity

The current implementation uses a **single lock** for all CRUD operations. This means:

- ✅ **Simple and reliable** - Easy to reason about, no deadlocks
- ✅ **Correct behavior** - Guaranteed serialization like a database
- ⚠️ **Coarse-grained** - All operations are serialized, even reads

### When Thread Safety Matters

Thread safety is most important when:

1. **Testing concurrent scenarios** - Your tests explicitly use parallel operations
2. **Plugin pipeline simulation** - Multiple plugins executing during a single operation
3. **Async workflow testing** - Background processes running during tests

### When It's Less Critical

Thread safety overhead is negligible when:

1. **Single-threaded tests** - Most unit tests run sequentially
2. **Simple CRUD operations** - Basic create/read/update/delete without concurrency
3. **Small data sets** - The in-memory store is fast even with locking

## Implementation Details

### Lock Object

```csharp
// In XrmFakedContext.cs
private readonly object _dataLock = new object();
```

### Protected Methods

All CRUD operations wrap their data access in lock statements:

```csharp
public void UpdateEntity(Entity e)
{
    // ... validation ...
    
    lock (_dataLock)
    {
        // All data access happens inside the lock
        if (Data.ContainsKey(e.LogicalName) && 
            Data[e.LogicalName].ContainsKey(e.Id))
        {
            // ... update logic ...
        }
    }
}
```

### Thread Safety Guarantees

The implementation guarantees:

1. **Atomicity** - Each CRUD operation is atomic (all-or-nothing)
2. **Consistency** - The data store is always in a consistent state
3. **Isolation** - Concurrent operations don't see partial updates
4. **Durability** - Changes are immediately visible to subsequent operations

## Key Differences from FakeXrmEasy v2

**Important**: The thread safety implementation in Fake4Dataverse differs from FakeXrmEasy v2+:

| Feature | FakeXrmEasy v2+ | Fake4Dataverse v4 |
|---------|----------------|-------------------|
| Thread Safety | Not documented | ✅ Built-in with lock-based serialization |
| Lock Granularity | Unknown | Single lock for all CRUD operations |
| Test Support | No specific tests | ✅ Comprehensive thread safety tests included |

## Best Practices

### ✅ Do

1. **Use parallel testing when needed**
   ```csharp
   // Test concurrent scenarios explicitly
   Parallel.For(0, 100, i => { /* operations */ });
   ```

2. **Verify thread safety in your tests**
   ```csharp
   // Ensure your code handles concurrent access
   var tasks = Enumerable.Range(0, 10)
       .Select(i => Task.Run(() => service.Create(...)))
       .ToArray();
   Task.WaitAll(tasks);
   ```

3. **Test realistic concurrency levels**
   ```csharp
   // Use realistic numbers of concurrent operations
   Parallel.For(0, 10, i => { /* ... */ }); // Not 10,000
   ```

### ❌ Don't

1. **Don't assume lock-free implementation**
   ```csharp
   // ❌ Don't write code that depends on lock-free semantics
   // The implementation uses locks for correctness
   ```

2. **Don't create your own locks around operations**
   ```csharp
   // ❌ Not needed - operations are already thread-safe
   lock (myLock)
   {
       service.Create(entity);
   }
   ```

3. **Don't share contexts across test classes**
   ```csharp
   // ❌ Each test should have its own context
   public class MyTests
   {
       private static IXrmFakedContext _sharedContext; // Don't do this
   }
   ```

## Testing Thread Safety

The framework includes comprehensive thread safety tests in `ThreadSafetyTests.cs`:

- `Should_Handle_Concurrent_Creates_Without_Race_Conditions`
- `Should_Handle_Concurrent_Updates_Without_Data_Loss`
- `Should_Handle_Concurrent_Reads_Without_Exceptions`
- `Should_Handle_Concurrent_Deletes_Without_Duplicate_Deletion_Errors`
- `Should_Handle_Mixed_Concurrent_CRUD_Operations`

Run these tests to verify thread safety:

```bash
dotnet test --filter "FullyQualifiedName~ThreadSafetyTests"
```

## See Also

- [CRUD Operations](usage/crud-operations.md) - Basic CRUD operation patterns
- [Plugin Testing](usage/testing-plugins.md) - Testing plugins with concurrent execution
- [Middleware Architecture](concepts/middleware.md) - Understanding the middleware pipeline
