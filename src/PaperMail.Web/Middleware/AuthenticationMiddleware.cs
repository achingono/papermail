using PaperMail.Infrastructure.Authentication;

namespace PaperMail.Web.Middleware;

/// <summary>
/// Middleware to ensure user is authenticated before accessing protected pages.
/// Redirects to OAuth login if not authenticated.
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/index",
        "/privacy",
        "/error",
        "/oauth/login",
        "/oauth/callback",
        "/oauth/logout"
    };

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOAuthService oauthService)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip authentication for public paths and static files
        if (IsPublicPath(path) || IsStaticFile(path))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        var userId = context.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User not authenticated, redirecting to login");
            context.Response.Redirect("/oauth/login");
            return;
        }

        // Verify user has valid refresh token
        var refreshToken = await oauthService.GetUserRefreshTokenAsync(userId);
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("User {UserId} has no refresh token, redirecting to login", userId);
            context.Session.Remove("UserId");
            context.Response.Redirect("/oauth/login");
            return;
        }

        // User is authenticated, continue
        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        return PublicPaths.Contains(path) || path.StartsWith("/oauth/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStaticFile(string path)
    {
        return path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }
}
