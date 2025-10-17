using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Tests
{
    /// <summary>
    /// Base test class with validation enabled (as of v4.0.0+).
    /// Tests using this class must load required metadata using InitializeMetadataFromCdmFiles or similar.
    /// This is the recommended base class for all new tests going forward.
    /// </summary>
    public class Fake4DataverseTests
    {
        protected readonly IXrmFakedContext _context;
        protected readonly IOrganizationService _service;
        
        protected Fake4DataverseTests()
        {
            // Create context with validation enabled by default (as of v4.0.0+)
            // Tests must load required metadata using InitializeMetadataFromCdmFiles or similar
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }
    }
    
    /// <summary>
    /// Legacy alias for Fake4DataverseTests - maintained for backward compatibility.
    /// </summary>
    public class Fake4DataverseValidationTests : Fake4DataverseTests
    {
    }
}