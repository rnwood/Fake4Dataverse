# Known Gaps and Limitations in Fake4Dataverse

**Last Updated:** October 12, 2025

This document provides a comprehensive overview of known gaps and limitations in Fake4Dataverse compared to both real Dataverse/Power Platform and the commercial FakeXrmEasy v2+ versions.

## Overview

Fake4Dataverse is a community-driven testing framework that aims to simulate Microsoft Dataverse for testing purposes. While it covers most common scenarios, there are some features that are not yet implemented or have limitations.

## ✅ Recently Completed Features (October 2025)

The following features were recently implemented and are now fully functional:

- **Cloud Flow Simulation** - Complete Power Automate flow testing with JSON import
- **Expression Language** - 80+ Power Automate expression functions (90%+ coverage)
- **Safe Navigation Operator (?)** - Null-safe property access in expressions
- **Path Separator (/)** - Simplified nested property access in expressions
- **Compose Actions** - Data transformation and composition in flows
- **Apply to Each Loops** - Collection iteration with `item()` function support
- **Nested Loop Support** - Stack-based item tracking for complex loop scenarios

## 🔴 High-Priority Missing Features

These features are commonly used in Dataverse development and would provide significant value:

### 1. Workflow/Custom Workflow Activities
**Status:** ❌ Not Supported (removed due to SDK limitations)

**Impact:** High - Many organizations use custom workflow activities

**Workaround:** 
- Use plugins instead where possible
- Use Cloud Flows for automation logic
- Test workflow logic separately from Dataverse integration

### 2. Advanced Custom Actions
**Status:** ⚠️ Limited Support

**Current Support:**
- Basic custom action execution via ExecuteRequest
- Simple input/output parameters

**Missing:**
- Complex parameter types
- Custom action metadata simulation
- Pre/post-operation events for custom actions

**Workaround:**
- Use Custom APIs instead (fully supported)
- Test custom action logic separately from Dataverse integration

### 3. Rollup Fields
**Status:** ❌ Not Supported

**Impact:** Medium - Used for aggregating related record data

**Workaround:**
- Calculate rollup values in test setup
- Use calculated fields where possible
- Test rollup logic separately

### 4. Business Rules
**Status:** ❌ Not Supported

**Impact:** Medium - Used for client-side and server-side validation

**Workaround:**
- Test business rule logic separately
- Simulate business rule effects in test setup
- Use plugins to enforce business rules (which can be tested)

### 5. Duplicate Detection
**Status:** ❌ Not Supported

**Impact:** Low-Medium - Used for preventing duplicate records

**Workaround:**
- Implement duplicate detection logic in plugins/tests
- Use query-based checks in tests

### 6. Audit Log Simulation
**Status:** ❌ Not Supported

**Impact:** Low - Audit log queries will not return data

**Workaround:**
- Test audit-related logic separately
- Mock audit data in tests if needed

## 🟡 Modern Dataverse Features Not Supported

These are newer Dataverse features that are not yet available:

### 7. Virtual Entities
**Status:** ❌ Not Supported

**Impact:** Medium for organizations using external data sources

**Workaround:**
- Mock virtual entity data as regular entities
- Test virtual entity plugins/logic separately

### 8. Elastic Tables
**Status:** ❌ Not Supported

**Impact:** Low - Relatively new feature, limited adoption

**Workaround:**
- Use regular tables in tests
- Test elastic table-specific logic separately

### 9. Power Automate Integration (Beyond Cloud Flows)
**Status:** ⚠️ Partial

**Supported:**
- ✅ Cloud Flow simulation
- ✅ Dataverse triggers (Create, Update, Delete)
- ✅ Dataverse actions (CRUD operations)
- ✅ Expression language (80+ functions)
- ✅ Compose actions
- ✅ Apply to Each loops

**Not Supported:**
- ❌ Approval actions
- ❌ Connector actions (SharePoint, Office 365, etc.)
- ❌ HTTP actions
- ❌ Condition actions (if/then/else)
- ❌ Switch actions
- ❌ Parallel branches
- ❌ Do Until loops

**Workaround:**
- Use extensibility to implement custom connector handlers for critical connectors
- Test Cloud Flow logic with Dataverse actions
- See [Cloud Flows Guide](./usage/cloud-flows.md) for custom connector examples

### 10. Connection References
**Status:** ❌ Not Supported

**Impact:** Low - Primarily relevant for ALM scenarios

**Workaround:**
- Connection references don't affect testing scenarios

### 11. Advanced Security Model
**Status:** ⚠️ Limited Support

**Current Support:**
- Basic security role/privilege checks
- Principal (User/Team) association
- Grant/Revoke access operations
- Basic ownership checks

**Missing:**
- Complex security role inheritance
- Field-level security
- Hierarchy security
- Business unit inheritance rules

**Workaround:**
- Test security logic with basic role checks
- Mock complex security scenarios in test setup

### 12. Concurrent Execution Testing
**Status:** ❌ Not Supported

**Impact:** Medium for high-volume scenarios

**Description:**
- No multi-threaded execution simulation
- No concurrent request handling

**Workaround:**
- Test concurrency logic separately with actual threading
- Use integration tests for concurrency scenarios

### 13. Performance Profiling
**Status:** ❌ Not Supported

**Impact:** Low - Profiling typically requires real environment

**Workaround:**
- Use performance testing tools against real environments
- Focus on logic correctness in unit tests

