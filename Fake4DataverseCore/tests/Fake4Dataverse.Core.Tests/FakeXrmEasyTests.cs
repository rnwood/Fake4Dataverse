using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Tests
{
    public class Fake4DataverseTests
    {
        protected readonly IXrmFakedContext _context;
        protected readonly IOrganizationService _service;
        
        protected Fake4DataverseTests()
        {
            // Create context with validation enabled by default (as of v4.0.0+)
            // Tests should load required metadata using InitializeMetadataFromCdmFiles or similar
            // For tests that specifically need validation disabled, create a custom context
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }
    }
}