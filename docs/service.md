# Fake4DataverseService: Network-Accessible Testing

**Implementation Date:** October 2025  
**Issue:** N/A (New feature)

> **💡 For ServiceClient Users:** See the [ServiceClient Compatibility Guide](../Fake4DataverseService/SERVICECLIENT.md) for patterns and best practices when testing existing ServiceClient-based code.

## Overview

Fake4DataverseService is a .NET 8.0 CLI application that exposes Fake4Dataverse as a network-accessible SOAP/WCF service. This enables integration testing scenarios where multiple applications or services need to interact with a fake Dataverse instance.

## Key Features

- **SOAP/WCF Protocol**: Standard `/XRMServices/2011/Organization.svc` endpoint matching real Dynamics 365
- **REST/OData v4.0 Protocol**: Standard `/api/data/v9.2` endpoints matching Dataverse Web API
- **Advanced OData Query Support**: Full $filter, $select, $orderby, $top, $skip, $count via Microsoft.AspNetCore.OData
- **No Authentication Required**: Bypasses OAuth complexity for testing scenarios
- **100% SDK Compatible**: Uses Microsoft's official Dataverse SDK types (IOrganizationService, Entity, etc.)
- **In-Memory Storage**: Fast, isolated test data powered by Fake4Dataverse
- **CLI Interface**: Simple command-line tool for starting/stopping the service

## When to Use Fake4DataverseService

### ✅ Good Use Cases

- **Integration Testing**: Testing multiple applications that communicate via Dataverse
- **Microservices Testing**: Multiple services sharing a common data layer
- **CI/CD Pipelines**: Fast, isolated test environments without real Dataverse
- **Development**: Local development without cloud instance
- **Cross-Language Testing**: Any language with WCF support can connect

### ❌ Not Recommended For

- **Unit Testing**: Use in-process Fake4Dataverse instead (faster, simpler)
- **Production**: This is a testing tool only
- **Performance Testing**: In-memory storage has different characteristics than real Dataverse

## Quick Start

### 1. Install the Service

Install as a global .NET tool:

```bash
dotnet tool install --global Fake4Dataverse.Service
```

### 2. Start the Service

```bash
fake4dataverse start --port 5000 --host localhost
```

Or if building from source:

```bash
cd Fake4DataverseService/src/Fake4Dataverse.Service
dotnet run -- start --port 5000 --host localhost
```

### 3. Connect from Your Application

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

// Create WCF channel
var binding = new BasicHttpBinding
{
    MaxReceivedMessageSize = 2147483647,
    MaxBufferSize = 2147483647
};

var endpoint = new EndpointAddress("http://localhost:5000/XRMServices/2011/Organization.svc");
var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
var service = factory.CreateChannel();

