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
- ✅ **IMPLEMENTED** in Fake4Dataverse (as of 2025-10-10)
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
- ✅ **IMPLEMENTED** in Fake4Dataverse (as of 2025-10-10)
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
- ✅ **IMPLEMENTED** in Fake4Dataverse (as of 2025-10-10)
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic support in FakeXrmEasy v1

### Required Operators
- `InFiscalPeriod` - Within specific fiscal period ✅
- `LastFiscalPeriod` - Previous fiscal period ✅
- `NextFiscalPeriod` - Next fiscal period ✅
- `LastFiscalYear` - Previous fiscal year ✅
- `NextFiscalYear` - Next fiscal year ✅
- `InFiscalPeriodAndYear` - Specific period and year ✅
- `InOrBeforeFiscalPeriodAndYear` - On or before specific period/year ✅
- `InOrAfterFiscalPeriodAndYear` - On or after specific period/year ✅

### Requirements
- Extend `ConditionExpressionExtensions` with fiscal period operators ✅
- Add fiscal calendar configuration support ✅
- Handle different fiscal year start dates ✅
- Support fiscal period definitions ✅
- Add comprehensive unit tests ✅

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.FiscalPeriod.cs` ✅
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Extensions/XmlExtensionsForFetchXml.cs` ✅
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/FetchXml/FiscalPeriodOperatorTests.cs` ✅

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
- ✅ **IMPLEMENTED** in Fake4Dataverse (as of 2025-10-10)
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Requirements
- Support custom API definition and registration ✅
- Handle custom API request/response parameters ✅
- Support both function and action style APIs ✅
- Handle Custom API execution in plugins ✅
- Add middleware for Custom API execution ✅
- Add comprehensive unit tests ✅

### Implementation Details
- Created `CustomApiExecutor` with full metadata validation
- Supports all parameter data types (Boolean, DateTime, Decimal, Entity, EntityCollection, EntityReference, Float, Integer, Money, Picklist, String, StringArray, Guid)
- Enhanced middleware for multiple OrganizationRequest executors
- 11 comprehensive test cases covering all scenarios
- All 801 tests passing (100% test coverage maintained)

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/CustomApiExecutor.cs` ✅
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/Messages/MiddlewareBuilderExtensions.Messages.cs` ✅
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/FakeMessageExecutors/OrganizationRequestExecutors.cs` ✅
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/CustomApiTests/CustomApiExecutorTests.cs` ✅

### Priority
High - Modern Dataverse feature, recommended over Custom Actions

---

## Issue 5: Implement Custom Actions Support ✅

**Title:** Add full support for Custom Actions

**Labels:** `enhancement`, `message-executor`, `high-priority`

**Status:** ✅ **COMPLETED** (2025-10-11)

**Description:**

### Feature Description
Implement full support for Custom Actions (process-based actions).

### Current Status
- ✅ **Implemented in Fake4Dataverse v4.x** (2025-10-11)
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Implementation Details
- Custom Actions are fully supported via the existing `CustomApiExecutor` infrastructure
- Both Custom APIs (modern) and Custom Actions (legacy) use `OrganizationRequest` with custom message names
- Plugins can be registered for custom action messages at any pipeline stage
- Custom actions work through metadata (customapi entity) with:
  - Entity-bound or global actions via `boundentitylogicalname` attribute
  - Input/output parameters handled through OrganizationRequest/Response
  - Enabled/disabled validation via `isenabled` attribute
- Comprehensive test coverage (9 tests total) in `CustomActionPluginTests.cs`

### Requirements
- ✅ Support custom action definition and registration
- ✅ Handle action input/output parameters
- ✅ Support both entity-bound and global actions
- ✅ Handle action execution in plugin pipeline
- ✅ Add middleware for action execution
- ✅ Add comprehensive unit tests

### Related Files
- `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/CustomApiExecutor.cs` ✅
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/Messages/MiddlewareBuilderExtensions.Messages.cs` ✅
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline/CustomActionPluginTests.cs` ✅

### Priority
High - Common customization pattern in Dynamics 365

---

## Issue 6: Add Calculated Fields Simulation ✅

**Title:** Implement calculated field evaluation and simulation

**Labels:** `enhancement`, `field-types`, `high-priority`

**Status:** ✅ **COMPLETED** - Implemented in v4.0.0

**Description:**

### Feature Description
Add support for simulating calculated fields in Dataverse entities.

### Current Status
- ✅ **Implemented in Fake4Dataverse v4.0.0**
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Implementation Details
- **NCalc Integration**: Uses NCalc 1.3.8 for expression evaluation
- **Supported Functions**: All Microsoft-documented functions (CONCAT, DIFFINDAYS, ADDDAYS, TRIMLEFT, TRIMRIGHT, etc.)
- **Formula Preprocessing**: Converts `[fieldname]` syntax to NCalc variables
- **Automatic Evaluation**: Calculated fields are evaluated on entity retrieve and update
- **Documentation**: Comprehensive guide at `docs/usage/calculated-fields.md`

### Implemented Features
- ✅ Support calculated field definition
- ✅ Parse and evaluate calculated field formulas
- ✅ Handle calculated field dependencies
- ✅ Update calculated fields on entity retrieve
- ✅ Update calculated fields on entity update
- ✅ Support all calculated field data types (string, number, date, boolean)
- ✅ String functions: CONCAT, UPPER, LOWER, TRIM, TRIMLEFT, TRIMRIGHT, LEFT, RIGHT, MID, REPLACE, LEN
- ✅ Date functions: DIFFINDAYS, DIFFINHOURS, DIFFINMINUTES, DIFFINMONTHS, DIFFINWEEKS, DIFFINYEARS
- ✅ Date manipulation: ADDHOURS, ADDDAYS, ADDWEEKS, ADDMONTHS, ADDYEARS, SUBTRACTHOURS, SUBTRACTDAYS, SUBTRACTWEEKS, SUBTRACTMONTHS, SUBTRACTYEARS
- ✅ Math functions: ROUND, ABS, FLOOR, CEILING
- ✅ Logical functions: IF, ISNULL
- ✅ Logical operators: AND, OR, NOT
- ✅ Comparison operators: >, <, >=, <=, ==, !=
- ✅ Null handling and type conversions
- ✅ Circular dependency detection
- ✅ Comprehensive unit tests
- ✅ Complete documentation with examples

### Related Files
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/CalculatedFields/CalculatedFieldDefinition.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/CalculatedFields/CalculatedFieldEvaluator.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/CalculatedFields/DataverseFunctionExtensions.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.CalculatedFields.cs`
- ✅ `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/CalculatedFields/CalculatedFieldBasicTests.cs`
- ✅ `docs/usage/calculated-fields.md`

