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

## Coming Soon

Detailed documentation for each team message with examples.

## See Also

- [Message Executors Overview](./README.md)
- [Microsoft Team Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-access-teams-owner-teams-collaborate-share-information)
