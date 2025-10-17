using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Middleware.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests
{
    public class FakeContextMockTests : Fake4DataverseTests
    {
        [Fact]
        public void Should_Execute_Mock_For_OrganizationRequests()
        {
            var context = MiddlewareBuilder
                        .New()
                        .AddExecutionMock<RetrieveEntityRequest>(RetrieveEntityMock)
                        .UseMessages()
                        .Build();
            var service = context.GetOrganizationService();             var e = new Entity("Contact") { Id = Guid.NewGuid() };          
            context.Initialize(new[] { e });
            
            var request = new RetrieveEntityRequest
            {
                LogicalName = "Contact",
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveEntityResponse)service.Execute(request);

            Assert.Equal("Successful", response.ResponseName);
        }

        public OrganizationResponse RetrieveEntityMock(OrganizationRequest req)
        {
            return new RetrieveEntityResponse { ResponseName = "Successful" };
        }

        public OrganizationResponse AnotherRetrieveEntityMock(OrganizationRequest req)
        {
            return new RetrieveEntityResponse { ResponseName = "Another" };
        }

        [Fact]
        public void Should_Override_FakeMessageExecutor()
        {
            var context = MiddlewareBuilder
                        .New()
                        .AddExecutionMock<RetrieveEntityRequest>(RetrieveEntityMock)
                        .AddExecutionMock<RetrieveEntityRequest>(AnotherRetrieveEntityMock)
                        .UseMessages()
                        .Build();
            var service = context.GetOrganizationService();            var e = new Entity("Contact") { Id = Guid.NewGuid() };
            context.Initialize(new[] { e });

            var request = new RetrieveEntityRequest
            {
                LogicalName = "Contact",
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveEntityResponse)service.Execute(request);

            Assert.Equal("Another", response.ResponseName);
        }

        [Fact]
        public void Should_Override_Execution_Mock()
        {
            var context = MiddlewareBuilder
                        .New()
                        .AddFakeMessageExecutors()
                        .AddFakeMessageExecutor(new FakeRetrieveEntityRequestExecutor())
                        .UseMessages()
                        .Build();
            var service = context.GetOrganizationService();            var e = new Entity("Contact") { Id = Guid.NewGuid() };
            context.Initialize(new[] { e });

            var request = new RetrieveEntityRequest
            {
                LogicalName = "Contact",
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveEntityResponse)service.Execute(request);

            Assert.Equal("Successful", response.ResponseName);
        }

        protected class FakeRetrieveEntityRequestExecutor : IFakeMessageExecutor
        {
            public bool CanExecute(OrganizationRequest request)
            {
                return request is RetrieveEntityRequest;
            }

            public Type GetResponsibleRequestType()
            {
                return typeof(RetrieveEntityRequest);
            }

            public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
            {
                return new RetrieveEntityResponse { ResponseName = "Successful" };
            }
        }
    }
}