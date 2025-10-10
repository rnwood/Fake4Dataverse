# Team Messages

> **📝 Note**: This documentation is currently under development.

## Overview

Team messages manage team membership in Dataverse, including adding and removing users from teams.

## Supported Messages

| Message | Request Type | Description |
|---------|-------------|-------------|
| AddMembersTeam | `AddMembersTeamRequest` | Add members to team |
| RemoveMembersTeam | `RemoveMembersTeamRequest` | Remove members from team |
| AddUserToRecordTeam | `AddUserToRecordTeamRequest` | Add user to record team |
| RemoveUserFromRecordTeam | `RemoveUserFromRecordTeamRequest` | Remove user from record team |
| AddMemberList | `AddMemberListRequest` | Add member to marketing list |
| AddListMembersList | `AddListMembersListRequest` | Add members to marketing list |

## Quick Example

```csharp
using Fake4Dataverse.Middleware;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

[Fact]
public void Should_Add_Members_To_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var teamId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("team") { Id = teamId, ["name"] = "Sales Team" },
        new Entity("systemuser") { Id = user1Id, ["fullname"] = "User 1" },
        new Entity("systemuser") { Id = user2Id, ["fullname"] = "User 2" }
    });
    
    var request = new AddMembersTeamRequest
    {
        TeamId = teamId,
        MemberIds = new[] { user1Id, user2Id }
    };
    
    var response = (AddMembersTeamResponse)service.Execute(request);
}
```

## Detailed Examples

### RemoveMembersTeam

Remove members from an owner team.

**Reference:** [RemoveMembersTeamRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.removemembersteamrequest) - Removes one or more system users from an owner team, revoking their membership and associated team privileges.

```csharp
using Microsoft.Crm.Sdk.Messages;

[Fact]
public void Should_Remove_Members_From_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var teamId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("team") { Id = teamId, ["name"] = "Sales Team" },
        new Entity("systemuser") { Id = user1Id, ["fullname"] = "User 1" },
        new Entity("systemuser") { Id = user2Id, ["fullname"] = "User 2" }
    });
    
    // Add members first
    var addRequest = new AddMembersTeamRequest
    {
        TeamId = teamId,
        MemberIds = new[] { user1Id, user2Id }
    };
    service.Execute(addRequest);
    
    // Remove members
    var removeRequest = new RemoveMembersTeamRequest
    {
        TeamId = teamId,
        MemberIds = new[] { user1Id }
    };
    
    var response = (RemoveMembersTeamResponse)service.Execute(removeRequest);
    Assert.NotNull(response);
}
```

### AddUserToRecordTeam

Add a user to an access team (record team).

**Reference:** [AddUserToRecordTeamRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.addusertoteamrequest) - Adds a system user to an access team associated with a specific record, granting them access to that record through team privileges.

