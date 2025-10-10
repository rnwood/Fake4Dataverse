#!/bin/bash

# Feature Parity GitHub Issues Creation Script
# This script creates GitHub issues for all features needed to achieve parity with FakeXrmEasy v2+
# Usage: ./create-feature-parity-issues.sh [--dry-run]

set -e

REPO="rnwood/Fake4Dataverse"
DRY_RUN=false

# Check for dry-run flag
if [ "$1" == "--dry-run" ]; then
    DRY_RUN=true
    echo "🔍 DRY RUN MODE - No issues will be created"
    echo ""
fi

# Function to create an issue
create_issue() {
    local title="$1"
    local body="$2"
    local labels="$3"
    
    if [ "$DRY_RUN" = true ]; then
        echo "Would create issue: $title"
        echo "  Labels: $labels"
        echo ""
    else
        echo "Creating issue: $title"
        gh issue create \
            --repo "$REPO" \
            --title "$title" \
            --body "$body" \
            --label "$labels"
        echo "✅ Created"
        echo ""
    fi
}

# Issue 1: Merge Request
create_issue \
    "Add support for Merge request operations" \
    "### Feature Description
Implement the \`MergeRequest\` message executor to support entity merge operations in Dataverse/Dynamics 365.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Limited in FakeXrmEasy v1

### Requirements
- Implement \`MergeRequestExecutor\` class
- Handle merging of two entity records
- Update all references to point to the surviving record
- Handle merge conflicts appropriately
- Add comprehensive unit tests

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`

### Priority
High - Common operation in CRM customizations

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,high-priority"

# Issue 2: Hierarchical Query Operators
create_issue \
    "Implement hierarchical query operators (Above, Under, ChildOf, etc.)" \
    "### Feature Description
Implement hierarchical query operators for querying parent-child relationships in entity hierarchies.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Required Operators
- \`Above\` - Records above in hierarchy
- \`AboveOrEqual\` - Records above or at same level
- \`Under\` - Records under in hierarchy
- \`UnderOrEqual\` - Records under or at same level
- \`ChildOf\` - Direct children

### Requirements
- Extend \`ConditionExpressionExtensions\` with hierarchical operators
- Handle hierarchical queries in QueryExpression
- Handle hierarchical queries in FetchXML
- Support self-referencing relationships
- Add comprehensive unit tests

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.*.cs\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Query/\`

### Priority
High - Required for hierarchical data structures (Account hierarchies, etc.)

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,query-support,high-priority"

# Issue 3: Advanced Fiscal Period Operators
create_issue \
    "Add support for advanced fiscal period query operators" \
    "### Feature Description
Implement advanced fiscal period operators for date-based queries using fiscal calendars.

### Current Status
- ⚠️ Basic support (InFiscalYear only) in Fake4Dataverse
- ✅ Full support in FakeXrmEasy v2+
- ⚠️ Basic support in FakeXrmEasy v1

### Required Operators
- \`InFiscalPeriod\` - Within specific fiscal period
- \`LastFiscalPeriod\` - Previous fiscal period
- \`NextFiscalPeriod\` - Next fiscal period
- \`LastFiscalYear\` - Previous fiscal year
- \`NextFiscalYear\` - Next fiscal year
- \`InFiscalPeriodAndYear\` - Specific period and year
- \`InOrBeforeFiscalPeriodAndYear\` - On or before specific period/year
- \`InOrAfterFiscalPeriodAndYear\` - On or after specific period/year

### Requirements
- Extend \`ConditionExpressionExtensions\` with fiscal period operators
- Add fiscal calendar configuration support
- Handle different fiscal year start dates
- Support fiscal period definitions
- Add comprehensive unit tests

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Query/ConditionExpressionExtensions.*.cs\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Extensions/DateTimeExtensions.cs\`

### Priority
Medium - Important for financial reporting scenarios

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,query-support,medium-priority"

# Issue 4: Custom API Support
create_issue \
    "Implement Custom API message execution support" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/\`

### Priority
High - Modern Dataverse feature, recommended over Custom Actions

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,high-priority,modern-dataverse"

# Issue 5: Custom Actions Support
create_issue \
    "Add full support for Custom Actions" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/\`

### Priority
High - Common customization pattern in Dynamics 365

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,high-priority"

# Issue 6: Calculated Fields
create_issue \
    "Implement calculated field evaluation and simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/CalculatedFields/\`

### Priority
High - Common pattern for business logic

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,field-types,high-priority"

# Issue 7: Rollup Fields
create_issue \
    "Implement rollup field calculation and simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/RollupFields/\`

### Priority
High - Common pattern for aggregating data

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,field-types,high-priority"

# Issue 8: Business Rules
create_issue \
    "Add business rules engine and simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/BusinessRules/\`

### Priority
High - Common low-code customization approach

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,business-logic,high-priority"

# Issue 9: Duplicate Detection
create_issue \
    "Implement duplicate detection rule evaluation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/DuplicateDetection/\`

### Priority
High - Important for data quality testing

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,data-quality,high-priority"

# Issue 10: Audit Log
create_issue \
    "Add audit log tracking and retrieval" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/Audit/\`

### Priority
Medium - Important for compliance testing

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,audit,medium-priority"

# Issue 11: Virtual Entities
create_issue \
    "Implement virtual entity data source simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/VirtualEntities/\`

### Priority
Medium - Growing adoption of virtual entities

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,virtual-entities,medium-priority,modern-dataverse"

# Issue 12: Elastic Tables
create_issue \
    "Add support for Elastic Tables (Dataverse for Teams)" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/ElasticTables/\`

### Priority
Low - Newer feature with limited adoption

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,elastic-tables,low-priority,modern-dataverse"

# Issue 13: Connection References
create_issue \
    "Implement Connection References for Power Platform" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`

