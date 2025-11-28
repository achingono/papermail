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
    private readonly ILogger<TokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for accessing account data.</param>
    /// <param name="_dataProtectionProvider">The data protection provider for encrypting and decrypting tokens.</param>
    /// <param name="smtpOptions">SMTP configuration settings for fallback authentication.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public TokenService(DataContext dbContext, IDataProtectionProvider _dataProtectionProvider, IOptions<SmtpSettings> smtpOptions, ILogger<TokenService> logger)
    {
        _dbContext = dbContext;
        _protector = _dataProtectionProvider.CreateProtector("RefreshTokens");
        _smtpSettings = smtpOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the decrypted access token for a user if it hasn't expired.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The decrypted access token if valid; otherwise, null.</returns>
    public async Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("GetAccessTokenAsync called for user {UserId}", userId);
        
        var account = await _dbContext.Accounts.SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken: ct);
        if (account == null)
        {
            _logger.LogWarning("Account not found for user {UserId}", userId);
            return null;
        }
        
        if (account.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Access token expired for user {UserId}, expired at {ExpiresAt}", userId, account.ExpiresAt);
            return null;
        }

        if (string.IsNullOrWhiteSpace(account.AccessToken))
        {
            _logger.LogWarning("Access token is empty for user {UserId}", userId);
            return null;
        }
        
        _logger.LogInformation("Successfully retrieved access token for user {UserId}", userId);
        return _protector.Unprotect(account.AccessToken);
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
        _logger.LogDebug("GetCredentialsAsync called for user {UserId}", userId);
        
        var account = await _dbContext.Accounts.SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken: ct);
        if (account == null)
        {
            _logger.LogWarning("Account not found for user {UserId}", userId);
            return (null, null);
        }

        // Try OAuth token first
        if (!string.IsNullOrWhiteSpace(account.AccessToken) && account.ExpiresAt > DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Successfully retrieved OAuth credentials for user {UserId}, email {Email}", userId, account.EmailAddress);
            return (account.EmailAddress, _protector.Unprotect(account.AccessToken));
        }

        // Fall back to SMTP configuration credentials for basic auth
        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username) && 
            !string.IsNullOrWhiteSpace(_smtpSettings.Password))
        {
            _logger.LogWarning("OAuth token not available or expired for user {UserId}, falling back to SMTP configuration credentials", userId);
            // Use account email and configured SMTP password
            return (account.EmailAddress, _smtpSettings.Password);
        }

        _logger.LogWarning("No valid credentials available for user {UserId}", userId);
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
        _logger.LogDebug("GetRefreshTokenAsync called for user {UserId}", userId);
        
        var account = await _dbContext.Accounts.SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken: ct);
        if (account == null)
        {
            _logger.LogWarning("Account not found for user {UserId}", userId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(account.RefreshToken))
        {
            _logger.LogWarning("Refresh token is empty for user {UserId}", userId);
            return null;
        }
        
        _logger.LogInformation("Successfully retrieved refresh token for user {UserId}", userId);
        return _protector.Unprotect(account.RefreshToken);
    }

    /// <summary>
    /// Encrypts a token using Data Protection for secure storage.
    /// </summary>
    /// <param name="token">The token to encrypt.</param>
    /// <returns>The encrypted token string.</returns>
    public string ProtectToken(string token)
    {
        _logger.LogDebug("ProtectToken called, token length: {TokenLength}", token?.Length ?? 0);
        var protectedToken = _protector.Protect(token);
        _logger.LogDebug("Token successfully encrypted, protected token length: {ProtectedTokenLength}", protectedToken.Length);
        return protectedToken;
    }
}