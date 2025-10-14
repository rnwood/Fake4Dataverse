#if !NET462
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.ServiceModel;
using Xunit;
using System.Diagnostics;

namespace Fake4Dataverse.Service.IntegrationTests;

/// <summary>
/// Tests for ServiceClient authentication with access tokens.
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect
/// ServiceClient supports AuthType=OAuth with AccessToken parameter.
/// </summary>
[Collection("Service Integration Tests")]
public class ServiceClientAuthTests : IAsyncLifetime
{
    private Process? _serviceProcess;
    private const string AccessToken = "test-access-token-12345";
    private string ServiceUrl { get; set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Start the Fake4DataverseService with authentication enabled
        var serviceProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "src", "Fake4Dataverse.Service");

        // Use port 0 for auto-assignment
        _serviceProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build -- start --port 0 --host localhost --access-token {AccessToken} --no-cdm",
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

        // Wait for the service to be ready
        var isServiceReady = false;
        while (DateTime.UtcNow - startTime < timeout && !isServiceReady)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                // Use dedicated health endpoint (bypasses auth) to verify service is fully initialized
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

    public Task DisposeAsync()
    {
        if (_serviceProcess != null && !_serviceProcess.HasExited)
        {
            _serviceProcess.Kill(entireProcessTree: true);
            _serviceProcess.WaitForExit();
            _serviceProcess.Dispose();
        }
        return Task.CompletedTask;
    }

    [Fact(Skip = "ServiceClient requires full OAuth flow - this test documents the attempt")]
    public void Should_Connect_With_ServiceClient_Using_AccessToken()
    {
        // Arrange - Create ServiceClient connection string with OAuth and AccessToken
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect
        var connectionString = $"AuthType=OAuth;Url={ServiceUrl};AccessToken={AccessToken}";

        // Act - Create ServiceClient (this may not work as expected due to OAuth requirements)
        // ServiceClient expects full OAuth flow, so this test documents the limitation
        using var serviceClient = new ServiceClient(connectionString);

        // Assert
        // Note: ServiceClient.IsReady may not be true because it expects OAuth provider
        // This test documents the attempt and expected behavior
        Assert.NotNull(serviceClient);
    }

    [Fact]
    public async Task Should_Reject_Requests_Without_Authorization_Header()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act - Try to access endpoint without auth
        var response = await httpClient.GetAsync($"{ServiceUrl}/");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.WwwAuthenticate.Any());
    }

    [Fact]
    public async Task Should_Accept_Requests_With_Valid_Authorization_Header()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

        // Act - Try to access endpoint with auth
        var response = await httpClient.GetAsync($"{ServiceUrl}/");

        // Assert - Should not return 401 Unauthorized (may redirect with 302 Found)
        Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Found,
            $"Expected success or redirect, but got {response.StatusCode}");
    }

    [Fact]
    public async Task Should_Reject_Requests_With_Invalid_Authorization_Header()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer wrong-token");

        // Act - Try to access endpoint with wrong token
        var response = await httpClient.GetAsync($"{ServiceUrl}/");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Should_Connect_With_WCF_Channel_Using_Custom_Header()
    {
        // Arrange - Create WCF channel with custom endpoint behavior for auth
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647,
            SendTimeout = TimeSpan.FromMinutes(20),
            ReceiveTimeout = TimeSpan.FromMinutes(20)
        };

        var endpoint = new EndpointAddress($"{ServiceUrl}/XRMServices/2011/Organization.svc");
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);

        // Add custom endpoint behavior to include Authorization header
        // Note: This is a workaround for testing; real ServiceClient handles this differently
        factory.Endpoint.EndpointBehaviors.Add(new AuthHeaderEndpointBehavior(AccessToken));

        // Act
        var service = factory.CreateChannel();

        // Assert - Create an entity to verify connection works
        var account = new Entity("account") { ["name"] = "Test with Auth" };
        var accountId = service.Create(account);
        Assert.NotEqual(Guid.Empty, accountId);

        // Cleanup
        ((IDisposable)service).Dispose();
    }
}

/// <summary>
/// Custom endpoint behavior to add Authorization header to WCF requests
/// </summary>
public class AuthHeaderEndpointBehavior : System.ServiceModel.Description.IEndpointBehavior
{
    private readonly string _accessToken;

    public AuthHeaderEndpointBehavior(string accessToken)
    {
        _accessToken = accessToken;
    }

    public void AddBindingParameters(System.ServiceModel.Description.ServiceEndpoint endpoint, 
        System.ServiceModel.Channels.BindingParameterCollection bindingParameters) { }

    public void ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, 
        System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
    {
        clientRuntime.ClientMessageInspectors.Add(new AuthHeaderMessageInspector(_accessToken));
    }

    public void ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, 
        System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher) { }

    public void Validate(System.ServiceModel.Description.ServiceEndpoint endpoint) { }
}

/// <summary>
/// Message inspector to add Authorization header to outgoing WCF requests
/// </summary>
public class AuthHeaderMessageInspector : System.ServiceModel.Dispatcher.IClientMessageInspector
{
    private readonly string _accessToken;

    public AuthHeaderMessageInspector(string accessToken)
    {
        _accessToken = accessToken;
    }

    public object? BeforeSendRequest(ref System.ServiceModel.Channels.Message request, 
        System.ServiceModel.IClientChannel channel)
    {
        var httpRequestPropertyName = System.ServiceModel.Channels.HttpRequestMessageProperty.Name;
        
        System.ServiceModel.Channels.HttpRequestMessageProperty? httpRequest = null;
        if (request.Properties.ContainsKey(httpRequestPropertyName))
        {
            httpRequest = request.Properties[httpRequestPropertyName] 
                as System.ServiceModel.Channels.HttpRequestMessageProperty;
        }

        if (httpRequest == null)
        {
            httpRequest = new System.ServiceModel.Channels.HttpRequestMessageProperty();
            request.Properties.Add(httpRequestPropertyName, httpRequest);
        }

        httpRequest.Headers["Authorization"] = $"Bearer {_accessToken}";
        return null;
    }

    public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object? correlationState) { }
}
#endif
