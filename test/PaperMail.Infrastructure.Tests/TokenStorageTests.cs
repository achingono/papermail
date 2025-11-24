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
}
