# Fake4Dataverse Documentation

Welcome to the Fake4Dataverse documentation! This testing framework allows you to write unit tests for Microsoft Dataverse (Dynamics 365) plugins, custom activities, and applications without requiring a live CRM instance.

## Documentation Structure

### 🚀 [Getting Started](./getting-started/)
- [Installation](./getting-started/installation.md) - Install and set up Fake4Dataverse
- [Quick Start](./getting-started/quickstart.md) - Your first test in 5 minutes
- [Basic Concepts](./getting-started/basic-concepts.md) - Understanding the fundamentals

### 📚 [Core Concepts](./concepts/)
- [XrmFakedContext](./concepts/xrm-faked-context.md) - The heart of the framework
- [Middleware Architecture](./concepts/middleware.md) - How the request pipeline works
- [Service Initialization](./concepts/service-initialization.md) - Setting up your test services
- [Data Management](./concepts/data-management.md) - Working with test data

### 💡 [Usage Guides](./usage/)
- [Testing Plugins](./usage/testing-plugins.md) - Plugin testing patterns and examples
- [CRUD Operations](./usage/crud-operations.md) - Create, Read, Update, Delete operations
- [Querying Data](./usage/querying-data.md) - LINQ and FetchXML queries
- [Testing Workflows](./usage/testing-workflows.md) - Custom workflow activity testing
- [Security and Permissions](./usage/security-permissions.md) - Testing security roles and access
- [ExecuteMultiple and Transactions](./usage/batch-operations.md) - Batch operations and transactions
- [Metadata Validation](./usage/metadata-validation.md) - IsValidForCreate/Update/Read enforcement ✅ **NEW**
- [CDM Import](./cdm-import.md) - Import entity metadata from Common Data Model JSON ✅ **NEW**
- [CDM vs Early-Bound](./cdm-vs-early-bound.md) - Choosing between CDM files and early-bound assemblies ✅ **NEW**
- [Thread Safety](./thread-safety.md) - Concurrent operations and thread safety ✅ **NEW**
- [Alternate Keys](./usage/alternate-keys.md) - Using alternate keys for record identification ✅ **NEW**
- [Duplicate Detection](./usage/duplicate-detection.md) - Testing duplicate detection rules ✅ **NEW**
- [Calculated Fields](./usage/calculated-fields.md) - Simulating calculated field evaluation
- [Rollup Fields](./usage/rollup-fields.md) - Simulating rollup field evaluation
- [Custom API Support](./usage/custom-api.md) - Implementing Custom APIs
- [Cloud Flows](./usage/cloud-flows.md) - Testing Power Automate flows ✅ **IMPLEMENTED** (requires Fake4DataverseCloudFlows package)
- [Expression Language](./expression-language.md) - Power Automate expressions ✅ **NEW**
- [Merge Request Operations](./usage/merge-request.md) - Merging entity records
- [Hierarchical Queries](./usage/hierarchical-queries.md) - Querying hierarchical data
- [Fiscal Period Operators](./usage/fiscal-period-operators.md) - Fiscal calendar queries

### 🌐 [Network Testing](.)
- [Fake4DataverseService](./service.md) - Network-accessible SOAP/WCF service for integration testing ✅ **NEW**
- [REST/OData API Endpoints](./rest-api.md) - OData v4.0 endpoints with advanced query support ✅ **NEW**

### 🏗️ [Architecture & Planning](.)
- [Testing Guide](./TESTING_GUIDE.md) - How to run all tests in the repository ✅ **NEW**
- [Cloud Flow API Design](./API_DESIGN_CLOUD_FLOWS.md) - Technical design for Cloud Flow simulation
- [Cloud Flow Architecture](./CLOUD_FLOW_ARCHITECTURE.md) - Architecture diagrams and patterns
- [Cloud Flow JSON Import](./CLOUD_FLOW_JSON_IMPORT_SUMMARY.md) - JSON flow definition import
- [Async Plugin Implementation](./async-plugin-implementation.md) - Async plugin execution design
- [Documentation Overview](./DOCUMENTATION_OVERVIEW.md) - This documentation structure
- [Known Gaps & Limitations](./GAPS.md) - Comprehensive list of unsupported features ✅ **NEW**

