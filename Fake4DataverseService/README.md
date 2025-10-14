# Fake4Dataverse Service

A .NET 8.0 CLI service that hosts a fake IOrganizationService backed by Fake4Dataverse. This service provides **100% compatibility** with Microsoft Dataverse SDK types and uses **SOAP/WCF** protocol matching the actual Dynamics 365/Dataverse Organization Service endpoints.

> **✨ ServiceClient Support:** The service now supports optional token-based authentication, allowing ServiceClient connections using `AuthType=OAuth;Url=...;AccessToken=...` connection strings. See [Authentication](#authentication) below.

## Overview

Fake4DataverseService exposes the Fake4Dataverse testing framework as a SOAP/WCF service, allowing clients to interact with a fake Dataverse/Dynamics 365 organization service over the network using the standard SOAP protocol. This enables:

- **Integration testing** using standard WCF channels (IOrganizationService interface)
- **ServiceClient testing** with simple token-based authentication (no OAuth setup required)
- **Development and debugging** without needing a live Dataverse instance
- **Continuous integration** with fast, isolated tests
- **SDK compatibility** - Works with any tool or library that uses IOrganizationService via WCF
- **Optional authentication** - Run with or without access token validation

## Features

- **Full IOrganizationService support**: All standard Dataverse operations (Create, Retrieve, Update, Delete, Associate, Disassociate, RetrieveMultiple, Execute)
- **SOAP/WCF protocol**: Uses standard SOAP 1.1/1.2 protocol matching Microsoft's implementation
- **Standard endpoints**: `/XRMServices/2011/Organization.svc` (matching actual Dynamics 365)
- **WSDL support**: Full WSDL available for service discovery
- **Microsoft SDK types**: Uses official `Microsoft.PowerPlatform.Dataverse.Client` types for 100% compatibility
- **CLI interface**: Simple command-line interface for starting/stopping the service
- **Configurable**: Specify host and port via command-line arguments
- **In-memory storage**: Fast, isolated test data powered by Fake4Dataverse
- **No authentication** - Perfect for testing without OAuth setup

## Installation

### Prerequisites

- .NET 8.0 SDK or later

### Option 1: Install as a Global Tool (Recommended)

Install the service as a global .NET tool from NuGet:

```bash
dotnet tool install --global Fake4DataverseService
```

Or update an existing installation:

```bash
dotnet tool update --global Fake4DataverseService
```

After installation, the `fake4dataverse` command will be available globally:

```bash
fake4dataverse start --port 5000 --host localhost
```

### Option 2: Install as a Local Tool

Install as a project-local tool:

```bash
dotnet tool install Fake4DataverseService
```

Then run using:

```bash
dotnet fake4dataverse start --port 5000 --host localhost
```

### Option 3: Build from Source

```bash
cd Fake4DataverseService/src/Fake4Dataverse.Service
dotnet build -c Release
```

### Run from Source

```bash
dotnet run -- start --port 5000 --host localhost
```

Or after building:

```bash
cd bin/Release/net8.0
./Fake4Dataverse.Service start --port 5000 --host localhost
```

## Usage

### Starting the Service

#### Without Authentication (Anonymous Access)

If installed as a global tool:

```bash
fake4dataverse start [options]

Options:
  --port <port>              The port to listen on (default: 5000)
  --host <host>              The host to bind to (default: localhost)
  --access-token <token>     Optional access token for authentication
```

If built from source:

```bash
Fake4Dataverse.Service start [options]
```

**Examples:**

```bash
# Start with anonymous access (no authentication) - Global tool
fake4dataverse start

# Start with anonymous access - From source
Fake4Dataverse.Service start

# Start on custom port
fake4dataverse start --port 8080

# Bind to all interfaces
fake4dataverse start --host 0.0.0.0 --port 5000
```

#### With Authentication (Token Required)

```bash
# Start with access token authentication
Fake4Dataverse.Service start --access-token "my-secret-token-12345"

# Use with custom port
Fake4Dataverse.Service start --port 8080 --access-token "my-secret-token"
```

When started with `--access-token`, the service requires all HTTP requests to include an `Authorization: Bearer <token>` header.

### Service Endpoints

Once started, the service exposes:

- **SOAP endpoint**: `http://<host>:<port>/XRMServices/2011/Organization.svc`
- **WSDL endpoint**: `http://<host>:<port>/XRMServices/2011/Organization.svc?wsdl`
- **Status check**: `http://<host>:<port>/` - Simple text status

### Supported Operations

The service implements the following IOrganizationService methods:

| Method | Description | Reference |
|--------|-------------|-----------|
| **Create** | Creates a new entity record | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.create) |
| **Retrieve** | Retrieves an entity record by ID | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrieve) |
| **Update** | Updates an existing entity record | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.update) |
| **Delete** | Deletes an entity record | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.delete) |
| **Associate** | Associates two entity records | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.associate) |
| **Disassociate** | Disassociates two entity records | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.disassociate) |
| **RetrieveMultiple** | Retrieves multiple entity records | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrievemultiple) |
| **Execute** | Executes an organization request | [MSDN](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.execute) |

