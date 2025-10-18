# Fake4Dataverse: Test Automation Framework for Microsoft Dataverse and Power Platform

## About This Project

Fake4Dataverse is an open-source testing framework for Microsoft Dynamics 365 / Dataverse and the Power Platform. Originally forked from FakeXrmEasy by Jordi Montaña, this project has evolved with a **different vision and direction** focused on comprehensive testing capabilities including network-accessible services, REST/OData endpoints, and modern integration testing patterns.

> [!NOTE]
>
> **A New Direction**: While Fake4Dataverse started as a fork of FakeXrmEasy, it has grown beyond a compatibility layer. This project focuses on providing comprehensive testing infrastructure including CLI services, REST/OData endpoints, and web interfaces - capabilities designed for modern integration and end-to-end testing scenarios that extend beyond the original framework's scope.

**Author:** Rob Wood  
**Original Author:** Jordi Montaña (@jordimontana)  
**License:** MIT (see LICENSE.txt files in each project folder)

## 🌐 Fake4DataverseService - Network-Accessible Testing

Fake4Dataverse includes a CLI service that exposes both SOAP/WCF and REST/OData endpoints for integration testing across services and applications.

### SOAP/WCF Endpoints
- **SOAP/WCF Protocol**: Uses standard `/XRMServices/2011/Organization.svc` endpoint
- **SDK Compatible**: Works with standard WCF channels and IOrganizationService interface
- **Type Compatible**: Uses Microsoft's official Dataverse SDK types

### REST/OData v4.0 Endpoints
- **OData v4.0 Protocol**: Uses `/api/data/v9.2` endpoint
- **Advanced Queries**: Full $filter, $select, $orderby, $top, $skip, $expand support
- **Microsoft.AspNetCore.OData**: Leverages official Microsoft OData library
- **JSON Format**: Standard Dataverse Web API JSON format with @odata annotations

### Common Features
- **Integration Testing**: Test across multiple services or applications
- **No Authentication Required**: Bypass OAuth for testing - just connect and go

### Roadmap: Expanding Integration Testing Capabilities

We're actively working to enhance Fake4DataverseService with additional testing capabilities:

#### Planned Features
- **📄 Web Resources Support**: Serve custom JavaScript, CSS, HTML, and image files for testing client-side code
- **🔌 Plugin Registration via SDK**: Register plugins programmatically using the SDK plugin registration messages
- **🌐 Enhanced REST API Support**: Additional OData endpoints and query capabilities
- **📊 Custom API Execution**: Full support for Custom API invocation via REST endpoints
- **🔐 OAuth Simulation**: Optional authentication simulation for testing security flows
- **📱 Power Apps Component Framework (PCF)**: Support for testing PCF controls with mock Dataverse context

#### Future Integrations
- **Power Automate Cloud Flow Testing**: Enhanced flow testing with HTTP trigger endpoints
- **Canvas App Testing**: Serve connectors and data sources for Canvas App testing
- **Model-Driven App Extensions**: Full web resource and form script testing support

**[Learn more about Fake4DataverseService →](./Fake4DataverseService/README.md)** | **[REST API Documentation →](./docs/rest-api.md)**

```bash
# Start the service
cd Fake4DataverseService/Fake4Dataverse.Service
dotnet run -- start --port 5000

# Connect from any application using standard WCF
var binding = new BasicHttpBinding();
var endpoint = new EndpointAddress("http://localhost:5000/XRMServices/2011/Organization.svc");
var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);
var service = factory.CreateChannel();
```

### Fork Basis

This fork is based on an early development version of FakeXrmEasy v2, which was released under the MIT License. The original FakeXrmEasy project subsequently changed its licensing model. This fork preserves the last MIT-licensed version to ensure continued open-source availability for the community.

**Important**: Fake4Dataverse has evolved significantly beyond the original fork with its own architecture decisions, feature set, and direction. While it maintains the core testing principles, it is not designed to be a drop-in replacement or maintain API compatibility with FakeXrmEasy v2+.

**Original Repositories:**
- Core: https://github.com/DynamicsValue/fake-xrm-easy-core
- Abstractions: https://github.com/DynamicsValue/fake-xrm-easy-abstractions
- Legacy: https://github.com/jordimontana82/fake-xrm-easy

