# Feature Parity Issues for Fake4Dataverse

This document contains GitHub issue templates for all features needed to achieve parity with FakeXrmEasy v2+. Each section below represents a separate GitHub issue that should be created.

---

## Issue 1: Implement Merge Request Message Executor

**Title:** Add support for Merge request operations

**Labels:** `enhancement`, `message-executor`, `high-priority`

**Description:**

### Feature Description
Implement the `MergeRequest` message executor to support entity merge operations in Dataverse/Dynamics 365.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Requirements
- Implement `MergeRequestExecutor` class
- Handle merging of two entity records
- Update all references to point to the surviving record
- Handle merge conflicts appropriately
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`

### Priority
High - Common operation in CRM customizations

---

## Issue 2: Add Hierarchical Query Operators Support

**Title:** Implement hierarchical query operators (Above, Under, ChildOf, etc.)

**Labels:** `enhancement`, `query-support`, `high-priority`

**Description:**

### Feature Description
Implement hierarchical query operators for querying parent-child relationships in entity hierarchies.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Required Operators
- `Above` - Records above in hierarchy
- `AboveOrEqual` - Records above or at same level
- `Under` - Records under in hierarchy
- `UnderOrEqual` - Records under or at same level
- `ChildOf` - Direct children

### Requirements
- Extend `ConditionExpressionExtensions` with hierarchical operators
- Handle hierarchical queries in QueryExpression
- Handle hierarchical queries in FetchXML
- Support self-referencing relationships
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.*.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Query/`

### Priority
High - Required for hierarchical data structures (Account hierarchies, etc.)

---

## Issue 3: Implement Advanced Fiscal Period Operators

**Title:** Add support for advanced fiscal period query operators

**Labels:** `enhancement`, `query-support`, `medium-priority`

**Description:**

### Feature Description
Implement advanced fiscal period operators for date-based queries using fiscal calendars.

### Current Status
- ⚠️ Basic support (InFiscalYear only) in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic support in FakeXrmEasy v1

### Required Operators
- `InFiscalPeriod` - Within specific fiscal period
- `LastFiscalPeriod` - Previous fiscal period
- `NextFiscalPeriod` - Next fiscal period
- `LastFiscalYear` - Previous fiscal year
- `NextFiscalYear` - Next fiscal year
- `InFiscalPeriodAndYear` - Specific period and year
- `InOrBeforeFiscalPeriodAndYear` - On or before specific period/year
- `InOrAfterFiscalPeriodAndYear` - On or after specific period/year

### Requirements
- Extend `ConditionExpressionExtensions` with fiscal period operators
- Add fiscal calendar configuration support
- Handle different fiscal year start dates
- Support fiscal period definitions
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.*.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Extensions/DateTimeExtensions.cs`

### Priority
Medium - Important for financial reporting scenarios

---

## Issue 4: Add Custom API Support

**Title:** Implement Custom API message execution support

**Labels:** `enhancement`, `message-executor`, `high-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for executing Custom APIs, the modern replacement for Custom Actions in Dataverse.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support custom API definition and registration
- Handle custom API request/response parameters
- Support both function and action style APIs
- Handle Custom API execution in plugins
- Add middleware for Custom API execution
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/`

### Priority
High - Modern Dataverse feature, recommended over Custom Actions

---

## Issue 5: Implement Custom Actions Support

**Title:** Add full support for Custom Actions

**Labels:** `enhancement`, `message-executor`, `high-priority`

**Description:**

### Feature Description
Implement full support for Custom Actions (process-based actions).

### Current Status
- ❌ Not implemented in Fake4Dataverse (removed from v1)
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Requirements
- Support custom action definition and registration
- Handle action input/output parameters
- Support both entity-bound and global actions
- Handle action execution in plugin pipeline
- Add middleware for action execution
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/`

### Priority
High - Common customization pattern in Dynamics 365

---

## Issue 6: Add Calculated Fields Simulation

**Title:** Implement calculated field evaluation and simulation

**Labels:** `enhancement`, `field-types`, `high-priority`

**Description:**

