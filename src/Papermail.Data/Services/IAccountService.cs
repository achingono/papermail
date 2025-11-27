using System.Security.Claims;
using Papermail.Core.Entities;

public interface IAccountService
{
    Task<Account> EnsureAccountAsync(ClaimsPrincipal principal, Action<Account> updateAccount, bool createIfNotExists = false);
}