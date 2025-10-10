# Installation

Fake4Dataverse is distributed as NuGet packages. This guide will help you install and configure the framework for your testing needs.

## Prerequisites

- **.NET SDK**: .NET Core 3.1 or later, or .NET Framework 4.6.2 or later
- **Test Framework**: xUnit, NUnit, or MSTest
- **IDE**: Visual Studio 2019+, Visual Studio Code, or JetBrains Rider

## Choosing the Right Package

Fake4Dataverse provides different packages based on your target Dynamics/Dataverse version:

| Dataverse/CRM Version | Package Name | NuGet |
|----------------------|--------------|-------|
| Dynamics v9 (>= 9.x) | `Fake4Dataverse.9` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse.9)](https://www.nuget.org/packages/Fake4Dataverse.9) |
| Dynamics 365 (8.2.x) | `Fake4Dataverse.365` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse.365)](https://www.nuget.org/packages/Fake4Dataverse.365) |
| Dynamics CRM 2016 (8.0-8.1) | `Fake4Dataverse.2016` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse.2016)](https://www.nuget.org/packages/Fake4Dataverse.2016) |
| Dynamics CRM 2015 (7.x) | `Fake4Dataverse.2015` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse.2015)](https://www.nuget.org/packages/Fake4Dataverse.2015) |
| Dynamics CRM 2013 (6.x) | `Fake4Dataverse.2013` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse.2013)](https://www.nuget.org/packages/Fake4Dataverse.2013) |
| Dynamics CRM 2011 (5.x) | `Fake4Dataverse` | [![NuGet](https://buildstats.info/nuget/Fake4Dataverse)](https://www.nuget.org/packages/Fake4Dataverse) |

> **💡 Tip**: For most modern projects using Dataverse or Dynamics 365, use `Fake4Dataverse.9`.

## Core Packages

The framework is split into modular packages:

### Required Packages
- **Fake4Dataverse.Abstractions** - Core interfaces and abstractions
- **Fake4Dataverse.Core** - Core implementation (middleware, CRUD, queries)

### Optional Packages
- **Fake4Dataverse.Plugins** - Plugin execution support (recommended for plugin testing)
- **Fake4Dataverse.Pipeline** - Pipeline simulation enhancements

> **Note**: When you install the main package (e.g., `Fake4Dataverse.9`), it automatically includes Abstractions and Core.

## Installation Methods

### Option 1: Package Manager Console

```powershell
# For Dynamics 365 / Dataverse (v9+)
Install-Package Fake4Dataverse.9

# For plugin testing, also install:
Install-Package Fake4Dataverse.Plugins
```

### Option 2: .NET CLI

```bash
# For Dynamics 365 / Dataverse (v9+)
dotnet add package Fake4Dataverse.9

# For plugin testing, also install:
dotnet add package Fake4Dataverse.Plugins
```

### Option 3: PackageReference in .csproj

Add to your test project's `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
  <PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
  
  <!-- Test framework (choose one) -->
  <PackageReference Include="xunit" Version="2.4.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
</ItemGroup>
```

## Setting Up Your Test Project

### Step 1: Create a Test Project

```bash
# Create a new xUnit test project
dotnet new xunit -n MyProject.Tests

cd MyProject.Tests
```

### Step 2: Install Fake4Dataverse

```bash
dotnet add package Fake4Dataverse.9
dotnet add package Fake4Dataverse.Plugins
```

### Step 3: Reference Your Plugin/Business Logic Project

```bash
dotnet add reference ../MyProject.Plugins/MyProject.Plugins.csproj
```

### Step 4: Create Your First Test

Create a new file `MyFirstTest.cs`:

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace MyProject.Tests
{
    public class MyFirstTest
    {
        [Fact]
        public void Should_CreateAccount_Successfully()
        {
            // Arrange
            var context = XrmFakedContextFactory.New();
            var service = context.GetOrganizationService();
            
            var account = new Entity("account")
            {
                ["name"] = "Test Account"
            };
            
            // Act
            var accountId = service.Create(account);
            
            // Assert
            Assert.NotEqual(Guid.Empty, accountId);
            
            var retrieved = service.Retrieve("account", accountId, 
                new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
            Assert.Equal("Test Account", retrieved["name"]);
        }
    }
}
```

### Step 5: Run Your Tests

```bash
dotnet test
```

## Early-Bound Entity Classes

If your project uses early-bound entity classes generated from the CRM schema, you need to enable proxy types:

```csharp
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();

// Enable proxy types for your generated entities
context.EnableProxyTypes(typeof(Account).Assembly);

var service = context.GetOrganizationService();
```

## Troubleshooting

### Issue: "Type or namespace 'XrmFakedContextFactory' could not be found"

**Solution**: Ensure you have the correct using statements:
```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions;
```

### Issue: "Plugin execution not working"

**Solution**: Install the `Fake4Dataverse.Plugins` package:
```bash
dotnet add package Fake4Dataverse.Plugins
```

### Issue: "Early-bound entities not recognized"

**Solution**: Call `EnableProxyTypes()` on your context:
```csharp
context.EnableProxyTypes(typeof(YourGeneratedEntity).Assembly);
```

### Issue: "NuGet package not found"

**Solution**: Ensure you're using the correct package name for your CRM/Dataverse version. Check the [package list](#choosing-the-right-package) above.

## Version Compatibility

| Fake4Dataverse | .NET Core | .NET Framework | Dynamics SDK |
|----------------|-----------|----------------|--------------|
| 4.x | 3.1+ | 4.6.2+ | 9.x |
| | 3.1+ | 4.6.2+ | 8.x (365) |

## Next Steps

- [Quick Start Guide](./quickstart.md) - Write your first test
- [Basic Concepts](./basic-concepts.md) - Understand the framework architecture
- [Testing Plugins](../usage/testing-plugins.md) - Learn plugin testing patterns

## Additional Resources

- [Official Dynamics 365 SDK Documentation](https://docs.microsoft.com/en-us/power-apps/developer/data-platform/)
- [NuGet Package Repository](https://www.nuget.org/packages?q=Fake4Dataverse)
- [GitHub Repository](https://github.com/rnwood/Fake4Dataverse)