### Feature Description
Add support for simulating calculated fields in Dataverse entities.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support calculated field definition
- Parse and evaluate calculated field formulas
- Handle calculated field dependencies
- Update calculated fields on entity retrieve
- Support all calculated field data types
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/CalculatedFields/`

### Priority
High - Common pattern for business logic

---

## Issue 7: Add Rollup Fields Simulation

**Title:** Implement rollup field calculation and simulation

**Labels:** `enhancement`, `field-types`, `high-priority`

**Description:**

### Feature Description
Add support for simulating rollup fields that aggregate related record data.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support rollup field definition
- Handle aggregate functions (SUM, COUNT, MIN, MAX, AVG)
- Support rollup across relationships
- Update rollup values when related records change
- Handle rollup field hierarchies
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/RollupFields/`

### Priority
High - Common pattern for aggregating data

---

## Issue 8: Implement Business Rules Simulation

**Title:** Add business rules engine and simulation

**Labels:** `enhancement`, `business-logic`, `high-priority`

**Description:**

### Feature Description
Implement simulation of Dataverse business rules for field validation and logic.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support business rule definition
- Handle field validation rules
- Support show/hide field logic
- Support set field value logic
- Support recommendation actions
- Execute rules at appropriate times (onCreate, onLoad, onChange)
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/`

### Priority
High - Common low-code customization approach

---

## Issue 9: Add Duplicate Detection Simulation

**Title:** Implement duplicate detection rule evaluation

**Labels:** `enhancement`, `data-quality`, `high-priority`

**Description:**

### Feature Description
Add support for simulating duplicate detection rules in Dataverse.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support duplicate detection rule definition
- Evaluate rules on Create and Update operations
- Support exact and fuzzy matching
- Return duplicate detection errors appropriately
- Handle multiple duplicate detection rules
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/DuplicateDetection/`

### Priority
High - Important for data quality testing

---

## Issue 10: Implement Audit Log Simulation

**Title:** Add audit log tracking and retrieval

**Labels:** `enhancement`, `audit`, `medium-priority`

**Description:**

### Feature Description
Implement audit log simulation for tracking entity changes.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Track Create, Update, Delete operations
- Store audit history
- Support RetrieveAuditDetails message
- Support RetrieveRecordChangeHistory message
- Handle attribute-level auditing
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/Audit/`

### Priority
Medium - Important for compliance testing

---

## Issue 11: Add Virtual Entities Support

**Title:** Implement virtual entity data source simulation

**Labels:** `enhancement`, `virtual-entities`, `medium-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for simulating virtual entities (external data sources).

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support virtual entity definition
- Mock external data provider
- Handle CRUD operations on virtual entities
- Support query operations on virtual entities
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/VirtualEntities/`

### Priority
Medium - Growing adoption of virtual entities

---

## Issue 12: Implement Elastic Tables Support

**Title:** Add support for Elastic Tables (Dataverse for Teams)

**Labels:** `enhancement`, `elastic-tables`, `low-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for Elastic Tables used in Dataverse for Teams.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support elastic table schema
- Handle JSON-based storage model
- Support elastic table queries
- Handle elastic table operations
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/ElasticTables/`

### Priority
Low - Newer feature with limited adoption

---

## Issue 13: Add Connection References Support

**Title:** Implement Connection References for Power Platform

**Labels:** `enhancement`, `power-platform`, `low-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for Connection References used in Power Platform solutions.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support connection reference entities
- Mock connection reference resolution
- Handle connector invocations through references
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`

### Priority
Low - Primarily for Power Apps/Flow integration

---

## Issue 14: Implement Cloud Flows Integration Testing

**Title:** Add Cloud Flows execution simulation

**Labels:** `enhancement`, `power-automate`, `low-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for testing Cloud Flows (Power Automate) triggered by Dataverse events.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Mock Cloud Flow triggers
- Support Dataverse connector actions
- Simulate flow execution
- Verify flow was triggered
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/CloudFlows/`

### Priority
Low - Complex integration scenario

---

## Issue 15: Add Power Automate Integration Testing

**Title:** Implement Power Automate integration testing capabilities

**Labels:** `enhancement`, `power-automate`, `low-priority`, `modern-dataverse`

**Description:**

### Feature Description
General Power Automate integration testing beyond Cloud Flows.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Mock Power Automate HTTP triggers
- Support custom connector testing
- Simulate Power Automate actions
- Verify integration points
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/PowerAutomate/`

### Priority
Low - Advanced integration scenario

---