### Priority
Low - Primarily for Power Apps/Flow integration

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,power-platform,low-priority,modern-dataverse"

# Issue 14: Cloud Flows
create_issue \
    "Add Cloud Flows execution simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/CloudFlows/\`

### Priority
Low - Complex integration scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,power-automate,low-priority,modern-dataverse"

# Issue 15: Power Automate Integration
create_issue \
    "Implement Power Automate integration testing capabilities" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/PowerAutomate/\`

### Priority
Low - Advanced integration scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,power-automate,low-priority,modern-dataverse"

# Issue 16: Multiple Plugins Per Message
create_issue \
    "Add full support for multiple plugins on same message" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/\`

### Priority
Medium - Common plugin scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,plugins,medium-priority"

# Issue 17: Complete Pipeline Simulation
create_issue \
    "Add full plugin pipeline stage simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs\`
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/\`

### Priority
Medium - Important for plugin testing

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,plugins,medium-priority"

# Issue 18: Async Plugin Support
create_issue \
    "Improve asynchronous plugin execution simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs\`

### Priority
Medium - Important for complex workflows

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,plugins,medium-priority"

# Issue 19: Pre/Post Image Support
create_issue \
    "Enhance plugin pre/post image simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/XrmFakedContext.Plugins.cs\`

### Priority
Medium - Common plugin pattern

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,plugins,medium-priority"

# Issue 20: Global OptionSet Support
create_issue \
    "Implement full global optionset metadata support" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/\`

### Priority
Medium - Common metadata scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,metadata,medium-priority"

# Issue 21: Publisher Metadata
create_issue \
    "Implement publisher metadata operations" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/\`

### Priority
Low - Advanced solution scenarios

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,metadata,low-priority"

# Issue 22: Solution Metadata
create_issue \
    "Implement solution metadata operations" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/Metadata/\`

### Priority
Low - Advanced ALM scenarios

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,metadata,low-priority"

# Issue 23: Security Role Simulation
create_issue \
    "Improve security role and privilege simulation" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`

### Priority
Medium - Important for security testing

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,security,medium-priority"

