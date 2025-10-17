using Crm;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.LoseOpportunityTests
{
    public class LoseOpportunityTests: Fake4DataverseTests
    {
        [Fact]
        public void Check_if_Opportunity_status_is_Lose_after_set()
        {
            _context.EnableProxyTypes(Assembly.GetExecutingAssembly());

            var opportunity = new Opportunity()
            {
                Id = Guid.NewGuid()
            };
            _context.Initialize(new[] { opportunity });

            var request = new LoseOpportunityRequest()
            {
                OpportunityClose = new OpportunityClose
                {
                    OpportunityId = new EntityReference(Opportunity.EntityLogicalName, opportunity.Id)
                },
                Status = new OptionSetValue((int)OpportunityState.Lost)
            };

            _service.Execute(request);

            var opp = (from op in _context.CreateQuery<Opportunity>()
                       where op.Id == opportunity.Id
                       select op).FirstOrDefault();

            Assert.Equal((int)OpportunityState.Lost, opp.StatusCode.Value);
        }
    }
}