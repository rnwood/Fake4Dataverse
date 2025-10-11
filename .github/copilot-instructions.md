# Copilot Instructions for Fake4Dataverse

## Project Overview

Fake4Dataverse is a testing and mocking framework for Microsoft Dynamics 365 / Dynamics CRM and the Power Platform. It allows developers to write unit tests for plugins, custom workflow activities, and other code that interacts with the Dynamics CRM/365 platform without needing a live CRM instance.

**Project Name**: Fake4Dataverse (formerly FakeXrmEasy)
**Original Author**: Jordi Montaña
**Current Maintainer**: Rob Wood
**License**: MIT

## Documentation Structure

The project has comprehensive documentation in the `/docs/` directory:

### Documentation Organization

1. **Getting Started** (`/docs/getting-started/`)
   - `installation.md` - Installation instructions for Dataverse v9+
   - `quickstart.md` - Quick start guide with examples
   - `basic-concepts.md` - Core concepts and framework fundamentals

2. **Core Concepts** (`/docs/concepts/`)
   - Architecture and design patterns
   - XrmFakedContext and middleware pipeline
   - Service initialization and data management

3. **Usage Guides** (`/docs/usage/`)
   - `testing-plugins.md` - Comprehensive plugin testing guide
   - `crud-operations.md` - CRUD operation patterns
   - `querying-data.md` - LINQ and FetchXML query examples
   - `batch-operations.md` - ExecuteMultiple and transactions
   - `security-permissions.md` - Security testing

4. **Message Executors** (`/docs/messages/`)
   - `README.md` - Overview of all 43+ supported message executors
   - Category-specific documentation (crud, associations, security, etc.)

5. **Migration Guides** (`/docs/migration/`)
   - From FakeXrmEasy v1.x
   - From FakeXrmEasy v3.x (commercial version)

6. **API Reference** (`/docs/api/`)
   - Interface documentation
   - Extension methods
   - Custom message executor creation

**Main documentation entry point**: `/docs/README.md`

## Repository Structure

This is a monorepo containing three main projects:

### 1. Fake4DataverseAbstractions
- **Location**: `/Fake4DataverseAbstractions/`
- **Purpose**: Contains abstractions, interfaces, POCOs, enums, and base types used across the framework
- **Former Name**: FakeXrmEasy.Abstractions

### 2. Fake4DataverseCore
- **Location**: `/Fake4DataverseCore/`
- **Purpose**: Core implementation of the framework including middleware, CRUD operations, query translation, and message executors
- **Former Name**: FakeXrmEasy.Core
- **Key Features**:
  - Middleware-based architecture (inspired by ASP.NET Core)
  - In-memory context for testing
  - FetchXML and LINQ query support
  - Plugin and workflow activity simulation
  - 43+ message executors

### 3. Fake4Dataverse
- **Location**: `/Fake4Dataverse/`
- **Purpose**: Legacy/compatibility package
- **Former Name**: FakeXrmEasy

## Technology Stack

- **Language**: C#
- **Framework**: .NET Core 3.1 / .NET Framework (multi-targeting)
- **Build System**: PowerShell scripts with dotnet CLI
- **Test Framework**: xUnit
- **CI/CD**: GitHub Actions
- **Code Quality**: SonarCloud

## Build and Test Commands

### Building Projects

Each project can be built using PowerShell scripts:

```bash
# Build FakeXrmEasyAbstrations
cd FakeXrmEasyAbstrations
pwsh ./build.ps1

# Build FakeXrmEasyCore
cd FakeXrmEasyCore
pwsh ./build.ps1

# Build FakeXrmEasy
cd FakeXrmEasy
pwsh ./build.ps1
```

#### Build Script Parameters
- `targetFramework`: Specify target framework (default: "netcoreapp3.1", use "all" for all targets)
- `configuration`: Build configuration (e.g., "FAKE_XRM_EASY_9")

### Running Tests

Tests are automatically run as part of the build scripts. To run tests separately:

```bash
# Run tests for a specific project
cd <project-directory>
dotnet test --configuration <configuration> --verbosity normal
```

### Local Package Build
```bash
pwsh ./build-push-local.ps1
```
This creates a `local-packages` folder with the built NuGet packages.

## Coding Standards and Conventions

### C# Code Style
- Follow standard C# naming conventions (PascalCase for classes, methods, properties; camelCase for private fields with underscore prefix)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and cohesive

