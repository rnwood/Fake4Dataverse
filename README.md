# Fake4Dataverse: Test Automation Framework for Microsoft Dataverse and Power Platform

## About This Fork

Fake4Dataverse is a fork of the FakeXrmEasy project, originally created by Jordi Montaña. This fork continues the development of the testing framework under the MIT License, based on the last version of FakeXrmEasy that was available under that license.

**Author:** Rob Wood  
**Original Author:** Jordi Montaña (@jordimontana)  
**License:** MIT (see LICENSE.txt files in each project folder)

### Fork Basis

This fork is based on an early development version of FakeXrmEasy v2 (version 2.0.1), which was released under the MIT License. The original FakeXrmEasy project subsequently changed its licensing model. This fork preserves the last MIT-licensed version to ensure continued open-source availability for the community.

**Original Repositories:**
- Core: https://github.com/DynamicsValue/fake-xrm-easy-core
- Abstractions: https://github.com/DynamicsValue/fake-xrm-easy-abstractions
- Legacy: https://github.com/jordimontana82/fake-xrm-easy

The original repositories were licensed under MIT at the time this fork was created, as evidenced by the LICENSE files in those repositories.

## Why This Fork?

The original Fake4Dataverse project was an invaluable tool for the Dynamics 365 / Dataverse community, providing a comprehensive testing framework that enabled developers to write unit tests without requiring a live CRM instance. However, the original project moved to a commercial licensing model after version 2.x.

This fork serves several purposes:

1. **Preserve Open Source Access**: By forking from the last MIT-licensed version, we ensure that the community continues to have access to a free, open-source testing framework for Dataverse development.

2. **Community-Driven Development**: This fork is maintained by the community, for the community. We welcome contributions and aim to keep the project aligned with community needs.

3. **Modern Platform Support**: While respecting the original codebase, we aim to update and maintain compatibility with modern versions of Dataverse, Power Platform, and .NET.

4. **Legal Clarity**: This fork is completely legal and in accordance with the MIT License under which the original Fake4Dataverse was released. The MIT License explicitly permits forking, modification, and redistribution of the code.

## Is Forking from the Last MIT Version Legal?

**Absolutely yes.** The MIT License is one of the most permissive open-source licenses and explicitly grants the rights to:
- Use the software for any purpose
- Make copies and distribute them
- Modify the source code
- Distribute modified versions

The original Fake4Dataverse was released under the MIT License, which means that version and all prior versions remain available under that license permanently. The license cannot be retroactively revoked. When Jordi Montaña chose to change the licensing model for future versions, previous MIT-licensed versions remained under the MIT License.

This fork:
- Is based on version 2.x, which was released under the MIT License
- Properly acknowledges the original author (Jordi Montaña) in all LICENSE files
- Maintains all original copyright notices as required by the MIT License
- Continues to use the MIT License for all derivatives

## Acknowledgments

We are deeply grateful to **Jordi Montaña** for creating Fake4Dataverse and releasing it under the MIT License. His work has been instrumental to thousands of developers in the Dynamics 365 and Power Platform community. This fork aims to honor that legacy by continuing to provide free, open-source testing tools to the community.

## Project Structure

This is a monorepo containing three main projects:

### 1. Fake4DataverseAbstractions
- **Location**: `/Fake4DataverseAbstractions/`
- **Purpose**: Contains abstractions, interfaces, POCOs, enums, and base types used across the framework
- **Former Name**: Fake4Dataverse.Abstractions

### 2. Fake4DataverseCore  
- **Location**: `/Fake4DataverseCore/`
- **Purpose**: Core implementation including middleware, CRUD operations, query translation, and message executors
- **Former Name**: Fake4Dataverse.Core

### 3. Fake4Dataverse (Legacy Package)
- **Location**: `/Fake4Dataverse/`
- **Purpose**: Legacy/compatibility package
- **Former Name**: Fake4Dataverse

## Getting Started

Please refer to the README files in each project folder for specific build instructions and usage examples:
- [Fake4DataverseAbstractions README](./Fake4DataverseAbstractions/README.md)
- [Fake4DataverseCore README](./Fake4DataverseCore/README.md)
- [Fake4Dataverse README](./Fake4Dataverse/README.md)

## Building

Each project has its own build script. See individual project READMEs for details.

## Feature Comparison: FakeXrmEasy v1 vs Fake4Dataverse vs FakeXrmEasy v2

The following table compares the features available across different versions of the testing framework. This comparison is designed to help you understand the capabilities and limitations of each version, particularly highlighting features that Fake4Dataverse does not yet have.

### Architecture & Core Framework

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **License** | MIT | MIT | Commercial/Subscription |
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
| **Hierarchical Queries (Above/Under)** | ❌ No | ❌ No | ✅ Yes |
| **Fiscal Period Operators** | ⚠️ Basic (InFiscalYear) | ⚠️ Basic (InFiscalYear) | ✅ Full support |
| **Aggregation (Sum, Count, etc.)** | ⚠️ Limited | ✅ Yes | ✅ Yes |
| **Joins (LinkEntity)** | ✅ Yes | ✅ Yes | ✅ Yes |

### Message Executors

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Total Message Executors** | ~30-40 | ✅ 47+ | ✅ 100+ |
| **Assign** | ✅ Yes | ✅ Yes | ✅ Yes |
| **SetState** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Merge** | ⚠️ Limited | ❌ No | ✅ Yes |
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
| **Custom API** | ❌ No | ❌ No | ✅ Yes |
| **Custom Actions** | ⚠️ Limited | ❌ No | ✅ Yes |

