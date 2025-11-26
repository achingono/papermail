using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace PaperMail.EndToEnd.Tests.Docker;

/// <summary>
/// Manages docker-compose environment lifecycle for UI tests.
/// Starts all services (proxy, client, mail, oidc) and waits for them to be ready.
/// </summary>
public sealed class DockerEnvironmentFixture : IAsyncLifetime
{
    private readonly string _projectDirectory;
    
    public DockerEnvironmentFixture()
    {
        // Navigate up from test assembly to repository root
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Cannot determine assembly directory");
        _projectDirectory = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
    }

    /// <summary>
    /// Base URL for accessing the PaperMail web application through nginx proxy
    /// </summary>
    public string BaseUrl { get; } = "https://papermail.local";

    /// <summary>
    /// OIDC provider URL for authentication
    /// </summary>
    public string OidcUrl { get; } = "https://oidc.papermail.local";

    /// <summary>
    /// Test user credentials from docker/oidc/users.json
    /// </summary>
    public (string Email, string Password) AdminUser { get; } = ("admin@papermail.local", "P@ssw0rd");
    public (string Email, string Password) RegularUser { get; } = ("user@papermail.local", "P@ssw0rd");

    /// <summary>
    /// Mail server connection details
    /// </summary>
    public string MailHost { get; } = "localhost";
    public int ImapPort { get; } = 993;
    public int SmtpPort { get; } = 587;

    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting docker-compose environment for UI tests...");
        
        // Build images first to ensure latest code
        await RunDockerComposeAsync("build", timeoutSeconds: 180);
        
        // Start all services
        await RunDockerComposeAsync("up -d", timeoutSeconds: 60);
        
        // Wait for services to be ready
        await WaitForServicesAsync();
        
        Console.WriteLine("Docker environment ready for UI tests");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping docker-compose environment...");
        await RunDockerComposeAsync("down -v", timeoutSeconds: 30);
    }

    private async Task RunDockerComposeAsync(string arguments, int timeoutSeconds = 30)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose {arguments}",
            WorkingDirectory = _projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start docker compose");
        
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        
        var completedInTime = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));
        
        if (!completedInTime)
        {
            process.Kill();
            throw new TimeoutException($"docker compose {arguments} timed out after {timeoutSeconds} seconds");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker compose {arguments} failed with exit code {process.ExitCode}\nOutput: {output}\nError: {error}");
        }

        Console.WriteLine($"docker compose {arguments} completed successfully");
    }

    private async Task WaitForServicesAsync()
    {
        var tasks = new List<Task>
        {
            WaitForHttpServiceAsync(BaseUrl, "PaperMail Web Application", timeoutSeconds: 60),
            WaitForHttpServiceAsync(OidcUrl, "OIDC Provider", timeoutSeconds: 30),
            WaitForTcpPortAsync(MailHost, SmtpPort, "SMTP Server", timeoutSeconds: 30),
            WaitForTcpPortAsync(MailHost, ImapPort, "IMAP Server", timeoutSeconds: 30)
        };

        await Task.WhenAll(tasks);
    }

    private async Task WaitForHttpServiceAsync(string url, string serviceName, int timeoutSeconds = 30)
    {
        Console.WriteLine($"Waiting for {serviceName} at {url}...");
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Accept self-signed certs
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    Console.WriteLine($"{serviceName} is ready");
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Service not ready yet, continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"{serviceName} did not become ready within {timeoutSeconds} seconds");
    }

    private async Task WaitForTcpPortAsync(string host, int port, string serviceName, int timeoutSeconds = 30)
    {
        Console.WriteLine($"Waiting for {serviceName} on {host}:{port}...");
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                Console.WriteLine($"{serviceName} is ready");
                return;
            }
            catch (SocketException)
            {
                // Port not open yet, continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"{serviceName} port {port} did not become available within {timeoutSeconds} seconds");
    }
}

[CollectionDefinition(nameof(DockerEnvironmentCollection))]
public sealed class DockerEnvironmentCollection : ICollectionFixture<DockerEnvironmentFixture> { }
