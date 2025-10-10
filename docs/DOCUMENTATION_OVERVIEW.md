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
│   └── middleware.md                 # Middleware pipeline architecture
├── usage/                             # Practical how-to guides
│   ├── testing-plugins.md            # Plugin testing patterns (17KB)
│   ├── crud-operations.md            # CRUD patterns & examples (14KB)
│   ├── querying-data.md              # LINQ & FetchXML queries (17KB)
│   └── batch-operations.md           # ExecuteMultiple & Transactions (16KB)
└── messages/                          # Message executor reference
    ├── README.md                      # Overview of 43+ executors
    ├── crud.md                        # Create, Retrieve, Update, Delete (14KB)
    └── associations.md                # Associate & Disassociate (15KB)
```

## Documentation Stats

- **Total Files**: 14 markdown files
- **Total Lines**: ~6,800 lines of documentation
- **Total Size**: ~120KB of comprehensive guides
- **Code Examples**: 100+ working code samples
- **Coverage**: Core concepts, usage patterns, and message executors

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

### Structure
- Each guide starts with Table of Contents
- Includes "Next Steps" for navigation
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

### File Naming
- Use lowercase with hyphens: `testing-plugins.md`
- Be descriptive: `batch-operations.md` not `batches.md`
- Group related docs in subdirectories

### Content Guidelines
- Write in second person ("you can...")
- Use active voice
- Keep sentences concise
- Explain "why" not just "how"

## What's Missing?

The following documentation is planned but not yet created:

### Migration Guides
- [ ] `migration/from-v1.md` - Migrate from FakeXrmEasy v1.x
- [ ] `migration/from-v3.md` - Migrate from FakeXrmEasy v3.x

### Additional Usage Guides
- [ ] `usage/security-permissions.md` - Testing security
- [ ] `usage/testing-workflows.md` - Workflow activity testing

### Message Categories
- [ ] `messages/security.md` - Security messages (Grant/Revoke Access)
- [ ] `messages/business-process.md` - Business process messages
- [ ] `messages/teams.md` - Team membership messages
- [ ] `messages/queues.md` - Queue operations
- [ ] `messages/metadata.md` - Metadata operations
- [ ] `messages/specialized.md` - Other messages

### API Reference
- [ ] `api/ixrm-faked-context.md` - Interface reference
- [ ] `api/extension-methods.md` - Available extensions
- [ ] `api/custom-executors.md` - Creating custom executors

### Concepts
- [ ] `concepts/data-management.md` - Managing test data
- [ ] `concepts/service-initialization.md` - Service patterns

## Feedback

Documentation feedback is welcome! Please:
- Open issues for errors or unclear sections
- Suggest improvements or new topics
- Share examples from your projects
- Help keep docs in sync with code

## License

Documentation is licensed under MIT, same as the code.
