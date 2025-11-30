using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Papermail.Core.Entities;
using Papermail.Data.Services;

namespace Papermail.Web.Controllers;

/// <summary>
/// Handles web authentication, user creation, and cookie-based authentication.
/// </summary>
[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AuthController> _logger;
    private const string ProtectorPurpose = "RefreshTokens";

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="securityTokenGenerator">The security token generator for creating JWT tokens.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public AuthController(IAccountService accountService, ILogger<AuthController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Friendly page for access denied or user consent refusal.
    /// </summary>
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return new ContentResult
        {
            Content = "Access was denied by the identity provider. Please try again and accept the requested permissions, or contact support if the problem persists.",
            ContentType = "text/plain",
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    /// <summary>
    /// Handles authentication for a specified scheme, creates or loads the user, and sets authentication cookie.
    /// </summary>
    /// <param name="scheme">The authentication scheme to use (google, microsoft, apple, facebook, openidconnect).</param>
    /// <param name="dbContext">The database context for accessing user data.</param>
    /// <param name="hostNameAccessor">The host name accessor for tenant resolution.</param>
    [HttpGet("{scheme}")]
    public async Task Get([FromRoute] string scheme,
    [FromServices] ITokenService tokenService)
    {
        var auth = await Request.HttpContext.AuthenticateAsync(scheme);

        if (!auth.Succeeded
            || auth?.Principal == null
            || !auth.Principal.Identities.Any(id => id.IsAuthenticated)
            || string.IsNullOrEmpty(auth.Properties.GetTokenValue("access_token")))
        {
            // Store the source URL in authentication properties before challenging
            var redirect = Request.Query.Where(x => x.Key == "Redirect").Select(x => x.Value.ToString()).FirstOrDefault();
            var sourceUrl = string.IsNullOrEmpty(redirect) ? "/" : redirect;

            var properties = new AuthenticationProperties
            {
                RedirectUri = Request.GetUri().ToString(),
                Items = { ["source_url"] = sourceUrl }
            }; await Request.HttpContext.ChallengeAsync(scheme);
            await Request.HttpContext.ChallengeAsync(scheme, properties);
        }
        else
        {
            // Get the source URL from the authentication properties stored during challenge
            var sourceUrl = auth.Properties?.Items.TryGetValue("source_url", out var storedUrl) == true
                ? storedUrl
                : "/";
            try
            {
                // Create or load the user from the database
                var account = await _accountService.EnsureAccountAsync(auth.Principal, (Account account) =>
                {
                    var expiresAt = auth.Properties?.GetTokenValue("expires_at");
                    var refreshToken = auth.Properties?.GetTokenValue("refresh_token");

                    account.AccessToken = auth.Properties?.GetTokenValue("access_token");
                    account.RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ?
                                            string.Empty : 
                                            tokenService.ProtectToken(refreshToken);
                    account.ExpiresAt = string.IsNullOrWhiteSpace(expiresAt) ?
                                            DateTimeOffset.UtcNow.AddHours(1) : 
                                            DateTimeOffset.Parse(expiresAt);
                    account.Scopes = auth.Properties?.GetTokenValue("scopes")?.Split(' ') ?? [];
                }, true);
                if (account == null)
                {
                    _logger.LogWarning("External auth succeeded but no user could be created (missing email)");
                    var builder = new UriBuilder("/")
                    {
                        Path = "/login",
                        Query = "error=authentication_failed"
                    };

                    Response.Redirect(builder.ToString(), permanent: false);
                    return;
                }

                // Use LocalRedirect or Redirect with proper cookie handling
                // The cookie should be set by SignInAsync above
                Response.Redirect(sourceUrl!, permanent: false);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication process for scheme {Scheme}", scheme);

                // Get the source URL from the authentication properties or use default
                var builder = new UriBuilder("/")
                {
                    Path = "/login",
                    Query = "error=authentication_failed"
                };

                Response.Redirect(builder.ToString(), permanent: false);
                return;
            }
        }
    }

    /// <summary>
    /// Handles logout by signing out of both cookie and OIDC authentication.
    /// </summary>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Sign out of cookie authentication
        await HttpContext.SignOutAsync("Cookies");
        
        // Sign out of OpenID Connect which will redirect to OIDC provider's logout
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "OpenIdConnect");
    }
}