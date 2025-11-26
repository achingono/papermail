using System.Net;
using Xunit;

namespace PaperMail.EndToEnd.Tests.Docker;

/// <summary>
/// Tests to validate the docker-compose environment is properly configured and all services are accessible.
/// These tests are skipped by default and require docker-compose to be running.
/// </summary>
[Collection(nameof(DockerEnvironmentCollection))]
public class DockerEnvironmentTests
{
    private readonly DockerEnvironmentFixture _fixture;

    public DockerEnvironmentTests(DockerEnvironmentFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProxyService_ShouldBeAccessible()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync(_fixture.BaseUrl);
        
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"Expected success or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task OidcService_ShouldBeAccessible()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        // OIDC provider should have a discovery endpoint
        var response = await client.GetAsync($"{_fixture.OidcUrl}/.well-known/openid-configuration");
        
        Assert.True(response.IsSuccessStatusCode, 
            $"OIDC discovery endpoint should be accessible, got {response.StatusCode}");
    }

    [Fact]
    public async Task ClientService_ShouldRedirectToLogin()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            AllowAutoRedirect = false
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync(_fixture.BaseUrl);
        
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || 
                   response.StatusCode == HttpStatusCode.Found ||
                   response.StatusCode == HttpStatusCode.OK,
            $"Expected redirect to login or OK, got {response.StatusCode}");
    }

    [Fact]
    public async Task MailServer_SmtpPort_ShouldBeListening()
    {
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(_fixture.MailHost, _fixture.SmtpPort);
        
        Assert.True(client.Connected, "SMTP port should be accessible");
    }

    [Fact]
    public async Task MailServer_ImapPort_ShouldBeListening()
    {
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(_fixture.MailHost, _fixture.ImapPort);
        
        Assert.True(client.Connected, "IMAP port should be accessible");
    }

    [Fact]
    public async Task OidcConfiguration_ShouldContainExpectedEndpoints()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{_fixture.OidcUrl}/.well-known/openid-configuration");
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("authorization_endpoint", content);
        Assert.Contains("token_endpoint", content);
        Assert.Contains("userinfo_endpoint", content);
    }

    [Fact]
    public async Task AllServices_ShouldBeHealthy()
    {
        var healthChecks = new List<Task<bool>>
        {
            CheckHttpServiceHealth(_fixture.BaseUrl, "PaperMail Web"),
            CheckHttpServiceHealth(_fixture.OidcUrl, "OIDC Provider"),
            CheckTcpServiceHealth(_fixture.MailHost, _fixture.SmtpPort, "SMTP"),
            CheckTcpServiceHealth(_fixture.MailHost, _fixture.ImapPort, "IMAP")
        };

        var results = await Task.WhenAll(healthChecks);
        
        Assert.All(results, healthy => Assert.True(healthy, "All services should be healthy"));
    }

    private async Task<bool> CheckHttpServiceHealth(string url, string serviceName)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            
            var response = await client.GetAsync(url);
            return response.StatusCode != HttpStatusCode.ServiceUnavailable;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckTcpServiceHealth(string host, int port, string serviceName)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(host, port);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
