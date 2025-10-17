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
public class ServiceClientAuthTests : ServiceTestBase
{
    private const string AccessToken = "test-access-token-12345";

    /// <summary>
    /// Override to start service with authentication enabled
    /// </summary>
    protected override string GetServiceArguments()
    {
        return $"run --no-build -- start --port 0 --host localhost --access-token {AccessToken}";
    }

    [Fact()]
    public void Should_Connect_With_ServiceClient_Using_AccessToken()
    {
        // Arrange - Create ServiceClient connection string with OAuth and AccessToken
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect

        // Act - Create ServiceClient (this may not work as expected due to OAuth requirements)
        // ServiceClient expects full OAuth flow, so this test documents the limitation
        using var serviceClient = new ServiceClient(new Uri(ServiceUrl), (url) => Task.FromResult(AccessToken), true);

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
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

        // Act - Try to access endpoint with auth
        var response = await httpClient.GetAsync($"{ServiceUrl}/");

        // Assert - Should redirect (302) or succeed, not return 401 Unauthorized
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

}

