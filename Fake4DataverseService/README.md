# Fake4Dataverse Service

A .NET 8.0 CLI service that hosts a fake IOrganizationService backed by Fake4Dataverse. This service provides 100% compatibility with Microsoft Dataverse SDK types, making it ideal for testing and development scenarios.

## Overview

Fake4DataverseService exposes the Fake4Dataverse testing framework as a gRPC service, allowing clients to interact with a fake Dataverse/Dynamics 365 organization service over the network. This enables:

- **Integration testing** across different applications and services
- **Development and debugging** without needing a live Dataverse instance
- **Continuous integration** with fast, isolated tests
- **Multi-language support** via gRPC clients (C#, Python, JavaScript, etc.)

## Features

- **Full IOrganizationService support**: All standard Dataverse operations (Create, Retrieve, Update, Delete, Associate, Disassociate, RetrieveMultiple, Execute)
- **gRPC-based communication**: High-performance, cross-platform protocol
- **Microsoft SDK types**: Uses official `Microsoft.PowerPlatform.Dataverse.Client` types for 100% compatibility
- **CLI interface**: Simple command-line interface for starting/stopping the service
- **Configurable**: Specify host and port via command-line arguments
- **In-memory storage**: Fast, isolated test data powered by Fake4Dataverse

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- (Optional) grpc_cli or other gRPC tools for testing

### Build from Source

```bash
cd Fake4DataverseService/src/Fake4Dataverse.Service
dotnet build -c Release
```

### Run

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

```bash
Fake4Dataverse.Service start [options]

Options:
  --port <port>    The port to listen on (default: 5000)
  --host <host>    The host to bind to (default: localhost)
```

Example:

```bash
# Start on default port (5000)
Fake4Dataverse.Service start

# Start on custom port
Fake4Dataverse.Service start --port 8080

# Bind to all interfaces
Fake4Dataverse.Service start --host 0.0.0.0 --port 5000
```

### Service Endpoints

Once started, the service exposes:

- **gRPC endpoint**: `http://<host>:<port>` - For gRPC clients
- **HTTP endpoint**: `http://<host>:<port>/` - Simple status check

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

## Protocol Buffer Definition

The service uses gRPC with the following proto definition located at `Protos/organizationservice.proto`. Key message types include:

- `CreateRequest` / `CreateResponse` - Create entity operations
- `RetrieveRequest` / `RetrieveResponse` - Retrieve entity operations
- `UpdateRequest` / `UpdateResponse` - Update entity operations
- `DeleteRequest` / `DeleteResponse` - Delete entity operations
- `EntityRecord` - Represents a Dataverse entity
- `AttributeValue` - Flexible attribute value type supporting all Dataverse data types

## Client Examples

### C# Client Example

```csharp
using Grpc.Net.Client;
using Fake4Dataverse.Service.Grpc;

// Create channel
var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new OrganizationService.OrganizationServiceClient(channel);

// Create an account
var createRequest = new CreateRequest
{
    EntityLogicalName = "account",
    Attributes =
    {
        { "name", new AttributeValue { StringValue = "Contoso" } },
        { "revenue", new AttributeValue { DoubleValue = 100000.0 } }
    }
};

var response = await client.CreateAsync(createRequest);
Console.WriteLine($"Created account with ID: {response.Id}");

// Retrieve the account
var retrieveRequest = new RetrieveRequest
{
    EntityLogicalName = "account",
    Id = response.Id,
    Columns = { "name", "revenue" }
};

var account = await client.RetrieveAsync(retrieveRequest);
Console.WriteLine($"Account name: {account.Entity.Attributes["name"].StringValue}");
```

### Python Client Example

```python
import grpc
from organizationservice_pb2 import CreateRequest, AttributeValue
from organizationservice_pb2_grpc import OrganizationServiceStub

# Create channel
channel = grpc.insecure_channel('localhost:5000')
client = OrganizationServiceStub(channel)

# Create an account
request = CreateRequest(
    entity_logical_name="account",
    attributes={
        "name": AttributeValue(string_value="Contoso"),
        "revenue": AttributeValue(double_value=100000.0)
    }
)

response = client.Create(request)
print(f"Created account with ID: {response.id}")
```

## Architecture

The Fake4DataverseService is built on:

- **Fake4Dataverse.Core**: In-memory organization service implementation
- **gRPC/ASP.NET Core**: Modern, high-performance service hosting
- **System.CommandLine**: CLI argument parsing
- **Microsoft.PowerPlatform.Dataverse.Client**: Official Dataverse SDK types

### Data Flow

```
Client Application (any language)
    ↓ gRPC call
OrganizationServiceImpl (gRPC service)
    ↓ converts proto messages to SDK types
IOrganizationService (Fake4Dataverse)
    ↓ processes request
In-Memory Data Store (XrmFakedContext)
```

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

- **grpc_cli**: Command-line tool for gRPC services
- **Postman**: Supports gRPC requests
- **BloomRPC**: GUI client for gRPC
- **Custom client applications**: Using generated gRPC stubs

Example using grpc_cli:

```bash
# List services
grpc_cli ls localhost:5000

# Describe service
grpc_cli ls localhost:5000 organizationservice.OrganizationService -l

# Call method
grpc_cli call localhost:5000 organizationservice.OrganizationService.Create \
  'entity_logical_name: "account" attributes: { key: "name" value: { string_value: "Test" }}'
```

## Limitations

- **Execute method**: Currently not fully implemented. Extend `OrganizationServiceImpl.Execute()` for specific request types
- **Complex data types**: Some complex Dataverse types (e.g., PartyList, AliasedValue) may require additional mapping
- **No persistence**: All data is stored in-memory and lost when the service stops
- **Single tenant**: One shared context for all clients

## Development

### Adding New Features

1. Update the proto file (`Protos/organizationservice.proto`) if adding new operations
2. Implement in `OrganizationServiceImpl.cs`
3. Rebuild the project (gRPC stubs are auto-generated)
4. Test with client applications

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

### gRPC client connection issues

- Ensure you're using HTTP/2 protocol
- For .NET clients, use `GrpcChannel.ForAddress()` with HTTP (not HTTPS) for local testing
- Check firewall settings if connecting remotely

### Data not persisting

This is expected behavior. The service uses in-memory storage that resets on restart. To persist data between sessions, you would need to implement custom serialization.

## License

MIT License - See [LICENSE.txt](../../LICENSE.txt)

## References

- [Fake4Dataverse Documentation](../../../docs/README.md)
- [Microsoft Dataverse SDK Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk)
- [IOrganizationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice)
- [gRPC Documentation](https://grpc.io/docs/)
- [ASP.NET Core gRPC](https://learn.microsoft.com/en-us/aspnet/core/grpc/)

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/rnwood/fake-xrm-free).