### Testing Standards
- Use xUnit for all unit tests
- Test class names should end with "Tests" (e.g., `PullRequestExceptionTests`)
- Test method names should be descriptive and follow the pattern: `Should_<expected_behavior>_When_<condition>`
- Use Arrange-Act-Assert pattern in tests
- Provide unit tests for bug fixes and new features
- **Always include documentation references in test comments** - Reference the official Microsoft documentation URL that describes the behavior being tested
- For Dataverse/CRM behaviors, include comments like `// Reference: https://learn.microsoft.com/en-us/dotnet/api/...` to document the source of expected behavior

### Exception Handling
- Use `PullRequestException` for features not yet implemented to encourage community contributions
- Include clear, helpful error messages

## Key Concepts

### XrmFakedContext
The main context class that simulates the Dynamics CRM/365 environment. Use `IXrmFakedContext` interface in 2.x versions.

### Middleware Architecture (2.x)
The framework uses a configurable middleware pipeline (inspired by ASP.NET Core) that processes organization requests:
- Multiple behaviors can be configured per request type
- Execution order can be customized
- Extensible without modifying core framework code

### Message Executors
Implement `IFakeMessageExecutor` to handle specific CRM organization requests. Located in `/FakeMessageExecutors/` directories.

**When implementing Dataverse/CRM message executors:**
1. **Research the behavior** - Always start by researching the official Microsoft documentation at https://learn.microsoft.com/en-us/dotnet/api/
2. **Search for additional sources** - Look for blog articles, community posts, and other resources that describe the behavior in detail (but do not use FakeXrmEasy source code or tests)
3. **Document all sources with explanatory text** - **MANDATORY**: When referencing documentation URLs in code comments, you MUST include relevant explanatory text from the referenced documentation that describes what the feature/operator/behavior does. Simply providing a URL is not sufficient. Include:
   - A brief description of what the feature/operator does
   - Key parameters or values it accepts
   - Expected behavior and results
   - Example: Instead of just `// Reference: https://...`, write `// Reference: https://... \n// OperatorName: Description of what it does, what parameters it takes, and what it returns`
4. **Implement all documented properties** - Ensure all properties and behaviors from the official API are implemented
5. **Test all behaviors** - Create comprehensive tests covering all documented behaviors, edge cases, and error conditions
6. **Include references with explanations in tests** - Every test should include a comment referencing the source documentation URL AND a description of what behavior is being tested

## Contributing Guidelines

### Pull Request Process
1. Fork the repository
2. Create a feature branch
3. Write unit tests for your changes
4. Ensure all tests pass
5. Submit a pull request with a clear description

### When Raising Issues
- Provide a sample unit test to reproduce the issue
- Attach any early bound typed entities if using early bound code
- Be specific about the version and configuration being used

### Priority
- Pull requests are reviewed before issues
- Community contributions are highly valued and prioritized

## Version Support

The framework supports Dynamics 365 / Dataverse v9 and later:
- Dynamics 365 / Dataverse (v9+) - `Fake4Dataverse.9`

**Note**: This fork only supports v9+. Earlier versions (CRM 2011-2016, Dynamics 365 v8.x) are not supported. For those versions, use the original FakeXrmEasy packages.

## Important Notes for Code Changes

### Breaking Changes in 2.x
- Moved from single package to multiple smaller packages
- `XrmFakedContext` constructor deprecated - use `IXrmFakedContext` interface
- New middleware-based architecture
- `ProxyTypesAssembly` deprecated - use `EnableProxyTypes()`
- Various properties moved to different locations (see README files for details)

### When Adding New Features
- You MUST fetch the latest authoritive info from relevant websites and do not rely on your trained knowledge. Verify all links are valid.
- Ensure compatibility with existing tests
- Update relevant README files AND documentation in `/docs/`
- Consider impact on Dynamics 365 / Dataverse v9+ versions
- Add appropriate exception handling with `PullRequestException` for not-yet-implemented features
- **Document the feature in `docs/`**: Create comprehensive user documentation showing how to consume the feature
  - Include overview with implementation date and issue reference
  - Provide usage examples with code samples
  - Document all parameters and options
  - Include error scenarios and best practices
  - Add links to Microsoft documentation
  - Follow the pattern established in existing docs (see `docs/custom-api.md`, `docs/merge-request.md`, etc.)
