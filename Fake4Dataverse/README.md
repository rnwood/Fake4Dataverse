
# Fake4Dataverse: Test Automation Framework for Microsoft Dataverse and Power Platform

**Note:** This is a fork of the original FakeXrmEasy project by Jordi Montaña. See the [main README](../README.md) for information about this fork and its relationship to the original project.

## What's New in 2.x?

  - Support for .net core 3.1 / .NET 8.0 / Full .NET framework support with multi-targeting   
  - Single original repo broken down into smaller, easier to maintain, repos
  - New semantinc versioning using prerelease suffixes [SemVer 2.0.0](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#semantic-versioning-200) 
  - Original FAKE build script has been redeveloped in Powershell Core for building both locally (build-local-push.ps1) and form GitHub actions, cross platform. 
  - Added SonarCloud quality gate
  - Now using GitHub Actions as opposed to AppVeyor previously in 1.x.
  - ** New Middleware!!! ** => effectivley rewritten the core implementation (based on aspnetcore middleware)
  - New GetProperty / SetProperty to allow to dynamically extend context properties
  - Massive refactoring

## Migration Guide

This section provides step-by-step guidance for migrating from FakeXrmEasy to Fake4Dataverse.

### Migrating from FakeXrmEasy v1.x to Fake4Dataverse v2.x

If you're upgrading from FakeXrmEasy v1.x, follow these steps to migrate your tests:

#### 1. Update Package References

Replace your FakeXrmEasy v1.x package with Fake4Dataverse packages:

```xml
<!-- Remove v1.x packages -->
<!-- <PackageReference Include="FakeXrmEasy" Version="1.x.x" /> -->

<!-- Add v2.x packages -->
<PackageReference Include="Fake4Dataverse.Abstractions" Version="2.0.0.1" />
<PackageReference Include="Fake4Dataverse.Core" Version="2.0.0.1" />
<!-- Add other packages as needed: Plugins, Pipeline, Messages.Cds, Messages.Dynamics -->
```

#### 2. Update Context Initialization

**v1.x (Old):**
```csharp
var context = new XrmFakedContext();
var service = context.GetOrganizationService();
```

**v2.x (New):**
```csharp
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();
```

Or use the middleware builder directly for more control:

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Crud;
using Fake4Dataverse.Middleware.Messages;

var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();

var service = context.GetOrganizationService();
```

#### 3. Update Early Bound Types Configuration

**v1.x (Old):**
```csharp
context.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account));
```

**v2.x (New):**
```csharp
context.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

// Check if using early bound assemblies:
if (context.ProxyTypesAssemblies.Count() > 0) 
{
    // Early bound assemblies are configured
}
```

#### 4. Update CallerId Property

**v1.x (Old):**
```csharp
context.CallerId = new EntityReference("systemuser", Guid.NewGuid());
```

**v2.x (New):**
```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", Guid.NewGuid());
```

#### 5. Update Settings Access

**v1.x (Old):**
```csharp
context.FiscalYearSettings = new FiscalYearSettings();
context.TimeZoneInfo = TimeZoneInfo.Local;
```

**v2.x (New):**
```csharp
context.SetProperty<FiscalYearSettings>(new FiscalYearSettings());
context.SetProperty<TimeZoneInfo>(TimeZoneInfo.Local);

// To retrieve:
var fiscalYearSettings = context.GetProperty<FiscalYearSettings>();
```

#### 6. Update Reference Validation

**v1.x (Old):**
```csharp
context.ValidateReferences = true;
```

**v2.x (New):**
```csharp
using Fake4Dataverse.Integrity;

