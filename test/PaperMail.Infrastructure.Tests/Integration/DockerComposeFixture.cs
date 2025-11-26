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
    public int ImapPort => 993;  // IMAPS (IMAP with SSL)
    public int SmtpPort => 587;  // SMTP with STARTTLS
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

        Console.WriteLine("Starting docker-compose mail service for integration tests...");
        
        // Start only the mail service for integration tests
        await RunDockerComposeAsync("up", "-d", "mail");
        
        Console.WriteLine("Waiting for mail server to be ready...");
        
        // Wait for both SMTP and IMAP ports to be ready
        // docker-mailserver can take 60-90 seconds to fully initialize
        // We need to wait for the port AND verify the service responds correctly
        await WaitForSmtpReadyAsync(TimeSpan.FromSeconds(120));
        // For IMAPS (993), just check port availability since it requires SSL handshake
        await WaitForPortAsync(ImapPort, "IMAPS", TimeSpan.FromSeconds(120));
        
        Console.WriteLine("Mail server is ready for integration tests");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping docker-compose mail service...");
        // Stop and remove containers
        await RunDockerComposeAsync("down", "-v");
        Console.WriteLine("Docker cleanup complete");
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

    private async Task WaitForSmtpReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        var attempt = 0;
        var pollInterval = TimeSpan.FromSeconds(3);
        
        Console.WriteLine($"Waiting for SMTP service on port {SmtpPort} (timeout: {timeout.TotalSeconds}s)...");
        
        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                await client.ConnectAsync(MailHost, SmtpPort);
                
                // Read the SMTP greeting to verify service is actually ready
                using var stream = client.GetStream();
                using var reader = new System.IO.StreamReader(stream);
                var greeting = await reader.ReadLineAsync();
                
                if (!string.IsNullOrEmpty(greeting) && greeting.StartsWith("220"))
                {
                    Console.WriteLine($"✓ SMTP service is ready (attempt {attempt}): {greeting}");
                    return;
                }
            }
            catch
            {
                // Log every 10th attempt
                if (attempt % 10 == 0)
                {
                    Console.WriteLine($"  Still waiting for SMTP... (attempt {attempt}, {(deadline - DateTime.UtcNow).TotalSeconds:F0}s remaining)");
                }
            }
            
            var remainingTime = deadline - DateTime.UtcNow;
            if (remainingTime > TimeSpan.Zero)
            {
                await Task.Delay(remainingTime < pollInterval ? remainingTime : pollInterval);
            }
        }

        throw new TimeoutException(
            $"SMTP service did not become ready within {timeout.TotalSeconds} seconds after {attempt} attempts");
    }

    private async Task WaitForPortAsync(int port, string serviceName, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        var attempt = 0;
        var pollInterval = TimeSpan.FromSeconds(3);
        
        Console.WriteLine($"Waiting for {serviceName} on port {port} (timeout: {timeout.TotalSeconds}s)...");
        
        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                await client.ConnectAsync(MailHost, port);
                Console.WriteLine($"✓ {serviceName} port {port} is ready (attempt {attempt})");
                return;
            }
            catch
            {
                if (attempt % 10 == 0)
                {
                    Console.WriteLine($"  Still waiting for {serviceName}... (attempt {attempt}, {(deadline - DateTime.UtcNow).TotalSeconds:F0}s remaining)");
                }
            }
            
            var remainingTime = deadline - DateTime.UtcNow;
            if (remainingTime > TimeSpan.Zero)
            {
                await Task.Delay(remainingTime < pollInterval ? remainingTime : pollInterval);
            }
        }

        throw new TimeoutException(
            $"{serviceName} port {port} did not become available within {timeout.TotalSeconds} seconds after {attempt} attempts");
    }
}

[CollectionDefinition("DockerCompose")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