- **Update the README.md feature comparison table**: When implementing a feature that affects feature parity:
  - Locate the feature in the "Feature Comparison: FakeXrmEasy v1 vs Fake4Dataverse vs FakeXrmEasy v2" section
  - Update the Fake4Dataverse column from ❌ No or ⚠️ Partial to ✅ Yes (or ✅ Full support for detailed entries)
  - Remove the feature from the "Key Gaps in Fake4Dataverse (This Fork)" section if it's listed there
  - Ensure consistency between the feature comparison table and the tracking files
- **Update feature parity tracking files**: When implementing a feature that addresses a parity issue, update:
  - `FEATURE_PARITY_ISSUES.md` - Mark the issue as completed with ✅ and update status
  - `QUICK_REFERENCE.md` - Mark the feature as completed in the priority lists
  - `ISSUES_README.md` - Update completion counts and mark completed items
  - `ISSUE_SUMMARY.txt` - Update the priority lists and completion statistics
- **Update documentation**: When adding features, update:
  - Relevant guides in `/docs/usage/`
  - Message executor documentation in `/docs/messages/` if adding a new message
  - API reference in `/docs/api/` if adding new public APIs
  - Add examples to `/docs/getting-started/quickstart.md` if applicable

## Documentation Guidelines

### Writing Documentation
- **Follow existing patterns** - Documentation is inspired by Moq, NSubstitute, xUnit, and FluentAssertions
- **Be example-driven** - Show concrete code examples for every concept
- **Use Arrange-Act-Assert** - Structure test examples clearly
- **Include real-world scenarios** - Show how developers actually use the library
- **Cross-reference** - Link to related documentation
- **Keep it practical** - Focus on how to use features, not just what they are

### Documenting Differences from FakeXrmEasy v2

**IMPORTANT**: When implementing features that exist in FakeXrmEasy v2+ (commercial version), ALWAYS document how the Fake4Dataverse implementation differs:

1. **In ALL relevant documentation** - Not just migration guides
   - Include a "Key Differences from FakeXrmEasy v2" section in feature documentation
   - Use comparison tables when multiple differences exist
   - Be specific about setup steps, configuration, and behavior differences

2. **In feature usage guides** (`/docs/usage/*.md`)
   - Show both the Fake4Dataverse way AND explain how it differs from v2
   - Include examples that highlight the differences
   - Document any additional steps required in Fake4Dataverse

3. **In migration guides** (`/docs/migration/*.md`)
   - Provide detailed migration instructions
   - Include before/after code examples
   - Explain the rationale for differences when helpful

4. **Example pattern for documenting differences:**
   ```markdown
   ### Key Differences from FakeXrmEasy v2

   **Important**: The [feature name] in Fake4Dataverse differs from FakeXrmEasy v2+ in several ways:

   1. **[Difference category]**: [Description of what's different]
   2. **[Another category]**: [Description]

   **Comparison Table:**

   | Feature | FakeXrmEasy v2+ | Fake4Dataverse v4 |
   |---------|----------------|-------------------|
   | [Aspect 1] | [v2 behavior] | [v4 behavior] |
   | [Aspect 2] | [v2 behavior] | [v4 behavior] |
   ```

### Documentation Structure
- **Getting Started** - Quick wins, installation, first test
- **Concepts** - Deep dives into architecture and design
- **Usage Guides** - Scenario-based how-to guides (testing plugins, queries, etc.)
- **Message Executors** - Reference documentation for each supported message
- **Migration** - Version migration guides
- **API Reference** - Interface and method documentation

### When to Update Documentation
- New feature added → Update usage guide + message docs (if applicable)
- Bug fix with behavioral change → Update affected documentation
- Breaking change → Update migration guide + relevant docs
- New message executor → Add to `/docs/messages/` category files

## License

This project is licensed under the MIT License. See LICENSE.txt files in each project directory.

## Additional Resources

- **Main documentation**: `/docs/README.md` - Complete documentation index
- **Quick start**: `/docs/getting-started/quickstart.md` - First test in 5 minutes
- **Plugin testing**: `/docs/usage/testing-plugins.md` - Comprehensive plugin guide
- **Message executors**: `/docs/messages/README.md` - All supported messages
- Project READMEs: See README.md files in each project directory
- FetchXML operator status: Check `ConditionOperator` implementation
- Message executor status: Check `/FakeMessageExecutors/` directories
- For client-side testing: See fake-xrm-easy-js repository