### Documentation
- [Calculated Fields Usage Guide](docs/usage/calculated-fields.md) - Comprehensive 18KB+ documentation
- [Microsoft: Define Calculated Fields](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields) - Official reference

### FakeXrmEasy v2+ Equivalent
**Note**: FakeXrmEasy v2+ has calculated field support but implementation details are in their commercial codebase. Our implementation is based on verified Microsoft documentation and provides similar functionality using the NCalc expression engine.

**References**:
- FakeXrmEasy v2+ calculated fields feature is documented as available but specific API documentation is in their commercial docs
- Implementation approach differs: Fake4Dataverse uses explicit `RegisterCalculatedField()` calls vs FakeXrmEasy v2+ metadata-based registration

### Priority
High - Common pattern for business logic

**Completed:** 2025-10-11 (PR #22)

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

## Issue 8: Implement Business Rules Simulation ✅

**Title:** Add business rules engine and simulation

**Labels:** `enhancement`, `business-logic`, `high-priority`

**Description:**

### Feature Description
Implement simulation of Dataverse business rules for field validation and logic.

### Current Status
- ✅ **Implemented** in Fake4Dataverse (2025-10-12)
- ⚠️ **Unverified** in FakeXrmEasy v2+ (no public documentation or code found as of October 2025)
- ❌ Not available in FakeXrmEasy v1

### Requirements
- ✅ Support business rule definition
- ✅ Handle field validation rules
- ✅ Support show/hide field logic (tracked, not enforced server-side)
- ✅ Support set field value logic
- ✅ Support recommendation actions
- ✅ Execute rules at appropriate times (onCreate, onUpdate)
- ✅ Add comprehensive unit tests (6/6 integration tests + 4/4 direct tests = 10/10 passing)

### Implementation Details
- ✅ Complete business rule engine with 8 core classes (~1,800 lines of code)
- ✅ Automatic execution during Create and Update operations
- ✅ Support for all ConditionOperators
- ✅ IF-THEN-ELSE logic with conditions and else actions
- ✅ AND/OR logic for multiple conditions
- ✅ 9 action types: SetFieldValue, ShowErrorMessage, SetBusinessRequired, etc.
- ✅ Comprehensive documentation (17KB user guide)

### Related Files
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleDefinition.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleExecutor.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleCondition.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleAction.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleActionType.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleScope.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleTrigger.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/BusinessRuleExecutionResult.cs`
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.BusinessRules.cs`
- ✅ `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/BusinessRules/BusinessRuleExecutorDirectTests.cs`
- ✅ `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/BusinessRules/BusinessRuleExecutorTests.cs`
- ✅ `docs/usage/business-rules.md`

### Documentation
- [Business Rules Usage Guide](docs/usage/business-rules.md) - Comprehensive 17KB documentation
- [Microsoft: Create Business Rules](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule) - Official reference

### FakeXrmEasy v2+ Equivalent
**Note**: As of October 2025 research, business rules support in FakeXrmEasy v2+ could not be verified through:
- Public GitHub repositories (DynamicsValue organization)
- Public documentation sites
- Code search in their repositories

Fake4Dataverse's implementation is based entirely on Microsoft's official documentation and provides comprehensive server-side business rule simulation. This may be the first open-source implementation of business rules testing for Dataverse.

**Potential Key Differences** (if FakeXrmEasy v2 has this feature):
1. **Context Setup**: Fake4Dataverse requires `XrmFakedContextFactory.New()` for middleware integration
2. **Accessing Executor**: Fake4Dataverse requires cast: `(XrmFakedContext)context.BusinessRuleExecutor`
3. **Rule Registration**: Fake4Dataverse uses explicit `BusinessRuleDefinition` objects
4. **Scope Support**: Fake4Dataverse supports Entity (server-side) scope only; client-side tracking but not enforced

### Priority
High - Common low-code customization approach (COMPLETED)

### Implementation Date
October 12, 2025

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

## Issue 14: Implement Cloud Flows Integration Testing ✅

**Title:** Add Cloud Flows execution simulation

**Labels:** `enhancement`, `power-automate`, `low-priority`, `modern-dataverse`

**Description:**

### Feature Description
Add support for testing Cloud Flows (Power Automate) triggered by Dataverse events.

### Current Status
- ✅ **Implemented in Fake4Dataverse** (2025-10-11)
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Implementation Summary
Complete Cloud Flow simulation with:
- Flow registration and manual triggering
- Automatic flow triggering on CRUD operations (Create, Update, Delete)
- Built-in Dataverse connector with full CRUD support
- Extensible connector system for mocking external systems
- Filtered attributes support (Update triggers)
- CreateOrUpdate message handling
- Comprehensive verification APIs
- 47 unit tests, all passing ✅

### Requirements
- ✅ Mock Cloud Flow triggers
- ✅ Support Dataverse connector actions
- ✅ Simulate flow execution
- ✅ Verify flow was triggered
- ✅ Add comprehensive unit tests

### Related Files
- ✅ `Fake4DataverseCore/src/Fake4Dataverse.Core/CloudFlows/`
- ✅ `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/CloudFlows/`
- ✅ `docs/usage/cloud-flows.md`
- ✅ `docs/API_DESIGN_CLOUD_FLOWS.md`

### Priority
Low - Complex integration scenario (COMPLETED)

### Implementation Date
October 11, 2025

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

## Issue 16: Enhance Multiple Plugins Per Message Support ✅

**Title:** Add full support for multiple plugins on same message

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Status:** ✅ **COMPLETED** (2025-10-10)

**Description:**

### Feature Description
Improve support for registering and executing multiple plugins for the same message/entity combination.

### Current Status
- ✅ **Implemented in Fake4Dataverse v4.x** (2025-10-10)
- ✅ Full support in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Implementation Details
- Added `PluginStepRegistration` class for plugin registration
- Implemented `IPluginPipelineSimulator` interface
- Support for multiple plugin registrations per message/entity/stage
- Plugins execute in order based on `ExecutionOrder` (rank) property
- Support for filtering attributes on Update messages
- Configuration parameters (secure/unsecure)
- Comprehensive test coverage (13 tests)

### Requirements
- ✅ Support multiple plugin registrations
- ✅ Execute plugins in correct order (execution order property)
- ✅ Handle plugin step registration configurations
- ✅ Support filtering by attributes
- ✅ Add comprehensive unit tests

### Related Files
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/PluginStepRegistration.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/IPluginPipelineSimulator.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/PluginPipelineSimulator.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Plugins/XrmFakedPluginContextProperties.cs`
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline/PluginPipelineSimulatorTests.cs`

### Documentation
- [Testing Plugins - Plugin Pipeline Simulator](./docs/usage/testing-plugins.md#plugin-pipeline-simulator)

### Priority
Medium - Common plugin scenario

---

## Issue 17: Implement Complete Pipeline Simulation ✅

**Title:** Add full plugin pipeline stage simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Status:** ✅ **COMPLETED** (2025-10-10)

**Description:**

### Feature Description
Implement complete plugin pipeline with all stages properly simulated.

### Current Status
- ✅ **Implemented in Fake4Dataverse v4.x** (2025-10-10)
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic in FakeXrmEasy v1

### Implementation Details
- All pipeline stages supported (PreValidation=10, PreOperation=20, PostOperation=40)
- Transaction boundary awareness (documented in context properties)
- Depth tracking with configurable maximum (default = 8)
- Accurate pipeline behavior simulation
- Comprehensive test coverage

### Requirements
- ✅ Support all pipeline stages (PreValidation, PreOperation, MainOperation, PostOperation)
- ✅ Handle transaction boundaries
- ✅ Support pipeline depth tracking
- ✅ Simulate pipeline behavior accurately
- ✅ Add comprehensive unit tests

### Related Files
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/IPluginPipelineSimulator.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/PluginPipelineSimulator.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/Plugins/XrmFakedPluginContextProperties.cs`
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline/PluginPipelineSimulatorTests.cs`

### Documentation
- [Testing Plugins - Plugin Pipeline Simulator](./docs/usage/testing-plugins.md#plugin-pipeline-simulator)

### Priority
Medium - Important for plugin testing

---

## Issue 18: Enhance Async Plugin Support ✅

**Title:** Improve asynchronous plugin execution simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Status:** ✅ **COMPLETED** (2025-10-11)

**Description:**

### Feature Description
Improve support for testing asynchronous plugins.

### Current Status
- ✅ **Implemented in Fake4Dataverse v4.x** (2025-10-11)
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Implementation Details
- Created `AsyncOperation` class mirroring Dataverse's asyncoperation entity
- Implemented `AsyncJobQueue` for queuing and executing async plugins
- Added `AsyncOperationState`, `AsyncOperationStatus`, and `AsyncOperationType` enums
- Async plugins are now queued instead of executed synchronously
- Full monitoring and control APIs for test writers
- Support for waiting on async operations (sync and async/await)
- Auto-execute mode for simpler test scenarios
- Comprehensive test coverage (18 tests)

### Requirements
- ✅ Support async plugin execution mode
- ✅ Handle async service context
- ✅ Simulate system job queue
- ✅ Support async plugin debugging
- ✅ Add comprehensive unit tests

### Related Files
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Enums/AsyncOperationState.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Enums/AsyncOperationStatus.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Enums/AsyncOperationType.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/AsyncOperation.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/IAsyncJobQueue.cs`
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/IPluginPipelineSimulator.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/AsyncJobQueue.cs`
- `Fake4DataverseCore/src/Fake4Dataverse.Core/PluginPipelineSimulator.cs`
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline/AsyncPluginExecutionTests.cs`

### Documentation
- [Testing Plugins - Async Plugins](./docs/usage/testing-plugins.md#async-plugins)

### Priority
Medium - Important for complex workflows

---

## Issue 19: Improve Pre/Post Image Support ✅

**Title:** Enhance plugin pre/post image simulation

**Labels:** `enhancement`, `plugins`, `medium-priority`

**Status:** ✅ **COMPLETED** (2025-10-11)

**Description:**

### Feature Description
Enhance support for pre and post images in plugin context.

### Current Status
- ✅ **Full support implemented in Fake4Dataverse v4.x** (2025-10-11)
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic in FakeXrmEasy v1

### Implementation Details
- Created `PluginStepImageRegistration` class with comprehensive image configuration:
  - Image name and entity alias
  - Image type (PreImage, PostImage, or Both)
  - Filtered attributes support
  - Message validation (pre-images for Update/Delete, post-images for Create/Update)
- Extended `PluginStepRegistration` with `PreImages` and `PostImages` collections
- Updated `PluginPipelineSimulator` to automatically create images based on registration:
  - Retrieves current entity state for pre-images on Update/Delete
  - Uses target entity for post-images on Create/Update
  - Applies attribute filters to reduce payload
  - Supports multiple named images per registration
- Comprehensive test coverage (10 tests) in `PluginImageTests.cs`:
  - Pre-image creation for Update with all attributes
  - Pre-image creation with filtered attributes
  - Post-image creation for Create and Update
  - Multiple images with different filters
  - Both pre and post images simultaneously
  - Message-specific validation (no pre-images for Create, no post-images for Delete)

### Requirements
- ✅ Support image registration configurations
- ✅ Handle filtered attributes in images
- ✅ Ensure correct image snapshots at each stage
- ✅ Support multiple images
- ✅ Add comprehensive unit tests

### Related Files
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/PluginStepImageRegistration.cs` ✅
- `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/Plugins/PluginStepRegistration.cs` ✅
- `Fake4DataverseCore/src/Fake4Dataverse.Core/PluginPipelineSimulator.cs` ✅
- `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/Pipeline/PluginImageTests.cs` ✅

### Priority
Medium - Common plugin pattern

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
- High Priority: 10 issues (3 remaining, 7 completed ✅)
- Medium Priority: 11 issues (8 remaining, 3 completed ✅)
- Low Priority: 9 issues (8 remaining, 1 completed ✅)

**Completed Issues:**
- ✅ Issue #1: Merge Request (Implemented 2025-10-10)
- ✅ Issue #2: Hierarchical Query Operators (Implemented 2025-10-10)
- ✅ Issue #3: Advanced Fiscal Period Operators (Implemented 2025-10-10)
- ✅ Issue #4: Custom API Support (Implemented 2025-10-10)
- ✅ Issue #5: Custom Actions Support (Implemented 2025-10-11)
- ✅ Issue #8: Business Rules Simulation (Implemented 2025-10-12)
- ✅ Issue #14: Cloud Flows Integration Testing (Implemented 2025-10-11)
- ✅ Issue #16: Multiple Plugins Per Message Support (Implemented 2025-10-10)
- ✅ Issue #17: Complete Pipeline Simulation (Implemented 2025-10-10)
- ✅ Issue #18: Async Plugin Support (Implemented 2025-10-11)
- ✅ Issue #19: Pre/Post Image Support (Implemented 2025-10-11)

**Category Breakdown:**
- Message Executors: 6 issues (2 completed ✅)
- Query Support: 3 issues (2 completed ✅)
- Plugin/Pipeline: 5 issues (5 completed ✅)
- Field Types: 2 issues
- Business Logic: 3 issues (1 completed ✅)
- Metadata: 4 issues
- Security: 2 issues
- Testing: 2 issues
- Modern Dataverse: 7 issues (1 completed ✅, overlapping with other categories)

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