### 📋 [Message Executors](./messages/)
- [Overview](./messages/README.md) - Supported Dataverse messages
- [CRUD Messages](./messages/crud.md) - Create, Retrieve, Update, Delete, Upsert
- [Association Messages](./messages/associations.md) - Associate, Disassociate
- [Metadata Messages](./messages/metadata.md) - Retrieve entity/attribute metadata
- [Security Messages](./messages/security.md) - Grant/Revoke access, sharing
- [Business Process Messages](./messages/business-process.md) - Win/Lose opportunity, Close incident, etc.
- [Queue Messages](./messages/queues.md) - Queue operations
- [Team Messages](./messages/teams.md) - Team membership management
- [Specialized Messages](./messages/specialized.md) - Other supported messages

### 🔄 [Migration Guides](./migration/)
- [From FakeXrmEasy v1.x](./migration/from-v1.md) - Migrate from v1.x
- [From FakeXrmEasy v3.x](./migration/from-v3.md) - Migrate from commercial v3.x

### 🔧 [API Reference](./api/)
- [IXrmFakedContext](./api/ixrm-faked-context.md) - Context interface reference
- [Extension Methods](./api/extension-methods.md) - Available extension methods
- [Custom Message Executors](./api/custom-executors.md) - Creating your own executors

## Common Scenarios

### I want to...

- **Write my first test**: Start with [Quick Start](./getting-started/quickstart.md)
- **Test a plugin**: See [Testing Plugins](./usage/testing-plugins.md)
- **Query test data**: Check [Querying Data](./usage/querying-data.md)
- **Use standard entity schemas**: See [CDM Import](./cdm-import.md)
- **Test concurrent operations**: See [Thread Safety](./thread-safety.md)
- **Use alternate keys**: See [Alternate Keys](./usage/alternate-keys.md)
- **Test duplicate detection**: Check [Duplicate Detection](./usage/duplicate-detection.md)
- **Understand the architecture**: Read [Middleware Architecture](./concepts/middleware.md)
- **Test security**: See [Security and Permissions](./usage/security-permissions.md)
- **Migrate from FakeXrmEasy**: Check the [Migration Guides](./migration/)
- **Find supported messages**: Browse [Message Executors](./messages/)
- **Do integration testing**: Use [Fake4DataverseService](./service.md) for network-accessible testing

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

public class MyPluginTests
{
    [Fact]
    public void Should_CreateContact_When_AccountIsCreated()
    {
        // Arrange - Create a fake Dataverse context
        var context = XrmFakedContextFactory.New();
        var service = context.GetOrganizationService();
        
        // Initialize with test data
        var account = new Entity("account")
        {
            Id = Guid.NewGuid(),
            ["name"] = "Test Account"
        };
        context.Initialize(account);
        
        // Act - Execute plugin
        context.ExecutePluginWith<MyPlugin>(
            pluginContext => {
                pluginContext.MessageName = "Create";
                pluginContext.Stage = 40; // Post-operation
            },
            account
        );
        
        // Assert - Verify results
        var contacts = context.CreateQuery("contact").ToList();
        Assert.Single(contacts);
        Assert.Equal("Test Account Contact", contacts[0]["fullname"]);
    }
}
```

## Getting Help

- **Questions?** Check the [FAQ](./getting-started/faq.md)
- **Issues?** Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
- **Contributing?** See the [Contributing Guide](../README.md#contributing)

## Inspiration

This documentation is inspired by popular .NET testing frameworks:
- **Moq** - Clear, example-driven documentation
- **NSubstitute** - Scenario-based guides
- **xUnit** - Comprehensive reference documentation
- **FluentAssertions** - Easy-to-follow patterns
