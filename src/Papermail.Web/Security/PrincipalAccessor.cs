using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace Papermail.Web.Security;

/// <summary>
/// Provides access to the current principal (user) from the HTTP context.
/// </summary>
public class PrincipalAccessor : IPrincipalAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor used to retrieve the current user.</param>
    public PrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current principal (user) from the HTTP context.
    /// </summary>
    public IPrincipal? Principal => httpContextAccessor?.HttpContext?.User;

}