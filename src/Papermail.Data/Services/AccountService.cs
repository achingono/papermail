namespace Papermail.Data.Services;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Papermail.Core.Entities;

public class AccountService : IAccountService
{
    private readonly DataContext _context;

    public AccountService(DataContext context)
    {
        _context = context;
    }

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