The original repositories were licensed under MIT at the time this fork was created, as evidenced by the LICENSE files in those repositories in the commit we forked from (the history is preserved in this repo).

## Why This Fork?

The original FakeXrmEasy project was an invaluable tool for the Dynamics 365 / Dataverse community, providing a comprehensive testing framework that enabled developers to write unit tests without requiring a live CRM instance. However, the original project moved to a commercial licensing model after version 2.x.

This fork serves several purposes:

1. **Preserve Open Source Access**: By forking from the last MIT-licensed version, we ensure that the community continues to have access to a free, open-source testing framework for Dataverse development.

2. **Community-Driven Development**: This fork is maintained by the community, for the community. We welcome contributions and aim to keep the project aligned with community needs.

3. **Different Direction and Vision**: Fake4Dataverse has evolved beyond a simple fork to include:
   - Network-accessible services for integration testing
   - REST/OData v4.0 endpoints
   - Web interface for visual testing
   - Focus on modern integration and end-to-end testing patterns
   - Extended capabilities for testing web resources, client-side code, and custom APIs

4. **Modern Platform Support**: We aim to support modern versions of Dataverse, Power Platform, and .NET while maintaining the testing-focused approach.

5. **Legal Clarity**: This fork is completely legal and in accordance with the MIT License under which the original FakeXrmEasy was released. The MIT License explicitly permits forking, modification, and redistribution of the code.

**Note**: Fake4Dataverse is not intended to be compatible with or a replacement for FakeXrmEasy v2+. It has its own architecture, API design, and feature set tailored to modern testing scenarios.

## Is Forking from the Last MIT Version Legal?

**Absolutely yes.** The MIT License is one of the most permissive open-source licenses and explicitly grants the rights to:
- Use the software for any purpose
- Make copies and distribute them
- Modify the source code
- Distribute modified versions

The original FakeXrmEasy was released under the MIT License, which means that version and all prior versions remain available under that license permanently. The license cannot be retroactively revoked. When Jordi Montaña chose to change the licensing model for future versions, previous MIT-licensed versions remained under the MIT License.

This fork:
- Is based on an early version 2.x, which was released under the MIT License
- Properly acknowledges the original author (Jordi Montaña) in all LICENSE files
- Maintains all original copyright notices as required by the MIT License
- Continues to use the MIT License for all derivatives

## Acknowledgments

We are deeply grateful to **Jordi Montaña** for creating FakeXrmEasy and releasing it under the MIT License. His work has been instrumental to thousands of developers in the Dynamics 365 and Power Platform community. Please consider supporting his commercial and original version.

## 📖 Documentation

**Complete documentation is available at [docs/README.md](./docs/README.md)**

### Quick Start
- **[Installation Guide](./docs/getting-started/installation.md)** - Get Fake4Dataverse installed (v9+ only)
- **[Quick Start](./docs/getting-started/quickstart.md)** - Your first test in 5 minutes

### 🔒 Security Model
Fake4Dataverse includes a **complete Dataverse security model** implementation for realistic security testing:

- **✅ Privilege-Based Access Control** - Users need specific privileges granted through roles
- **✅ Privilege Depth Enforcement** - Basic, Local, Deep, and Global access levels
- **✅ Role Shadow Copies** - Automatic role copying across business units
- **✅ Business Unit Hierarchy** - Traditional and modern BU security modes
- **✅ System Administrator Role** - Auto-initialized with implicit privileges
- **✅ Organization-Owned Entities** - Proper handling of system tables
- **✅ System Tables Readable by Everyone** - Matches Dataverse behavior

**[Learn more about Security Testing →](./docs/usage/security-model.md)**

```csharp
// Enable security with middleware
var builder = MiddlewareBuilder.New()
    .AddRoleLifecycle()  // Role lifecycle management
    .AddSecurity()       // Security enforcement
    .AddCrud();
    
var context = builder.Build();
context.SecurityConfiguration.SecurityEnabled = true;

// Set the calling user
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

// Operations are now checked against user's privileges
var service = context.GetOrganizationService();
```
- **[Basic Concepts](./docs/getting-started/basic-concepts.md)** - Understand the framework
- **[FAQ](./docs/getting-started/faq.md)** - Common questions and troubleshooting

