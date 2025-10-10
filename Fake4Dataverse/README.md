
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

**Important Note:** Fake4Dataverse is based on FakeXrmEasy v2.0.1 (the last MIT-licensed version). If you're using the commercial FakeXrmEasy v3.x, this guide will help you migrate to the open-source Fake4Dataverse v2.x.

#### Key Differences from v3.x

Since Fake4Dataverse v2.x is based on an earlier version of FakeXrmEasy, you may need to account for features that were added in v3.x but are not present in Fake4Dataverse v2.x. Here are the main considerations:

#### 1. Check Feature Availability

Before migrating from v3.x, verify that all features you're using are available in Fake4Dataverse v2.x. Review the feature set documented in the [Fake4DataverseCore README](../Fake4DataverseCore/README.md).

#### 2. Context Initialization

If you're using v3.x-specific initialization patterns, adapt them to use the v2.x middleware approach:

**v3.x patterns:**
```csharp
// v3.x may have different initialization patterns
// Adapt them to the v2.x middleware approach shown below
```

**Fake4Dataverse v2.x (Target):**
```csharp
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();
```

#### 3. API Changes

Any v3-specific APIs will need to be replaced with their v2.x equivalents:

- Review the v2.x API documentation in the package README files
- Use the middleware builder pattern for customization
- Leverage `GetProperty<T>()` and `SetProperty<T>()` for dynamic properties

#### 4. Testing Strategy

For a smooth migration:

1. **Run your existing test suite** against FakeXrmEasy v3.x and document all passing tests
2. **Create a feature compatibility list** of what you're currently using
3. **Update one test file at a time** to Fake4Dataverse v2.x
4. **Compare results** to ensure behavior consistency
5. **Report any missing features** as GitHub issues to help improve Fake4Dataverse

#### 5. Namespace Changes

Update all namespace references:

```csharp
// Remove v3.x namespaces
// using FakeXrmEasy.*;

// Add v2.x namespaces
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Crud;
using Fake4Dataverse.Middleware.Messages;
```

#### 6. Middleware Configuration

Leverage the flexible middleware system in v2.x:

```csharp
var context = MiddlewareBuilder
    .New()
    
    // Add middleware configuration
    .AddCrud()
    .AddFakeMessageExecutors()
    
    // Define pipeline sequence
    .UseCrud()
    .UseMessages()
    
    .Build();
```

This middleware approach allows you to:
- Configure multiple behaviors per request
- Customize pipeline execution order
- Extend functionality without modifying core framework code

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

  ## Breaking changes summary from 1.x -> 2.x

  - The major updates is that, since the release of Powerplatform.Cds.Client nuget package, we broke the original package into several smaller ones:

      - Fake4Dataverse.Abstractions  (base package with abstractions - interfaces, poco, enums, etc)
      - Fake4Dataverse.Core  ( the core of the framework, middleware, crud, query translation)
      - Fake4Dataverse.Plugins
      - Fake4Dataverse.Pipeline  (pipeline simulation behaviors, test interaction between plugins and react to messages)
      - Fake4Dataverse.CodeActivities
      - Fake4Dataverse.Messages.Cds   (CDS specific messages, this matches the separation introduced by MS)
      - Fake4Dataverse.Messages.Dynamics (Dynamics specific messages, again, matches separation by MS)
      - Fake4Dataverse (package with a default)
      - Fake4Dataverse.Integration  (XrmRealContext moved to this separate package)

 - XrmFakedContext constructor is deprecated => use IXrmFakedContext interface directly and encouraging to use a factory method instead. The factory method can be used to put the middleware initialisation in the one place, to be easily maintained and reused across unit tests.

  - Introduced PluginContextProperties: some methods to retrieve properties related to plugin context that were previously accessible from the XrmFakedContext class (GetFakedTracingService, GetServiceEndpointNotificationService, etc), have been refactored and moved into a new PluginContextProperties class / interface, available in the Fake4Dataverse.Plugins package (fake-xrm-easy-plugins repo)
  
  - New Middleware! Previously one could introduce any custom messages that would react to specific requests. This implementation was 1 to 1, meaning there could up to one fake message executor to react to one specific request... 
  
  We have now rewrittten the core of FakXrmEasy to introduce a new fully configurable middleware, inspired on aspnet core, that will make each request to be executed through a confiurable pipeline, effectively allowing multiple interactions / behaviors per request, plus the ability to define the order of execution of those yourself, without having to "touch" or mantain the "core" of the framework. This will allow for much more flexibility and much less maintenance involved.

   - The middleware will allow you to extend the framework in a more flexible and easier way, while also
   giving you the ability to customize the pipeline execution order, or to extend it to cater for your own needs.

   - ProxyTypesAssembly is deprecated => use EnableProxyTypes() which allows multiple assemblies and ProxyTypesAssemblies.Count() > 0 to check if using early bound assemblies

   - FiscalYearSettings and TimeZoneInfo settings have been moved to .Abstractions and will use the new GetProperty&lt;T&gt; / SetProperty&lt;T&gt; methods

   - The enumeration in XrmFakeRelationship has been renamed to FakeRelationshipType, to meet Sonar quality rules

   - IFakeMessageExecutor interface has been moved to Fake4Dataverse.Abstractions.FakeMessageExecutors and its Execute method must receive an IXrmFakedContext as opposed to a XrmFakedContext class

   - CallerId => Moved to .CallerProperties.CallerId

   - New IStatusAttributeMetadataRepository and IOptionSetMetadataRepository

  - ValidateReferences public property has been moved to the middleware initialisation, defaulting to false while also adding the option to initialise it via .AddCrud(IIntegrityOptions)

  - DateBehaviour has been removed since it belongs to Metadata, and so it will use now DateTimeBehaviors based on the injected EntityMetadata
