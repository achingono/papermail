using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for logout functionality.
/// </summary>
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        // Sign out of cookie authentication
        await HttpContext.SignOutAsync("Cookies");
        
        // Sign out of OpenID Connect which will redirect to OIDC provider's logout
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "OpenIdConnect");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await OnGetAsync();
    }
}