### Core Guides
- **[Testing Plugins](./docs/usage/testing-plugins.md)** - Comprehensive plugin testing patterns
- **[CRUD Operations](./docs/usage/crud-operations.md)** - Create, Read, Update, Delete operations
- **[Querying Data](./docs/usage/querying-data.md)** - LINQ and FetchXML queries
- **[Batch Operations](./docs/usage/batch-operations.md)** - ExecuteMultiple and Transactions
- **[Metadata Validation](./docs/usage/metadata-validation.md)** - IsValidForCreate/Update/Read enforcement
- **[CDM Import](./docs/cdm-import.md)** - Import entity metadata from Common Data Model JSON
- **[Cloud Flows](./docs/usage/cloud-flows.md)** - Testing Power Automate flows

### Advanced Topics
- **[XrmFakedContext](./docs/concepts/xrm-faked-context.md)** - Deep dive into the context
- **[Middleware Architecture](./docs/concepts/middleware.md)** - Understanding the pipeline
- **[Message Executors](./docs/messages/README.md)** - All supported Dataverse messages
- **[Custom Executors](./docs/api/custom-executors.md)** - Creating your own executors
- **[Cloud Flows](./docs/usage/cloud-flows.md)** - Testing Power Automate flows
- **[Expression Language](./docs/expression-language.md)** - Power Automate expression evaluation
- **[Known Gaps & Limitations](./docs/GAPS.md)** - Comprehensive limitations guide

### Migration
- **[From FakeXrmEasy v1.x](./docs/migration/from-v1.md)** - Migrate from v1.x
- **[From FakeXrmEasy v3.x](./docs/migration/from-v3.md)** - Migrate from commercial v3.x

## Project Structure

This is a monorepo containing multiple projects:

### 1. Fake4DataverseAbstractions
- **Location**: `/Fake4DataverseAbstractions/`
- **Purpose**: Contains abstractions, interfaces, POCOs, enums, and base types used across the framework
- **Former Name**: FakeXrmEasy.Abstractions

### 2. Fake4DataverseCore  
- **Location**: `/Fake4DataverseCore/`
- **Purpose**: Core implementation including middleware, CRUD operations, query translation, and message executors
- **Former Name**: FakeXrmEasy.Core
- **Note**: Plugins and workflows are tested within Core (no separate plugin project)

### 3. Fake4DataverseCloudFlows
- **Location**: `/Fake4DataverseCloudFlows/`
- **Purpose**: Cloud Flow (Power Automate) simulation for testing automated flows
- **Target**: .NET 8.0 only (for advanced OData support via Microsoft.OData.Core)
- **Features**: Flow execution, expression evaluation, OData query support

### 4. Fake4DataverseService
- **Location**: `/Fake4DataverseService/`
- **Purpose**: Network-accessible service exposing SOAP/WCF and REST/OData endpoints
- **Features**: 
  - SOAP endpoint at `/XRMServices/2011/Organization.svc`
  - REST/OData v4.0 endpoints at `/api/data/v9.2`
  - Integration testing across services and applications

### 5. Fake4Dataverse (Legacy Package)
- **Location**: `/Fake4Dataverse/`
- **Purpose**: Legacy/compatibility package
- **Former Name**: FakeXrmEasy

For project-specific information, see:
- [Fake4DataverseAbstractions README](./Fake4DataverseAbstractions/README.md)
- [Fake4DataverseCore README](./Fake4DataverseCore/README.md)
- [Fake4DataverseCloudFlows](./Fake4DataverseCloudFlows/) (see test files for usage examples)
- [Fake4DataverseService README](./Fake4DataverseService/README.md)
- [Fake4Dataverse README](./Fake4Dataverse/README.md)

## Building and Testing

### Quick Start

Build the entire solution and run unit tests:

