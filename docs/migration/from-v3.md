# Migrating from FakeXrmEasy v3.x

> **📝 Note**: This documentation is currently under development. For detailed migration information, see the [Fake4Dataverse package README](../../Fake4Dataverse/README.md#migration-guide).

## Important Note

Fake4Dataverse is based on FakeXrmEasy v2.0.1 (the last MIT-licensed version). FakeXrmEasy v3.x is a commercial product that continued development after the license change.

If you're migrating from v3.x to Fake4Dataverse v4.x, you are moving from a more recent commercial version back to an earlier open-source version. Some v3.x features may not be available.

## Migration Path

1. **Assess Compatibility** - Determine if you're using v3-specific features
2. **Update Package References** - Switch to Fake4Dataverse packages
3. **Update Context Initialization** - Use v4.x patterns
4. **Apply v1 → v4 Migration** - Follow v1 migration patterns
5. **Handle Missing Features** - Find workarounds or alternatives

## Quick Package Update

```xml
<!-- Remove v3.x packages -->
<!-- <PackageReference Include="FakeXrmEasy.9" Version="3.x.x" /> -->

<!-- Add v4.x packages -->
<PackageReference Include="Fake4Dataverse.9" Version="4.0.0" />
<PackageReference Include="Fake4Dataverse.Plugins" Version="4.0.0" />
```

## Key Differences

Since v3.x was developed after v2.0.1, it likely includes features and improvements not present in Fake4Dataverse v4.x. This migration is about moving from commercial software to an open-source alternative.

### What to Check

1. Review your test code for v3-specific APIs
2. Check the v3.x changelog for features you're using
3. Make a list of dependencies on v3-specific functionality
4. Consider alternatives for v3-specific features in v4.x

## Complete Migration Guide

For comprehensive migration instructions from v3.x, see the [Fake4Dataverse README](../../Fake4Dataverse/README.md#migrating-from-fakexrmeasy-v3x-to-fake4dataverse-v4x).

## Common Migration Steps

### Context Creation

```csharp
// v3.x and v4.x both use factory
var context = XrmFakedContextFactory.New();
```

### Use Interface Types

```csharp
// ✅ Good - use interface
IXrmFakedContext context = XrmFakedContextFactory.New();

// ❌ Avoid - concrete type
XrmFakedContext context = new XrmFakedContext();
```

### CallerProperties

```csharp
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
```

## Missing v3 Features?

If you encounter v3-specific features that don't exist in v4.x:

1. Check if there's a workaround using v4.x APIs
2. Simplify your tests if the feature was for convenience
3. Contribute back by opening an issue or pull request
4. Consider creating extension methods for missing functionality

## Need Help?

- See [Fake4Dataverse v3 Migration Guide](../../Fake4Dataverse/README.md#migrating-from-fakexrmeasy-v3x-to-fake4dataverse-v4x)
- Check [FAQ](../getting-started/faq.md)
- Open an issue on [GitHub](https://github.com/rnwood/Fake4Dataverse/issues)
