using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Web.Services;

namespace Papermail.Web.Tests;

public class TokenServiceTests
{
    private readonly DataContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly Mock<SmtpSettings> _mockSmtpSettings;
    private readonly Mock<ILogger<TokenService>> _mockLogger;
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);

        // Create real Data Protection provider for testing
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();

        _mockSmtpSettings = new Mock<SmtpSettings>();
        _mockSmtpSettings.Setup(o => o).Returns(new SmtpSettings
        {
            Username = "smtp-user",
            Password = "smtp-password"
        });

        _mockLogger = new Mock<ILogger<TokenService>>();

        _service = new TokenService(
            _context,
            _dataProtectionProvider,
            _mockSmtpSettings.Object,
            _mockLogger.Object
        );
    }


    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenValid_ReturnsDecryptedToken()
    {
        var userId = "user-123";
        var rawToken = "my-access-token";
        var encryptedToken = _service.ProtectToken(rawToken);
        
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = "user@test.com",
            Provider = provider,
            AccessToken = encryptedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _service.GetAccessTokenAsync(userId);

        Assert.Equal(rawToken, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenExpired_ReturnsNull()
    {
        var userId = "user-123";
        var rawToken = "my-access-token";
        var encryptedToken = _service.ProtectToken(rawToken);
        
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = "user@test.com",
            Provider = provider,
            AccessToken = encryptedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _service.GetAccessTokenAsync(userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenAccountNotFound_ReturnsNull()
    {
        var result = await _service.GetAccessTokenAsync("nonexistent-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCredentialsAsync_WithValidOAuthToken_ReturnsCredentials()
    {
        var userId = "user-123";
        var rawToken = "my-access-token";
        var encryptedToken = _service.ProtectToken(rawToken);
        
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = "user@test.com",
            Provider = provider,
            AccessToken = encryptedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var (username, token) = await _service.GetCredentialsAsync(userId);

        Assert.Equal("user@test.com", username);
        Assert.Equal(rawToken, token);
    }

    [Fact]
    public async Task GetCredentialsAsync_WhenOAuthExpired_FallsBackToSmtp()
    {
        var userId = "user-123";
        var rawToken = "my-access-token";
        var encryptedToken = _service.ProtectToken(rawToken);
        
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = "user@test.com",
            Provider = provider,
            AccessToken = encryptedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var (username, token) = await _service.GetCredentialsAsync(userId);

        Assert.Equal("user@test.com", username);
        Assert.Equal("smtp-password", token);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WhenFound_ReturnsDecryptedToken()
    {
        var userId = "user-123";
        var rawToken = "my-refresh-token";
        var encryptedToken = _service.ProtectToken(rawToken);
        
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = "user@test.com",
            Provider = provider,
            RefreshToken = encryptedToken
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _service.GetRefreshTokenAsync(userId);

        Assert.Equal(rawToken, result);
    }

    [Fact]
    public void ProtectToken_EncryptsToken()
    {
        var rawToken = "raw-token";

        var result = _service.ProtectToken(rawToken);

        Assert.NotNull(result);
        Assert.NotEqual(rawToken, result); // Encrypted should differ from raw
    }
}
