using System.Diagnostics;
using Xunit;

namespace PaperMail.Infrastructure.Tests.Integration;

/// <summary>
/// Fixture that manages docker-compose lifecycle for integration tests.
/// Ensures mail server is running before tests execute.
/// </summary>
public class DockerComposeFixture : IAsyncLifetime
{
    private const string ComposeFile = "docker-compose.yml";
    private const string ProjectName = "papermail-test";
    private readonly string _workingDirectory;

    public DockerComposeFixture()
    {
        // Navigate to repository root (3 levels up from bin/Debug/net8.0)
        var assemblyPath = Path.GetDirectoryName(typeof(DockerComposeFixture).Assembly.Location)!;
        _workingDirectory = Path.GetFullPath(Path.Combine(assemblyPath, "..", "..", "..", "..", ".."));
    }

    public string MailHost => "localhost";
    public int ImapPort => 143;
    public int SmtpPort => 587;
    public string TestUser => "admin@papermail.local";
    public string TestPassword => "P@ssw0rd";

    public async Task InitializeAsync()
    {
        // Check if docker-compose file exists
        var composeFilePath = Path.Combine(_workingDirectory, ComposeFile);
        if (!File.Exists(composeFilePath))
        {
            throw new InvalidOperationException($"docker-compose.yml not found at {composeFilePath}");
        }

        // Start only the mail service for integration tests
        await RunDockerComposeAsync("up", "-d", "mail");
        
        // Wait for mail server to be ready (max 30 seconds)
        await WaitForMailServerAsync(TimeSpan.FromSeconds(30));
    }

    public async Task DisposeAsync()
    {
        // Stop and remove containers
        await RunDockerComposeAsync("down", "-v");
    }

    private async Task RunDockerComposeAsync(params string[] args)
    {
        var allArgs = new[] { "compose", "-p", ProjectName, "-f", ComposeFile }
            .Concat(args)
            .ToArray();

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in allArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker compose");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"docker compose failed: {error}");
        }
    }

    private async Task WaitForMailServerAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                await client.ConnectAsync(MailHost, SmtpPort);
                
                // If connection succeeds, server is ready
                return;
            }
            catch
            {
                // Server not ready yet, wait and retry
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("Mail server did not start within the timeout period");
    }
}

[CollectionDefinition("DockerCompose")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