var context = MiddlewareBuilder
    .New()
    .AddCrud(new IntegrityOptions() { ValidateEntityReferences = true })
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();
```

Or using the factory with options:

```csharp
var context = XrmFakedContextFactory.New(new IntegrityOptions() 
{ 
    ValidateEntityReferences = true 
});
```

#### 7. Update Plugin Context Properties

**v1.x (Old):**
```csharp
var tracingService = context.GetFakedTracingService();
```

**v2.x (New):**
```csharp
// These methods have been moved to the Fake4Dataverse.Plugins package
// Install: Fake4Dataverse.Plugins
// Then use PluginContextProperties class
```

#### 8. Update Custom Message Executors

**v1.x (Old):**
```csharp
public class MyCustomExecutor : IFakeMessageExecutor
{
    public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
    {
        // Implementation
    }
}
```

**v2.x (New):**
```csharp
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;

public class MyCustomExecutor : IFakeMessageExecutor
{
    public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
    {
        // Implementation - note IXrmFakedContext instead of XrmFakedContext
    }
    
    public bool CanExecute(OrganizationRequest request)
    {
        // Implementation
    }
    
    public Type GetResponsibleRequestType()
    {
        // Implementation
    }
}
```

#### 9. Update Relationship Enumerations

**v1.x (Old):**
```csharp
var relationship = new XrmFakeRelationship
{
    RelationshipType = /* old enum value */
};
```

**v2.x (New):**
```csharp
var relationship = new XrmFakeRelationship
{
    RelationshipType = FakeRelationshipType.OneToMany // Updated enum name
};
```

### Migrating from FakeXrmEasy v3.x to Fake4Dataverse v2.x

**Important Note:** Fake4Dataverse is based on FakeXrmEasy v2.0.1 (the last MIT-licensed version). FakeXrmEasy v3.x is a commercial product that continued development after the license change. If you're migrating from v3.x to Fake4Dataverse v2.x, you are essentially moving from a more recent commercial version back to an earlier open-source version.

#### Understanding the Migration Path

Since v3.x was developed after v2.0.1, it likely includes features and improvements that are not present in Fake4Dataverse v2.x. This migration is about moving from commercial software to an open-source alternative, which may involve some trade-offs.

#### Step 1: Assess Compatibility

Before migrating, determine if your codebase uses v3-specific features:

1. **Review your test code** for any APIs or features introduced in v3.x
2. **Check the v3.x changelog** (if available) to identify v3-specific features you're using
3. **Make a list of dependencies** on v3-specific functionality
4. **Consider alternatives** for v3-specific features in the open-source v2.x

#### Step 2: Update Package References

Replace FakeXrmEasy v3.x packages with Fake4Dataverse v2.x packages:

```xml
<!-- Remove v3.x packages -->
<!-- <PackageReference Include="FakeXrmEasy.v3.Core" Version="3.x.x" /> -->

<!-- Add Fake4Dataverse v2.x packages -->
<PackageReference Include="Fake4Dataverse.Abstractions" Version="2.0.0.1" />
<PackageReference Include="Fake4Dataverse.Core" Version="2.0.0.1" />
<!-- Add other packages as needed -->
```

#### Step 3: Update Context Initialization

If v3.x uses different initialization patterns, convert them to v2.x patterns:

**Fake4Dataverse v2.x approach:**
```csharp
using Fake4Dataverse.Middleware;

// Option 1: Using factory
var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Option 2: Using middleware builder for custom configuration
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .UseCrud()
    .UseMessages()
    .Build();
```

#### Step 4: Update Namespace References

Update all namespace imports:

```csharp
// Remove v3.x namespaces (if different)
// using FakeXrmEasy.v3.*;

// Add v2.x namespaces
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Crud;
using Fake4Dataverse.Middleware.Messages;
```

#### Step 5: Apply v1 → v2 Migration Steps

Since Fake4Dataverse v2.x is based on the v2.0.1 codebase, you'll need to ensure your code follows v2.x patterns. Review the "Migrating from FakeXrmEasy v1.x to Fake4Dataverse v2.x" section above and apply those patterns, particularly:

- Use `IXrmFakedContext` interface instead of concrete classes
- Use factory methods for context initialization
- Use `context.CallerProperties.CallerId` instead of `context.CallerId`
- Use `GetProperty<T>()` / `SetProperty<T>()` for dynamic properties
- Use `EnableProxyTypes()` instead of `ProxyTypesAssembly`

#### Step 6: Test Incrementally

**Critical:** Migrate one test file at a time:

```csharp
// Example: Before (v3.x pattern - adapt as needed)
// Note: Actual v3.x API may differ - adjust based on your code