# Issue 24: Business Unit Support
create_issue \
    "Enhance business unit operations and hierarchy" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`

### Priority
Medium - Common enterprise scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,security,medium-priority"

# Issue 25: Concurrent Execution Testing
create_issue \
    "Implement concurrent execution testing capabilities" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`

### Priority
Low - Advanced testing scenario

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,testing,low-priority"

# Issue 26: Performance Profiling
create_issue \
    "Implement performance profiling and metrics" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- New: \`Fake4DataverseCore/src/Fake4Dataverse.Core/Profiling/\`

### Priority
Low - Nice-to-have feature

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,testing,low-priority"

# Issue 27: Async/Await Support
create_issue \
    "Improve async/await pattern support throughout framework" \
    "### Feature Description
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
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/\`
- Multiple files throughout codebase

### Priority
Medium - Modern development pattern

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,async,medium-priority"

# Issue 28: Business-Specific Message Executors
create_issue \
    "Implement business-specific message executors" \
    "### Feature Description
Implement various business-specific message executors for Sales, Service, Marketing modules.

### Current Status
- ❌ Many not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Some in FakeXrmEasy v1

### Required Executors (Examples)
- \`CalculatePrice\` - Calculate price for quote/order
- \`GetInvoiceProductsFromOpportunity\` - Convert opportunity to invoice
- \`LockInvoicePricing\` - Lock invoice pricing
- \`UnlockInvoicePricing\` - Unlock invoice pricing
- \`ReviseQuote\` - Revise quote
- \`CalculateActualValue\` - Calculate opportunity value
- Additional Sales/Service message executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`

### Priority
Low - Specific business module operations

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,low-priority"

# Issue 29: Advanced Relationship Message Executors
create_issue \
    "Implement advanced relationship message executors" \
    "### Feature Description
Implement advanced relationship management message executors.

### Current Status
- ❌ Many not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ⚠️ Some in FakeXrmEasy v1

### Required Executors (Examples)
- \`AddMembersTeam\` - Add members to team
- \`RemoveMembersTeam\` - Remove members from team
- \`AddUserToRecordTeam\` - Add user to record team
- \`RemoveUserFromRecordTeam\` - Remove user from record team
- Connection entity operations
- Additional relationship executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`

### Priority
Medium - Common relationship operations

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,medium-priority"

# Issue 30: Modern Dataverse Message Executors
create_issue \
    "Implement modern Dataverse-specific message executors" \
    "### Feature Description
Implement message executors for modern Dataverse features.

### Current Status
- ❌ Not implemented in Fake4Dataverse
- ✅ Available in FakeXrmEasy v2+
- ❌ Not available in FakeXrmEasy v1

### Required Executors (Examples)
- \`CreateMultiple\` - Batch create operation
- \`UpdateMultiple\` - Batch update operation
- \`UpsertMultiple\` - Batch upsert operation
- \`DeleteMultiple\` - Batch delete operation
- Elastic table operations
- Additional modern Dataverse executors

### Requirements
- Implement each message executor
- Handle request/response properly
- Add unit tests for each

### Related Files
- \`Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/\`

### Priority
Medium - Modern Dataverse patterns

### Reference
See FEATURE_PARITY_ISSUES.md for complete feature comparison" \
    "enhancement,message-executor,medium-priority,modern-dataverse"

if [ "$DRY_RUN" = true ]; then
    echo ""
    echo "✅ DRY RUN COMPLETE - 30 issues would be created"
    echo ""
    echo "To actually create the issues, run:"
    echo "  ./create-feature-parity-issues.sh"
else
    echo ""
    echo "✅ COMPLETE - 30 issues created successfully!"
    echo ""
    echo "Summary:"
    echo "  - High Priority: 10 issues"
    echo "  - Medium Priority: 11 issues"
    echo "  - Low Priority: 9 issues"
    echo ""
    echo "View all issues at: https://github.com/$REPO/issues"
fi
