# Documentation Overview

This directory contains comprehensive documentation for Fake4Dataverse, organized to help developers at every stage of using the framework.

## Documentation Structure

```
docs/
├── README.md                          # Main documentation index
├── getting-started/                   # New user guides
│   ├── installation.md               # Package installation guide
│   ├── quickstart.md                 # 5-minute first test
│   ├── basic-concepts.md             # Framework fundamentals
│   └── faq.md                        # Common questions & troubleshooting
├── concepts/                          # Deep dive into architecture
│   ├── xrm-faked-context.md         # Context properties & methods
│   ├── middleware.md                 # Middleware pipeline architecture
│   ├── service-initialization.md     # Service creation patterns  
│   └── data-management.md            # Managing test data
├── usage/                             # Practical how-to guides
│   ├── testing-plugins.md            # Plugin testing patterns (17KB)
│   ├── crud-operations.md            # CRUD patterns & examples (14KB)
│   ├── querying-data.md              # LINQ & FetchXML queries (17KB)
│   ├── batch-operations.md           # ExecuteMultiple & Transactions (16KB)
│   ├── testing-workflows.md          # Workflow activity testing
│   ├── security-permissions.md       # Security testing patterns
│   ├── custom-api.md                 # Custom API implementation
│   ├── merge-request.md              # Merge request operations
│   ├── hierarchical-queries.md       # Hierarchical query operators
│   └── fiscal-period-operators.md    # Fiscal period operators
├── messages/                          # Message executor reference
│   ├── README.md                     # Overview of 43+ executors
│   ├── crud.md                       # Create, Retrieve, Update, Delete (14KB)
│   ├── associations.md               # Associate & Disassociate (15KB)
│   ├── business-process.md           # Business process messages
│   ├── specialized.md                # Specialized messages
│   ├── security.md                   # Security messages
│   ├── metadata.md                   # Metadata messages
│   ├── queues.md                     # Queue operations
│   └── teams.md                      # Team membership
├── api/                               # API reference
│   ├── ixrm-faked-context.md         # Context interface reference
│   ├── extension-methods.md          # Extension methods
│   └── custom-executors.md           # Creating custom executors
└── migration/                         # Migration guides
    ├── from-v1.md                    # Migrate from v1.x
    └── from-v3.md                    # Migrate from commercial v3.x
```

## Documentation Stats

- **Total Files**: 30+ markdown files
- **Total Lines**: ~25,000+ lines of documentation
- **Total Size**: ~500KB+ of comprehensive guides
- **Code Examples**: 300+ working code samples
- **Coverage**: Complete framework documentation

## Quick Access

### For New Users
1. [Installation](./getting-started/installation.md) - Install the right package
2. [Quick Start](./getting-started/quickstart.md) - Your first test in 5 minutes
3. [Basic Concepts](./getting-started/basic-concepts.md) - Understand the framework

### For Plugin Developers
1. [Testing Plugins](./usage/testing-plugins.md) - Complete plugin testing guide
2. [CRUD Operations](./usage/crud-operations.md) - Create, read, update, delete
3. [Querying Data](./usage/querying-data.md) - LINQ and FetchXML

### For Advanced Users
1. [XrmFakedContext](./concepts/xrm-faked-context.md) - Context deep dive
2. [Middleware Architecture](./concepts/middleware.md) - Pipeline customization
3. [Message Executors](./messages/README.md) - All supported messages
4. [Custom Executors](./api/custom-executors.md) - Extend the framework

## Documentation Philosophy

This documentation follows best practices from popular .NET testing frameworks:

### Inspired By
- **Moq** - Clear, example-driven documentation
- **NSubstitute** - Scenario-based guides
- **xUnit** - Comprehensive reference
- **FluentAssertions** - Easy-to-follow patterns

### Key Principles
1. **Example-Driven** - Every concept includes working code
2. **Practical** - Focus on real-world scenarios
3. **Progressive** - From beginner to advanced
4. **Searchable** - Clear structure and cross-references
5. **Maintainable** - Keep in sync with code changes

## Documentation Standards

### Code Examples
- All examples use xUnit (but work with any test framework)
- Follow Arrange-Act-Assert pattern
- Include complete, runnable code
- Show both success and error cases
- Include Microsoft documentation references where applicable

### Structure
- Each guide starts with Table of Contents
- Includes "See Also" for navigation
- Cross-references related documentation
- Links to official Microsoft documentation

### Style
- Clear headings and sections
- Code comments where helpful
- ✅ Do / ❌ Don't examples
- Real-world scenarios

## Contributing to Documentation

When contributing to documentation:

1. **Maintain consistency** with existing style
2. **Include examples** for every feature
3. **Cross-reference** related docs
4. **Test code samples** to ensure they work
5. **Update index** (README.md) if adding new files
6. **Reference Microsoft documentation** - Include URLs and explanatory text

### File Naming
- Use lowercase with hyphens: `testing-plugins.md`
- Be descriptive: `batch-operations.md` not `batches.md`
- Group related docs in subdirectories

### Content Guidelines
- Write in second person ("you can...")
- Use active voice
- Keep sentences concise
- Explain "why" not just "how"
- **Include Microsoft documentation references** with explanatory text

## Recent Updates (2025-10-10)

All documentation has been completed and enhanced:

### Completed Sections
✅ All concept documentation fully written
✅ All usage guides completed
✅ All message executor documentation completed
✅ All API reference documentation completed
✅ All migration guides completed

### Key Improvements
- Removed all "Coming Soon" placeholders
- Added comprehensive examples to all guides
- Included Microsoft documentation references
- Moved misplaced files to proper locations
- Enhanced migration guides with detailed steps

### Reorganized Files
The following files were moved from `docs/` root to `docs/usage/`:
- `custom-api.md` → `usage/custom-api.md`
- `merge-request.md` → `usage/merge-request.md`
- `hierarchical-queries.md` → `usage/hierarchical-queries.md`
- `fiscal-period-operators.md` → `usage/fiscal-period-operators.md`

## Feedback

Documentation feedback is welcome! Please:
- Open issues for errors or unclear sections
- Suggest improvements or new topics
- Share examples from your projects
- Help keep docs in sync with code

## License

Documentation is licensed under MIT, same as the code.
