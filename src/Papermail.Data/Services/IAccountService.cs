using System.Security.Claims;
using Papermail.Core.Entities;

/// <summary>
/// Defines account management operations for user authentication and account creation.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Ensures an account exists for the authenticated user, creating one if needed.
    /// </summary>
    /// <param name="principal">The claims principal containing user information.</param>
    /// <param name="updateAccount">An action to update account properties.</param>
    /// <param name="createIfNotExists">Whether to create the account if it doesn't exist.</param>
    /// <returns>The existing or newly created account.</returns>
    Task<Account> EnsureAccountAsync(ClaimsPrincipal principal, Action<Account> updateAccount, bool createIfNotExists = false);
}