## Authentication

### Anonymous Mode (Default)

By default, the service runs without authentication, allowing any client to connect:

```bash
Fake4Dataverse.Service start
```

### Token-Based Authentication

For scenarios where you want basic access control, start the service with an access token:

```bash
Fake4Dataverse.Service start --access-token "your-secret-token"
```

When authentication is enabled:
- All HTTP requests must include: `Authorization: Bearer <token>` header
- WCF/SOAP clients must add custom headers (see examples below)
- ServiceClient can connect using the token in the connection string

## Client Examples

### ServiceClient with Access Token

When the service is started with `--access-token`, you can connect using ServiceClient:

```csharp
using Microsoft.PowerPlatform.Dataverse.Client;

// Start service with: dotnet run -- start --access-token "test-token-123"

string connectionString = $"AuthType=OAuth;Url=http://localhost:5000;AccessToken=test-token-123";
using var client = new ServiceClient(connectionString);

if (client.IsReady)
{
    var account = new Entity("account") { ["name"] = "Test Account" };
    var accountId = client.Create(account);
    Console.WriteLine($"Created account with ID: {accountId}");
}
```

**Note:** ServiceClient is designed for OAuth flows, so while it accepts the `AccessToken` parameter, it may have limitations. For simpler testing, WCF channels (below) are recommended.

### C# Client using WCF Channel (Recommended)

The recommended way to connect to Fake4DataverseService is using standard WCF channels.

#### Without Authentication

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

// Create WCF channel to connect to the service
var binding = new BasicHttpBinding
{
    MaxReceivedMessageSize = 2147483647,
    MaxBufferSize = 2147483647,
    SendTimeout = TimeSpan.FromMinutes(20),
    ReceiveTimeout = TimeSpan.FromMinutes(20)
};

var endpoint = new EndpointAddress("http://localhost:5000/XRMServices/2011/Organization.svc");
var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
var service = factory.CreateChannel();

// Create an account
var account = new Entity("account");
account["name"] = "Contoso Ltd";
account["revenue"] = new Money(100000m);

var accountId = service.Create(account);
Console.WriteLine($"Created account with ID: {accountId}");
```

#### With Authentication

When the service requires authentication, add a custom endpoint behavior to include the Authorization header:

```csharp
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

var binding = new BasicHttpBinding
{
    MaxReceivedMessageSize = 2147483647,
    MaxBufferSize = 2147483647
};

var endpoint = new EndpointAddress("http://localhost:5000/XRMServices/2011/Organization.svc");
var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);

// Add authorization header
factory.Endpoint.EndpointBehaviors.Add(new AuthHeaderEndpointBehavior("your-secret-token"));

var service = factory.CreateChannel();

