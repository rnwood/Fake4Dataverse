# Security and Permissions

Testing security and permissions is essential for ensuring your Dataverse applications properly control access to data. This guide covers testing security roles, record-level security, sharing, and permission checks.

## Table of Contents

- [Overview](#overview)
- [Setting the Calling User](#setting-the-calling-user)
- [Testing Security Roles](#testing-security-roles)
- [Record-Level Security](#record-level-security)
- [Team Ownership](#team-ownership)
- [Sharing and Access Control](#sharing-and-access-control)
- [Testing Permission Checks](#testing-permission-checks)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [See Also](#see-also)

## Overview

Dataverse security operates on multiple levels:
- **User-level**: Who is performing the operation
- **Role-level**: What privileges the user has
- **Record-level**: Access to specific records through ownership or sharing
- **Field-level**: Access to specific fields (column-level security)

**Reference:** [Dataverse Security Model](https://learn.microsoft.com/en-us/power-platform/admin/wp-security) - Microsoft documentation on security concepts including security roles, business units, and access control in Dataverse.

## Setting the Calling User

The calling user determines who is executing operations:

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Execute_As_Specific_User()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create a user
    var userId = Guid.NewGuid();
    var user = new Entity("systemuser")
    {
        Id = userId,
        ["fullname"] = "Test User",
        ["domainname"] = "DOMAIN\\testuser"
    };
    
    context.Initialize(user);
    
    // Set the calling user
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // Act - Operations execute as this user
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Assert
    var account = service.Retrieve("account", accountId, new ColumnSet("ownerid"));
    Assert.Equal(userId, account.GetAttributeValue<EntityReference>("ownerid").Id);
}
```

## Testing Security Roles

### Testing with Different User Contexts

```csharp
[Fact]
public void Should_Allow_Manager_To_Access_All_Records()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var managerId = Guid.NewGuid();
    var salesRepId = Guid.NewGuid();
    
    var manager = new Entity("systemuser")
    {
        Id = managerId,
        ["fullname"] = "Sales Manager"
    };
    
    var salesRep = new Entity("systemuser")
    {
        Id = salesRepId,
        ["fullname"] = "Sales Rep"
    };
    
    context.Initialize(new[] { manager, salesRep });
    
    // Sales rep creates an account
    context.CallerProperties.CallerId = new EntityReference("systemuser", salesRepId);
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Sales Rep Account"
    });
    
    // Manager can access the account
    context.CallerProperties.CallerId = new EntityReference("systemuser", managerId);
    var account = service.Retrieve("account", accountId, new ColumnSet("name"));
    Assert.Equal("Sales Rep Account", account["name"]);
}
```

### Testing Role-Based Access

```csharp
public class RoleBasedSecurityTests
{
    [Fact]
    public void Should_Restrict_Access_Based_On_Role()
    {
        var context = XrmFakedContextFactory.New();
        var service = context.GetOrganizationService();
        
        // Create security roles
        var adminRoleId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        
        var adminRole = new Entity("role")
        {
            Id = adminRoleId,
            ["name"] = "System Administrator"
        };
        
        var userRole = new Entity("role")
        {
            Id = userRoleId,
            ["name"] = "Basic User"
        };
        
        // Create users
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var admin = new Entity("systemuser")
        {
            Id = adminId,
            ["fullname"] = "Admin User"
        };
        
        var user = new Entity("systemuser")
        {
            Id = userId,
            ["fullname"] = "Regular User"
        };
        
        context.Initialize(new[] { adminRole, userRole, admin, user });
        
        // Associate users with roles using systemuserroles_association
        service.Associate(
            "systemuser",
            adminId,
            new Relationship("systemuserroles_association"),
            new EntityReferenceCollection { new EntityReference("role", adminRoleId) }
        );
        
        service.Associate(
            "systemuser",
            userId,
            new Relationship("systemuserroles_association"),
            new EntityReferenceCollection { new EntityReference("role", userRoleId) }
        );
        
        // Test that admin can perform privileged operations
        context.CallerProperties.CallerId = new EntityReference("systemuser", adminId);
        var accountId = service.Create(new Entity("account") { ["name"] = "Admin Account" });
        Assert.NotEqual(Guid.Empty, accountId);
    }
}
```

## Record-Level Security

### Testing Record Ownership

```csharp
[Fact]
public void Should_Respect_Record_Ownership()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var owner1Id = Guid.NewGuid();
    var owner2Id = Guid.NewGuid();
    
    var owner1 = new Entity("systemuser") { Id = owner1Id, ["fullname"] = "Owner 1" };
    var owner2 = new Entity("systemuser") { Id = owner2Id, ["fullname"] = "Owner 2" };
    
    context.Initialize(new[] { owner1, owner2 });
    
    // Owner 1 creates an account
    context.CallerProperties.CallerId = new EntityReference("systemuser", owner1Id);
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Owner 1 Account"
    });
    
    // Verify ownership
    var account = service.Retrieve("account", accountId, new ColumnSet("ownerid"));
    var ownerId = account.GetAttributeValue<EntityReference>("ownerid").Id;
    Assert.Equal(owner1Id, ownerId);
    
    // Owner 2 cannot modify without access (in real scenario)
    // In Fake4Dataverse, you'd need to implement custom security checks
}
```

### Testing Assign Operation

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Assign_Record_To_Different_Owner()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var owner1Id = Guid.NewGuid();
    var owner2Id = Guid.NewGuid();
    
    var owner1 = new Entity("systemuser") { Id = owner1Id, ["fullname"] = "Owner 1" };
    var owner2 = new Entity("systemuser") { Id = owner2Id, ["fullname"] = "Owner 2" };
    
    context.Initialize(new[] { owner1, owner2 });
    
    // Create account owned by Owner 1
    context.CallerProperties.CallerId = new EntityReference("systemuser", owner1Id);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
    
    // Assign to Owner 2
    var assignRequest = new AssignRequest
    {
        Assignee = new EntityReference("systemuser", owner2Id),
        Target = new EntityReference("account", accountId)
    };
    
    service.Execute(assignRequest);
    
    // Verify new owner
    var account = service.Retrieve("account", accountId, new ColumnSet("ownerid"));
    Assert.Equal(owner2Id, account.GetAttributeValue<EntityReference>("ownerid").Id);
}
```

## Team Ownership

### Testing Team-Owned Records

```csharp
[Fact]
public void Should_Create_Team_Owned_Record()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var teamId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    var team = new Entity("team")
    {
        Id = teamId,
        ["name"] = "Sales Team"
    };
    
    var user = new Entity("systemuser")
    {
        Id = userId,
        ["fullname"] = "Team Member"
    };
    
    context.Initialize(new[] { team, user });
    
    // User creates account owned by team
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    var accountId = service.Create(new Entity("account")
    {
        ["name"] = "Team Account",
        ["ownerid"] = new EntityReference("team", teamId)
    });
    
    var account = service.Retrieve("account", accountId, new ColumnSet("ownerid"));
    var owner = account.GetAttributeValue<EntityReference>("ownerid");
    
    Assert.Equal("team", owner.LogicalName);
    Assert.Equal(teamId, owner.Id);
}
```

## Sharing and Access Control

### Granting Access to Records

**Reference:** [GrantAccessRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.grantaccessrequest) - Grants access to a record to a security principal (user or team), specifying the access rights (Read, Write, Delete, Append, AppendTo, Assign, Share).

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Grant_Access_To_User()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    var owner = new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" };
    var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User" };
    
    context.Initialize(new[] { owner, user });
    
    // Owner creates account
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Shared Account" });
    
    // Grant read access to another user
    var grantRequest = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
        }
    };
    
    service.Execute(grantRequest);
    
    // User can now access the account
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    var account = service.Retrieve("account", accountId, new ColumnSet("name"));
    Assert.Equal("Shared Account", account["name"]);
}
```

### Revoking Access

**Reference:** [RevokeAccessRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.revokeaccessrequest) - Revokes all access rights to a record for a specified security principal, removing their ability to access the record unless they have access through other means (ownership, role privileges, etc.).

```csharp
[Fact]
public void Should_Revoke_Access_From_User()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    var owner = new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" };
    var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User" };
    
    context.Initialize(new[] { owner, user });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
    
    // Grant access
    var grantRequest = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess
        }
    };
    service.Execute(grantRequest);
    
    // Revoke access
    var revokeRequest = new RevokeAccessRequest
    {
        Target = new EntityReference("account", accountId),
        Revokee = new EntityReference("systemuser", userId)
    };
    service.Execute(revokeRequest);
    
    // User no longer has access (in real scenario with security enabled)
}
```

### Retrieving Access Rights

**Reference:** [RetrievePrincipalAccessRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveprincipalaccessrequest) - Retrieves the access rights a security principal has to a specific record, returning an AccessRights enum value indicating Read, Write, Delete, etc.

```csharp
[Fact]
public void Should_Retrieve_User_Access_Rights()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    var owner = new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" };
    var user = new Entity("systemuser") { Id = userId, ["fullname"] = "User" };
    
    context.Initialize(new[] { owner, user });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test Account" });
    
    // Grant access
    var grantRequest = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
        }
    };
    service.Execute(grantRequest);
    
    // Retrieve access rights
    var retrieveRequest = new RetrievePrincipalAccessRequest
    {
        Target = new EntityReference("account", accountId),
        Principal = new EntityReference("systemuser", userId)
    };
    
    var response = (RetrievePrincipalAccessResponse)service.Execute(retrieveRequest);
    
    Assert.True(response.AccessRights.HasFlag(AccessRights.ReadAccess));
    Assert.True(response.AccessRights.HasFlag(AccessRights.WriteAccess));
}
```

## Testing Permission Checks

### Custom Permission Validation

```csharp
public class PermissionValidator
{
    private readonly IOrganizationService _service;
    
