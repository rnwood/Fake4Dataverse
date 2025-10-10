# Copilot Instructions for fake-xrm-free

## Project Overview

Fake Xrm Easy is a testing and mocking framework for Microsoft Dynamics 365 / Dynamics CRM and the Power Platform. It allows developers to write unit tests for plugins, custom workflow activities, and other code that interacts with the Dynamics CRM/365 platform without needing a live CRM instance.

## Repository Structure

This is a monorepo containing three main projects:

### 1. FakeXrmEasyAbstrations
- **Location**: `/FakeXrmEasyAbstrations/`
- **Purpose**: Contains abstractions, interfaces, POCOs, enums, and base types used across the framework
- **Solution**: `FakeXrmEasy.Abstractions.sln`
- **Project**: `src/FakeXrmEasy.Abstractions/FakeXrmEasy.Abstractions.csproj`
- **Tests**: `tests/FakeXrmEasy.Abstractions.Tests/FakeXrmEasy.Abstractions.Tests.csproj`

### 2. FakeXrmEasyCore
- **Location**: `/FakeXrmEasyCore/`
- **Purpose**: Core implementation of the framework including middleware, CRUD operations, query translation, and message executors
- **Solution**: `FakeXrmEasy.Core.sln`
- **Project**: `src/FakeXrmEasy.Core/FakeXrmEasy.Core.csproj`
- **Tests**: `tests/FakeXrmEasy.Core.Tests/FakeXrmEasy.Core.Tests.csproj`
- **Key Features**:
  - Middleware-based architecture (inspired by ASP.NET Core)
  - In-memory context for testing
  - FetchXML and LINQ query support
  - Plugin and workflow activity simulation

### 3. FakeXrmEasy
- **Location**: `/FakeXrmEasy/`
- **Purpose**: Legacy/compatibility package
- **Solution**: `FakeXrmEasy.sln`
- **Project**: `src/FakeXrmEasy/FakeXrmEasy.csproj`
- **Tests**: `tests/FakeXrmEasy.Tests/FakeXrmEasy.Tests.csproj`

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
3. **Document all sources** - Reference the documentation URL in code comments and test comments
4. **Implement all documented properties** - Ensure all properties and behaviors from the official API are implemented
5. **Test all behaviors** - Create comprehensive tests covering all documented behaviors, edge cases, and error conditions
6. **Include references in tests** - Every test should include a comment referencing the source documentation URL

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

The framework supports multiple Dynamics CRM/365 versions:
- Dynamics v9 (>= 9.x) - `FakeXrmEasy.9`
- Dynamics 365 (8.2.x) - `FakeXrmEasy.365`
- Dynamics CRM 2016 (8.0-8.1) - `FakeXrmEasy.2016`
- Dynamics CRM 2015 (7.x) - `FakeXrmEasy.2015`
- Dynamics CRM 2013 (6.x) - `FakeXrmEasy.2013`
- Dynamics CRM 2011 (5.x) - `FakeXrmEasy`

## Important Notes for Code Changes

### Breaking Changes in 2.x
- Moved from single package to multiple smaller packages
- `XrmFakedContext` constructor deprecated - use `IXrmFakedContext` interface
- New middleware-based architecture
- `ProxyTypesAssembly` deprecated - use `EnableProxyTypes()`
- Various properties moved to different locations (see README files for details)

### When Adding New Features
- Ensure compatibility with existing tests
- Update relevant README files
- Consider impact on different CRM/365 versions
- Add appropriate exception handling with `PullRequestException` for not-yet-implemented features

## License

This project is licensed under the MIT License. See LICENSE.txt files in each project directory.

## Additional Resources

- Main documentation: See README.md files in each project directory
- FetchXML operator status: Check `ConditionOperator` implementation
- Message executor status: Check `/FakeMessageExecutors/` directories
- For client-side testing: See fake-xrm-easy-js repository
