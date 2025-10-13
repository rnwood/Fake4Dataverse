# ServiceClient Compatibility Guide

## Overview

Fake4DataverseService exposes SOAP/WCF endpoints that are structurally compatible with Microsoft's `ServiceClient` and `CrmServiceClient`. However, these clients require OAuth authentication by default, which adds unnecessary complexity for testing scenarios.

## Recommended Approach: IOrganizationService Interface

The best approach for testability is to write code that depends on the `IOrganizationService` interface rather than concrete client types:

```csharp
// ✅ Good: Depends on interface
public class AccountService
{
    public Guid CreateAccount(IOrganizationService service, string name, decimal revenue)
    {
        var account = new Entity("account")
        {
            ["name"] = name,
            ["revenue"] = new Money(revenue)
        };
        return service.Create(account);
    }
}
```

### Why This Works

Both `ServiceClient` (production) and WCF channels (testing) implement `IOrganizationService`:

- **Production**: `ServiceClient` → `IOrganizationService` (with OAuth)
- **Testing**: `ChannelFactory<IOrganizationService>` → `IOrganizationService` (no auth needed)

## Migration Patterns for ServiceClient Users

### Pattern 1: Interface Injection (Recommended)

**Before:**
```csharp
public class CustomerProcessor
{
    private readonly ServiceClient _client;
    
    public CustomerProcessor(string connectionString)
    {
        _client = new ServiceClient(connectionString);
    }
    
    public void ProcessCustomer(Guid customerId)
    {
        var customer = _client.Retrieve("contact", customerId, new ColumnSet(true));
        // Process customer...
    }
}
```

**After:**
```csharp
public class CustomerProcessor
{
    private readonly IOrganizationService _service;
    
    public CustomerProcessor(IOrganizationService service)
    {
        _service = service;
    }
    
    public void ProcessCustomer(Guid customerId)
    {
        var customer = _service.Retrieve("contact", customerId, new ColumnSet(true));
        // Process customer...
    }
}

// Production usage:
var client = new ServiceClient(connectionString);
var processor = new CustomerProcessor(client);

// Test usage:
var testService = CreateTestService();
var processor = new CustomerProcessor(testService);
```

### Pattern 2: Factory Pattern

Create a factory to abstract the service creation:

```csharp
public interface IOrganizationServiceFactory
{
    IOrganizationService CreateService();
}

public class ProductionServiceFactory : IOrganizationServiceFactory
{
    private readonly string _connectionString;
    
    public ProductionServiceFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public IOrganizationService CreateService()
    {
        return new ServiceClient(_connectionString);
    }
}

public class TestServiceFactory : IOrganizationServiceFactory
{
    private readonly string _serviceUrl;
    
    public TestServiceFactory(string serviceUrl = "http://localhost:5000/XRMServices/2011/Organization.svc")
    {
        _serviceUrl = serviceUrl;
    }
    
    public IOrganizationService CreateService()
    {
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647
        };
        
        var endpoint = new EndpointAddress(_serviceUrl);
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
        return factory.CreateChannel();
    }
}
```

### Pattern 3: Adapter Pattern (Minimal Code Changes)

If you can't change method signatures that accept `ServiceClient`:

```csharp
// Wrapper that mimics ServiceClient for testing
public class TestServiceClientAdapter : IDisposable
{
    private readonly IOrganizationService _service;
    
    public TestServiceClientAdapter(IOrganizationService service)
    {
        _service = service;
    }
    
    // Expose same methods as your code uses from ServiceClient
    public Guid Create(Entity entity) => _service.Create(entity);
    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) 
        => _service.Retrieve(entityName, id, columnSet);
    public void Update(Entity entity) => _service.Update(entity);
    public void Delete(string entityName, Guid id) => _service.Delete(entityName, id);
    
    public void Dispose() { }
}

// In tests:
var wcfService = CreateWcfChannel();
var adapter = new TestServiceClientAdapter(wcfService);
// Pass adapter to methods expecting ServiceClient-like interface
```

## Creating WCF Channel for Testing

Helper method to create a test-ready `IOrganizationService`:

```csharp
public static class Fake4DataverseServiceHelper
{
    public static IOrganizationService CreateService(
        string serviceUrl = "http://localhost:5000/XRMServices/2011/Organization.svc")
    {
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647,
            MaxBufferPoolSize = 2147483647,
            SendTimeout = TimeSpan.FromMinutes(20),
            ReceiveTimeout = TimeSpan.FromMinutes(20)
        };

        var endpoint = new EndpointAddress(serviceUrl);
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
        
        return factory.CreateChannel();
    }
}
```

## Why Not Just Add OAuth to Fake4DataverseService?

While technically possible, adding OAuth authentication to Fake4DataverseService would:

1. **Add Complexity**: Requires Azure AD app registration, token validation, certificate management
2. **Slow Down Tests**: OAuth token acquisition adds latency
3. **Increase Maintenance**: OAuth protocols and libraries need updates
4. **Miss the Point**: Testing should be simple and fast

The WCF channel approach:
- ✅ Works immediately with zero configuration
- ✅ Fast (no auth overhead)
- ✅ Simple (just create channel)
- ✅ Reliable (no external auth dependencies)

## ServiceClient-Specific Features

If your code relies on ServiceClient-specific features (not part of IOrganizationService):

| ServiceClient Feature | Testing Strategy |
|----------------------|------------------|
| `IsReady` property | Return `true` in test wrapper |
| Connection retry logic | Not needed for in-memory testing |
| Telemetry/logging | Mock or use test logger |
| Transaction support | Supported via IOrganizationService.Execute with ExecuteTransactionRequest |

## Example: Complete Test Setup

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

public class CustomerServiceTests
{
    private readonly IOrganizationService _service;
    
    public CustomerServiceTests()
    {
        _service = Fake4DataverseServiceHelper.CreateService();
    }
    
    [Fact]
    public void Should_Create_Customer()
    {
        // Arrange
        var customerService = new CustomerService(_service);
        
        // Act
        var customerId = customerService.CreateCustomer("John Doe", "john@example.com");
        
        // Assert
        Assert.NotEqual(Guid.Empty, customerId);
        
        var customer = _service.Retrieve("contact", customerId, new ColumnSet("fullname", "emailaddress1"));
        Assert.Equal("John Doe", customer["fullname"]);
        Assert.Equal("john@example.com", customer["emailaddress1"]);
    }
}
```

## Summary

- ✅ **Best Practice**: Write code against `IOrganizationService` interface
- ✅ **For Testing**: Use WCF `ChannelFactory<IOrganizationService>` with Fake4DataverseService
- ✅ **For Production**: Use `ServiceClient` (implements `IOrganizationService`)
- ❌ **Don't**: Try to configure OAuth for testing scenarios
- ❌ **Don't**: Tightly couple code to `ServiceClient` concrete type

This approach gives you:
- Fast, reliable tests with Fake4DataverseService
- Production-ready code that works with real Dataverse
- No authentication complexity in tests
- Easy to maintain and understand
