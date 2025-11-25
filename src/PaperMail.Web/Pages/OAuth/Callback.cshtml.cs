using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Infrastructure.Authentication;
using System.IdentityModel.Tokens.Jwt;

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
            _logger.LogInformation("Auth URL generated: {AuthUrl}", authUrl);
            
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
            
            // Extract email from id_token JWT
            var handler = new JwtSecurityTokenHandler();
            var idToken = handler.ReadJwtToken(tokens.IdToken);
            
            _logger.LogInformation("ID Token claims: {Claims}", 
                string.Join(", ", idToken.Claims.Select(c => $"{c.Type}={c.Value}")));
            
            var emailClaim = idToken.Claims.FirstOrDefault(c => c.Type == "email");
            
            if (emailClaim == null)
            {
                _logger.LogWarning("Email claim not found. Available claims: {Claims}",
                    string.Join(", ", idToken.Claims.Select(c => c.Type)));
                ErrorMessage = "Email claim not found in token. Please contact support.";
                return Page();
            }
            
            var userId = emailClaim.Value;
            _logger.LogInformation("Extracted user email from id_token: {Email}", userId);
            
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
