# Impersonation

**Issue:** [#116](https://github.com/rnwood/Fake4Dataverse/issues/116)

## Overview

Impersonation allows a user with appropriate privileges to perform operations on behalf of another user. This is useful for:
- Service accounts performing actions for end users
- Administrators troubleshooting user-specific issues
- Integration scenarios where one system acts on behalf of users

**Reference:** [Microsoft Learn - Impersonate another user](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api)

## How Impersonation Works

When impersonating:
1. The **calling user** (impersonator) must have the `prvActOnBehalfOfAnotherUser` privilege
2. Operations are performed as if the **impersonated user** made them
3. Security checks use the impersonated user's permissions
4. Audit fields reflect both users:
   - `createdby`/`modifiedby` = impersonated user
   - `createdonbehalfof`/`modifiedonbehalfof` = calling user (impersonator)

## Core API Usage

### Setting Up Impersonation

```csharp
var context = new XrmFakedContext();
context.SecurityConfiguration.SecurityEnabled = true;
var service = context.GetOrganizationService();

// Create users
var adminUserId = Guid.NewGuid();
var targetUserId = Guid.NewGuid();

var adminUser = new Entity("systemuser")
{
    Id = adminUserId,
    ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
    ["fullname"] = "Admin User"
};
context.AddEntity(adminUser);

var targetUser = new Entity("systemuser")
{
    Id = targetUserId,
    ["businessunitid"] = new EntityReference("businessunit", businessUnitId),
    ["fullname"] = "Target User"
};
context.AddEntity(targetUser);

// Assign System Administrator role to admin
service.Associate("systemuser", adminUserId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", context.SecurityManager.SystemAdministratorRoleId) });

// Set up impersonation
context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

// All subsequent operations are performed as targetUser
var account = new Entity("account") { ["name"] = "Contoso Ltd" };
var accountId = service.Create(account);

// The account is created as if targetUser created it
var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));
Assert.Equal(targetUserId, retrieved.GetAttributeValue<EntityReference>("createdby").Id);
Assert.Equal(adminUserId, retrieved.GetAttributeValue<EntityReference>("createdonbehalfof").Id);
```

### Clearing Impersonation

```csharp
// Stop impersonating
context.CallerProperties.ImpersonatedUserId = null;

// Operations now performed as the actual calling user
```

## Privilege Requirements

The calling user must have one of the following:
1. **System Administrator role** (has `prvActOnBehalfOfAnotherUser` implicitly)
2. **Custom role** with the `prvActOnBehalfOfAnotherUser` privilege assigned

### Granting Impersonation Privilege

```csharp
// Create the impersonation privilege if it doesn't exist
var impersonationPrivilege = new Entity("privilege")
{
    Id = Guid.NewGuid(),
    ["name"] = "prvActOnBehalfOfAnotherUser",
    ["accessright"] = 0, // Special privilege (not entity-specific)
    ["privilegeid"] = Guid.NewGuid()
};
context.AddEntity(impersonationPrivilege);

// Assign to a role
var rolePrivilege = new Entity("roleprivileges")
{
    Id = Guid.NewGuid(),
    ["roleid"] = new EntityReference("role", customRoleId),
    ["privilegeid"] = new EntityReference("privilege", impersonationPrivilege.Id),
    ["privilegedepthmask"] = 8 // Global depth
};
context.AddEntity(rolePrivilege);
```

## Service Layer Usage

### Web API / REST (HTTP Header)

When using Fake4DataverseService with HTTP/REST endpoints, set the `MSCRMCallerID` header:

```csharp
using (var client = new HttpClient())
{
    client.BaseAddress = new Uri("http://localhost:5000/api/data/v9.2/");
    
    // Set the impersonation header
    client.DefaultRequestHeaders.Add("MSCRMCallerID", targetUserId.ToString());
    
    // Create an account - operation performed as targetUserId
    var account = new { name = "Contoso Ltd" };
    var response = await client.PostAsJsonAsync("accounts", account);
}
```

### SOAP / WCF (SOAP Header)

When using the SOAP endpoint, set the `CallerObjectId` in the SOAP header:

```csharp
var binding = new BasicHttpBinding();
var endpoint = new EndpointAddress("http://localhost:5000/XRMServices/2011/Organization.svc");
var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);

using (var scope = new OperationContextScope((IContextChannel)service))
{
    // Add the impersonation header
    var header = MessageHeader.CreateHeader(
        "CallerId",
        "http://schemas.microsoft.com/xrm/2011/Contracts",
        targetUserId);
    OperationContext.Current.OutgoingMessageHeaders.Add(header);
    
    // Create an account - operation performed as targetUserId
    var account = new Entity("account") { ["name"] = "Contoso Ltd" };
    var accountId = service.Create(account);
}
```

## Security Behavior

### Permission Checks

When impersonating, **all security checks use the impersonated user's permissions**:

```csharp
// Admin impersonates a user with limited permissions
context.CallerProperties.CallerId = new EntityReference("systemuser", adminUserId);
context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", limitedUserId);

// This will FAIL if limitedUser doesn't have create privilege for account
// Even though adminUser is a System Administrator
var account = new Entity("account") { ["name"] = "Test" };
service.Create(account); // Throws UnauthorizedAccessException
```

### Impersonation Validation

If the calling user lacks the impersonation privilege:

```csharp
// Regular user without impersonation privilege
context.CallerProperties.CallerId = new EntityReference("systemuser", regularUserId);
context.CallerProperties.ImpersonatedUserId = new EntityReference("systemuser", targetUserId);

// This will throw UnauthorizedAccessException
service.Create(account);
// Error: "User {regularUserId} does not have the 'prvActOnBehalfOfAnotherUser' privilege"
```

## Audit Trail

Impersonation creates a complete audit trail:

```csharp
// After creating with impersonation
var account = service.Retrieve("account", accountId, new ColumnSet(true));

// Fields show both users
Console.WriteLine($"Created by: {account.GetAttributeValue<EntityReference>("createdby").Id}");
// Output: Created by: {targetUserId}

Console.WriteLine($"Created on behalf of: {account.GetAttributeValue<EntityReference>("createdonbehalfof").Id}");
// Output: Created on behalf of: {adminUserId}

// Audit records also reflect the impersonated user
var auditRecords = context.AuditRepository.GetAuditRecordsForEntity("account", accountId);
var auditUserId = auditRecords.First().GetAttributeValue<EntityReference>("userid").Id;
Assert.Equal(targetUserId, auditUserId);
```

## Common Patterns

### Bulk Operations

```csharp
// Impersonate once, perform multiple operations
context.CallerProperties.CallerId = adminRef;
context.CallerProperties.ImpersonatedUserId = userRef;

foreach (var item in items)
{
    var entity = new Entity("account") { ["name"] = item.Name };
    service.Create(entity);
}

// Clear impersonation
context.CallerProperties.ImpersonatedUserId = null;
```

### Plugin Context

```csharp
// In a plugin test, set impersonation for the plugin execution
var pluginContext = context.GetDefaultPluginContext();
pluginContext.InitiatingUserId = adminUserId;
pluginContext.UserId = targetUserId; // The impersonated user

// Execute plugin
var plugin = new YourPlugin();
plugin.Execute(pluginContext);
```

### Testing Impersonation Logic

```csharp
[Fact]
public void Should_Respect_Impersonated_User_Permissions()
{
    // Arrange
    var context = new XrmFakedContext();
    context.SecurityConfiguration.SecurityEnabled = true;
    
    var admin = CreateAdminUser(context);
    var limitedUser = CreateUserWithLimitedPermissions(context);
    
    context.CallerProperties.CallerId = admin.ToEntityReference();
    context.CallerProperties.ImpersonatedUserId = limitedUser.ToEntityReference();
    
    var service = context.GetOrganizationService();
    
    // Act & Assert - Should fail due to limited user's permissions
    var account = new Entity("account") { ["name"] = "Test" };
    Assert.Throws<UnauthorizedAccessException>(() => service.Create(account));
}
```

## Best Practices

1. **Always enable security** when testing impersonation:
   ```csharp
   context.SecurityConfiguration.SecurityEnabled = true;
   ```

2. **Clear impersonation** when no longer needed:
   ```csharp
   context.CallerProperties.ImpersonatedUserId = null;
   ```

3. **Test both success and failure** scenarios:
   - Verify operations succeed with proper privileges
   - Verify operations fail without proper privileges
   - Verify audit fields are set correctly

4. **Use meaningful user names** in tests:
   ```csharp
   var admin = new Entity("systemuser") { ["fullname"] = "Admin User" };
   var target = new Entity("systemuser") { ["fullname"] = "Target User" };
   ```

## Troubleshooting

### "User does not have the 'prvActOnBehalfOfAnotherUser' privilege"

**Cause:** The calling user (impersonator) lacks the impersonation privilege.

**Solution:** Assign System Administrator role or grant the specific privilege:
```csharp
service.Associate("systemuser", callerUserId, 
    new Relationship("systemuserroles_association"),
    new EntityReferenceCollection { new EntityReference("role", sysAdminRoleId) });
```

### Operations Fail with Impersonation

**Cause:** The impersonated user lacks necessary permissions.

**Solution:** Grant appropriate privileges to the impersonated user or verify role assignments.

### createdonbehalfof Not Being Set

**Cause:** Impersonation is not active (ImpersonatedUserId is null).

**Solution:** Ensure ImpersonatedUserId is set before the operation:
```csharp
context.CallerProperties.ImpersonatedUserId = targetUserRef;
```

## See Also

- [Security Model](security-model.md) - Overview of Fake4Dataverse security
- [Security Permissions](security-permissions.md) - Managing roles and privileges
- [Audit](auditing.md) - Audit trail functionality
- [Microsoft Learn - Impersonation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api)
