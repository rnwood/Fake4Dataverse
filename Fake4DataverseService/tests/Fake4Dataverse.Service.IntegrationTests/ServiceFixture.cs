using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fake4Dataverse.Service.IntegrationTests
{
    /// <summary>
    /// Fixture that starts a single Fake4DataverseService instance for all tests in the collection.
    /// This ensures the service starts once and is shared across all tests, avoiding port conflicts
    /// and reducing test execution time.
    /// </summary>
    public class ServiceFixture : IAsyncLifetime
    {
        private Process? _serviceProcess;
        public const int ServicePort = 5559;
        public static readonly string ServiceUrl = $"http://localhost:{ServicePort}";
        public static readonly string BaseUrl = $"{ServiceUrl}/api/data/v9.2";

        public async Task InitializeAsync()
        {
            // Start the Fake4DataverseService in the background
            var serviceProjectPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "..", "src", "Fake4Dataverse.Service");

            // Use local CDM files for faster, more reliable tests (no network download required)
            var cdmFilesPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "..", "..", "cdm-schema-files");
            var accountFile = Path.Combine(cdmFilesPath, "Account.cdm.json");
            var contactFile = Path.Combine(cdmFilesPath, "Contact.cdm.json");
            var opportunityFile = Path.Combine(cdmFilesPath, "Opportunity.cdm.json");

            _serviceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --no-build -- start --port {ServicePort} --host localhost --cdm-files {accountFile} --cdm-files {contactFile} --cdm-files {opportunityFile}",
                    WorkingDirectory = serviceProjectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _serviceProcess.Start();

            // Wait for the service to start with proper health check
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(30);
            var isServiceReady = false;

            while (DateTime.UtcNow - startTime < timeout && !isServiceReady)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(2);
                    // Use dedicated health endpoint to verify service is fully initialized
                    var response = await httpClient.GetAsync($"{ServiceUrl}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        isServiceReady = true;
                    }
                }
                catch
                {
                    // Service not ready yet, wait and retry
                    await Task.Delay(500);
                }
            }

            if (!isServiceReady)
            {
                throw new Exception("Failed to start Fake4DataverseService within timeout period");
            }

            // Give the service a moment to ensure all endpoints are ready
            await Task.Delay(1000);
        }

        public async Task DisposeAsync()
        {
            if (_serviceProcess != null && !_serviceProcess.HasExited)
            {
                _serviceProcess.Kill();
                await _serviceProcess.WaitForExitAsync();
                _serviceProcess.Dispose();
            }
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
