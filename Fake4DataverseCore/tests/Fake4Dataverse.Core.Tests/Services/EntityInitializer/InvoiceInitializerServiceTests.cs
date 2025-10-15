using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using Xunit;
using Fake4Dataverse.Services;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;

namespace Fake4Dataverse.Tests.Services.EntityInitializer
{
    public class InvoiceInitializerServiceTests : Fake4DataverseTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;
        public InvoiceInitializerServiceTests()
        {
            // Use context and service from base class

            _context = base._context;

            _service = base._service;
        }

        [Fact]
        public void TestPopulateFields()
        {
            (_context as XrmFakedContext).InitializationLevel = EntityInitializationLevel.PerEntity;
            List<Entity> initialEntities = new List<Entity>();

            Entity invoice = new Entity("invoice");
            invoice.Id = Guid.NewGuid();
            initialEntities.Add(invoice);

            _context.Initialize(initialEntities);
            Entity testPostCreate = _service.Retrieve("invoice", invoice.Id, new ColumnSet(true));
            Assert.NotNull(testPostCreate["invoicenumber"]);
        }

        [Fact]
        public void When_InvoiceNumberSet_DoesNot_Overridde_It()
        {
            List<Entity> initialEntities = new List<Entity>();

            Entity invoice = new Entity("invoice");
            invoice.Id = Guid.NewGuid();
            invoice["invoicenumber"] = "TEST";
            initialEntities.Add(invoice);

            _context.Initialize(initialEntities);
            Entity testPostCreate = _service.Retrieve("invoice", invoice.Id, new ColumnSet(true));
            Assert.NotNull(testPostCreate["invoicenumber"]);
            Assert.Equal("TEST", testPostCreate["invoicenumber"]);
        }
    }
}
