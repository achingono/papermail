using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace Papermail.Web.Security;

/// <summary>
/// Defines a contract for accessing the current principal (user) in the HTTP context.
/// </summary>
public interface IPrincipalAccessor
{
    /// <summary>
    /// Gets the current principal (user) from the HTTP context.
    /// </summary>
    IPrincipal? Principal { get; }
}