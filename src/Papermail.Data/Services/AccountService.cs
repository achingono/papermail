namespace Papermail.Data.Services;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papermail.Core.Entities;

/// <summary>
/// Provides account management services including user account creation and retrieval.
/// </summary>
public class AccountService : IAccountService
{
    private readonly DataContext _context;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="context">The database context for account operations.</param>
    /// <param name="logger">The logger for logging account operations.</param>
    public AccountService(DataContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ensures an account exists for the authenticated user, creating one if needed.
    /// </summary>
    /// <param name="principal">The claims principal containing user information.</param>
    /// <param name="updateAccount">An action to update account properties with OAuth tokens.</param>
    /// <param name="createIfNotExists">Whether to create the account if it doesn't exist.</param>
    /// <returns>The existing or newly created account.</returns>
    /// <exception cref="ArgumentException">Thrown when the principal doesn't contain a valid 'sub' claim.</exception>
    public async Task<Account> EnsureAccountAsync(ClaimsPrincipal principal, Action<Account> updateAccount, bool createIfNotExists = false)
    {
        var sub = principal.Id();
        if (string.IsNullOrWhiteSpace(sub))
        {
            _logger.LogWarning("EnsureAccountAsync called with principal missing 'sub' claim");
            throw new ArgumentException("The ClaimsPrincipal does not contain a valid 'sub' claim.");
        }

        _logger.LogDebug("Ensuring account exists for user {UserId}", sub);
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == sub);

        if (account == null && createIfNotExists)
        {
            _logger.LogInformation("Account not found for user {UserId}, creating new account", sub);
            var identityProvider = principal.Claims
                            .FirstOrDefault(c => c.Type == "idp")?.Value;
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Name == identityProvider);

            if (provider == null)
            {
                _logger.LogInformation("Provider {ProviderName} not found, creating new provider", identityProvider ?? "Unknown");
                provider = new Provider
                {
                    Name = identityProvider ?? "Unknown"
                };
                _context.Providers.Add(provider);
                await _context.SaveChangesAsync();
            }

            account = new Account
            {
                UserId = sub,
                Provider = provider, // Set appropriate provider ID
                EmailAddress = principal.Email(),
            };

            updateAccount(account);

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new account for user {UserId} with email {Email}", sub, account.EmailAddress);
        }
        else if (account != null)
        {
            _logger.LogDebug("Account found for user {UserId}", sub);
        }
        else
        {
            _logger.LogWarning("Account not found for user {UserId} and createIfNotExists is false", sub);
        }

        return account!;
    }
}