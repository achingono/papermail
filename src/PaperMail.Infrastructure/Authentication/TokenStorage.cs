using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using PaperMail.Core.Interfaces;
using System.Text;

namespace PaperMail.Infrastructure.Authentication;

public sealed class TokenStorage : ITokenStorage
{
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IDistributedCache _cache;
    private const string ProtectorPurpose = "RefreshTokens";

    public TokenStorage(IDataProtectionProvider dataProtection, IDistributedCache cache)
    {
        _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task StoreTokenAsync(string userId, string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new ArgumentException("Token required", nameof(refreshToken));

        var protector = _dataProtection.CreateProtector(ProtectorPurpose);
        var encryptedToken = protector.Protect(refreshToken);

        await _cache.SetStringAsync(
            GetCacheKey(userId),
            encryptedToken,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(90)
            },
            ct);
    }

    public async Task<string?> GetTokenAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));

        var encryptedToken = await _cache.GetStringAsync(GetCacheKey(userId), ct);
        if (string.IsNullOrEmpty(encryptedToken)) return null;

        var protector = _dataProtection.CreateProtector(ProtectorPurpose);
        return protector.Unprotect(encryptedToken);
    }

    public async Task StoreAccessTokenAsync(string userId, string accessToken, int expiresIn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token required", nameof(accessToken));

        var protector = _dataProtection.CreateProtector("AccessTokens");
        var encryptedToken = protector.Protect(accessToken);

        await _cache.SetStringAsync(
            GetAccessTokenCacheKey(userId),
            encryptedToken,
            new DistributedCacheEntryOptions
            {
                // Set expiration slightly before actual token expiration to allow for refresh
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(expiresIn - 60, 60))
            },
            ct);
    }

    public async Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));

        var encryptedToken = await _cache.GetStringAsync(GetAccessTokenCacheKey(userId), ct);
        if (string.IsNullOrEmpty(encryptedToken)) return null;

        var protector = _dataProtection.CreateProtector("AccessTokens");
        return protector.Unprotect(encryptedToken);
    }

    public async Task RevokeTokenAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));
        await _cache.RemoveAsync(GetCacheKey(userId), ct);
        await _cache.RemoveAsync(GetAccessTokenCacheKey(userId), ct);
    }

    private static string GetCacheKey(string userId) => $"token:{userId}";
    private static string GetAccessTokenCacheKey(string userId) => $"access_token:{userId}";
}