## 🟢 Pipeline & Plugin Limitations

### 14. Complete Pre/Post Image Support
**Status:** ⚠️ Basic Implementation

**Current Support:**
- Pre/post images in plugin context
- Basic image filtering

**Missing:**
- Automatic image population based on step configuration
- Complex image filtering scenarios

**Workaround:**
- Manually set up images in test context
- Test image-related logic explicitly

### 15. Async Plugin Execution
**Status:** ✅ **FULLY SUPPORTED**

**Implementation:**
- Async operation queue
- AsyncOperation entity simulation
- Wait for completion capabilities

**See:** [Async Plugin Implementation](./async-plugin-implementation.md)

## 🔵 Metadata Limitations

### 16. Global OptionSets
**Status:** ⚠️ Partial Support

**Current Support:**
- Basic option set value retrieval
- Option set metadata in entity metadata

**Missing:**
- Global option set management
- Option set dependencies

**Workaround:**
- Define option sets in test setup
- Use local option sets where possible

### 17. Publisher/Solution Metadata
**Status:** ❌ Not Supported

**Impact:** Low - Primarily relevant for solution development

**Workaround:**
- Publisher and solution metadata not needed for logic testing

### 18. Complete Metadata Operations
**Status:** ⚠️ Partial Support

**Supported:**
- RetrieveEntityRequest
- RetrieveAttributeRequest
- Basic entity/attribute metadata

**Missing (estimated 50+ metadata messages):**
- CreateEntityRequest
- CreateAttributeRequest
- UpdateEntityRequest
- DeleteEntityRequest
- Many specialized metadata operations

**Workaround:**
- Metadata operations rarely need testing in unit tests
- Use integration tests for metadata operations

## 📊 Missing Message Executors

Fake4Dataverse supports **43+ message executors**, but there are approximately **100+ additional messages** in the Dataverse SDK.

**High-Value Missing Messages:**
- ApplyRoutingRuleRequest
- CalculateRollupFieldRequest
- ExecuteWorkflowRequest (workflow limitations)
- SendEmailRequest (can be mocked with custom connector)
- Many specialized business process messages

**See:** [Message Executors Reference](./messages/README.md) for complete list of supported messages.

## 🎯 What Fake4Dataverse Does Best

Despite these gaps, Fake4Dataverse excels at:

✅ **Plugin Testing** - Comprehensive plugin simulation with full pipeline support  
✅ **CRUD Operations** - Complete Create, Read, Update, Delete testing  
✅ **Query Testing** - LINQ and FetchXML query simulation  
✅ **Security Testing** - Principal-based security and access control  
✅ **Batch Operations** - ExecuteMultiple and transaction testing  
✅ **Calculated Fields** - Formula evaluation and testing  
✅ **Custom APIs** - Modern Dataverse Custom API implementation  
✅ **Cloud Flows** - Power Automate flow simulation with expressions  
✅ **Relationship Testing** - Associate/Disassociate operations  
✅ **Metadata Queries** - Basic entity and attribute metadata retrieval

## 🔄 Comparison with FakeXrmEasy v2+ (Commercial)

| Feature Category | Fake4Dataverse (Free) | FakeXrmEasy v2+ (Commercial) |
|-----------------|----------------------|------------------------------|
| **Core CRUD** | ✅ Full Support | ✅ Full Support |
| **Plugin Testing** | ✅ Full Support | ✅ Full Support |
| **Query Simulation** | ✅ Full Support | ✅ Full Support |
| **Cloud Flows** | ✅ Full Support | ✅ Full Support |
| **Expression Language** | ✅ 80+ functions | ✅ Full Support |
| **Custom APIs** | ✅ Full Support | ✅ Full Support |
| **Workflows** | ❌ Not Supported | ✅ Supported |
| **Rollup Fields** | ❌ Not Supported | ✅ Supported |
| **Business Rules** | ❌ Not Supported | ✅ Supported |
| **Virtual Entities** | ❌ Not Supported | ✅ Supported |
| **Performance Profiling** | ❌ Not Supported | ✅ Supported |
| **Support** | Community | Commercial |
| **License** | MIT (Free) | Commercial/Subscription |

## 🚀 Future Roadmap

Community contributions are welcome for:

1. **High Priority:**
   - Condition actions (if/then/else) for Cloud Flows
   - Switch actions for Cloud Flows
   - HTTP connector for Cloud Flows
   - Additional connector implementations

2. **Medium Priority:**
   - Business rule simulation
   - Rollup field simulation
   - Enhanced security model
   - More metadata operations

3. **Nice to Have:**
   - Virtual entity support
   - Performance profiling tools
   - Advanced duplicate detection

## 📝 Contributing

If you'd like to help fill any of these gaps:

1. Check the [GitHub Issues](https://github.com/rnwood/Fake4Dataverse/issues) for planned work
2. Discuss your approach in an issue before starting work
3. Follow the contribution guidelines in the main README
4. Include tests and documentation with your changes

## 📚 Related Documentation

- [Cloud Flows Guide](./usage/cloud-flows.md) - Full Cloud Flow simulation capabilities
- [Expression Language](./expression-language.md) - Power Automate expression reference
- [Message Executors](./messages/README.md) - Complete list of supported messages
- [Feature Comparison](../README.md#feature-comparison) - Detailed feature comparison table

---

**Questions?** Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
