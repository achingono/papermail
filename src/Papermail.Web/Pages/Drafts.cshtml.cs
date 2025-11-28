using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data.Models;
using Papermail.Data.Services;
using System.Security.Claims;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for the Drafts folder.
/// </summary>
[Authorize]
public class DraftsModel : PageModel
{
    private readonly IEmailService _emailService;

    public DraftsModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public List<EmailItemModel> Items { get; private set; } = new();

    public async Task OnGetAsync(int page = 0, int pageSize = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            Items = await _emailService.GetDraftsAsync(userId, page, pageSize);
        }
    }
}
