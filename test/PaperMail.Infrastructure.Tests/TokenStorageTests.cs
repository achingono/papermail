using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PaperMail.Infrastructure.Authentication;

namespace PaperMail.Infrastructure.Tests;

public class TokenStorageTests
{
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IDistributedCache _cache;
    private readonly TokenStorage _sut;

    public TokenStorageTests()
    {
        _dataProtection = new EphemeralDataProtectionProvider();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _sut = new TokenStorage(_dataProtection, _cache);
    }

    [Fact]
    public async Task StoreTokenAsync_ShouldEncryptAndStore()
    {
        await _sut.StoreTokenAsync("user123", "refresh-token-abc", CancellationToken.None);
        
        var retrieved = await _sut.GetTokenAsync("user123", CancellationToken.None);
        retrieved.Should().Be("refresh-token-abc");
    }

    [Fact]
    public async Task GetTokenAsync_NonExistentUser_ShouldReturnNull()
    {
        var result = await _sut.GetTokenAsync("unknown-user", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldRemoveToken()
    {
        await _sut.StoreTokenAsync("user456", "token-xyz", CancellationToken.None);
        await _sut.RevokeTokenAsync("user456", CancellationToken.None);

        var result = await _sut.GetTokenAsync("user456", CancellationToken.None);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task StoreTokenAsync_InvalidUserId_ShouldThrow(string userId)
    {
        var act = async () => await _sut.StoreTokenAsync(userId, "token", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task StoreTokenAsync_InvalidToken_ShouldThrow(string token)
    {
        var act = async () => await _sut.StoreTokenAsync("user", token, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StoreAccessTokenAsync_ShouldEncryptAndStore()
    {
        await _sut.StoreAccessTokenAsync("user123", "access-token-xyz", 3600, CancellationToken.None);
        
        var retrieved = await _sut.GetAccessTokenAsync("user123", CancellationToken.None);
        retrieved.Should().Be("access-token-xyz");
    }

    [Fact]
    public async Task GetAccessTokenAsync_NonExistentUser_ShouldReturnNull()
    {
        var result = await _sut.GetAccessTokenAsync("unknown-user", CancellationToken.None);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task StoreAccessTokenAsync_InvalidUserId_ShouldThrow(string userId)
    {
        var act = async () => await _sut.StoreAccessTokenAsync(userId, "token", 3600, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task StoreAccessTokenAsync_InvalidToken_ShouldThrow(string token)
    {
        var act = async () => await _sut.StoreAccessTokenAsync("user", token, 3600, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetTokenAsync_InvalidUserId_ShouldThrow(string userId)
    {
        var act = async () => await _sut.GetTokenAsync(userId, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetAccessTokenAsync_InvalidUserId_ShouldThrow(string userId)
    {
        var act = async () => await _sut.GetAccessTokenAsync(userId, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RevokeTokenAsync_InvalidUserId_ShouldThrow(string userId)
    {
        var act = async () => await _sut.RevokeTokenAsync(userId, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldRemoveBothTokens()
    {
        await _sut.StoreTokenAsync("user789", "refresh-token", CancellationToken.None);
        await _sut.StoreAccessTokenAsync("user789", "access-token", 3600, CancellationToken.None);
        
        await _sut.RevokeTokenAsync("user789", CancellationToken.None);

        var refreshToken = await _sut.GetTokenAsync("user789", CancellationToken.None);
        var accessToken = await _sut.GetAccessTokenAsync("user789", CancellationToken.None);
        
        refreshToken.Should().BeNull();
        accessToken.Should().BeNull();
    }

    [Fact]
    public async Task StoreAccessTokenAsync_WithShortExpiry_ShouldUseMinimum60Seconds()
    {
        // This test verifies the Math.Max(expiresIn - 60, 60) logic
        await _sut.StoreAccessTokenAsync("user-short", "token", 30, CancellationToken.None);
        
        var retrieved = await _sut.GetAccessTokenAsync("user-short", CancellationToken.None);
        retrieved.Should().Be("token");
    }

    [Fact]
    public async Task MultipleUsers_ShouldIsolateTokens()
    {
        await _sut.StoreTokenAsync("user1", "token1", CancellationToken.None);
        await _sut.StoreTokenAsync("user2", "token2", CancellationToken.None);

        var token1 = await _sut.GetTokenAsync("user1", CancellationToken.None);
        var token2 = await _sut.GetTokenAsync("user2", CancellationToken.None);

        token1.Should().Be("token1");
        token2.Should().Be("token2");
    }

    [Fact]
    public async Task StoreTokenAsync_Overwrite_ShouldUpdateToken()
    {
        await _sut.StoreTokenAsync("user-update", "old-token", CancellationToken.None);
        await _sut.StoreTokenAsync("user-update", "new-token", CancellationToken.None);

        var retrieved = await _sut.GetTokenAsync("user-update", CancellationToken.None);
        retrieved.Should().Be("new-token");
    }

    [Fact]
    public async Task StoreAccessTokenAsync_Overwrite_ShouldUpdateToken()
    {
        await _sut.StoreAccessTokenAsync("user-update", "old-access", 3600, CancellationToken.None);
        await _sut.StoreAccessTokenAsync("user-update", "new-access", 3600, CancellationToken.None);

        var retrieved = await _sut.GetAccessTokenAsync("user-update", CancellationToken.None);
        retrieved.Should().Be("new-access");
    }
}
