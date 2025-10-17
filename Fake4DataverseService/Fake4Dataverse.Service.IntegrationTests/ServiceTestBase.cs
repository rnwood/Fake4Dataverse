using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fake4Dataverse.Service.IntegrationTests
{
    /// <summary>
    /// Base class for integration tests that need to start a Fake4DataverseService instance.
    /// Provides common service lifecycle management including startup, URL discovery, readiness checking, and cleanup.
    /// </summary>
    public abstract class ServiceTestBase : IAsyncLifetime
    {
        protected Process? _serviceProcess;
        protected readonly StringBuilder _serviceOutput = new StringBuilder();
        protected readonly StringBuilder _serviceError = new StringBuilder();

        public string ServiceUrl { get; protected set; } = string.Empty;
        public string BaseUrl { get; protected set; } = string.Empty;

        /// <summary>
        /// Override this method to provide custom command line arguments for starting the service.
        /// Default implementation provides basic startup arguments.
        /// </summary>
        protected virtual string GetServiceArguments()
        {
            return "run --no-build -- start --port 0 --host localhost";
        }

        /// <summary>
        /// Override this method to perform additional setup after the service is ready.
        /// Called after the service URL is discovered and health check passes.
        /// </summary>
        protected virtual Task OnServiceReadyAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            // Find repository root by looking for the solution file
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

            // Verify service project path exists
            if (!Directory.Exists(serviceProjectPath))
            {
                throw new DirectoryNotFoundException($"Service project path not found: {serviceProjectPath}. Repo root: {repoRoot}");
            }

            _serviceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = GetServiceArguments(),
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
            var timeout = TimeSpan.FromSeconds(30);
            var actualUrl = string.Empty;
            var urlFound = new TaskCompletionSource<bool>();

            // CRITICAL: Continuously consume stdout to prevent the service process from blocking
            // If we stop reading after finding ACTUAL_URL, the service's output buffer can fill up
            // and cause the service to hang when writing to stdout, leading to timeouts in CI.
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput
            var outputTask = Task.Run(() =>
            {
                while (!_serviceProcess.StandardOutput.EndOfStream)
                {
                    var line = _serviceProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        _serviceOutput.AppendLine(line);
                        if (line.StartsWith("ACTUAL_URL:") && string.IsNullOrEmpty(actualUrl))
                        {
                            actualUrl = line.Substring("ACTUAL_URL:".Length).Trim();
                            urlFound.TrySetResult(true);
                        }
                    }
                }
            });

            // CRITICAL: Continuously consume stderr to prevent the service process from blocking
            // Same reasoning as stdout - must continuously read to prevent buffer overflow
            var errorTask = Task.Run(() =>
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
            var waitResult = await Task.WhenAny(urlFound.Task, Task.Delay(timeout));
            if (waitResult != urlFound.Task || string.IsNullOrEmpty(actualUrl))
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
                // Collect all output before throwing exception
                await Task.Delay(500); // Give a moment for more output to be captured

                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("Failed to start Fake4DataverseService within timeout period");
                errorMessage.AppendLine($"Service URL: {ServiceUrl}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Standard Output ===");
                errorMessage.Append(_serviceOutput.ToString());
                errorMessage.AppendLine();
                errorMessage.AppendLine("=== Service Error Output ===");
                errorMessage.Append(_serviceError.ToString());

                throw new Exception(errorMessage.ToString());
            }

            // Allow derived classes to perform additional setup
            await OnServiceReadyAsync();

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
}