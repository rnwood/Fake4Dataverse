using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fake4Dataverse.Service.IntegrationTests
{
    /// <summary>
    /// Fixture that starts a single Fake4DataverseService instance for all tests in the collection.
    /// This ensures the service starts once and is shared across all tests, avoiding port conflicts
    /// and reducing test execution time.
    /// Uses port 0 for auto-assignment to enable concurrent test execution.
    /// </summary>
    public class ServiceFixture : ServiceTestBase, IAsyncLifetime
    {
        public ServiceFixture()
        {
            // ServiceTestBase handles initialization
        }

        /// <summary>
        /// Override to provide CDM files for the service startup
        /// </summary>
        protected override string GetServiceArguments()
        {
            // Find repository root to locate CDM files
            var testBinaryDir = Directory.GetCurrentDirectory();
            DirectoryInfo? currentDir = new DirectoryInfo(testBinaryDir);
            string? repoRoot = null;

            for (int i = 0; i < 10 && currentDir != null; i++)  // Safety limit of 10 levels
            {
                var solutionFile = Path.Combine(currentDir.FullName, "Fake4Dataverse.sln");
                if (File.Exists(solutionFile))
                {
                    repoRoot = currentDir.FullName;
                    break;
                }
                currentDir = currentDir.Parent;
            }

            if (repoRoot == null)
            {
                throw new DirectoryNotFoundException($"Could not find repository root (with Fake4Dataverse.sln) from test binary dir: {testBinaryDir}");
            }

            // Use local CDM files for faster, more reliable tests (no network download required)
            var cdmFilesPath = Path.Combine(repoRoot, "cdm-schema-files");
            var accountFile = Path.Combine(cdmFilesPath, "Account.cdm.json");
            var contactFile = Path.Combine(cdmFilesPath, "Contact.cdm.json");
            var opportunityFile = Path.Combine(cdmFilesPath, "Opportunity.cdm.json");

            // Verify paths exist before starting service
            if (!File.Exists(accountFile))
            {
                throw new FileNotFoundException($"Account CDM file not found: {accountFile}");
            }

            return $"run --no-build -- start --port 0 --host localhost --cdm-files \"{accountFile}\" --cdm-files \"{contactFile}\" --cdm-files \"{opportunityFile}\"";
        }
    }

    /// <summary>
    /// Collection definition for service integration tests.
    /// All tests in this collection share a single service instance.
    /// </summary>
    [CollectionDefinition("Service Integration Tests")]
    public class ServiceCollection : ICollectionFixture<ServiceFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
