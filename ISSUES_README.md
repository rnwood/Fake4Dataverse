# Feature Parity Issues - Creation Guide

This directory contains resources for creating GitHub issues to track features needed to achieve parity with FakeXrmEasy v2+.

## Overview

Based on the feature comparison in [README.md](./README.md), we have identified **30 feature gaps** that need to be addressed to achieve feature parity with FakeXrmEasy v2+ (commercial version).

## Files in This Package

1. **FEATURE_PARITY_ISSUES.md** - Detailed documentation of all 30 issues with complete descriptions, requirements, and priorities
2. **create-feature-parity-issues.sh** - Automated script to create all issues using GitHub CLI
3. **ISSUES_README.md** - This file

## Quick Start

### Prerequisites

- GitHub CLI (`gh`) installed and authenticated
- Write access to the repository (or fork)
- Bash shell environment

### Creating All Issues

```bash
# Test first with dry-run mode to see what would be created
./create-feature-parity-issues.sh --dry-run

# Create all issues
./create-feature-parity-issues.sh
```

### Creating Issues Manually

If you prefer to create issues manually or selectively, refer to `FEATURE_PARITY_ISSUES.md` which contains formatted issue templates ready to copy and paste into GitHub's issue creation form.

## Issue Categories

### By Priority

- **High Priority (10 issues)**: Core functionality gaps that significantly impact testing capabilities
  - ~~Merge operations~~ ✅
  - ~~Hierarchical queries~~ ✅
  - ~~Advanced fiscal periods~~ ✅
  - ~~Custom APIs~~ ✅
  - Custom Actions
  - Calculated/Rollup fields
  - Business rules
  - Duplicate detection

- **Medium Priority (11 issues)**: Important enhancements that improve testing coverage
  - ~~Advanced fiscal periods~~ ✅ (moved to completed)
  - ~~Multiple plugins per message~~ ✅
  - ~~Complete pipeline simulation~~ ✅
  - Plugin pipeline improvements
  - Metadata enhancements
  - Security simulation
  - Async support

- **Low Priority (9 issues)**: Advanced or niche features
  - Virtual entities
  - Elastic tables
  - Power Platform integration
  - Performance profiling
  - Concurrent testing

### By Feature Area

- **Message Executors** (6 issues): New message types and executors
- **Query Support** (3 issues): Advanced query operators
- **Plugin/Pipeline** (5 issues): Plugin execution enhancements
- **Field Types** (2 issues): Calculated and rollup fields
- **Business Logic** (3 issues): Business rules and validation
- **Metadata** (4 issues): Metadata operation improvements
- **Security** (2 issues): Security and permission simulation
- **Testing** (2 issues): Testing infrastructure improvements
- **Modern Dataverse** (7 issues): Modern platform features

## Implementation Approach

### Recommended Order

1. **Phase 1 - Core Functionality** (High Priority)
   - Issues 1-10 (4 completed ✅, 6 remaining)
   - Focus: Essential features for most testing scenarios

2. **Phase 2 - Enhanced Testing** (Medium Priority)
   - Issues 3, 10-11, 16-20, 23-24, 27, 29 (Issues 16-17 completed ✅)
   - Focus: Improved test coverage and accuracy

3. **Phase 3 - Advanced Features** (Low Priority)
   - Issues 12-15, 21-22, 25-26, 28
   - Focus: Specialized and advanced scenarios

### Contributing

Each issue includes:
- Clear feature description
- Current status assessment
- Specific requirements checklist
- Related file locations
- Priority and labels

Contributors can:
1. Pick an issue that matches their expertise
2. Comment on the issue to claim it
3. Implement the feature following the requirements
4. Submit a pull request referencing the issue

## Issue Labels

The script automatically applies these labels:

- **Priority**: `high-priority`, `medium-priority`, `low-priority`
- **Type**: `enhancement`
- **Category**: `message-executor`, `query-support`, `plugins`, `field-types`, `business-logic`, `metadata`, `security`, `testing`, `audit`, `data-quality`, `async`, `virtual-entities`, `elastic-tables`, `power-platform`, `power-automate`, `modern-dataverse`

## Tracking Progress

After issues are created:

1. Create a GitHub Project board to track progress
2. Organize issues by priority and status
3. Link related issues (e.g., calculated fields and rollup fields)
4. Update issue status as work progresses
5. Reference issues in pull requests

## Notes

- All issues reference the source comparison in README.md
- Each issue is self-contained and can be worked on independently
- Some issues may have dependencies (noted in requirements)
- Priority levels are recommendations and can be adjusted based on community needs

## Questions or Feedback?

If you have questions about specific issues or want to discuss implementation approaches, please comment on the relevant GitHub issue or start a discussion in the repository.

## See Also

- [Main README](./README.md) - Feature comparison table
- [Fake4Dataverse README](./Fake4Dataverse/README.md) - Migration guide and documentation
- [Contributing Guidelines](./CONTRIBUTING.md) - How to contribute (if exists)
