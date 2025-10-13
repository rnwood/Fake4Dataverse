# Fake4Dataverse Service

A .NET 8.0 CLI service that hosts a fake IOrganizationService backed by Fake4Dataverse. This service provides **100% compatibility** with Microsoft Dataverse SDK types and uses **SOAP/WCF** protocol matching the actual Dynamics 365/Dataverse Organization Service endpoints.

## Overview

Fake4DataverseService exposes the Fake4Dataverse testing framework as a SOAP/WCF service, allowing clients to interact with a fake Dataverse/Dynamics 365 organization service over the network using the standard SOAP protocol. This enables:

- **Integration testing** using real Microsoft SDK clients (CrmServiceClient, ServiceClient)
- **Development and debugging** without needing a live Dataverse instance
- **Continuous integration** with fast, isolated tests
- **SDK compatibility** - Works with any tool or library that uses IOrganizationService

## Features

- **Full IOrganizationService support**: All standard Dataverse operations (Create, Retrieve, Update, Delete, Associate, Disassociate, RetrieveMultiple, Execute)
- **SOAP/WCF protocol**: Uses standard SOAP 1.1/1.2 protocol matching Microsoft's implementation
- **Standard endpoints**: `/XRMServices/2011/Organization.svc` (matching actual Dynamics 365)
- **WSDL support**: Full WSDL available for service discovery
- **Microsoft SDK types**: Uses official `Microsoft.PowerPlatform.Dataverse.Client` types for 100% compatibility
- **CLI interface**: Simple command-line interface for starting/stopping the service
- **Configurable**: Specify host and port via command-line arguments
- **In-memory storage**: Fast, isolated test data powered by Fake4Dataverse

## Installation

### Prerequisites

- .NET 8.0 SDK or later

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

## Client Examples

### C# Client with ServiceClient

```csharp
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// Connect to the Fake4Dataverse service
var connectionString = "Url=http://localhost:5000/XRMServices/2011/Organization.svc;";
var serviceClient = new ServiceClient(connectionString);

// Create an account
var account = new Entity("account");
account["name"] = "Contoso Ltd";
account["revenue"] = new Money(100000m);

var accountId = serviceClient.Create(account);
Console.WriteLine($"Created account with ID: {accountId}");

// Retrieve the account
var retrievedAccount = serviceClient.Retrieve("account", accountId, new ColumnSet("name", "revenue"));
Console.WriteLine($"Account name: {retrievedAccount["name"]}");

// Query accounts
var query = new QueryExpression("account");
query.ColumnSet.AddColumns("name", "revenue");
query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 50000m);

var results = serviceClient.RetrieveMultiple(query);
Console.WriteLine($"Found {results.Entities.Count} high-revenue accounts");
```

### C# Client with CrmServiceClient

```csharp
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;

// Connect to the service
var connectionString = "Url=http://localhost:5000/XRMServices/2011/Organization.svc;";
var service = new CrmServiceClient(connectionString);

// Create a contact
var contact = new Entity("contact");
contact["firstname"] = "John";
contact["lastname"] = "Doe";
contact["emailaddress1"] = "john.doe@example.com";

var contactId = service.Create(contact);
Console.WriteLine($"Created contact: {contactId}");
```

### Using Standard OrganizationRequest Messages

```csharp
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

// Execute WhoAmI request
var whoAmIRequest = new WhoAmIRequest();
var whoAmIResponse = (WhoAmIResponse)serviceClient.Execute(whoAmIRequest);
Console.WriteLine($"User ID: {whoAmIResponse.UserId}");

// Execute RetrieveVersion request
var versionRequest = new RetrieveVersionRequest();
var versionResponse = (RetrieveVersionResponse)serviceClient.Execute(versionRequest);
Console.WriteLine($"Version: {versionResponse.Version}");
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

- **Microsoft SDK**: Standard CrmServiceClient or ServiceClient
- **PowerShell**: Using Microsoft.Xrm.Tooling.CrmConnector.PowerShell
- **Plugin Registration Tool**: Configure it to connect to the local service
- **Custom client applications**: Any tool that supports SOAP/WCF

Example PowerShell test:

```powershell
# Install module if needed
Install-Module Microsoft.Xrm.Tooling.CrmConnector.PowerShell

# Connect
$conn = Get-CrmConnection -ConnectionString "Url=http://localhost:5000/XRMServices/2011/Organization.svc;"

# Create a record
$account = @{
    "name" = "Test Account"
    "revenue" = [decimal]100000
}
New-CrmRecord -conn $conn -EntityLogicalName "account" -Fields $account
```

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
