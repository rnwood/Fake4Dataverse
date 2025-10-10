# Security and Permissions

> **📝 Note**: This documentation is currently under development. For now, see related documentation below.

## Related Documentation

- [Message Executors Overview](../messages/README.md) - Security-related messages
- [XrmFakedContext](../concepts/xrm-faked-context.md) - Setting caller properties

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Test_Security_Context()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Set the calling user
    var userId = Guid.NewGuid();
    context.CallerProperties.CallerId = new EntityReference("systemuser", userId);
    
    // Act - Perform operations as this user
    var accountId = service.Create(new Entity("account") { ["name"] = "Test" });
    
    // Assert
    Assert.NotEqual(Guid.Empty, accountId);
}
```

## Security Messages

The following security-related messages are supported:
- `GrantAccessRequest` - Grant access to a record
- `ModifyAccessRequest` - Modify access rights
- `RevokeAccessRequest` - Revoke access
- `RetrievePrincipalAccessRequest` - Get access rights

See [Message Executors Overview](../messages/README.md) for complete list.

## Coming Soon

This guide will cover:
- Testing with different user contexts
- Security roles and privileges
- Record-level security
- Team ownership
- Sharing and access control
- Testing permission checks

For now, refer to [XrmFakedContext documentation](../concepts/xrm-faked-context.md#callerproperties) for setting caller properties.
