#if !NET462
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
    /// Uses port 0 for auto-assignment to enable concurrent test execution.
    /// </summary>
    public class ServiceFixture : IAsyncLifetime
    {
        private Process? _serviceProcess;
        public string ServiceUrl { get; private set; } = string.Empty;
        public string BaseUrl { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            // Find repository root by looking for the solution file
            // Start from test binary directory and walk up until we find it
            var testBinaryDir = Directory.GetCurrentDirectory();
            DirectoryInfo? currentDir = new DirectoryInfo(testBinaryDir);
            string? repoRoot = null;
            
            for (int i = 0; i < 10 && currentDir != null; i++)  // Safety limit of 10 levels
            {
                var solutionFile = Path.Combine(currentDir.FullName, "Fake4DataverseFree.sln");
                if (File.Exists(solutionFile))
                {
                    repoRoot = currentDir.FullName;
                    break;
                }
                currentDir = currentDir.Parent;
            }
            
            if (repoRoot == null)
            {
                throw new DirectoryNotFoundException($"Could not find repository root (with Fake4DataverseFree.sln) from test binary dir: {testBinaryDir}");
            }
            
            // Start the Fake4DataverseService in the background
            var serviceProjectPath = Path.Combine(repoRoot, "Fake4DataverseService", "src", "Fake4Dataverse.Service");

            // Use local CDM files for faster, more reliable tests (no network download required)
            var cdmFilesPath = Path.Combine(repoRoot, "cdm-schema-files");
            var accountFile = Path.Combine(cdmFilesPath, "Account.cdm.json");
            var contactFile = Path.Combine(cdmFilesPath, "Contact.cdm.json");
            var opportunityFile = Path.Combine(cdmFilesPath, "Opportunity.cdm.json");

            // Verify paths exist before starting service
            if (!Directory.Exists(serviceProjectPath))
            {
                throw new DirectoryNotFoundException($"Service project path not found: {serviceProjectPath}. Repo root: {repoRoot}");
            }
            if (!File.Exists(accountFile))
            {
                throw new FileNotFoundException($"Account CDM file not found: {accountFile}");
            }

            // Use port 0 for auto-assignment to avoid port conflicts and enable concurrent tests
            _serviceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --no-build -- start --port 0 --host localhost --cdm-files \"{accountFile}\" --cdm-files \"{contactFile}\" --cdm-files \"{opportunityFile}\"",
                    WorkingDirectory = serviceProjectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _serviceProcess.Start();

            // Read output to find the actual assigned port
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(30);
            var actualUrl = string.Empty;

            // Start reading output on a background task
            var outputTask = Task.Run(() =>
            {
                while (!_serviceProcess.StandardOutput.EndOfStream)
                {
                    var line = _serviceProcess.StandardOutput.ReadLine();
                    if (line != null && line.StartsWith("ACTUAL_URL:"))
                    {
                        actualUrl = line.Substring("ACTUAL_URL:".Length).Trim();
                        break;
                    }
                }
            });

            // Wait for the ACTUAL_URL to be parsed or timeout
            var waitResult = await Task.WhenAny(outputTask, Task.Delay(timeout));
            if (waitResult != outputTask || string.IsNullOrEmpty(actualUrl))
            {
                throw new Exception("Failed to get actual URL from service startup");
            }

            ServiceUrl = actualUrl;
            // Note: BaseUrl must end with '/' for relative URIs to work correctly with HttpClient
            BaseUrl = $"{actualUrl}/api/data/v9.2/";

            // Wait for the service to be ready with proper health check
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
#endif
