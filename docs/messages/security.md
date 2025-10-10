# Security Messages

> **📝 Note**: This documentation is currently under development.

## Overview

Security messages manage access rights and sharing in Dataverse. These operations control who can access records and what they can do with them.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| GrantAccess | `GrantAccessRequest` | Grant access to a record |
| ModifyAccess | `ModifyAccessRequest` | Modify access rights to a record |
| RevokeAccess | `RevokeAccessRequest` | Revoke access to a record |
| RetrievePrincipalAccess | `RetrievePrincipalAccessRequest` | Get access rights for a principal |
| RetrieveSharedPrincipalsAndAccess | `RetrieveSharedPrincipalsAndAccessRequest` | Get shared access information |

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Grant_Access_To_Record()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Test" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "Test User" }
    });
    
    var request = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
        }
    };
    
    var response = (GrantAccessResponse)service.Execute(request);
}
```

## Detailed Examples

### ModifyAccess

Modify existing access rights to a record.

**Reference:** [ModifyAccessRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.modifyaccessrequest) - Modifies the access rights for a security principal on a record, replacing the existing access rights with the new ones specified in the request.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Modify_Access_Rights()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "User" }
    });
    
    // Owner creates account
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Grant read access
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
    
    // Modify to include write access
    var modifyRequest = new ModifyAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
        }
    };
    
    var response = (ModifyAccessResponse)service.Execute(modifyRequest);
    Assert.NotNull(response);
}
```

### RetrieveSharedPrincipalsAndAccess

Retrieve all principals who have access to a record and their access rights.

**Reference:** [RetrieveSharedPrincipalsAndAccessRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrievesharedprincipalsandaccessrequest) - Retrieves a list of all security principals (users and teams) who have been granted access to a specific record along with their respective access rights.

```csharp
[Fact]
public void Should_Retrieve_Shared_Principals()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" },
        new Entity("systemuser") { Id = user1Id, ["fullname"] = "User 1" },
        new Entity("systemuser") { Id = user2Id, ["fullname"] = "User 2" }
    });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Shared Account" });
    
    // Share with User 1
    service.Execute(new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", user1Id),
            AccessMask = AccessRights.ReadAccess
        }
    });
    
    // Share with User 2
    service.Execute(new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", user2Id),
            AccessMask = AccessRights.WriteAccess
        }
    });
    
    // Retrieve shared principals
    var request = new RetrieveSharedPrincipalsAndAccessRequest
    {
        Target = new EntityReference("account", accountId)
    };
    
    var response = (RetrieveSharedPrincipalsAndAccessResponse)service.Execute(request);
    
    Assert.NotNull(response.PrincipalAccesses);
    Assert.Equal(2, response.PrincipalAccesses.Length);
}
```

## Testing Security Scenarios

### Testing Team Access

```csharp
[Fact]
public void Should_Grant_Access_To_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var teamId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" },
        new Entity("team") { Id = teamId, ["name"] = "Sales Team" }
    });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Team Account" });
    
    var request = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("team", teamId),
            AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess
        }
    };
    
    var response = (GrantAccessResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### Testing Multiple Access Rights

```csharp
[Fact]
public void Should_Grant_Multiple_Access_Rights()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "User" }
    });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Grant multiple rights
    var request = new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = AccessRights.ReadAccess 
                | AccessRights.WriteAccess 
                | AccessRights.DeleteAccess
                | AccessRights.AppendAccess
                | AccessRights.AppendToAccess
        }
    };
    
    service.Execute(request);
    
    // Verify access
    var retrieveRequest = new RetrievePrincipalAccessRequest
    {
        Target = new EntityReference("account", accountId),
        Principal = new EntityReference("systemuser", userId)
    };
    
    var response = (RetrievePrincipalAccessResponse)service.Execute(retrieveRequest);
    
    Assert.True(response.AccessRights.HasFlag(AccessRights.ReadAccess));
    Assert.True(response.AccessRights.HasFlag(AccessRights.WriteAccess));
    Assert.True(response.AccessRights.HasFlag(AccessRights.DeleteAccess));
}
```

### Testing Access Revocation

```csharp
[Theory]
[InlineData(AccessRights.ReadAccess)]
[InlineData(AccessRights.WriteAccess)]
[InlineData(AccessRights.DeleteAccess)]
public void Should_Revoke_Specific_Access_Type(AccessRights accessRight)
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var ownerId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner" },
        new Entity("systemuser") { Id = userId, ["fullname"] = "User" }
    });
    
    context.CallerProperties.CallerId = new EntityReference("systemuser", ownerId);
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Grant access
    service.Execute(new GrantAccessRequest
    {
        Target = new EntityReference("account", accountId),
        PrincipalAccess = new PrincipalAccess
        {
            Principal = new EntityReference("systemuser", userId),
            AccessMask = accessRight
        }
    });
    
    // Revoke access
    var revokeRequest = new RevokeAccessRequest
    {
        Target = new EntityReference("account", accountId),
        Revokee = new EntityReference("systemuser", userId)
    };
    
    var response = (RevokeAccessResponse)service.Execute(revokeRequest);
    Assert.NotNull(response);
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Security & Permissions Guide](../usage/security-permissions.md)
