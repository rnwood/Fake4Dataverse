# Migrating from FakeXrmEasy v3.x

This guide helps you migrate from FakeXrmEasy v3.x (commercial) to Fake4Dataverse v4.x (open-source MIT).

## Table of Contents

- [Important Note](#important-note)
- [Understanding the Versions](#understanding-the-versions)
- [Migration Assessment](#migration-assessment)
- [Migration Path](#migration-path)
- [Key Differences](#key-differences)
- [Step-by-Step Migration](#step-by-step-migration)
- [Feature Availability](#feature-availability)
- [Alternatives and Workarounds](#alternatives-and-workarounds)
- [Getting Help](#getting-help)

## Important Note

Fake4Dataverse v4.x is based on FakeXrmEasy v2.0.1 (the last MIT-licensed version). FakeXrmEasy v3.x is a commercial product that continued development after the license change.

**This means:**
- v3.x has features not in v4.x (it's newer commercial software)
- v4.x is open-source and community-driven
- Some v3.x features may not be available in v4.x

## Understanding the Versions

### Version Timeline

```
FakeXrmEasy v1.x (MIT) → v2.0.1 (MIT) → v3.x (Commercial)
                                     ↓
                              Fake4Dataverse v4.x (MIT, forked from 2.0.1)
```

### License Changes

- **v1.x and v2.0.1**: MIT licensed (open-source)
- **v3.x**: Commercial license (paid)
- **Fake4Dataverse v4.x**: MIT licensed (open-source, based on v2.0.1)

## Migration Assessment

Before migrating, assess your usage:

### Questions to Answer

1. **Which v3.x features do you use?**
   - List all v3-specific APIs you depend on
   - Check if they're available in v4.x (see [Feature Availability](#feature-availability))

2. **Why are you migrating?**
   - Cost considerations?
   - Open-source preference?
   - Community involvement?

3. **Can you accept feature gaps?**
   - Are missing features critical?
   - Can you implement workarounds?

4. **What's your timeline?**
   - Urgent migration?
   - Gradual transition?

## Migration Path

### Option 1: Direct Migration (If Compatible)

If you're not using v3-specific features:

1. Update packages to Fake4Dataverse
2. Apply v1 → v4 migration steps
3. Test thoroughly
4. Deploy

### Option 2: Gradual Migration

If you have v3-specific dependencies:

1. Identify v3-specific code
2. Create abstraction layer
3. Implement alternatives in Fake4Dataverse
4. Migrate module by module
5. Remove v3 dependencies

### Option 3: Custom Implementation

For critical missing features:

1. Implement custom message executors
2. Extend Fake4Dataverse
3. Contribute back to community
4. Maintain your extensions

## Key Differences

### Package Names

**v3.x:**
```xml
<PackageReference Include="FakeXrmEasy.9" Version="3.x.x" />
```

**v4.x:**
```xml
<PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
<PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
```

### Namespaces

**v3.x:**
```csharp
using FakeXrmEasy;
using FakeXrmEasy.Abstractions;
```

**v4.x:**
```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions;
```

### Context Creation

Both v3.x and v4.x use factory methods, but with different namespaces:

**v3.x:**
```csharp
var context = XrmFakedContextFactory.New();
```

**v4.x:**
```csharp
var context = XrmFakedContextFactory.New(); // Same API, different package
```

### System Entity Metadata (Key Difference)

**🆕 Unique to Fake4Dataverse v4.x**: System entity metadata is embedded in Core.

**v3.x:**
- System entity metadata must be loaded externally
- Requires CDM files or early-bound assemblies for solution, appmodule, etc.

**v4.x:**
```csharp
// System entities (solution, appmodule, sitemap, etc.) are embedded in Core
var context = XrmFakedContextFactory.New();
context.InitializeSystemEntityMetadata();

// Now you can work with system entities with validation enabled
var solution = new Entity("solution")
{
    ["uniquename"] = "MySolution",
    ["friendlyname"] = "My Solution"
};
service.Create(solution);
```

**Available System Entities in v4.x:**
- solution
- appmodule
- sitemap
- savedquery
- systemform
- webresource
- appmodulecomponent

This feature enables Model-Driven App testing and ALM scenarios without external dependencies.

## Step-by-Step Migration

### Step 1: Backup Your Code

```bash
git checkout -b migrate-to-fake4dataverse
git commit -am "Checkpoint before migration"
```

### Step 2: Update Packages

Remove v3.x packages:
```bash
dotnet remove package FakeXrmEasy.9
```

Add v4.x packages:
```bash
dotnet add package Fake4Dataverse.9
dotnet add package Fake4Dataverse.Plugins
```

### Step 3: Update Usings

Use find-and-replace in your IDE:
- Find: `using FakeXrmEasy;`
- Replace: `using Fake4Dataverse.Middleware;`

Also add:
```csharp
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Plugins;
```

### Step 4: Apply v1 Migration Patterns

Follow the [v1 migration guide](./from-v1.md) for common patterns:
- CallerProperties updates
- Plugin execution syntax
- EnableProxyTypes()

### Step 5: Test Everything

```bash
dotnet test
```

Review and fix any failures.

### Step 6: Check for v3-Specific Code

Search your codebase for v3-specific features (see [Feature Availability](#feature-availability)).

## Feature Availability

### Core Features (Available in v4.x)

✅ Context creation and initialization
✅ CRUD operations (Create, Retrieve, Update, Delete)
✅ LINQ queries
✅ FetchXML queries
✅ Plugin execution
✅ Associate/Disassociate
✅ ExecuteMultiple
✅ Basic message executors
✅ Early-bound entities
✅ Caller properties
✅ **Calculated Fields** (NEW in v4.0.0) - See [Calculated Fields Guide](../usage/calculated-fields.md)
✅ **Metadata Validation** (NEW in v4.0.0) - IsValidForCreate/Update/Read enforcement - See [Metadata Validation Guide](../usage/metadata-validation.md)

### Features That May Differ

⚠️ Some advanced message executors may not be implemented
⚠️ Metadata operations have limited support (but validation is enhanced in v4.0.0)
⚠️ Some v3-specific APIs may not exist
⚠️ **Metadata validation is stricter** in v4.0.0 than in v3.x (can be disabled for compatibility)

### If a Feature is Missing

1. **Check the documentation**: It might be implemented differently
2. **Implement a custom executor**: See [Custom Executors](../api/custom-executors.md)
3. **Open an issue**: Request the feature
4. **Contribute**: Implement and contribute it back

## Alternatives and Workarounds

### Missing Message Executor

If a message executor is missing:

```csharp
// Create a custom executor
public class MyMissingMessageExecutor : IFakeMessageExecutor
{
    public bool CanExecute(OrganizationRequest request)
    {
        return request.RequestName == "missing_Message";
    }
    
    public OrganizationResponse Execute(
        OrganizationRequest request, 
        IXrmFakedContext ctx)
    {
        // Implement the behavior
        return new OrganizationResponse();
    }
    
    public Type GetResponsibleRequestType()
    {
        return typeof(OrganizationRequest);
    }
}

// Register it
var context = MiddlewareBuilder
    .New()
    .AddCrud()
    .AddFakeMessageExecutors()
    .Use(next => (ctx, request) =>
    {
        var executor = new MyMissingMessageExecutor();
        if (executor.CanExecute(request))
        {
            return executor.Execute(request, ctx);
        }
        return next(ctx, request);
    })
    .UseCrud()
    .UseMessages()
    .Build();
```

### Limited Metadata Support

Work around limited metadata:

```csharp
// Instead of relying on metadata retrieval
// Use initialized entities with the metadata you need
var entityMetadata = new Entity("metadata");
entityMetadata["entitylogicalname"] = "account";
entityMetadata["primarynamefield"] = "name";

context.Initialize(entityMetadata);
```

### Calculated Fields (NEW in v4.0.0)

**Fake4Dataverse v4.0.0+** now supports calculated fields simulation:

```csharp
using Fake4Dataverse.CalculatedFields;

// Register calculated field
var evaluator = context.CalculatedFieldEvaluator;
var definition = new CalculatedFieldDefinition
{
    EntityLogicalName = "product",
    AttributeLogicalName = "totalprice",
    Formula = "[quantity] * [unitprice]",
    ResultType = typeof(decimal)
};
evaluator.RegisterCalculatedField(definition);

// Calculated fields are automatically evaluated on retrieve/update
var product = service.Retrieve("product", productId, new ColumnSet(true));
// totalprice is automatically calculated
```

**Key Differences from FakeXrmEasy v3.x:**
- **Registration**: v4.x uses explicit `RegisterCalculatedField()` calls
- **Formula Syntax**: Same as Dataverse (uses `[fieldname]` syntax)
- **Supported Functions**: All Microsoft-documented functions (CONCAT, DIFFINDAYS, ADDDAYS, TRIMLEFT, TRIMRIGHT, etc.)
- **Evaluation**: Automatic on retrieve and update operations

See the [Calculated Fields Guide](../usage/calculated-fields.md) for complete documentation.

### Metadata Validation (NEW in v4.0.0)

**Fake4Dataverse v4.0.0+** enforces attribute metadata validation using IsValidForCreate, IsValidForUpdate, and IsValidForRead properties:

```csharp
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;

// Validation is enabled by default
var context = XrmFakedContextFactory.New();
context.InitializeMetadata(typeof(Account).Assembly);
var service = context.GetOrganizationService();

// ❌ FAILS - statecode is not valid for Create
var account = new Entity("account")
{
    ["name"] = "Test",
    ["statecode"] = new OptionSetValue(1)
};
service.Create(account); // Throws FaultException

// ✅ WORKS - Disable validation for backward compatibility
var compatContext = XrmFakedContextFactory.New(new IntegrityOptions
{
    ValidateAttributeTypes = false
});
service.Create(account); // Success
```

**Key Differences from FakeXrmEasy v3.x:**
- **Validation Enforcement**: v4.0.0 enforces IsValidForCreate/Update by default (v3.x behavior may vary)
- **Metadata Requirement**: Validation requires metadata to be initialized
- **statecode/statuscode**: Cannot be set during Create when validation is enabled
- **Backward Compatibility**: Can be disabled via `IntegrityOptions.ValidateAttributeTypes = false`

See the [Metadata Validation Guide](../usage/metadata-validation.md) for complete documentation.

## Migration Checklist

- [ ] Back up your code (Git branch/tag)
- [ ] Document v3-specific features you use
- [ ] Update NuGet packages
- [ ] Update using statements
- [ ] Apply v1 migration patterns
- [ ] Run all tests
- [ ] Fix test failures
- [ ] Check for v3-specific code
- [ ] Implement workarounds for missing features
- [ ] Document any custom implementations
- [ ] Update team documentation
- [ ] Deploy to test environment
- [ ] Validate in test environment
- [ ] Deploy to production

## Getting Help

### Resources

- [Quick Start Guide](../getting-started/quickstart.md) - Learn v4.x quickly
- [Testing Plugins](../usage/testing-plugins.md) - Plugin testing in v4.x
- [Custom Executors](../api/custom-executors.md) - Implement missing features
- [FAQ](../getting-started/faq.md) - Common questions
- [Migration from v1](./from-v1.md) - Similar migration patterns

### Support

- **GitHub Issues**: [Report issues or ask questions](https://github.com/rnwood/Fake4Dataverse/issues)
- **Discussions**: Check existing discussions
- **Contributing**: Help improve the project

### Common Questions

**Q: Will v4.x have feature parity with v3.x?**
A: Not necessarily. v3.x continued development after v2.0.1. v4.x focuses on maintaining the MIT-licensed codebase and adding community-requested features.

**Q: Can I use both v3.x and v4.x?**
A: Not in the same project (package conflicts), but you could have different projects use different versions.

**Q: Should I migrate?**
A: Consider:
- Do you need v3-specific features?
- Is open-source important to you?
- Can you contribute missing features?
- What are the cost implications?

**Q: What if a critical feature is missing?**
A: You have options:
- Implement a custom executor
- Request the feature
- Contribute it to the project
- Stay on v3.x until it's available

## Contributing

If you implement missing features during migration, consider contributing them back:

1. Fork Fake4Dataverse
2. Implement the feature
3. Add tests and documentation
4. Submit a pull request

This helps the entire community!

## See Also

- [Migration from v1.x](./from-v1.md) - Similar patterns apply
- [Quick Start](../getting-started/quickstart.md) - Get started with v4.x
- [API Reference](../api/ixrm-faked-context.md) - Complete API documentation
- [Custom Executors](../api/custom-executors.md) - Extend the framework
