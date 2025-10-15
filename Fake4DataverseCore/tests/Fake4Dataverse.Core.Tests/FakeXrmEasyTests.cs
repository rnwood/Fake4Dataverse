using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Tests
{
    /// <summary>
    /// Base test class with validation disabled for backward compatibility.
    /// NEW TESTS SHOULD USE Fake4DataverseValidationTests INSTEAD.
    /// This class is maintained for existing tests that don't have metadata loaded.
    /// </summary>
    public class Fake4DataverseTests
    {
        protected readonly IXrmFakedContext _context;
        protected readonly IOrganizationService _service;
        
        protected Fake4DataverseTests()
        {
            // For backward compatibility with existing tests, disable validation
            // Tests that want validation should use Fake4DataverseValidationTests base class
            _context = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false
            });
            _service = _context.GetOrganizationService();
        }
    }
    
    /// <summary>
    /// Base test class with validation ENABLED (recommended for new tests as of v4.0.0+).
    /// Tests using this class must load required metadata using InitializeMetadataFromCdmFiles or similar.
    /// This is the recommended base class for all new tests going forward.
    /// </summary>
    public class Fake4DataverseValidationTests
    {
        protected readonly IXrmFakedContext _context;
        protected readonly IOrganizationService _service;
        
        protected Fake4DataverseValidationTests()
        {
            // Create context with validation enabled by default (as of v4.0.0+)
            // Tests must load required metadata using InitializeMetadataFromCdmFiles or similar
            _context = XrmFakedContextFactory.New();
            _service = _context.GetOrganizationService();
        }
    }
}