### Plugin & Workflow Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Plugin Execution** | ✅ Yes | ✅ Yes (Basic) | ✅ Full support |
| **Plugin Context Simulation** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Multiple Plugins per Message** | ❌ No | ⚠️ Partial | ✅ Yes |
| **Pre/Post Images** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Pipeline Stages** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Workflow Activities** | ✅ Yes | ❌ Removed (SDK limitation) | ✅ Yes |
| **Custom Workflow Activities** | ✅ Yes | ❌ Removed (SDK limitation) | ✅ Yes |
| **Async Plugins** | ⚠️ Limited | ⚠️ Limited | ✅ Yes |

### Metadata Support

| Feature | FakeXrmEasy v1 (MIT) | Fake4Dataverse (This Fork) | FakeXrmEasy v2+ (Commercial) |
|---------|---------------------|---------------------------|------------------------------|
| **Entity Metadata** | ⚠️ Basic | ⚠️ Basic | ✅ Full support |
| **Attribute Metadata** | ⚠️ Basic | ✅ Yes | ✅ Full support |
| **Relationship Metadata** | ⚠️ Basic | ✅ Yes | ✅ Full support |
| **OptionSet Metadata** | ⚠️ Limited | ✅ Yes (Repository) | ✅ Full support |
| **Status/State Metadata** | ⚠️ Limited | ✅ Yes (Repository) | ✅ Full support |
| **Global OptionSets** | ❌ No | ⚠️ Partial | ✅ Yes |
| **Publisher Metadata** | ❌ No | ❌ No | ✅ Yes |
| **Solution Metadata** | ❌ No | ❌ No | ✅ Yes |

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
| **Calculated Fields** | ❌ No | ❌ No | ✅ Yes |
| **Rollup Fields** | ❌ No | ❌ No | ✅ Yes |
| **Business Rules** | ❌ No | ❌ No | ✅ Yes |
| **Duplicate Detection** | ❌ No | ❌ No | ✅ Yes |
| **Audit Log** | ❌ No | ❌ No | ✅ Yes |
| **Virtual Entities** | ❌ No | ❌ No | ✅ Yes |
| **Elastic Tables** | ❌ No | ❌ No | ✅ Yes |
| **Connection References** | ❌ No | ❌ No | ✅ Yes |
| **Cloud Flows** | ❌ No | ❌ No | ✅ Yes |
| **Power Automate Integration** | ❌ No | ❌ No | ✅ Yes |

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
   - Actual message executor count: 47 executors in `/Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`
   - Condition operator count: 60+ operators in `/Fake4DataverseCore/src/Fake4Dataverse.Core/Query/`
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

### Key Gaps in Fake4Dataverse (This Fork)

Based on this analysis, Fake4Dataverse is missing several features compared to the commercial FakeXrmEasy v2+:

#### High-Priority Missing Features:
1. **Workflow/Custom Workflow Activities** - Removed due to SDK limitations
2. **Hierarchical Query Operators** - Above, AboveOrEqual, Under, UnderOrEqual, ChildOf
3. **Advanced Fiscal Period Operators** - InFiscalPeriod, LastFiscalPeriod, NextFiscalPeriod, etc.
4. **Custom API Support** - Modern Dataverse Custom APIs not supported
5. **Custom Actions** - Limited support for custom actions
6. **Merge Request** - Entity merge operations not implemented
7. **Calculated/Rollup Fields** - No simulation of calculated or rollup fields
8. **Business Rules** - No business rule simulation
9. **Duplicate Detection** - No duplicate detection simulation
10. **Audit Log** - No audit log simulation

#### Modern Dataverse Features Not Supported:
11. **Virtual Entities** - No virtual entity support
12. **Elastic Tables** - No elastic table support
13. **Cloud Flows Integration** - No cloud flow simulation
14. **Power Automate Testing** - No Power Automate integration testing
15. **Connection References** - No connection reference support
16. **Advanced Security Model** - Limited security role and privilege simulation
17. **Concurrent Execution Testing** - No multi-threaded execution testing
18. **Performance Profiling** - No built-in profiling tools

#### Pipeline & Plugin Limitations:
19. **Multiple Plugins per Message** - Limited support for multiple plugins
20. **Complete Pipeline Simulation** - Pipeline stages not fully simulated
21. **Async Plugin Support** - Limited async plugin testing
22. **Complete Pre/Post Image Support** - Basic implementation only

#### Metadata Limitations:
23. **Global OptionSets** - Partial support only
24. **Publisher Metadata** - Not supported
25. **Solution Metadata** - Not supported
26. **Complete Metadata Operations** - Many RetrieveMetadata variants missing

#### Additional Missing Message Executors (estimated 50+ messages):
27. Various business-specific messages (Marketing, Service, Field Service specific)
28. Advanced relationship messages
29. Modern Dataverse-specific messages
30. And many more organization requests added in recent Dynamics 365 versions

This honest assessment shows that while Fake4Dataverse provides a solid foundation with 47 message executors and core testing capabilities, it represents an early v2 development snapshot and lacks the maturity and feature completeness of the actively developed commercial FakeXrmEasy v2+. The commercial version has continued to add significant features, especially around modern Dataverse capabilities, advanced pipeline simulation, and quality-of-life improvements that benefit from ongoing commercial development and support.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

## Support

This is a community-maintained project. While we strive to provide a quality framework, this software is provided "as is" without warranty of any kind, as specified in the MIT License.

## License

This project is licensed under the MIT License - see the LICENSE.txt files in each project directory for details.
