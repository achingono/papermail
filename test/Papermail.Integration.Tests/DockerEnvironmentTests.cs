using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Papermail.Integration.Tests;

/// <summary>
/// Integration tests that verify the Docker Compose environment is properly configured.
/// These tests should run against a running Docker environment.
/// Set the environment variable RUN_DOCKER_TESTS=true to enable these tests.
/// </summary>
[Collection("Docker")]
public class DockerEnvironmentTests
{
    private readonly bool _runTests;
    private const string BaseUrl = "https://papermail.local";
    private const string OidcUrl = "https://oidc.papermail.local";

    public DockerEnvironmentTests()
    {
        _runTests = Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS") == "true";
    }

    [Fact]
    public async Task WebApplication_IsAccessible()
    {
        if (!_runTests) return;

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync(BaseUrl);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected OK or Redirect, got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task OidcProvider_IsAccessible()
    {
        if (!_runTests) return;

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{OidcUrl}/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var config = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(config);
        Assert.True(config.ContainsKey("issuer"));
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        if (!_runTests) return;

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{BaseUrl}/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SmtpServer_IsReachable()
    {
        if (!_runTests) return;

        using var tcpClient = new System.Net.Sockets.TcpClient();
        
        try
        {
            await tcpClient.ConnectAsync("localhost", 587);
            Assert.True(tcpClient.Connected, "SMTP server should be reachable on port 587");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to connect to SMTP server: {ex.Message}");
        }
    }

    [Fact]
    public async Task ImapServer_IsReachable()
    {
        if (!_runTests) return;

        using var tcpClient = new System.Net.Sockets.TcpClient();
        
        try
        {
            await tcpClient.ConnectAsync("localhost", 143);
            Assert.True(tcpClient.Connected, "IMAP server should be reachable on port 143");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to connect to IMAP server: {ex.Message}");
        }
    }

    [Fact]
    public async Task SqlServer_IsReachable()
    {
        if (!_runTests) return;

        using var tcpClient = new System.Net.Sockets.TcpClient();
        
        try
        {
            await tcpClient.ConnectAsync("localhost", 1433);
            Assert.True(tcpClient.Connected, "SQL Server should be reachable on port 1433");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to connect to SQL Server: {ex.Message}");
        }
    }
}

[CollectionDefinition("Docker")]
public class DockerCollection : ICollectionFixture<DockerEnvironmentFixture>
{
}

public class DockerEnvironmentFixture : IDisposable
{
    public DockerEnvironmentFixture()
    {
        // Fixture for shared Docker environment setup if needed
        // Can add logic to start Docker Compose if not running
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}