```bash
# Restore dependencies
dotnet restore Fake4Dataverse.sln

# Build all projects
dotnet build Fake4Dataverse.sln --configuration Debug --no-restore

# Run unit tests (excluding integration tests which require the service to be running)
dotnet test Fake4Dataverse.sln --configuration Debug --framework net8.0 --no-build --filter "FullyQualifiedName!~IntegrationTests"
```

### Build the MDA App

```bash
cd Fake4DataverseService/mda-app
npm ci                  # Install dependencies
npm test                # Run unit tests
npm run build           # Build Next.js app
```

### More Information

- **[Testing Guide](./docs/TESTING_GUIDE.md)** - Comprehensive guide to running all tests
- Each project has its own build script. See individual project READMEs for details.

## Feature Comparison: FakeXrmEasy v1 vs Fake4Dataverse vs FakeXrmEasy v2

The following table compares the features available across different versions of the testing framework. **Note**: Fake4Dataverse has evolved with its own architecture and is not designed to be compatible with or a replacement for FakeXrmEasy v2+. This comparison highlights the capabilities and differences between versions.

### Architecture & Core Framework

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **License** | MIT | MIT | Commercial/Subscription or reciprocal open source |
| **Middleware Architecture** | ❌ No - Single executor per request | ✅ Yes - ASP.NET Core inspired | ✅ Yes - Enhanced middleware |
| **Extensibility Model** | ⚠️ Limited | ✅ GetProperty/SetProperty pattern | ✅ Full extensibility |
| **Pipeline Configuration** | ❌ Fixed pipeline | ✅ Configurable middleware pipeline | ✅ Advanced pipeline control |
| **.NET Core 3.1 Support** | ❌ No | ✅ Yes | ✅ Yes |
| **.NET 6/7/8 Support** | ❌ No | ⚠️ .NET 8.0 only | ✅ Full support |
| **.NET Framework Support** | ✅ Yes | ✅ Yes (Multi-targeting) | ✅ Yes |
| **Active Development** | ❌ Discontinued | ✅ Community-driven | ✅ Commercial support |

### CRUD & Basic Operations

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Create** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Retrieve** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Update** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Delete** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Upsert** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Associate/Disassociate** | ✅ Yes | ✅ Yes | ✅ Yes |
| **ExecuteMultiple** | ⚠️ Basic | ✅ Yes | ✅ Yes |
| **ExecuteTransaction** | ❌ No | ✅ Yes | ✅ Yes |

### Query Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **QueryExpression** | ✅ Yes | ✅ Yes | ✅ Yes |
| **FetchXML** | ⚠️ Basic | ✅ Yes with aggregation | ✅ Full support |
| **LINQ Queries** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Condition Operators** | ~40 operators | ✅ 60+ operators | ✅ 70+ operators |
| **Hierarchical Queries (Above/Under)** | ❌ No | ✅ Yes | ✅ Yes |
| **Fiscal Period Operators** | ⚠️ Basic (InFiscalYear) | ✅ Full support | ✅ Full support |
| **Aggregation (Sum, Count, etc.)** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Joins (LinkEntity)** | ✅ Yes | ✅ Yes | ✅ Yes |

### Message Executors

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Total Message Executors** | ~30-40 | ✅ 47+ | ✅ 100+ |
| **Assign** | ✅ Yes | ✅ Yes | ✅ Yes |
| **SetState** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Merge** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **GrantAccess/RevokeAccess** | ⚠️ Basic | ✅ Yes | ✅ Yes |
| **WhoAmI** | ✅ Yes | ✅ Yes | ✅ Yes |
| **RetrieveVersion** | ❌ No | ✅ Yes | ✅ Yes |
| **RetrieveMetadata** | ⚠️ Limited | ⚠️ Partial | ✅ Full support |
| **InitializeFrom** | ❌ No | ✅ Yes | ✅ Yes |
| **QualifyLead** | ❌ No | ✅ Yes | ✅ Yes |
| **WinOpportunity/LoseOpportunity** | ❌ No | ✅ Yes | ✅ Yes |
| **CloseIncident/CloseQuote** | ❌ No | ✅ Yes | ✅ Yes |
| **SendEmail** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **BulkDelete** | ❌ No | ✅ Yes | ✅ Yes |
| **AddToQueue/RemoveFromQueue** | ❌ No | ✅ Yes | ✅ Yes |
| **Team Operations** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Custom API** | ❌ No | ✅ Yes | ✅ Yes |
| **Custom Actions** | ⚠️ Limited | ❌ No | ✅ Yes |

