# Testing Guide

This guide explains how to run tests in the Fake4Dataverse repository.

## Overview

The repository contains several types of tests:

1. **Unit Tests** - Fast, isolated tests that don't require external services
2. **Integration Tests** - Tests that require the Fake4DataverseService to be running
3. **MDA Unit Tests** - Jest tests for the Model-Driven App front-end
4. **MDA E2E Tests** - Playwright tests for the Model-Driven App (require service running)

## Running Unit Tests (.NET)

Unit tests can be run for all projects in the solution:

```bash
# From repository root
dotnet test Fake4Dataverse.sln --configuration Debug --framework net8.0 --filter "FullyQualifiedName!~IntegrationTests"
```

For .NET Framework 4.6.2 (Windows only):

```bash
dotnet test Fake4Dataverse.sln --configuration Debug --framework net462 --filter "FullyQualifiedName!~IntegrationTests"
```

### Running Tests for Individual Projects

```bash
# Fake4DataverseCore tests
cd Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests
dotnet test --configuration Debug --framework net8.0

# Fake4Dataverse tests
cd Fake4Dataverse/tests/Fake4Dataverse.Tests
dotnet test --configuration Debug --framework net8.0

# Fake4DataverseCloudFlows tests
cd Fake4DataverseCloudFlows/tests/Fake4Dataverse.CloudFlows.Tests
dotnet test --configuration Debug --framework net8.0

# Fake4DataverseService tests (unit tests only)
cd Fake4DataverseService/tests/Fake4Dataverse.Service.Tests
dotnet test --configuration Debug --framework net8.0
```

## Running Integration Tests (.NET)

Integration tests require the Fake4DataverseService to be running on `http://localhost:5559`.

### Step 1: Start the Service

```bash
cd Fake4DataverseService/Fake4Dataverse.Service
dotnet run --configuration Debug
```

The service will start and listen on:
- HTTP: `http://localhost:5559`
- HTTPS: `https://localhost:5560`

### Step 2: Run Integration Tests

In a separate terminal:

```bash
cd Fake4DataverseService/tests/Fake4Dataverse.Service.IntegrationTests
dotnet test --configuration Debug --framework net8.0
```

Or run all integration tests:

```bash
dotnet test Fake4Dataverse.sln --configuration Debug --framework net8.0 --filter "FullyQualifiedName~IntegrationTests"
```

## Running MDA Tests

The Model-Driven App has its own test suite using Jest and Playwright.

### MDA Unit Tests

```bash
cd Fake4DataverseService/mda-app

# Install dependencies (first time only)
npm ci

# Run all unit tests
npm test

# Run tests with coverage report
npm test -- --coverage

# Run tests in watch mode (for development)
npm run test:watch
```

**Test Coverage:**
- 22 unit tests covering:
  - Navigation component (8 tests)
  - EntityListView component (7 tests)
  - EntityForm component (7 tests)

### MDA E2E Tests

E2E tests require the Fake4DataverseService to be running with MDA metadata initialized.

#### Step 1: Start the Service

```bash
cd Fake4DataverseService/Fake4Dataverse.Service
dotnet run --configuration Debug
```

Make sure to initialize MDA metadata (the service should do this automatically on startup).

#### Step 2: Run E2E Tests

In a separate terminal:

```bash
cd Fake4DataverseService/mda-app

# Install Playwright browsers (first time only)
npx playwright install --with-deps chromium

# Run E2E tests
npm run test:e2e

# Run E2E tests in UI mode (interactive)
npm run test:e2e:ui
```

**Test Coverage:**
- 14 E2E tests covering:
  - Navigation (6 tests)
  - Form rendering and interaction (8 tests)

## Continuous Integration

The GitHub Actions workflow runs all tests in a single job on windows-latest:

1. **Unit tests** for net8.0 and net462
2. **MDA unit tests** with coverage reporting
3. **MDA build** to verify the Next.js app compiles
4. **MDA E2E tests** with Playwright (Playwright works on Windows)
5. **Package creation** for all 4 NuGet packages (Abstractions, Core, Fake4Dataverse, Fake4DataverseService)
6. **Publishing to NuGet** (only on non-PR builds)
7. **GitHub Release creation** (only on non-PR builds)

All tests must pass before any packages are published to NuGet or GitHub Releases.

Integration tests are excluded from the main CI build to prevent timeouts, but they can be run manually or in a separate workflow that starts the service first.

## Troubleshooting

### Integration Tests Fail with "Connection refused"

**Problem**: Integration tests can't connect to the service.

**Solution**: Make sure the Fake4DataverseService is running on `http://localhost:5559` before running integration tests.

### E2E Tests Fail with Service Not Available

**Problem**: Playwright E2E tests can't reach the service.

**Solution**: 
1. Start the Fake4DataverseService on `http://localhost:3000` (or update the Playwright config)
2. Verify MDA metadata is initialized using `MdaInitializer.InitializeExampleMda()`

### Tests Hang or Timeout

**Problem**: Tests hang indefinitely or timeout.

**Solution**:
- For integration tests: Verify the service is running and responding
- For E2E tests: Check the Playwright config and ensure the dev server auto-start is configured correctly
- Increase test timeout if needed for slower environments

### .NET Framework Tests Don't Run on Linux/macOS

**Problem**: Tests targeting net462 fail on non-Windows platforms.

**Solution**: This is expected. .NET Framework 4.6.2 only runs on Windows. Use net8.0 tests on Linux/macOS.

## Test Organization

### Unit Tests
- Located in `tests/` directories within each project
- Use xUnit as the test framework
- Follow the naming pattern: `Should_<expected_behavior>_When_<condition>`
- Use Arrange-Act-Assert pattern

### Integration Tests
- Located in `Fake4DataverseService/tests/Fake4Dataverse.Service.IntegrationTests/`
- Test the service endpoints (WCF, OData, MDA)
- Require the service to be running

### MDA Tests
- **Unit tests**: `Fake4DataverseService/mda-app/app/components/__tests__/`
- **E2E tests**: `Fake4DataverseService/mda-app/e2e/`
- Use Jest for unit tests, Playwright for E2E tests

## Best Practices

1. **Run unit tests frequently** during development to catch issues early
2. **Use the filter** to exclude integration tests when running the full suite
3. **Start the service** before running integration or E2E tests
4. **Check test output** for warnings and failures
5. **Keep tests fast** - unit tests should complete in seconds
6. **Document test requirements** - if a test needs specific setup, document it

## See Also

- [Testing Plugins](usage/testing-plugins.md) - Guide to testing Dynamics 365 plugins
- [Testing Workflows](usage/testing-workflows.md) - Guide to testing custom workflow activities
- [MDA App Testing](../Fake4DataverseService/mda-app/TESTING.md) - Detailed MDA testing documentation
