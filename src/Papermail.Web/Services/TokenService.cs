using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Papermail.Core.Configuration;
using Papermail.Data;
using Papermail.Data.Services;

namespace Papermail.Web.Services;

/// <summary>
/// Provides token management services including encryption, decryption, and retrieval of OAuth tokens.
/// Uses ASP.NET Core Data Protection for secure token storage.
/// </summary>
public class TokenService : ITokenService
{
    private readonly DataContext _dbContext;
    private readonly IDataProtector _protector;
    private readonly SmtpSettings _smtpSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for accessing account data.</param>
    /// <param name="_dataProtectionProvider">The data protection provider for encrypting and decrypting tokens.</param>
    /// <param name="smtpOptions">SMTP configuration settings for fallback authentication.</param>
    public TokenService(DataContext dbContext, IDataProtectionProvider _dataProtectionProvider, IOptions<SmtpSettings> smtpOptions)
    {
        _dbContext = dbContext;
        _protector = _dataProtectionProvider.CreateProtector("RefreshTokens");
        _smtpSettings = smtpOptions.Value;
    }

    /// <summary>
    /// Retrieves the decrypted access token for a user if it hasn't expired.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The decrypted access token if valid; otherwise, null.</returns>
    public async Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { userId }, cancellationToken: ct);
        if (account == null || account.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(account.AccessToken) ? null : _protector.Unprotect(account.AccessToken);
    }

    /// <summary>
    /// Retrieves the username (email address) and decrypted access token for a user.
    /// Falls back to SMTP configuration credentials if OAuth tokens are not available.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A tuple containing the username and access token/password if valid; otherwise, (null, null).</returns>
    public async Task<(string? Username, string? AccessToken)> GetCredentialsAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken: ct);
        if (account == null)
        {
            return (null, null);
        }

        // Try OAuth token first
        if (!string.IsNullOrWhiteSpace(account.AccessToken) && account.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return (account.EmailAddress, _protector.Unprotect(account.AccessToken));
        }

        // Fall back to SMTP configuration credentials for basic auth
        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username) && 
            !string.IsNullOrWhiteSpace(_smtpSettings.Password))
        {
            // Use account email and configured SMTP password
            return (account.EmailAddress, _smtpSettings.Password);
        }

        return (null, null);
    }

    /// <summary>
    /// Retrieves the decrypted refresh token for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The decrypted refresh token if found; otherwise, null.</returns>
    public async Task<string?> GetRefreshTokenAsync(string userId, CancellationToken ct = default)
    {
        var account = await _dbContext.Accounts.FindAsync(new object[] { userId }, cancellationToken: ct);
        if (account == null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(account.RefreshToken) ? null : _protector.Unprotect(account.RefreshToken);
    }

    /// <summary>
    /// Encrypts a token using Data Protection for secure storage.
    /// </summary>
    /// <param name="token">The token to encrypt.</param>
    /// <returns>The encrypted token string.</returns>
    public string ProtectToken(string token)
    {
        return _protector.Protect(token);
    }
}