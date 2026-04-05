using System;
using Microsoft.Xrm.Sdk;
using Moq;

namespace Fake4Dataverse.Moq
{
    /// <summary>
    /// Extension methods for bridging <see cref="FakeOrganizationService"/> with Moq.
    /// </summary>
    public static class MoqServiceExtensions
    {
        /// <summary>
        /// Creates a <see cref="Mock{IOrganizationService}"/> that delegates all calls
        /// to the specified <see cref="FakeOrganizationService"/>.
        /// This enables scenarios where production code accepts <c>Mock&lt;IOrganizationService&gt;</c>
        /// and you want to back it with the full in-memory fake.
        /// </summary>
        public static Mock<IOrganizationService> AsMock(this FakeOrganizationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var mock = new Mock<IOrganizationService>();
            IOrganizationService svc = service;

            mock.Setup(m => m.Create(It.IsAny<Entity>()))
                .Returns((Entity e) => svc.Create(e));

            mock.Setup(m => m.Retrieve(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Microsoft.Xrm.Sdk.Query.ColumnSet>()))
                .Returns((string name, Guid id, Microsoft.Xrm.Sdk.Query.ColumnSet cs) => svc.Retrieve(name, id, cs));

            mock.Setup(m => m.RetrieveMultiple(It.IsAny<Microsoft.Xrm.Sdk.Query.QueryBase>()))
                .Returns((Microsoft.Xrm.Sdk.Query.QueryBase q) => svc.RetrieveMultiple(q));

            mock.Setup(m => m.Update(It.IsAny<Entity>()))
                .Callback((Entity e) => svc.Update(e));

            mock.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<Guid>()))
                .Callback((string name, Guid id) => svc.Delete(name, id));

            mock.Setup(m => m.Execute(It.IsAny<OrganizationRequest>()))
                .Returns((OrganizationRequest r) => svc.Execute(r));

            mock.Setup(m => m.Associate(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Relationship>(), It.IsAny<EntityReferenceCollection>()))
                .Callback((string name, Guid id, Relationship rel, EntityReferenceCollection refs) => svc.Associate(name, id, rel, refs));

            mock.Setup(m => m.Disassociate(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Relationship>(), It.IsAny<EntityReferenceCollection>()))
                .Callback((string name, Guid id, Relationship rel, EntityReferenceCollection refs) => svc.Disassociate(name, id, rel, refs));

            return mock;
        }

        /// <summary>
        /// Creates a <see cref="Mock{IOrganizationServiceFactory}"/> that returns
        /// a mock organization service backed by the specified <see cref="FakeOrganizationService"/>.
        /// Useful for plugin testing where <c>IOrganizationServiceFactory</c> is injected.
        /// </summary>
        public static Mock<IOrganizationServiceFactory> AsMockFactory(this FakeOrganizationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var mockFactory = new Mock<IOrganizationServiceFactory>();
            mockFactory.Setup(f => f.CreateOrganizationService(It.IsAny<Guid?>()))
                .Returns(service);

            return mockFactory;
        }
    }
}
