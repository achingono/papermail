using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Infrastructure.Authentication;

namespace PaperMail.Web.Pages.OAuth;

public class CallbackModel : PageModel
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(IOAuthService oauthService, ILogger<CallbackModel> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? error)
    {
        // Handle OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth error: {Error}", error);
            ErrorMessage = $"Authentication failed: {error}";
            return Page();
        }

        // If no code, start OAuth flow
        if (string.IsNullOrEmpty(code))
        {
            var (authUrl, codeVerifier, stateValue) = _oauthService.GetAuthorizationUrl();
            
            // Store PKCE parameters in session
            HttpContext.Session.SetString("CodeVerifier", codeVerifier);
            HttpContext.Session.SetString("State", stateValue);
            
            return Redirect(authUrl);
        }

        // Validate state to prevent CSRF
        var expectedState = HttpContext.Session.GetString("State");
        if (state != expectedState)
        {
            _logger.LogWarning("State mismatch: expected {Expected}, got {Actual}", expectedState, state);
            ErrorMessage = "Invalid state parameter. Please try again.";
            return Page();
        }

        try
        {
            // Exchange code for tokens
            var codeVerifier = HttpContext.Session.GetString("CodeVerifier");
            if (string.IsNullOrEmpty(codeVerifier))
            {
                ErrorMessage = "Session expired. Please try again.";
                return Page();
            }

            var tokens = await _oauthService.ExchangeCodeForTokensAsync(code, codeVerifier);
            
            // Get user email from token (decode JWT id_token to get email claim)
            // For simplicity, we'll use the username from the OIDC provider
            // In production, decode the id_token JWT to get the email claim
            var userId = "admin@papermail.local"; // TODO: Extract from id_token JWT
            
            // Store tokens
            await _oauthService.StoreUserTokensAsync(userId, tokens);
            
            // Set user session
            HttpContext.Session.SetString("UserId", userId);
            HttpContext.Session.SetString("AccessToken", tokens.AccessToken);
            
            // Clear PKCE parameters
            HttpContext.Session.Remove("CodeVerifier");
            HttpContext.Session.Remove("State");
            
            _logger.LogInformation("User {UserId} authenticated successfully", userId);
            
            return RedirectToPage("/Inbox");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth callback");
            ErrorMessage = "An error occurred during authentication. Please try again.";
            return Page();
        }
    }
}
