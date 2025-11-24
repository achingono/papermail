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

    public async Task RevokeTokenAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID required", nameof(userId));
        await _cache.RemoveAsync(GetCacheKey(userId), ct);
    }

    private static string GetCacheKey(string userId) => $"token:{userId}";
}
