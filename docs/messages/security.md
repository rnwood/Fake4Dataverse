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

## Coming Soon

Detailed documentation for each security message with examples and best practices.

## See Also

- [Message Executors Overview](./README.md)
- [Security & Permissions Guide](../usage/security-permissions.md)
