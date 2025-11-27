using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Papermail.Data;
using Papermail.Data.Services;

namespace Papermail.Web.Services;

public class TokenService : ITokenService
{
    private readonly DataContext _dbContext;
    private readonly IDataProtector _protector;

    public TokenService(DataContext dbContext, IDataProtectionProvider _dataProtectionProvider)
    {
        _dbContext = dbContext;
        _protector = _dataProtectionProvider.CreateProtector("RefreshTokens");
    }

    public async Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { userId }, cancellationToken: ct);
        if (account == null || account.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(account.AccessToken) ? null : _protector.Unprotect(account.AccessToken);
    }

    public async Task<(string? Username, string? AccessToken)> GetCredentialsAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken: ct);
        if (account == null || account.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return (null, null);
        }

        return string.IsNullOrWhiteSpace(account.AccessToken) ?
                    (null, null) : 
                    (account.EmailAddress, _protector.Unprotect(account.AccessToken));
    }

    public async Task<string?> GetRefreshTokenAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { userId }, cancellationToken: ct);
        if (account == null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(account.RefreshToken) ? null : _protector.Unprotect(account.RefreshToken);
    }

    public string ProtectToken(string token)
    {
        return _protector.Protect(token);
    }
}