### Plugin & Workflow Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Plugin Execution** | ✅ Yes | ✅ Yes (Basic) | ✅ Full support |
| **Plugin Context Simulation** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Multiple Plugins per Message** | ❌ No | ✅ Yes | ✅ Yes |
| **Plugin Auto-Discovery (SPKL)** | ❌ No | ✅ Yes | ✅ Yes |
| **Plugin Auto-Discovery (XrmTools.Meta)** | ❌ No | ✅ Yes | ⚠️ Unknown |
| **Pre/Post Images** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Pipeline Stages** | ⚠️ Basic | ✅ Full support | ✅ Full support |
| **Workflow Activities** | ✅ Yes | ❌ Removed (SDK limitation) | ✅ Yes |
| **Custom Workflow Activities** | ✅ Yes | ❌ Removed (SDK limitation) | ✅ Yes |
| **Async Plugins** | ✅ Yes | ⚠️ Limited | ✅ Yes |

### Metadata Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Entity Metadata** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Attribute Metadata** | ⚠️ Basic | ✅ Yes | ✅ Full support |
| **Relationship Metadata** | ⚠️ Basic | ✅ Yes | ✅ Full support |
| **OptionSet Metadata** | ⚠️ Limited | ✅ Yes (Repository) | ✅ Full support |
| **Status/State Metadata** | ⚠️ Limited | ✅ Yes (Repository) | ✅ Full support |
| **Global OptionSets** | ❌ No | ⚠️ Partial | ✅ Yes |
| **IsValidForCreate/Update/Read Validation** | ❌ No | ✅ Yes | ⚠️ Unknown¹ |
| **Publisher Metadata** | ❌ No | ❌ No | ✅ Yes |
| **Solution Metadata** | ❌ No | ❌ No | ✅ Yes |
| **System Entity Metadata** | ❌ No | ✅ Yes (Embedded in Core) | ⚠️ Unknown¹ |
| **CDM Import** | ❌ No | ✅ Yes | ⚠️ Unknown¹ |

### Early-Bound Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Early-Bound Entities** | ✅ Yes (Single assembly) | ✅ Yes (Multiple assemblies) | ✅ Yes (Multiple assemblies) |
| **ProxyTypesAssembly** | ✅ Yes | ⚠️ Deprecated (use EnableProxyTypes) | ⚠️ Deprecated |
| **EnableProxyTypes()** | ❌ No | ✅ Yes | ✅ Yes |

### Advanced Features

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Security Roles Simulation** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Business Units** | ⚠️ Limited | ⚠️ Limited | ✅ Full support |
| **Calculated Fields** | ❌ No | ✅ Yes | ✅ Yes |
| **Business Rules** | ❌ No | ✅ Yes | ✅ Yes |
| **Rollup Fields** | ❌ No | ✅ Yes | ⚠️ Unknown¹ |
| **Duplicate Detection** | ❌ No | ❌ No | ✅ Yes |
| **Audit Log** | ❌ No | ✅ Yes | ✅ Yes |
| **Virtual Entities** | ❌ No | ❌ No | ✅ Yes |
| **Elastic Tables** | ❌ No | ❌ No | ✅ Yes |
| **Connection References** | ❌ No | ❌ No | ✅ Yes |
| **Cloud Flows** | ❌ No | ✅ Yes (with JSON import & expressions) | ✅ Yes |
| **Power Automate Integration** | ❌ No | ❌ No | ✅ Yes |

¹ FakeXrmEasy v2+ is a commercial product with documentation not publicly accessible. Feature availability cannot be independently verified.

### Testing & Quality Features

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **In-Memory Context** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Integration Testing (XrmRealContext)** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Concurrent Execution Testing** | ❌ No | ❌ No | ✅ Yes |
| **Performance Profiling** | ❌ No | ❌ No | ✅ Yes |
| **Transaction Rollback** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Async/Await Support** | ❌ No | ⚠️ Limited | ✅ Yes |