## Issue 16: Enhance Multiple Plugins Per Message Support

**Title:** Add full support for multiple plugins on same message

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Description:**

### Feature Description
Improve support for registering and executing multiple plugins for the same message/entity combination.

### Current Status
- ⚠️ Partial support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support multiple plugin registrations
- Execute plugins in correct order (execution order property)
- Handle plugin step registration configurations
- Support filtering by attributes
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/`

### Priority
Medium - Common plugin scenario

---

## Issue 17: Implement Complete Pipeline Simulation

**Title:** Add full plugin pipeline stage simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Description:**

### Feature Description
Implement complete plugin pipeline with all stages properly simulated.

### Current Status
- ⚠️ Basic pipeline in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic in FakeXrmEasy v1

### Requirements
- Support all pipeline stages (PreValidation, PreOperation, MainOperation, PostOperation)
- Handle transaction boundaries
- Support pipeline depth tracking
- Simulate pipeline behavior accurately
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/`

### Priority
Medium - Important for plugin testing

---

## Issue 18: Enhance Async Plugin Support

**Title:** Improve asynchronous plugin execution simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Description:**

### Feature Description
Improve support for testing asynchronous plugins.

### Current Status
- ⚠️ Limited support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Requirements
- Support async plugin execution mode
- Handle async service context
- Simulate system job queue
- Support async plugin debugging
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs`

### Priority
Medium - Important for complex workflows

---

## Issue 19: Improve Pre/Post Image Support

**Title:** Enhance plugin pre/post image simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Description:**

### Feature Description
Enhance support for pre and post images in plugin context.

### Current Status
- ⚠️ Basic support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic in FakeXrmEasy v1

### Requirements
- Support image registration configurations
- Handle filtered attributes in images
- Ensure correct image snapshots at each stage
- Support multiple images
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs`

### Priority
Medium - Common plugin pattern

---

## Issue 20: Enhance Global OptionSet Support

**Title:** Implement full global optionset metadata support

**Labels:** `enhancement`, `metadata`, `medium-priority`

**Description:**

### Feature Description
Improve support for global optionsets in metadata.

### Current Status
- ⚠️ Partial support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support global optionset definitions
- Handle global optionset retrieval
- Support global optionset updates
- Link entity attributes to global optionsets
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/`

### Priority
Medium - Common metadata scenario

---

## Issue 21: Add Publisher Metadata Support

**Title:** Implement publisher metadata operations

**Labels:** `enhancement`, `metadata`, `low-priority`

**Description:**

### Feature Description
Add support for publisher metadata entities and operations.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support publisher entity metadata
- Handle publisher prefixes
- Support publisher operations
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/`

### Priority
Low - Advanced solution scenarios

---

## Issue 22: Add Solution Metadata Support

**Title:** Implement solution metadata operations

**Labels:** `enhancement`, `metadata`, `low-priority`

**Description:**

### Feature Description
Add support for solution metadata and solution component operations.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support solution entity metadata
- Handle solution components
- Support solution import/export operations
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/`

### Priority
Low - Advanced ALM scenarios

---

## Issue 23: Enhance Security Role Simulation

**Title:** Improve security role and privilege simulation

**Labels:** `enhancement`, `security`, `medium-priority`

**Description:**

### Feature Description
Enhance security role and privilege checking in the fake context.

### Current Status
- ⚠️ Basic support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic in FakeXrmEasy v1

### Requirements
- Support privilege checking
- Handle record-level security
- Support field-level security
- Simulate security role hierarchies
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`

### Priority
Medium - Important for security testing

---

## Issue 24: Improve Business Unit Support

**Title:** Enhance business unit operations and hierarchy

**Labels:** `enhancement`, `security`, `medium-priority`

**Description:**

### Feature Description
Improve business unit hierarchy and operations support.

### Current Status
- ⚠️ Limited support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Requirements
- Support business unit hierarchy
- Handle ownership across business units
- Support business unit-based security
- Simulate business unit operations
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`

### Priority
Medium - Common enterprise scenario

---

## Issue 25: Add Concurrent Execution Testing

**Title:** Implement concurrent execution testing capabilities

**Labels:** `enhancement`, `testing`, `low-priority`

**Description:**

### Feature Description
Add support for testing concurrent operations and multi-threading scenarios.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support concurrent context access
- Handle thread-safe operations
- Test locking scenarios
- Detect race conditions
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`