// Use IOrganizationService as normal
var account = new Entity("account");
account["name"] = "Contoso Ltd";
var accountId = service.Create(account);
```

### 3. Run Your Tests

Your application can now interact with Fake4DataverseService exactly as it would with real Dataverse, using the standard IOrganizationService interface.

## Architecture

```
┌─────────────────────────────────┐
│   Your Application/Tests        │
│  (C#, Python, JavaScript, etc.) │
└────────────┬────────────────────┘
             │ SOAP/WCF
             │ (IOrganizationService)
┌────────────▼────────────────────┐
│   Fake4DataverseService         │
│   (CoreWCF Host)                │
└────────────┬────────────────────┘
             │ Direct calls
┌────────────▼────────────────────┐
│   Fake4Dataverse.Core           │
│   (In-Memory Context)           │
└─────────────────────────────────┘
```

## Connection Methods

### Recommended: WCF Channels

The recommended approach is using WCF `ChannelFactory<IOrganizationService>` directly:

```csharp
public static class Fake4DataverseClient
{
    public static IOrganizationService CreateService(
        string serviceUrl = "http://localhost:5000/XRMServices/2011/Organization.svc")
    {
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647,
            SendTimeout = TimeSpan.FromMinutes(20),
            ReceiveTimeout = TimeSpan.FromMinutes(20)
        };

        var endpoint = new EndpointAddress(serviceUrl);
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
        
        return factory.CreateChannel();
    }
}
```

**Benefits:**
- ✅ No authentication setup required
- ✅ Works immediately
- ✅ Same IOrganizationService interface as production
- ✅ Lightweight and fast

### Why Not ServiceClient/CrmServiceClient?

`ServiceClient` and `CrmServiceClient` are production-oriented clients that require OAuth authentication:

```csharp
// This won't work without OAuth setup
var client = new ServiceClient("Url=http://localhost:5000/...");  // ❌ Requires OAuth
```

While it's technically possible to implement OAuth token validation, it adds complexity without benefit for testing. The WCF channel approach is simpler and more direct.

**For ServiceClient Users:**

If you have existing code using ServiceClient, consider these approaches:

1. **Refactor for Testability (Recommended)**
   ```csharp
   // Production code accepts IOrganizationService
   public class MyService
   {
       public void DoWork(IOrganizationService org) 
       {
           // Your logic here
       }
   }
   
   // Production: Pass ServiceClient (implements IOrganizationService)
   var client = new ServiceClient(prodConnectionString);
   myService.DoWork(client);
   
   // Testing: Pass WCF channel to Fake4DataverseService
   var testService = Fake4DataverseClient.CreateService();
   myService.DoWork(testService);
   ```

2. **Create Test Wrapper**
   ```csharp
   // Wrap WCF channel with same interface as ServiceClient
   public class TestOrganizationServiceWrapper : IOrganizationService
   {
       private readonly IOrganizationService _inner;
       
       public TestOrganizationServiceWrapper(IOrganizationService inner)
       {
           _inner = inner;
       }
       
       // Implement IOrganizationService by delegating to _inner
   }
   ```

3. **OAuth Setup (Advanced, Not Recommended for Testing)**
   - Requires Azure AD app registration and OAuth configuration
   - Adds significant complexity for minimal benefit in testing scenarios

## Supported Operations

The service implements the full `IOrganizationService` interface:

| Method | Description | Status |
|--------|-------------|--------|
| **Create** | Create entity records | ✅ Full support |
| **Retrieve** | Retrieve by ID with column sets | ✅ Full support |
| **Update** | Update entity records | ✅ Full support |
| **Delete** | Delete entity records | ✅ Full support |
| **Associate** | Create entity relationships | ✅ Full support |
| **Disassociate** | Remove entity relationships | ✅ Full support |
| **RetrieveMultiple** | Query with QueryExpression, FetchXML | ✅ Full support |
| **Execute** | Execute organization requests | ✅ Supported (depends on Fake4Dataverse executors) |

Reference: [IOrganizationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice)

## Configuration

### Command-Line Options

```bash
Fake4Dataverse.Service start [options]

Options:
  --port <port>    Port to listen on (default: 5000)
  --host <host>    Host to bind to (default: localhost)
```

### Service Endpoints

Once started, the service exposes:

- **SOAP Service**: `http://<host>:<port>/XRMServices/2011/Organization.svc`
- **WSDL**: `http://<host>:<port>/XRMServices/2011/Organization.svc?wsdl`
- **Status Check**: `http://<host>:<port>/`

## Examples

### Basic CRUD Operations

```csharp
var service = Fake4DataverseClient.CreateService();

// Create
var account = new Entity("account");
account["name"] = "Fabrikam Inc";
account["revenue"] = new Money(500000m);
var accountId = service.Create(account);

// Retrieve
var retrieved = service.Retrieve("account", accountId, new ColumnSet("name", "revenue"));
Console.WriteLine($"Name: {retrieved["name"]}");

// Update
var update = new Entity("account", accountId);
update["name"] = "Fabrikam Corporation";
service.Update(update);

// Delete
service.Delete("account", accountId);
```

### Query Operations

```csharp
// QueryExpression
var query = new QueryExpression("account");
query.ColumnSet.AddColumns("name", "revenue");
query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 100000m);

var results = service.RetrieveMultiple(query);
foreach (var entity in results.Entities)
{
    Console.WriteLine($"{entity["name"]}: ${((Money)entity["revenue"]).Value}");
}

// FetchXML
var fetchXml = @"
    <fetch>
        <entity name='account'>
            <attribute name='name' />
            <filter>
                <condition attribute='name' operator='like' value='%Corp%' />
            </filter>
        </entity>
    </fetch>";

var fetchResults = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

### Execute Requests

```csharp
using Microsoft.Crm.Sdk.Messages;

// WhoAmI
var whoAmIRequest = new WhoAmIRequest();
var whoAmIResponse = (WhoAmIResponse)service.Execute(whoAmIRequest);
Console.WriteLine($"User ID: {whoAmIResponse.UserId}");
```

## Testing Strategies

### Integration Test Pattern

```csharp
[TestClass]
public class MultiServiceIntegrationTests
{
    private static Process _serviceProcess;
    private static IOrganizationService _service;

