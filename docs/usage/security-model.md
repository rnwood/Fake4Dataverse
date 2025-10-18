# Dataverse Security Model

**Status:** ✅ Complete  
**Issue:** [#114](https://github.com/rnwood/Fake4Dataverse/issues/114)

Fake4Dataverse implements a comprehensive Dataverse security model that accurately replicates Microsoft Dataverse's security behavior. This enables realistic security testing for plugins, workflows, and applications.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Core Concepts](#core-concepts)
- [Security Entities](#security-entities)
- [Privilege-Based Enforcement](#privilege-based-enforcement)
- [Role Lifecycle Management](#role-lifecycle-management)
- [Business Unit Security](#business-unit-security)
- [System Administrator Role](#system-administrator-role)
- [Organization-Owned Entities](#organization-owned-entities)
- [System Tables](#system-tables)
- [Examples](#examples)
- [Testing Security](#testing-security)
- [References](#references)

## Overview

The Dataverse security model in Fake4Dataverse provides:

- **Privilege-based access control** - Users need specific privileges granted through roles
- **Privilege depth enforcement** - Basic, Local, Deep, and Global access levels
- **Role shadow copies** - Automatic role copying across business units
- **Business unit hierarchy** - Traditional and modern BU security modes
- **System Administrator role** - Automatic initialization with implicit privileges
- **Organization-owned entity support** - Proper handling of system tables
- **Record-level security** - Ownership and sharing validation
- **Field-level security** - Attribute-level access control

**Security is disabled by default** to ensure backward compatibility with existing tests.

## Quick Start

### Enable Basic Security

```csharp
using Fake4Dataverse;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Security.Middleware;
using Microsoft.Xrm.Sdk;

// Build context with security middleware
var builder = MiddlewareBuilder.New()
    .AddRoleLifecycle()  // Add role lifecycle management
    .AddSecurity()       // Add security enforcement
    .AddCrud();
    
var context = builder.Build();

// Enable security
context.SecurityConfiguration.SecurityEnabled = true;

// Set the calling user
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

// Operations are now checked against user's privileges
var service = context.GetOrganizationService();
```

### Enable Complete Security

```csharp
var context = new XrmFakedContext();

// Use pre-configured full security settings
context.SecurityConfiguration.SecurityEnabled = true;
context.SecurityConfiguration.EnforcePrivilegeDepth = true;
context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
context.SecurityConfiguration.EnforceFieldLevelSecurity = true;
```

## Configuration

Security behavior is controlled through the `ISecurityConfiguration` interface accessible via `context.SecurityConfiguration`.

### Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `SecurityEnabled` | `false` | Master toggle for security enforcement |
| `UseModernBusinessUnits` | `false` | Enable matrix-based BU security (allows cross-BU role assignments) |
| `AutoGrantSystemAdministratorPrivileges` | `true` | System Administrator role grants all privileges implicitly |
| `EnforcePrivilegeDepth` | `false` | Enforce Basic/Local/Deep/Global privilege depths |
| `EnforceRecordLevelSecurity` | `false` | Enforce ownership and sharing rules |
| `EnforceFieldLevelSecurity` | `false` | Enforce column-level security |

### Configuration Examples

```csharp
// Default configuration (security disabled)
var context = new XrmFakedContext();
Assert.False(context.SecurityConfiguration.SecurityEnabled);

// Enable security with all features
context.SecurityConfiguration.SecurityEnabled = true;
context.SecurityConfiguration.EnforcePrivilegeDepth = true;
context.SecurityConfiguration.EnforceRecordLevelSecurity = true;
context.SecurityConfiguration.EnforceFieldLevelSecurity = true;

// Modern business units (matrix security)
context.SecurityConfiguration.UseModernBusinessUnits = true;

// Factory methods
var basicConfig = SecurityConfiguration.CreateBasicSecurity();
var fullConfig = SecurityConfiguration.CreateFullySecured();
```

## Core Concepts

### Security Model Architecture

```
User/Team
    └── Role Assignment (via systemuserroles or teamroles)
        └── Role (with shadow copies per BU)
            └── Role Privileges (via roleprivileges)
                └── Privilege (prvCreateAccount, prvReadAccount, etc.)
                    └── Privilege Depth (Basic=1, Local=2, Deep=4, Global=8)
```

### Key Components

1. **SecurityManager** - Main interface for security operations
2. **PrivilegeManager** - Manages privilege creation and checking
3. **RoleLifecycleManager** - Handles role shadow copies and BU lifecycle
4. **SecurityMiddleware** - Integrates security checks into the middleware pipeline
5. **RoleLifecycleMiddleware** - Manages role/BU lifecycle events

## Security Entities

Fake4Dataverse automatically loads metadata for all security entities:

| Entity | Logical Name | Purpose |
|--------|--------------|---------|
| **User** | `systemuser` | Represents users in the system |
| **Business Unit** | `businessunit` | Organizational hierarchy |
| **Team** | `team` | Groups of users for collaboration |
| **Role** | `role` | Collection of privileges |
| **Privilege** | `privilege` | Individual permission (prvCreateAccount, etc.) |
| **Role Privileges** | `roleprivileges` | Junction between roles and privileges |
| **Organization** | `organization` | Top-level organization |
| **Principal Object Access** | `principalobjectaccess` | Record-level sharing |

### Accessing Root Entities

Root entities are automatically created with variable IDs (not constants) to avoid hardcoded GUID bugs:

```csharp
var context = new XrmFakedContext();

// Access root entity IDs easily
var rootOrgId = context.SecurityManager.RootOrganizationId;
var rootBUId = context.SecurityManager.RootBusinessUnitId;
var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;

// Retrieve entities
var rootOrg = context.GetEntityById("organization", rootOrgId);
var rootBU = context.GetEntityById("businessunit", rootBUId);
var sysAdminRole = context.GetEntityById("role", sysAdminRoleId);

// Different context = different IDs
var context2 = new XrmFakedContext();
Assert.NotEqual(context.SecurityManager.RootBusinessUnitId, 
               context2.SecurityManager.RootBusinessUnitId);
```

## Privilege-Based Enforcement

### Auto-Created Privileges

Privileges are automatically created when entity metadata is loaded (when security is enabled):

**For User-Owned Entities (8 privileges):**
- `prvCreate{EntityName}` - Create privilege
- `prvRead{EntityName}` - Read privilege
- `prvWrite{EntityName}` - Update privilege
- `prvDelete{EntityName}` - Delete privilege
- `prvAppend{EntityName}` - Append privilege
- `prvAppendTo{EntityName}` - AppendTo privilege
- `prvAssign{EntityName}` - Assign privilege
- `prvShare{EntityName}` - Share privilege

**For Organization-Owned Entities (4 privileges only):**
- `prvCreate{EntityName}` - Create privilege
- `prvRead{EntityName}` - Read privilege
- `prvWrite{EntityName}` - Update privilege
- `prvDelete{EntityName}` - Delete privilege

### Privilege Depth

Dataverse uses privilege depth to control access scope:

| Depth | Value | Description | Scope |
|-------|-------|-------------|-------|
| **Basic** | 1 | User's own records | Records owned by the user |
| **Local** | 2 | Business unit | Records owned by users in the same BU |
| **Deep** | 4 | Business unit and child BUs | Records in BU hierarchy |
| **Global** | 8 | Organization-wide | All records in the organization |

### Granting Privileges to Roles

```csharp
var context = new XrmFakedContext();
context.SecurityConfiguration.SecurityEnabled = true;

// Load account metadata (creates privileges)
context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "Account" }).Wait();

// Find the privilege
var prvCreateAccount = context.CreateQuery("privilege")
    .First(p => p.GetAttributeValue<string>("name") == "prvCreateAccount");

// Grant privilege to role
var rolePrivilege = new Entity("roleprivileges")
{
    Id = Guid.NewGuid(),
    ["roleid"] = new EntityReference("role", salesRoleId),
    ["privilegeid"] = new EntityReference("privilege", prvCreateAccount.Id),
    ["privilegedepthmask"] = 2  // Local depth (business unit level)
};
context.Initialize(rolePrivilege);
```

### Checking Privileges

The `PrivilegeManager` checks privileges based on user roles:

```csharp
var privilegeManager = context.SecurityManager.PrivilegeManager;

// Check if user has privilege
bool hasPrivilege = privilegeManager.HasPrivilege(userId, "prvCreateAccount", 2);

// System Administrators automatically have all privileges
bool isAdmin = context.SecurityManager.IsSystemAdministrator(userId);
// If true, HasPrivilege always returns true
```

## Role Lifecycle Management

Fake4Dataverse implements Dataverse's role shadow copy mechanism, where roles are automatically replicated across business units.

### Role Shadow Copies

When a role is created:
1. It becomes a "root" role (`parentrootroleid` points to itself)
2. Shadow copies are automatically created for all other business units
3. Shadow copies have `parentroleid` pointing to the root role
4. All role privileges are copied to shadow copies

```csharp
var builder = MiddlewareBuilder.New()
    .AddRoleLifecycle()  // Required for role lifecycle
    .AddCrud();
    
var context = builder.Build();
var service = context.GetOrganizationService();

// Create a role in BU1
var roleId = Guid.NewGuid();
var role = new Entity("role")
{
    Id = roleId,
    ["name"] = "Sales Manager",
    ["businessunitid"] = new EntityReference("businessunit", bu1Id)
};
service.Create(role);

// Shadow copies are automatically created for all other BUs
var allRoles = context.CreateQuery("role")
    .Where(r => r.GetAttributeValue<string>("name") == "Sales Manager")
    .ToList();

// Root role
var rootRole = allRoles.First(r => r.Id == roleId);
Assert.Equal(roleId, rootRole.GetAttributeValue<EntityReference>("parentrootroleid").Id);

// Shadow role
var shadowRole = allRoles.First(r => r.Id != roleId);
Assert.Equal(roleId, shadowRole.GetAttributeValue<EntityReference>("parentroleid").Id);
```

### Business Unit Lifecycle

When a business unit is created, shadow copies of all root roles are automatically created for it:

```csharp
// Create a new business unit
var bu2Id = Guid.NewGuid();
var bu2 = new Entity("businessunit")
{
    Id = bu2Id,
    ["name"] = "West Region",
    ["parentbusinessunitid"] = new EntityReference("businessunit", rootBUId)
};
service.Create(bu2);

// Shadow copies of all existing roles are created for bu2
var rolesInBU2 = context.CreateQuery("role")
    .Where(r => r.GetAttributeValue<EntityReference>("businessunitid").Id == bu2Id)
    .ToList();

// Each role has parentroleid pointing to its root
```

### Deleting Roles

- **Root roles** can be deleted, which automatically deletes all shadow copies
- **Shadow roles** cannot be deleted directly (throws `InvalidOperationException`)

```csharp
// Delete root role - all shadows deleted automatically
service.Delete("role", rootRoleId);

// Cannot delete shadow role directly
service.Delete("role", shadowRoleId); // Throws InvalidOperationException
```

## Business Unit Security

### Traditional Business Unit Security (Default)

In traditional mode (`UseModernBusinessUnits = false`):

- Roles can only be assigned to users/teams in the **same business unit**
- When a user/team changes business units, **all role assignments are removed**
- Users must be re-assigned roles from their new BU

```csharp
context.SecurityConfiguration.UseModernBusinessUnits = false; // Default

// User in BU1 can only be assigned roles from BU1
service.Associate("systemuser", userId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", roleInBU1) });

// This throws InvalidOperationException (role from different BU)
service.Associate("systemuser", userId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", roleInBU2) });
```

### Modern Business Unit Security (Matrix Security)

In modern mode (`UseModernBusinessUnits = true`):

- Roles can be assigned **across business units**
- Users can have different access levels in different BUs
- More flexible security model

```csharp
context.SecurityConfiguration.UseModernBusinessUnits = true;

// User in BU1 can be assigned roles from any BU
service.Associate("systemuser", userId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", roleInBU2) });
// Works!
```

### Business Unit Changes

When a user or team's business unit is updated, all role assignments are automatically removed:

```csharp
// User has roles assigned
var user = context.GetEntityById("systemuser", userId);
var roles = context.SecurityManager.GetUserRoles(userId);
Assert.NotEmpty(roles);

// Update user's business unit
user["businessunitid"] = new EntityReference("businessunit", newBUId);
service.Update(user);

// All role assignments removed
roles = context.SecurityManager.GetUserRoles(userId);
Assert.Empty(roles);
```

## System Administrator Role

The System Administrator role is automatically created during context initialization.

### Key Characteristics

1. **Variable ID** - ID varies per context instance (accessible via `SecurityManager.SystemAdministratorRoleId`)
2. **Implicit Privileges** - Has all privileges without database entries (not materialized)
3. **Bypasses Security Checks** - System Administrators always have access
4. **Auto-Initialization** - Created automatically with root organization and business unit

```csharp
var context = new XrmFakedContext();

// System Administrator role is automatically created
var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
var sysAdminRole = context.GetEntityById("role", sysAdminRoleId);
Assert.Equal("System Administrator", sysAdminRole.GetAttributeValue<string>("name"));

// Assign System Administrator role to user
service.Associate("systemuser", userId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", sysAdminRoleId) });

// User now has all privileges implicitly
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
service.Create(new Entity("account") { ["name"] = "Test" }); // Always allowed
```

### Disable Auto-Initialization

```csharp
var context = new XrmFakedContext();
context.SecurityConfiguration.AutoGrantSystemAdministratorPrivileges = false;
// System Administrator role still exists but doesn't grant implicit privileges
```

## Organization-Owned Entities

Organization-owned entities (system tables) have different security characteristics:

### Characteristics

1. **No Owner** - Do not have `ownerid` attribute
2. **Global Scope Only** - Only support Organization (Global) privilege depth
3. **Limited Privileges** - Only Create, Read, Write, Delete (no Assign/Share/Append/AppendTo)
4. **Examples** - systemuser, businessunit, role, privilege, organization, solution, publisher

```csharp
// Organization-owned entity privileges
var privileges = context.CreateQuery("privilege")
    .Where(p => p.GetAttributeValue<string>("name").Contains("Systemuser"))
    .ToList();

// Only 4 privileges created
Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvCreateSystemuser");
Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvReadSystemuser");
Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvWriteSystemuser");
Assert.Contains(privileges, p => p.GetAttributeValue<string>("name") == "prvDeleteSystemuser");

// These do NOT exist for org-owned entities
Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvAssignSystemuser");
Assert.DoesNotContain(privileges, p => p.GetAttributeValue<string>("name") == "prvShareSystemuser");

// Privilege scope settings
foreach (var privilege in privileges)
{
    Assert.False(privilege.GetAttributeValue<bool>("canbebasic"));
    Assert.False(privilege.GetAttributeValue<bool>("canbelocal"));
    Assert.False(privilege.GetAttributeValue<bool>("canbedeep"));
    Assert.True(privilege.GetAttributeValue<bool>("canbeglobal"));
}
```

## System Tables

System tables are readable by everyone, regardless of privileges:

```csharp
var context = new XrmFakedContext();
context.SecurityConfiguration.SecurityEnabled = true;

// Regular user without special privileges
var userId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

var service = context.GetOrganizationService();

// Can read system tables without privileges
var roles = service.RetrieveMultiple(new QueryExpression("role"));
var users = service.RetrieveMultiple(new QueryExpression("systemuser"));
var businessUnits = service.RetrieveMultiple(new QueryExpression("businessunit"));

// All succeed - system tables are readable by everyone
```

**System Tables Include:**
- organization
- businessunit
- systemuser
- team
- role
- privilege
- roleprivileges
- entitydefinition
- attribute
- solution
- publisher
- webresource
- sitemap
- appmodule

## Examples

### Complete Security Testing Scenario

```csharp
using Fake4Dataverse;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Security.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// 1. Set up security environment
var builder = MiddlewareBuilder.New()
    .AddRoleLifecycle()  // Role lifecycle management
    .AddSecurity()       // Security enforcement
    .AddCrud();
    
var context = builder.Build();
context.SecurityConfiguration.SecurityEnabled = true;
context.SecurityConfiguration.EnforcePrivilegeDepth = true;

var service = context.GetOrganizationService();

// 2. Load account metadata (creates privileges)
context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "Account" }).Wait();

// 3. Create business unit
var buId = context.SecurityManager.RootBusinessUnitId;

// 4. Create user
var userId = Guid.NewGuid();
var user = new Entity("systemuser")
{
    Id = userId,
    ["fullname"] = "Sales Representative",
    ["businessunitid"] = new EntityReference("businessunit", buId)
};
context.Initialize(user);

// 5. Create role
var roleId = Guid.NewGuid();
var role = new Entity("role")
{
    Id = roleId,
    ["name"] = "Sales Rep",
    ["businessunitid"] = new EntityReference("businessunit", buId)
};
context.Initialize(role);

// 6. Grant privileges to role
var prvCreate = context.CreateQuery("privilege")
    .First(p => p.GetAttributeValue<string>("name") == "prvCreateAccount");
var prvRead = context.CreateQuery("privilege")
    .First(p => p.GetAttributeValue<string>("name") == "prvReadAccount");
var prvWrite = context.CreateQuery("privilege")
    .First(p => p.GetAttributeValue<string>("name") == "prvWriteAccount");

context.Initialize(new[]
{
    new Entity("roleprivileges")
    {
        Id = Guid.NewGuid(),
        ["roleid"] = new EntityReference("role", roleId),
        ["privilegeid"] = new EntityReference("privilege", prvCreate.Id),
        ["privilegedepthmask"] = 1  // Basic - own records only
    },
    new Entity("roleprivileges")
    {
        Id = Guid.NewGuid(),
        ["roleid"] = new EntityReference("role", roleId),
        ["privilegeid"] = new EntityReference("privilege", prvRead.Id),
        ["privilegedepthmask"] = 2  // Local - business unit
    },
    new Entity("roleprivileges")
    {
        Id = Guid.NewGuid(),
        ["roleid"] = new EntityReference("role", roleId),
        ["privilegeid"] = new EntityReference("privilege", prvWrite.Id),
        ["privilegedepthmask"] = 1  // Basic - own records only
    }
});

// 7. Assign role to user
service.Associate("systemuser", userId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", roleId) });

// 8. Set caller
context.CallerProperties.CallerId = new EntityReference("systemuser", userId);

// 9. Test operations
var accountId = service.Create(new Entity("account") { ["name"] = "My Account" });
// Success - user has prvCreateAccount with Basic depth

var account = service.Retrieve("account", accountId, new ColumnSet("name"));
// Success - user has prvReadAccount with Local depth

service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated" });
// Success - user owns the record and has prvWriteAccount with Basic depth

// 10. Test access denial
var otherUserId = Guid.NewGuid();
context.CallerProperties.CallerId = new EntityReference("systemuser", otherUserId);

try
{
    service.Update(new Entity("account") { Id = accountId, ["name"] = "Hacked" });
    // Throws UnauthorizedAccessException - other user doesn't own the record
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Access denied: {ex.Message}");
}
```

### Testing Plugin Security

```csharp
using Fake4Dataverse;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

public class AccountCreationPluginTests
{
    [Fact]
    public void Plugin_Should_Respect_User_Privileges()
    {
        // Arrange
        var builder = MiddlewareBuilder.New()
            .AddRoleLifecycle()
            .AddSecurity()
            .AddCrud();
            
        var context = builder.Build();
        context.SecurityConfiguration.SecurityEnabled = true;
        
        // Register plugin
        context.RegisterPluginStep<AccountValidationPlugin>(
            PluginStage.PreValidation,
            "Create",
            "account");
        
        var service = context.GetOrganizationService();
        
        // Create user without privileges
        var userId = Guid.NewGuid();
        context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
        
        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Create(new Entity("account") { ["name"] = "Test" });
        });
    }
}
```

## Testing Security

### Unit Testing Security Rules

```csharp
[Fact]
public void Should_Enforce_Basic_Privilege_Depth()
{
    // Arrange
    var builder = MiddlewareBuilder.New()
        .AddSecurity()
        .AddCrud();
        
    var context = builder.Build();
    context.SecurityConfiguration.SecurityEnabled = true;
    context.SecurityConfiguration.EnforcePrivilegeDepth = true;
    
    var service = context.GetOrganizationService();
    
    // Create user with Basic depth privilege
    // User can only access their own records
    var userId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // Act - create account (user owns it)
    var accountId = service.Create(new Entity("account") { ["name"] = "My Account" });
    
    // Can read own account
    var account = service.Retrieve("account", accountId, new ColumnSet("name"));
    Assert.NotNull(account);
    
    // Cannot read other user's account
    var otherUserId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", otherUserId);
    
    Assert.Throws<UnauthorizedAccessException>(() =>
    {
        service.Retrieve("account", accountId, new ColumnSet("name"));
    });
}
```

### Integration Testing with Security

```csharp
[Fact]
public void Should_Allow_System_Administrator_Full_Access()
{
    // Arrange
    var context = new XrmFakedContext();
    context.SecurityConfiguration.SecurityEnabled = true;
    
    var service = context.GetOrganizationService();
    
    // Assign System Administrator role
    var userId = Guid.NewGuid();
    var sysAdminRoleId = context.SecurityManager.SystemAdministratorRoleId;
    
    service.Associate("systemuser", userId,
        new Relationship("systemuserroles_association"),
        new EntityReferenceCollection { new EntityReference("role", sysAdminRoleId) });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // Act - perform any operation without explicit privileges
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    var account = service.Retrieve("account", accountId, new ColumnSet("name"));
    service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated" });
    service.Delete("account", accountId);
    
    // Assert - all operations succeed
    Assert.NotEqual(Guid.Empty, accountId);
}
```

## References

### Microsoft Documentation

- [Dataverse Security Model](https://learn.microsoft.com/en-us/power-platform/admin/wp-security)
- [Security Roles and Privileges](https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges)
- [Business Units](https://learn.microsoft.com/en-us/power-platform/admin/wp-security-cds)
- [System Administrator Role](https://learn.microsoft.com/en-us/power-platform/admin/database-security#system-administrator-role)
- [Organization-Owned Entities](https://learn.microsoft.com/en-us/power-platform/admin/wp-security#organization-owned-entities)
- [System Tables](https://learn.microsoft.com/en-us/power-platform/admin/wp-security#system-tables)
- [Create and Edit Business Units](https://learn.microsoft.com/en-us/power-platform/admin/create-edit-business-units)
- [Modern Business Units](https://learn.microsoft.com/en-us/power-platform/admin/wp-security-cds)
- [Record Sharing and Access](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/security-sharing-assigning)
- [Field-Level Security](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/field-security-entities)

### Entity References

- [Role Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/role?view=dataverse-latest)
- [BusinessUnit Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/businessunit?view=dataverse-latest)
- [RolePrivileges Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/roleprivileges?view=dataverse-latest)

### See Also

- [Testing Plugins](./testing-plugins.md)
- [CRUD Operations](./crud-operations.md)
- [Querying Data](./querying-data.md)
- [Security Permissions](./security-permissions.md)