### Priority
Low - Advanced testing scenario

---

## Issue 26: Add Performance Profiling

**Title:** Implement performance profiling and metrics

**Labels:** `enhancement`, `testing`, `low-priority`

**Description:**

### Feature Description
Add built-in performance profiling capabilities for test execution.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Track operation execution time
- Count query operations
- Measure plugin execution time
- Provide profiling reports
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- New: `Fake4DataverseCore/src/Fake4Dataverse.Core/Profiling/`

### Priority
Low - Nice-to-have feature

---

## Issue 27: Enhance Async/Await Support

**Title:** Improve async/await pattern support throughout framework

**Labels:** `enhancement`, `async`, `medium-priority`

**Description:**

### Feature Description
Enhance async/await support throughout the framework.

### Current Status
- ⚠️ Limited support in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Add async versions of core operations
- Support async plugin execution
- Handle async message executors
- Ensure proper async context flow
- Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/`
- Multiple files throughout codebase

### Priority
Medium - Modern development pattern

---

## Issue 28: Add Missing Message Executors (Batch 1 - Business-Specific)

**Title:** Implement business-specific message executors

**Labels:** `enhancement`, `message-executor`, `low-priority`

**Description:**

### Feature Description
Implement various business-specific message executors for Sales, Service, Marketing modules.

### Current Status
- ❌ Many not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Some in FakeXrmEasy v1

### Required Executors (Examples)
- `CalculatePrice` - Calculate price for quote/order
- `GetInvoiceProductsFromOpportunity` - Convert opportunity to invoice
- `LockInvoicePricing` - Lock invoice pricing
- `UnlockInvoicePricing` - Unlock invoice pricing
- `ReviseQuote` - Revise quote
- `CalculateActualValue` - Calculate opportunity value
- Additional Sales/Service message executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`

### Priority
Low - Specific business module operations

---

## Issue 29: Add Missing Message Executors (Batch 2 - Advanced Relationships)

**Title:** Implement advanced relationship message executors

**Labels:** `enhancement`, `message-executor`, `medium-priority`

**Description:**

### Feature Description
Implement advanced relationship management message executors.

### Current Status
- ❌ Many not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Some in FakeXrmEasy v1

### Required Executors (Examples)
- `AddMembersTeam` - Add members to team
- `RemoveMembersTeam` - Remove members from team
- `AddUserToRecordTeam` - Add user to record team
- `RemoveUserFromRecordTeam` - Remove user from record team
- Connection entity operations
- Additional relationship executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`

### Priority
Medium - Common relationship operations

---

## Issue 30: Add Missing Message Executors (Batch 3 - Modern Dataverse)

**Title:** Implement modern Dataverse-specific message executors

**Labels:** `enhancement`, `message-executor`, `medium-priority`, `modern-dataverse`

**Description:**

### Feature Description
Implement message executors for modern Dataverse features.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Required Executors (Examples)
- `CreateMultiple` - Batch create operation
- `UpdateMultiple` - Batch update operation
- `UpsertMultiple` - Batch upsert operation
- `DeleteMultiple` - Batch delete operation
- Elastic table operations
- Additional modern Dataverse executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/`

### Priority
Medium - Modern Dataverse patterns

---

## Summary

This document contains 30 GitHub issues covering all major feature gaps identified in the README.md feature comparison. Issues are organized by:

**Priority Breakdown:**
- High Priority: 10 issues (core functionality gaps)
- Medium Priority: 11 issues (important enhancements)
- Low Priority: 9 issues (advanced/niche features)

**Category Breakdown:**
- Message Executors: 6 issues
- Query Support: 3 issues
- Plugin/Pipeline: 5 issues
- Field Types: 2 issues
- Business Logic: 3 issues
- Metadata: 4 issues
- Security: 2 issues
- Testing: 2 issues
- Modern Dataverse: 7 issues (overlapping with other categories)

**Recommended Implementation Order:**
1. Start with High Priority issues (1-10)
2. Address Medium Priority issues based on community needs
3. Consider Low Priority issues for specific use cases

Each issue includes:
- Clear title and description
- Current status assessment
- Specific requirements
- Related files
- Priority level
- Appropriate labels