    [ClassInitialize]
    public static void StartService(TestContext context)
    {
        // Start Fake4DataverseService
        _serviceProcess = Process.Start("dotnet", 
            "run --project Fake4Dataverse.Service -- start --port 5000");
        
        // Wait for startup
        Thread.Sleep(5000);
        
        // Create client
        _service = Fake4DataverseClient.CreateService();
    }

    [ClassCleanup]
    public static void StopService()
    {
        _serviceProcess?.Kill();
        _serviceProcess?.Dispose();
    }

    [TestMethod]
    public void TestMultiServiceScenario()
    {
        // Your integration test using _service
    }
}
```

### CI/CD Pipeline Usage

```yaml
# Example GitHub Actions workflow
steps:
  - name: Start Fake4DataverseService
    run: |
      cd Fake4DataverseService/src/Fake4Dataverse.Service
      dotnet run -- start --port 5000 &
      sleep 5
    
  - name: Run Integration Tests
    run: dotnet test ./IntegrationTests
    
  - name: Stop Service
    if: always()
    run: pkill -f Fake4Dataverse.Service
```

## Limitations

- **No Persistence**: Data is stored in-memory only. Service restart clears all data.
- **Single Tenant**: One shared context for all clients. Not suitable for multi-tenant testing.
- **No Authentication**: Service accepts all connections. Not for production use.
- **Dataverse Feature Coverage**: Limited to what Fake4Dataverse supports (see [Message Executors](../messages/README.md))

## Troubleshooting

### Port Already in Use

```bash
# Check what's using the port
lsof -i :5000

# Use a different port
Fake4Dataverse.Service start --port 5001
```

### Connection Refused

- Ensure the service has fully started (check console output)
- Verify the port matches in both service and client
- Check firewall settings for local connections

### Service Crashes

- Check console output for exceptions
- Ensure .NET 8.0 SDK is installed
- Try building the service first: `dotnet build`

## Comparison with In-Process Testing

| Aspect | In-Process Fake4Dataverse | Fake4DataverseService |
|--------|---------------------------|------------------------|
| **Speed** | ⚡ Fastest | 🐢 Network overhead |
| **Setup** | 📦 NuGet package only | 🔧 Start service process |
| **Isolation** | ✅ Per test | ⚠️ Shared across clients |
| **Use Case** | Unit testing | Integration testing |
| **Multi-language** | ❌ .NET only | ✅ Any WCF-compatible |
| **CI/CD** | ✅ Simple | 🔧 Requires service management |

**Recommendation**: Use in-process Fake4Dataverse for unit tests, Fake4DataverseService for integration tests.

## See Also

- [Fake4DataverseService README](../../Fake4DataverseService/README.md) - Full service documentation
- [IOrganizationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice) - Microsoft documentation
- [Organization Service Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview) - Dataverse SOAP service
- [WCF Channel Factory](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-create-a-wcf-client) - Creating WCF clients

## Contributing

The Fake4DataverseService is a new addition. Contributions welcome for:

- Additional authentication options (if there's demand)
- Performance optimizations
- Extended client examples (Python, JavaScript, etc.)
- Docker containerization
- Additional endpoint versions for backward compatibility

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## REST/OData Endpoints

In addition to SOAP/WCF endpoints, Fake4DataverseService also provides REST/OData v4.0 endpoints compatible with the Dataverse Web API.

**Base URL:** `http://localhost:5000/api/data/v9.2`

### Quick Example

```bash
# List accounts with OData query
curl "http://localhost:5000/api/data/v9.2/accounts?\$filter=revenue gt 100000&\$select=name,revenue"

# Create a new account
curl -X POST http://localhost:5000/api/data/v9.2/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Account","revenue":150000}'

# Retrieve an account by ID
curl http://localhost:5000/api/data/v9.2/accounts(12345678-1234-1234-1234-123456789012)

# Update an account
curl -X PATCH http://localhost:5000/api/data/v9.2/accounts(guid) \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Name"}'

# Delete an account
curl -X DELETE http://localhost:5000/api/data/v9.2/accounts(guid)
```

### Supported Features

- ✅ Full CRUD operations (GET, POST, PATCH, DELETE)
- ✅ Advanced OData query options via Microsoft.AspNetCore.OData v9.4.0
  - `$select` - Choose specific columns
  - `$filter` - Complex filter expressions with full OData syntax
  - `$orderby` - Sort results
  - `$top` - Limit results
  - `$skip` - Pagination
  - `$count` - Include total count
  - `$expand` - Include related entities
- ✅ OData v4.0 compliance
- ✅ Automatic type conversion (OptionSet, Money, EntityReference, etc.)

**For complete REST API documentation, see [REST/OData API Documentation](./rest-api.md)**