### Documentation & Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Documentation** | ✅ Community docs | ✅ README + code comments | ✅ Premium documentation |
| **Code Examples** | ✅ Community samples | ✅ Tests as examples | ✅ Extensive examples |
| **Support** | ❌ No official support | ✅ Community support | ✅ Commercial support |
| **Updates** | ❌ Discontinued | ✅ Community-driven | ✅ Regular updates |
| **Bug Fixes** | ❌ No | ✅ Community fixes | ✅ Priority fixes |

### Legend
- ✅ **Yes** - Feature fully implemented and supported
- ⚠️ **Partial/Limited** - Feature partially implemented or has limitations
- ❌ **No** - Feature not available

### Sources & References

This comparison is based on:

1. **FakeXrmEasy v1 Documentation**: Original GitHub repository (archived/historical versions)
   - https://github.com/jordimontana82/fake-xrm-easy (v1.x branches, last MIT-licensed versions)

2. **Fake4Dataverse Source Code**: Direct analysis of this repository
   - Actual message executor count: 47 executors in `/Fake4DataverseCore/Fake4Dataverse.Core/FakeMessageExecutors/`
   - Condition operator count: 60+ operators in `/Fake4DataverseCore/Fake4Dataverse.Core/Query/`
   - Architecture analysis from `MiddlewareBuilder.cs` and related files

3. **FakeXrmEasy v2+ Commercial**: Based on commercial product website and documentation
   - https://dynamicsvalue.com/products/fake-xrm-easy (Commercial product page)
   - Feature lists based on marketing materials and product documentation
   - Note: Exact implementation details not verifiable without subscription

4. **Microsoft Dynamics 365 SDK Documentation**:
   - https://docs.microsoft.com/en-us/power-apps/developer/data-platform/
   - ConditionOperator enumeration reference
   - OrganizationRequest message reference

5. **Code Analysis**: Direct examination of source code in this repository
   - Message executors: `find . -name "*Executor.cs" | wc -l` = 47
   - Middleware architecture: `Middleware/MiddlewareBuilder.cs`
   - Query operators: `Query/ConditionExpressionExtensions.*.cs`

### Differences and Limitations in Fake4Dataverse

Fake4Dataverse has taken a different architectural direction focused on integration testing and network-accessible services. While it provides solid core testing capabilities, it differs significantly from FakeXrmEasy v2+ in scope and feature set:

#### Architectural Differences:
- **Focus on Integration Testing**: Network-accessible services, REST/OData endpoints, and web interfaces
- **Different API Design**: Not designed for API compatibility with FakeXrmEasy v2+
- **Extended Service Model**: CLI service, web UI, and REST API capabilities beyond traditional unit testing

#### Features Not Available (compared to FakeXrmEasy v2+):
1. **Workflow/Custom Workflow Activities** - Removed due to SDK limitations
2. **Custom Actions** - Limited support for custom actions
3. **Duplicate Detection** - Not yet implemented
4. **Virtual Entities** - Not supported
5. **Elastic Tables** - Not supported
6. **Connection References** - Not supported
7. **Advanced Security Model** - More limited than commercial version
8. **Complete Pre/Post Image Support** - Basic implementation only
9. **Global OptionSets** - Partial support only
10. **Publisher/Solution Metadata** - Not supported
11. **Many Advanced Message Executors** - ~47 executors vs 100+ in commercial version

#### Unique Capabilities in Fake4Dataverse:
- **Network-Accessible Service**: SOAP/WCF and REST/OData endpoints for integration testing
- **Web Interface**: Visual testing and user impersonation
- **Roadmap for Web Resources**: Planned support for client-side testing
- **Community-Driven**: Open-source with community contributions

This reflects Fake4Dataverse's focus as an integration testing framework rather than a complete Dataverse simulator. We do not claim 100% Dataverse compatibility or feature parity with the commercial FakeXrmEasy v2+.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

## Support

This is a community-maintained project. While we strive to provide a quality framework, this software is provided "as is" without warranty of any kind, as specified in the MIT License.

## License

This project is licensed under the MIT License - see the LICENSE.txt files in each project directory for details.
