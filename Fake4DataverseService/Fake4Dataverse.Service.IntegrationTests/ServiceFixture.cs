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
    public class ServiceFixture : IAsyncLifetime
    {
        private Process? _serviceProcess;
        private readonly StringBuilder _serviceOutput = new StringBuilder();
        private readonly StringBuilder _serviceError = new StringBuilder();
        private Task? _outputMonitorTask;
        private Task? _errorMonitorTask;
        
        public string ServiceUrl { get; private set; } = string.Empty;
        public string BaseUrl { get; private set; } = string.Empty;
        
        /// <summary>
        /// Gets whether the service process is still running.
        /// Used for diagnostics when tests fail.
        /// </summary>
        public bool IsServiceRunning => _serviceProcess != null && !_serviceProcess.HasExited;

        public async Task InitializeAsync()
        {
            // Find repository root by looking for the solution file
            // Start from test binary directory and walk up until we find it
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
            
            // Start the Fake4DataverseService in the background
            var serviceProjectPath = Path.Combine(repoRoot, "Fake4DataverseService", "Fake4Dataverse.Service");

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
            
            // Set environment variables for verbose logging to aid in CI debugging
            _serviceProcess.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            _serviceProcess.StartInfo.Environment["Logging__LogLevel__Default"] = "Information";
            _serviceProcess.StartInfo.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
            _serviceProcess.StartInfo.Environment["ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS"] = "true";

            _serviceProcess.Start();

            // Read output to find the actual assigned port
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(60); // Increased from 30 to 60 for slower CI environments
            var actualUrl = string.Empty;

            // Start reading output on a background task and capture it for debugging
            var urlDetectionTask = Task.Run(() =>
            {
                while (!_serviceProcess.StandardOutput.EndOfStream)
                {
                    var line = _serviceProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        _serviceOutput.AppendLine(line);
                        if (line.StartsWith("ACTUAL_URL:"))
                        {
                            actualUrl = line.Substring("ACTUAL_URL:".Length).Trim();
                            break;
                        }
                    }
                }
            });
            
            // Continue reading output after URL detection (runs for the lifetime of the service)
            _outputMonitorTask = Task.Run(async () =>
            {
                await urlDetectionTask; // Wait for URL detection first
                while (!_serviceProcess.StandardOutput.EndOfStream)
                {
                    var line = _serviceProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        _serviceOutput.AppendLine(line);
                    }
                }
            });
            
            // Capture error output in background (runs for the lifetime of the service)
            _errorMonitorTask = Task.Run(() =>
            {
                while (!_serviceProcess.StandardError.EndOfStream)
                {
                    var line = _serviceProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        _serviceError.AppendLine(line);
                    }
                }
            });

            // Wait for the ACTUAL_URL to be parsed or timeout
            var waitResult = await Task.WhenAny(urlDetectionTask, Task.Delay(timeout));
            if (waitResult != urlDetectionTask || string.IsNullOrEmpty(actualUrl))
            {
                // Collect all output before throwing exception
                await Task.Delay(500); // Give a moment for more output to be captured
                
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("Failed to get actual URL from service startup");
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Standard Output ===");
                errorMessage.Append(_serviceOutput.ToString());
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Error Output ===");
                errorMessage.Append(_serviceError.ToString());
                
                throw new Exception(errorMessage.ToString());
            }

            ServiceUrl = actualUrl;
            // Note: BaseUrl must end with '/' for relative URIs to work correctly with HttpClient
            BaseUrl = $"{actualUrl}/api/data/v9.2/";

            // Wait for the service to be ready with proper health check
            var isServiceReady = false;
            var healthCheckAttempts = 0;
            var maxHealthCheckAttempts = 60; // 30 seconds with 500ms delays
            
            while (DateTime.UtcNow - startTime < timeout && !isServiceReady && healthCheckAttempts < maxHealthCheckAttempts)
            {
                try
                {
                    healthCheckAttempts++;
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5); // Increased from 2 to 5 for CI
                    // Use dedicated health endpoint to verify service is fully initialized
                    var response = await httpClient.GetAsync($"{ServiceUrl}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        isServiceReady = true;
                    }
                }
                catch (Exception ex)
                {
                    // Log health check failures for debugging
                    if (healthCheckAttempts % 10 == 0) // Log every 5 seconds
                    {
                        _serviceOutput.AppendLine($"Health check attempt {healthCheckAttempts} failed: {ex.GetType().Name}: {ex.Message}");
                    }
                    
                    // Service not ready yet, wait and retry
                    await Task.Delay(500);
                }
            }

            if (!isServiceReady)
            {
                // Collect all output before throwing exception
                await Task.Delay(500); // Give a moment for more output to be captured
                
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("Failed to start Fake4DataverseService within timeout period");
                errorMessage.AppendLine($"Service URL: {ServiceUrl}");
                errorMessage.AppendLine($"Health check attempts: {healthCheckAttempts}");
                errorMessage.AppendLine($"Process exited: {_serviceProcess?.HasExited ?? true}");
                if (_serviceProcess?.HasExited == true)
                {
                    errorMessage.AppendLine($"Process exit code: {_serviceProcess.ExitCode}");
                }
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Standard Output ===");
                errorMessage.Append(_serviceOutput.ToString());
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Error Output ===");
                errorMessage.Append(_serviceError.ToString());
                
                throw new Exception(errorMessage.ToString());
            }

            // Give the service a moment to ensure all endpoints are fully initialized
            // Increased delay for CI environments which may be slower
            await Task.Delay(2000);
        }

        public async Task DisposeAsync()
        {
            if (_serviceProcess != null && !_serviceProcess.HasExited)
            {
                _serviceProcess.Kill();
                await _serviceProcess.WaitForExitAsync();
                
                // Wait for monitoring tasks to complete (with timeout)
                var monitoringTasks = new List<Task>();
                if (_outputMonitorTask != null) monitoringTasks.Add(_outputMonitorTask);
                if (_errorMonitorTask != null) monitoringTasks.Add(_errorMonitorTask);
                
                if (monitoringTasks.Count > 0)
                {
                    await Task.WhenAny(Task.WhenAll(monitoringTasks), Task.Delay(5000));
                }
                
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
