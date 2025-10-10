using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using Xunit;

#if FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using Xunit.Sdk;
#endif

using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Crm;
using System.ServiceModel;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;

namespace Fake4Dataverse.Tests.FakeContextTests.AddMembersTeamRequestTests
{
    public class Tests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public Tests()
        {
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }

        [Fact]
        public void When_a_member_is_added_to_a_non_existing_team_exception_is_thrown()
        {
            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = new[]
                {
                    Guid.NewGuid()
                },
                TeamId = Guid.NewGuid()
            };

            // Execute the request.
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(addMembersTeamRequest));
        }

        [Fact]
        public void When_a_request_is_called_with_an_empty_teamid_parameter_exception_is_thrown()
        {
            
            

            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = new[]
                {
                    Guid.NewGuid()
                },
                TeamId = Guid.Empty
            };

            // Execute the request.
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(addMembersTeamRequest));
        }

        [Fact]
        public void When_a_request_is_called_with_a_null_memberid_parameter_exception_is_thrown()
        {
            
            

            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = null,
                TeamId = Guid.NewGuid()
            };

            // Execute the request.
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(addMembersTeamRequest));
        }

        [Fact]
        public void When_a_request_is_called_with_an_empty_memberid_parameter_exception_is_thrown()
        {
            
            

            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = new[]
                {
                    Guid.Empty
                },
                TeamId = Guid.NewGuid()
            };

            // Execute the request.
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(addMembersTeamRequest));
        }

        [Fact]
        public void When_a_non_existing_member_is_added_to_an_existing_list_exception_is_thrown()
        {
            
            

            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = "Some team"
            };

            _context.Initialize(new List<Entity>
            {
                team
            });

            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = new[]
                {
                    Guid.NewGuid()
                },
                TeamId = team.ToEntityReference().Id
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Execute(addMembersTeamRequest));
        }

        [Fact]
        public void When_a_member_is_added_to_an_existing_list_member_is_added_successfully()
        {
            
            

            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = "Some team"
            };

            var systemuser = new SystemUser
            {
                Id = Guid.NewGuid()
            };

            _context.Initialize(new List<Entity>
            {
                team,
                systemuser
            });

            AddMembersTeamRequest addMembersTeamRequest = new AddMembersTeamRequest
            {
                MemberIds = new[]
                {
                    systemuser.Id
                },
                TeamId = team.ToEntityReference().Id
            };

            _service.Execute(addMembersTeamRequest);

            using (var context = new XrmServiceContext(_service))
            {
                var member = context.CreateQuery<TeamMembership>().FirstOrDefault(tm => tm.TeamId == team.Id && tm.SystemUserId == systemuser.Id);

                Assert.NotNull(member);
            }
        }
    }
}