```csharp
[Fact]
public void Should_Add_User_To_Record_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var teamId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Test Account" },
        new Entity("team") 
        { 
            Id = teamId, 
            ["name"] = "Access Team",
            ["teamtype"] = new OptionSetValue(1) // Access team
        },
        new Entity("systemuser") { Id = userId, ["fullname"] = "Team Member" }
    });
    
    var request = new AddUserToRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = userId,
        TeamTemplateId = Guid.NewGuid() // In real scenario, this would be a real template ID
    };
    
    var response = (AddUserToRecordTeamResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### RemoveUserFromRecordTeam

Remove a user from an access team.

**Reference:** [RemoveUserFromRecordTeamRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.removeuserfromrecordteamrequest) - Removes a system user from an access team associated with a specific record, revoking their team-based access to that record.

```csharp
[Fact]
public void Should_Remove_User_From_Record_Team()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var teamId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") { Id = accountId, ["name"] = "Test Account" },
        new Entity("team") 
        { 
            Id = teamId, 
            ["name"] = "Access Team",
            ["teamtype"] = new OptionSetValue(1)
        },
        new Entity("systemuser") { Id = userId, ["fullname"] = "Team Member" }
    });
    
    // Add user first
    var addRequest = new AddUserToRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = userId,
        TeamTemplateId = Guid.NewGuid()
    };
    service.Execute(addRequest);
    
    // Remove user
    var removeRequest = new RemoveUserFromRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = userId,
        TeamTemplateId = Guid.NewGuid()
    };
    
    var response = (RemoveUserFromRecordTeamResponse)service.Execute(removeRequest);
    Assert.NotNull(response);
}
```

### AddMemberList

Add a member to a marketing list.

**Reference:** [AddMemberListRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.addmemberlistrequest) - Adds a single member (account, contact, or lead) to a static marketing list for campaign and marketing purposes.

```csharp
[Fact]
public void Should_Add_Member_To_Marketing_List()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var listId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("list") { Id = listId, ["listname"] = "Newsletter List" },
        new Entity("contact") 
        { 
            Id = contactId, 
            ["firstname"] = "John",
            ["lastname"] = "Doe",
            ["emailaddress1"] = "john@example.com"
        }
    });
    
    var request = new AddMemberListRequest
    {
        ListId = listId,
        EntityId = contactId
    };
    
    var response = (AddMemberListResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

### AddListMembersList

Add multiple members to a marketing list.

**Reference:** [AddListMembersListRequest](https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.addlistmemberslistrequest) - Adds multiple members (accounts, contacts, or leads) to a static marketing list in a single operation for bulk list management.

```csharp
[Fact]
public void Should_Add_Multiple_Members_To_Marketing_List()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var listId = Guid.NewGuid();
    var contact1Id = Guid.NewGuid();
    var contact2Id = Guid.NewGuid();
    var contact3Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("list") { Id = listId, ["listname"] = "Email Campaign List" },
        new Entity("contact") { Id = contact1Id, ["fullname"] = "Contact 1" },
        new Entity("contact") { Id = contact2Id, ["fullname"] = "Contact 2" },
        new Entity("contact") { Id = contact3Id, ["fullname"] = "Contact 3" }
    });
    
    var request = new AddListMembersListRequest
    {
        ListId = listId,
        MemberIds = new[] { contact1Id, contact2Id, contact3Id }
    };
    
    var response = (AddListMembersListResponse)service.Execute(request);
    Assert.NotNull(response);
}
```

## Complete Team Management Examples

### Owner Team Lifecycle

```csharp
[Fact]
public void Should_Manage_Owner_Team_Membership()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    // Setup
    var teamId = Guid.NewGuid();
    var manager = Guid.NewGuid();
    var member1Id = Guid.NewGuid();
    var member2Id = Guid.NewGuid();
    var member3Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("team") { Id = teamId, ["name"] = "Sales Team" },
        new Entity("systemuser") { Id = managerId, ["fullname"] = "Team Manager" },
        new Entity("systemuser") { Id = member1Id, ["fullname"] = "Member 1" },
        new Entity("systemuser") { Id = member2Id, ["fullname"] = "Member 2" },
        new Entity("systemuser") { Id = member3Id, ["fullname"] = "Member 3" }
    });
    
    // Add initial members
    var addRequest = new AddMembersTeamRequest
    {
        TeamId = teamId,
        MemberIds = new[] { member1Id, member2Id, member3Id }
    };
    service.Execute(addRequest);
    
    // Remove one member
    var removeRequest = new RemoveMembersTeamRequest
    {
        TeamId = teamId,
        MemberIds = new[] { member3Id }
    };
    service.Execute(removeRequest);
    
    // Verify team membership via teammembership intersect entity
    var memberships = context.CreateQuery("teammembership")
        .Where(tm => tm.GetAttributeValue<Guid>("teamid") == teamId)
        .ToList();
    
    Assert.Equal(2, memberships.Count);
}
```

### Access Team Scenario

```csharp
[Fact]
public void Should_Manage_Access_Team_For_Account()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var accountId = Guid.NewGuid();
    var teamTemplateId = Guid.NewGuid();
    var user1Id = Guid.NewGuid();
    var user2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("account") 
        { 
            Id = accountId, 
            ["name"] = "Enterprise Account" 
        },
        new Entity("systemuser") 
        { 
            Id = user1Id, 
            ["fullname"] = "Account Manager" 
        },
        new Entity("systemuser") 
        { 
            Id = user2Id, 
            ["fullname"] = "Sales Rep" 
        }
    });
    
    // Add account manager to record team
    var addManager = new AddUserToRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = user1Id,
        TeamTemplateId = teamTemplateId
    };
    service.Execute(addManager);
    
    // Add sales rep to record team
    var addSalesRep = new AddUserToRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = user2Id,
        TeamTemplateId = teamTemplateId
    };
    service.Execute(addSalesRep);
    
    // Later, remove sales rep access
    var removeSalesRep = new RemoveUserFromRecordTeamRequest
    {
        Record = new EntityReference("account", accountId),
        SystemUserId = user2Id,
        TeamTemplateId = teamTemplateId
    };
    service.Execute(removeSalesRep);
}
```

### Marketing List Management

```csharp
[Fact]
public void Should_Build_Marketing_List()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var listId = Guid.NewGuid();
    
    // Create marketing list
    context.Initialize(new Entity("list")
    {
        Id = listId,
        ["listname"] = "Q4 Campaign List",
        ["createdfromcode"] = new OptionSetValue(2) // Contact
    });
    
    // Create contacts
    var contacts = new List<Guid>();
    for (int i = 1; i <= 5; i++)
    {
        var contactId = Guid.NewGuid();
        context.Initialize(new Entity("contact")
        {
            Id = contactId,
            ["firstname"] = $"Contact",
            ["lastname"] = $"{i}",
            ["emailaddress1"] = $"contact{i}@example.com"
        });
        contacts.Add(contactId);
    }
    
    // Add all contacts to list at once
    var addRequest = new AddListMembersListRequest
    {
        ListId = listId,
        MemberIds = contacts.ToArray()
    };
    service.Execute(addRequest);
    
    // Verify list members
    var listMembers = context.CreateQuery("listmember")
        .Where(lm => lm.GetAttributeValue<Guid>("listid") == listId)
        .ToList();
    
    Assert.Equal(5, listMembers.Count);
}
```

### Dynamic Team Assignment

```csharp
[Fact]
public void Should_Assign_Team_Based_On_Business_Logic()
{
    var context = XrmFakedContextFactory.New();
    var service = context.GetOrganizationService();
    
    var tier1TeamId = Guid.NewGuid();
    var tier2TeamId = Guid.NewGuid();
    var agent1Id = Guid.NewGuid();
    var agent2Id = Guid.NewGuid();
    
    context.Initialize(new[]
    {
        new Entity("team") { Id = tier1TeamId, ["name"] = "Tier 1 Support" },
        new Entity("team") { Id = tier2TeamId, ["name"] = "Tier 2 Support" },
        new Entity("systemuser") { Id = agent1Id, ["fullname"] = "Junior Agent" },
        new Entity("systemuser") { Id = agent2Id, ["fullname"] = "Senior Agent" }
    });
    
    // Assign agents to appropriate teams
    service.Execute(new AddMembersTeamRequest
    {
        TeamId = tier1TeamId,
        MemberIds = new[] { agent1Id }
    });
    
    service.Execute(new AddMembersTeamRequest
    {
        TeamId = tier2TeamId,
        MemberIds = new[] { agent2Id }
    });
    
    // Promote agent1 to tier 2
    service.Execute(new RemoveMembersTeamRequest
    {
        TeamId = tier1TeamId,
        MemberIds = new[] { agent1Id }
    });
    
    service.Execute(new AddMembersTeamRequest
    {
        TeamId = tier2TeamId,
        MemberIds = new[] { agent1Id }
    });
}
```

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Team Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-access-teams-owner-teams-collaborate-share-information)
