using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Infrastructure.Authentication;

namespace PaperMail.Web.Pages.OAuth;

public class LogoutModel : PageModel
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(IOAuthService oauthService, ILogger<LogoutModel> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Revoke tokens
            try
            {
                var refreshToken = await _oauthService.GetUserRefreshTokenAsync(userId);
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _oauthService.StoreUserTokensAsync(userId, new OAuthTokenResponse());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking tokens for user {UserId}", userId);
            }
            
            _logger.LogInformation("User {UserId} logged out", userId);
        }
        
        // Clear session
        HttpContext.Session.Clear();
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await OnGetAsync();
    }
}
