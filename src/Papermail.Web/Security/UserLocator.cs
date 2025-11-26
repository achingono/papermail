using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Papermail.Core.Entities;
using Papermail.Data;

namespace Papermail.Web.Security;

/// <summary>
/// Provides methods to locate and retrieve user entities from the database based on claims or email.
/// </summary>
public class UserLocator : IUserLocator
{
    private readonly Papermail.Data.Context _entityContext;

    /// <summary>
    /// Constructor accepting an instance of the EntityFramework data context
    /// </summary>
    /// <param name="entityContext">the EntityFramework data context</param>
    public UserLocator(Papermail.Data.Context entityContext)
    {
        _entityContext = entityContext;
    }

    /// <summary>
    /// Get the user by email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns>A <see cref="User"/> entity with associated email address.</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return await _entityContext.Users
            .SingleOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Get the user from the claims.
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    public async Task<User?> GetUserFromClaimsAsync(ClaimsPrincipal principal)
    {
        var id = principal.Id();
        var email = principal.Email();

        return await _entityContext.Users.SingleOrDefaultAsync(x =>
                    x.Id == id ||
                    x.Email.ToLower() == email.ToLower());
    }
}