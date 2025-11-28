namespace Papermail.Data.Services;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Papermail.Core.Entities;

/// <summary>
/// Provides account management services including user account creation and retrieval.
/// </summary>
public class AccountService : IAccountService
{
    private readonly DataContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="context">The database context for account operations.</param>
    public AccountService(DataContext context)
    {
        _context = context;
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
            throw new ArgumentException("The ClaimsPrincipal does not contain a valid 'sub' claim.");
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == sub);

        if (account == null && createIfNotExists)
        {
            var identityProvider = principal.Claims
                            .FirstOrDefault(c => c.Type == "idp")?.Value;
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Name == identityProvider);

            if (provider == null)
            {
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
        }

        return account!;
    }
}