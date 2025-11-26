using Papermail.Core.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Papermail.Web.Security;

public interface IUserLocator
{
    /// <summary>
    /// Retrieves the user associated with the specified claims principal.
    /// </summary>
    /// <param name="principal">The claims principal representing the user.</param>
    /// <returns>The user entity associated with the claims principal.</returns>
    Task<User?> GetUserFromClaimsAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Get the user by email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns>A <see cref="User"/> entity with associated email address.</returns>
    Task<User?> GetUserByEmailAsync(string email);
}