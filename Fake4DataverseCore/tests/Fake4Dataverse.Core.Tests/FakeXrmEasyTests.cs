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
            // For backward compatibility with existing tests, disable validation
            // Tests that want validation should create their own context
            _context = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = false,
                ValidateAttributeTypes = false
            });
            _service = _context.GetOrganizationService();
        }
    }
}