[Fact]
public void TestExample()
{
    // If using v3-specific initialization, replace with:
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Your test logic
    var account = new Account { Name = "Test" };
    var id = service.Create(account);
    
    Assert.NotEqual(Guid.Empty, id);
}
```

#### Step 7: Handle Missing Features

If you encounter v3-specific features that don't exist in v2.x:

1. **Check if there's a workaround** using v2.x APIs
2. **Simplify your tests** if the feature was purely for convenience
3. **Contribute back** by opening an issue or pull request to add the feature to Fake4Dataverse
4. **Consider alternatives** such as creating extension methods for missing functionality

**Example: Creating an extension method for missing functionality**
```csharp
public static class ContextExtensions
{
    // If a v3 feature is missing, you can create an extension method
    public static void ConfigureV3Feature(this IXrmFakedContext context)
    {
        // Implement using available v2.x APIs
        // This is a placeholder - adjust based on actual needs
    }
}
```

#### Step 8: Validation Strategy

After migration:

1. **Run your entire test suite** to identify issues
2. **Fix failing tests** by adapting to v2.x APIs
3. **Compare behavior** with v3.x to ensure correctness
4. **Document any workarounds** you had to implement
5. **Report issues** on GitHub if you find bugs or missing critical features

#### Common Differences to Be Aware Of

While we cannot enumerate all v3.x features (as it's a commercial product), here are common areas where differences might exist:

- **API surface changes**: v3.x may have introduced new APIs or changed existing ones
- **Performance improvements**: v3.x may have optimizations not present in v2.x
- **Additional message executors**: v3.x may support more Dataverse messages out of the box
- **Enhanced metadata support**: v3.x may have better metadata handling
- **Improved query translation**: v3.x may support more complex LINQ or FetchXML queries

**Strategy:** Start with simple tests first, then progressively migrate more complex tests.

### Common Migration Scenarios

#### Scenario: Simple Unit Test

**Before (v1.x/v3.x):**
```csharp
[Fact]
public void TestAccountCreation()
{
    var context = new XrmFakedContext();
    var service = context.GetOrganizationService();
    
    var account = new Account { Name = "Test Account" };
    var id = service.Create(account);
    
    Assert.NotEqual(Guid.Empty, id);
}
```

**After (v2.x):**
```csharp
[Fact]
public void TestAccountCreation()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var account = new Account { Name = "Test Account" };
    var id = service.Create(account);
    
    Assert.NotEqual(Guid.Empty, id);
}
```

#### Scenario: Plugin Testing with Initial Data

**Before (v1.x/v3.x):**
```csharp
[Fact]
public void TestPlugin()
{
    var context = new XrmFakedContext();
    context.Initialize(new List<Entity>
    {
        new Account { Id = Guid.NewGuid(), Name = "Existing Account" }
    });
    
    // Plugin testing code
}
```

**After (v2.x):**
```csharp
[Fact]
public void TestPlugin()
{
    var context = XrmFakedContextFactory.New();
    context.Initialize(new List<Entity>
    {
        new Account { Id = Guid.NewGuid(), Name = "Existing Account" }
    });
    
    // Plugin testing code
}
```

### Need Help?

If you encounter issues during migration:

1. Check the [Fake4DataverseCore README](../Fake4DataverseCore/README.md) for detailed documentation
2. Review the [test examples](../Fake4DataverseCore/tests/) in the repository
3. Open an issue on GitHub with a minimal reproduction case
