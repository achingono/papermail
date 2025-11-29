namespace Papermail.Data.Services;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papermail.Core.Configuration;
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
        var userId = principal.Id();
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Missing sub claim", nameof(principal));

        var existing = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (existing != null)
            return existing;

        if (!createIfNotExists)
            return null!;

        var email = principal.Email();
        var identityProvider = principal.Claims.FirstOrDefault(c => c.Type == "idp")?.Value;
        var domain = email?.Split('@').LastOrDefault() ?? "unknown.local";

        // Resolve provider (by idp name first, then domain)
        var providerService = new ProviderService(_context); // TODO: inject
        var provider = await providerService.GetOrCreateProviderAsync(domain, identityProvider, new ImapSettings(), new SmtpSettings());
        if (!string.IsNullOrWhiteSpace(identityProvider) && provider.Name != identityProvider)
        {
            provider.Name = identityProvider!;
            await _context.SaveChangesAsync();
        }

        var account = new Account
        {
            UserId = userId,
            EmailAddress = email ?? string.Empty,
            Provider = provider,
            ProviderId = provider.Id
        };

        updateAccount(account);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }
}