// Use service as normal
var account = new Entity("account") { ["name"] = "Test Account" };
var accountId = service.Create(account);
```

<details>
<summary>Click to see AuthHeaderEndpointBehavior implementation</summary>

```csharp
public class AuthHeaderEndpointBehavior : IEndpointBehavior
{
    private readonly string _accessToken;

    public AuthHeaderEndpointBehavior(string accessToken)
    {
        _accessToken = accessToken;
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    {
        clientRuntime.ClientMessageInspectors.Add(new AuthHeaderMessageInspector(_accessToken));
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
    public void Validate(ServiceEndpoint endpoint) { }
}

public class AuthHeaderMessageInspector : IClientMessageInspector
{
    private readonly string _accessToken;

    public AuthHeaderMessageInspector(string accessToken)
    {
        _accessToken = accessToken;
    }

    public object? BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var httpRequest = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
        if (httpRequest == null)
        {
            httpRequest = new HttpRequestMessageProperty();
            request.Properties[HttpRequestMessageProperty.Name] = httpRequest;
        }
        httpRequest.Headers["Authorization"] = $"Bearer {_accessToken}";
        return null;
    }

    public void AfterReceiveReply(ref Message reply, object? correlationState) { }
}
```
</details>

### Using Execute for Organization Requests

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

// Execute WhoAmI request
var whoAmIRequest = new WhoAmIRequest();
var whoAmIResponse = (WhoAmIResponse)service.Execute(whoAmIRequest);
Console.WriteLine($"User ID: {whoAmIResponse.UserId}");
```

### Helper Class for Connection Management

For convenience, you can create a helper class to manage connections:

```csharp
public static class Fake4DataverseClient
{
    public static IOrganizationService CreateService(string serviceUrl = "http://localhost:5000/XRMServices/2011/Organization.svc")
    {
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647
        };

        var endpoint = new EndpointAddress(serviceUrl);
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
        
        return factory.CreateChannel();
    }
}

// Usage
var service = Fake4DataverseClient.CreateService();
var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
```

### ServiceClient Support

✅ **ServiceClient is now supported!** Start the service with `--access-token` to enable simple token-based authentication.

```bash
Fake4Dataverse.Service start --access-token "your-token"
```

Then connect using ServiceClient:

```csharp
string connectionString = $"AuthType=OAuth;Url=http://localhost:5000;AccessToken=your-token";
using var client = new ServiceClient(connectionString);
```

**When to use ServiceClient vs WCF Channels:**

| Scenario | Recommended Approach |
|----------|---------------------|
| **Existing code uses ServiceClient** | ✅ Use `--access-token` with ServiceClient connection string |
| **New test code** | ✅ Use WCF channels (simpler, no token needed) |
| **Testing without auth** | ✅ Use WCF channels with anonymous mode |
| **Need to test auth flows** | ✅ Use `--access-token` mode |

**For Refactoring Existing Code:**

If you have existing code that uses ServiceClient and want maximum testability:

1. **Best Practice: Accept IOrganizationService interface**
   ```csharp
   // Accepts both ServiceClient and WCF channels
   public class AccountService
   {
       public Guid CreateAccount(IOrganizationService service, string name)
       {
           return service.Create(new Entity("account") { ["name"] = name });
       }
   }
   ```

2. **Use dependency injection**
   - Production: Inject ServiceClient instance
   - Testing: Inject WCF channel or use Fake4DataverseService with token

**Example: Refactoring for Testability**

```csharp
// Before: Tightly coupled to ServiceClient
public class AccountService
{
    public Guid CreateAccount(ServiceClient client, string name)
    {
        return client.Create(new Entity("account") { ["name"] = name });
    }
}

// After: Accepts IOrganizationService interface (testable!)
public class AccountService
{
    public Guid CreateAccount(IOrganizationService service, string name)
    {
        return service.Create(new Entity("account") { ["name"] = name });
    }
}

// In production: Pass ServiceClient (it implements IOrganizationService)
var client = new ServiceClient(connectionString);
var accountService = new AccountService();
accountService.CreateAccount(client, "Contoso");

// In tests: Pass WCF channel
var service = Fake4DataverseClient.CreateService();
var accountService = new AccountService();
accountService.CreateAccount(service, "Test Account");
```

## Architecture

The Fake4DataverseService is built on:

- **Fake4Dataverse.Core**: In-memory organization service implementation
- **CoreWCF**: Modern WCF implementation for .NET Core/5+
- **ASP.NET Core**: Modern hosting infrastructure
- **System.CommandLine**: CLI argument parsing
- **Microsoft.PowerPlatform.Dataverse.Client**: Official Dataverse SDK types

### Data Flow

```
Client Application (C#, PowerShell, etc.)
    ↓ SOAP call (HTTP)
CoreWCF Service Host
    ↓ IOrganizationService interface
OrganizationServiceImpl (WCF service)
    ↓ native SDK types
Fake4Dataverse Context
    ↓
In-Memory Data Store (XrmFakedContext)
```

### Endpoint Structure

The service uses the same endpoint structure as Microsoft Dynamics 365/Dataverse:

- `/XRMServices/2011/Organization.svc` - Standard Organization Service endpoint
- `/XRMServices/2011/Organization.svc?wsdl` - WSDL definition

Reference: [Microsoft Dynamics 365 Organization Service](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview#about-the-legacy-soap-endpoint)

## Configuration

### Context Initialization

The service uses `XrmFakedContextFactory.New()` to create the Fake4Dataverse context with default settings. To customize:

1. Modify `Program.cs` in the `StartService` method
2. Use middleware configuration to add custom behaviors
3. Pre-populate data by calling `context.Initialize(entities)` before starting

Example customization:

```csharp
// In Program.cs, replace:
var context = XrmFakedContextFactory.New();

// With:
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();

// Pre-populate with test data
var testAccount = new Entity("account", Guid.NewGuid())
{
    ["name"] = "Test Account"
};
context.Initialize(new[] { testAccount });
```

## Testing

The service can be tested using:

- **WCF Channels**: Standard ChannelFactory<IOrganizationService> (recommended for testing)
- **Custom client applications**: Any tool that supports SOAP/WCF and IOrganizationService
- **Plugin Registration Tool**: May work if configured to connect to the local service (not tested)

Note: ServiceClient and CrmServiceClient require OAuth authentication setup and are not recommended for simple testing scenarios. Use WCF channels directly as shown in the examples above.

## Limitations

- **No authentication**: The service does not implement authentication (suitable for testing only)
- **No persistence**: All data is stored in-memory and lost when the service stops
- **Single tenant**: One shared context for all clients
- **SOAP only**: Does not implement the Web API (OData) endpoint

## Development

### Adding New Features

1. Extend `OrganizationServiceImpl.cs` for custom request handling
2. Update the Fake4Dataverse context configuration in `Program.cs`
3. Rebuild the project
4. Test with Microsoft SDK clients

### Contributing

See the main [Fake4Dataverse README](../../../README.md) for contribution guidelines.

## Troubleshooting

### Port already in use

```bash
# Check what's using the port
lsof -i :5000

# Use a different port
Fake4Dataverse.Service start --port 5001
```

### Connection issues

- Ensure you're using HTTP (not HTTPS) for local testing
- Check firewall settings if connecting remotely
- Verify the service is running by accessing `http://localhost:5000/`

### WSDL not loading

- Ensure the service has started successfully
- Access the WSDL directly: `http://localhost:5000/XRMServices/2011/Organization.svc?wsdl`
- Check the console output for any errors

## License

MIT License - See [LICENSE.txt](../../LICENSE.txt)

## References

- [Fake4Dataverse Documentation](../../../docs/README.md)
- [Microsoft Dataverse SDK Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk)
- [IOrganizationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice)
- [Organization Service Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview)
- [CoreWCF Documentation](https://github.com/CoreWCF/CoreWCF)

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/rnwood/fake-xrm-free).