    public PermissionValidator(IOrganizationService service)
    {
        _service = service;
    }
    
    public bool CanUserAccessRecord(Guid userId, EntityReference record)
    {
        try
        {
            var request = new RetrievePrincipalAccessRequest
            {
                Principal = new EntityReference("systemuser", userId),
                Target = record
            };
            
            var response = (RetrievePrincipalAccessResponse)_service.Execute(request);
            return response.AccessRights.HasFlag(AccessRights.ReadAccess);
        }
        catch
        {
            return false;
        }
    }
}

[Fact]
public void Should_Validate_User_Permissions()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var userId = Guid.NewGuid();
    var user = new Entity("systemuser") { Id = userId, ["fullname"] = "Test User" };
    context.Initialize(user);
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    var validator = new PermissionValidator(service);
    var canAccess = validator.CanUserAccessRecord(userId, new EntityReference("account", accountId));
    
    Assert.True(canAccess);
}
```

## Complete Examples

### Multi-User Scenario Test

```csharp
[Fact]
public void Should_Handle_Complex_Security_Scenario()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Create users
    var managerId = Guid.NewGuid();
    var salesRep1Id = Guid.NewGuid();
    var salesRep2Id = Guid.NewGuid();
    
    var manager = new Entity("systemuser") { Id = managerId, ["fullname"] = "Manager" };
    var salesRep1 = new Entity("systemuser") { Id = salesRep1Id, ["fullname"] = "Sales Rep 1" };
    var salesRep2 = new Entity("systemuser") { Id = salesRep2Id, ["fullname"] = "Sales Rep 2" };
    
    context.Initialize(new[] { manager, salesRep1, salesRep2 });
    
    // Sales Rep 1 creates opportunity
    context.CallerProperties.CallerId = new EntityReference("systemuser", salesRep1Id);
    var oppId = service.Create(new Entity("opportunity")
    {
        ["name"] = "Big Deal",
        ["estimatedvalue"] = new Money(100000)
    });
    
    // Share with Sales Rep 2
    var grantRequest = new GrantAccessRequest
    {
        Target = new EntityReference("opportunity", oppId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", salesRep2Id),
            AccessMask = AccessRights.ReadAccess
        }
    };
    service.Execute(grantRequest);
    
    // Sales Rep 2 can read but not write
    context.CallerProperties.CallerId = new EntityReference("systemuser", salesRep2Id);
    var opp = service.Retrieve("opportunity", oppId, new ColumnSet("name"));
    Assert.Equal("Big Deal", opp["name"]);
    
    // Manager can access (assuming appropriate role)
    context.CallerProperties.CallerId = new EntityReference("systemuser", managerId);
    var oppForManager = service.Retrieve("opportunity", oppId, new ColumnSet("name"));
    Assert.NotNull(oppForManager);
}
```

## Best Practices

### ✅ Do

- **Always set caller context** - Explicitly set `CallerProperties.CallerId`
- **Test different user roles** - Verify behavior for different privilege levels
- **Test ownership scenarios** - Verify owner-specific logic
- **Test sharing operations** - Test grant, revoke, and modify access
- **Use realistic security structures** - Mirror production security model

### ❌ Don't

- **Don't assume default user** - Always explicitly set the caller
- **Don't skip security tests** - Security bugs are critical
- **Don't hard-code user IDs** - Use generated GUIDs
- **Don't test UI security** - Focus on data-level security

## See Also

- [Security Messages](../messages/security.md) - Grant/Revoke access message executors
- [Message Executors Overview](../messages/README.md) - All security-related messages
- [XrmFakedContext](../concepts/xrm-faked-context.md) - Setting caller properties
- [Service Initialization](../concepts/service-initialization.md) - Creating services for different users
- [Microsoft Security Documentation](https://learn.microsoft.com/en-us/power-platform/admin/wp-security)
