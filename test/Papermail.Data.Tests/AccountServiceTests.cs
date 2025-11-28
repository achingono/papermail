using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

public class AccountServiceTests
{
    private readonly DataContext _context;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _mockLogger = new Mock<ILogger<AccountService>>();
        _service = new AccountService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task EnsureAccountAsync_WhenAccountExists_ReturnsExistingAccount()
    {
        var provider = new Provider { Name = "TestProvider" };
        var account = new Account
        {
            UserId = "user-123",
            EmailAddress = "user@test.com",
            Provider = provider
        };
        _context.Providers.Add(provider);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var claims = new[] { new Claim("sub", "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await _service.EnsureAccountAsync(principal, a => { }, createIfNotExists: false);

        Assert.NotNull(result);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("user@test.com", result.EmailAddress);
    }

    [Fact]
    public async Task EnsureAccountAsync_WhenAccountDoesNotExist_CreatesNewAccount()
    {
        var claims = new[]
        {
            new Claim("sub", "new-user-456"),
            new Claim(ClaimTypes.Email, "newuser@test.com"),
            new Claim("idp", "Google")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await _service.EnsureAccountAsync(
            principal, 
            a => a.RefreshToken = "token", 
            createIfNotExists: true
        );

        Assert.NotNull(result);
        Assert.Equal("new-user-456", result.UserId);
        Assert.Equal("newuser@test.com", result.EmailAddress);
        Assert.Equal("token", result.RefreshToken);

        var savedAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == "new-user-456");
        Assert.NotNull(savedAccount);
    }

    [Fact]
    public async Task EnsureAccountAsync_WithNoSubClaim_ThrowsArgumentException()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "user@test.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EnsureAccountAsync(principal, a => { }, createIfNotExists: false)
        );
    }

    [Fact]
    public async Task EnsureAccountAsync_CreatesProviderIfNotExists()
    {
        var claims = new[]
        {
            new Claim("sub", "user-789"),
            new Claim(ClaimTypes.Email, "user@test.com"),
            new Claim("idp", "NewProvider")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await _service.EnsureAccountAsync(principal, a => { }, createIfNotExists: true);

        Assert.NotNull(result);
        var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Name == "NewProvider");
        Assert.NotNull(provider);
        Assert.Equal(provider.Id, result.Provider.Id);
    }

    [Fact]
    public async Task EnsureAccountAsync_WhenCreateIfNotExistsFalse_ReturnsNullForNewUser()
    {
        var claims = new[]
        {
            new Claim("sub", "nonexistent-user"),
            new Claim(ClaimTypes.Email, "user@test.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await _service.EnsureAccountAsync(principal, a => { }, createIfNotExists: false);

        Assert.Null(result);
    }
}
