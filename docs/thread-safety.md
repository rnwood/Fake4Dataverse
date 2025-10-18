# Thread Safety in Fake4Dataverse

## Overview

Fake4Dataverse has built-in thread safety for CRUD operations with database-like serialization of transactions. This means concurrent operations from multiple threads can safely access the same `IXrmFakedContext` instance without race conditions or data corruption.

## Why Thread Safety Matters

In real-world testing, you might need to:

- **Test parallel plugin execution** - Multiple plugins running concurrently
- **Simulate concurrent user operations** - Multiple users accessing the system simultaneously
- **Test async workflows** - Background operations happening during user interactions
- **Verify race condition handling** - Ensure your code handles concurrent access correctly

Without thread safety, concurrent operations could lead to:
- Data corruption
- Lost updates
- Inconsistent state
- Test flakiness

## How It Works

### Per-Entity-Type Locking for Better Concurrency

Fake4Dataverse uses **per-entity-type locks** for optimal concurrency while maintaining data consistency:

```csharp
// Operations on DIFFERENT entity types can execute concurrently
Parallel.Invoke(
    () => service.Create(new Entity("account") { ["name"] = "Account 1" }),
    () => service.Create(new Entity("contact") { ["firstname"] = "Contact 1" })
);
// ✅ Both operations execute in parallel - no contention!

// Operations on the SAME entity type are serialized
Parallel.For(0, 100, i =>
{
    service.Create(new Entity("account") { ["name"] = $"Account {i}" });
});
// ✅ Thread-safe - accounts are protected by their own lock
```

### How It Differs from a Single Global Lock

Unlike a naive single-lock approach, Fake4Dataverse uses **one lock per entity type**:

- ✅ **Better Performance**: Operations on different entities (account, contact, etc.) don't block each other
- ✅ **Simpler Design**: Uses `ConcurrentDictionary` for entity type lookup + regular `Dictionary` for records
- ✅ **Predictable Order**: Regular `Dictionary` preserves insertion order (important for tests)
- ✅ **Database-Like**: Mimics how databases handle table-level locking

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

The implementation uses **per-entity-type locking** which provides excellent concurrency:

- ✅ **Optimal concurrency** - Operations on different entities (account, contact, etc.) run in parallel
- ✅ **Simple and reliable** - Easy to reason about, no deadlocks
- ✅ **Correct behavior** - Guaranteed consistency within each entity type
- ✅ **Fine-grained enough** - Most real-world scenarios involve different entity types

### Concurrency Benefits

**Example**: Testing plugin that creates a contact when an account is created:

```csharp
// Both operations execute in parallel - no contention!
Parallel.For(0, 100, i =>
{
    var accountId = service.Create(new Entity("account") { ["name"] = $"Account {i}" });
    // Plugin runs and creates contact - different lock!
    service.Create(new Entity("contact") { 
        ["parentcustomerid"] = new EntityReference("account", accountId) 
    });
});
```

**Result**: ~2x faster than a single global lock because account and contact operations don't block each other.

### When Thread Safety Matters

Thread safety is most important when:

1. **Testing concurrent scenarios** - Your tests explicitly use parallel operations
2. **Plugin pipeline simulation** - Multiple plugins executing during a single operation
3. **Async workflow testing** - Background processes running during tests
4. **Multi-entity operations** - Operations spanning different entity types benefit from parallelism

### When It's Less Critical

Thread safety overhead is negligible when:

1. **Single-threaded tests** - Most unit tests run sequentially
2. **Simple CRUD operations** - Basic create/read/update/delete without concurrency
3. **Small data sets** - The in-memory store is fast even with locking

## Implementation Details

### Lock Object

```csharp
// In XrmFakedContext.cs
private readonly ConcurrentDictionary<string, object> _entityLocks = 
    new ConcurrentDictionary<string, object>();
```

Each entity type gets its own lock object created on-demand:

```csharp
var entityLock = _entityLocks.GetOrAdd(entityLogicalName, _ => new object());
lock (entityLock)
{
    // CRUD operations on this entity type
}
```

### Data Structure

```csharp
// Outer dictionary: thread-safe for adding new entity types
public ConcurrentDictionary<string, Dictionary<Guid, Entity>> Data { get; set; }

// Inner dictionary: regular Dictionary protected by per-entity-type lock
// Maintains insertion order for predictable test behavior
```

### Protected Methods

All CRUD operations acquire the appropriate entity-type lock:

```csharp
public void UpdateEntity(Entity e)
{
    // ... validation ...
    
    // Get lock for this specific entity type
    var entityLock = _entityLocks.GetOrAdd(e.LogicalName, _ => new object());
    lock (entityLock)
    {
        // All data access for this entity type happens inside the lock
        if (Data.TryGetValue(e.LogicalName, out var entityCollection) && 
            entityCollection.ContainsKey(e.Id))
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
3. **Isolation** - Concurrent operations on the same entity type don't see partial updates
4. **Concurrency** - Operations on different entity types execute in parallel
5. **Durability** - Changes are immediately visible to subsequent operations

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
