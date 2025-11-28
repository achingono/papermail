using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Services;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for the Inbox folder, lists recent emails.
/// </summary>
[Authorize]
public class InboxModel : PageModel
{
    private readonly IEmailService _emailService;

    /// <summary>
    /// The items to render in the inbox list.
    /// </summary>
    public List<EmailItemModel> Items { get; private set; } = new();

    public InboxModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task OnGetAsync(int page = 0, int pageSize = 50)
    {
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Not authenticated or no user ID available
            Items = new List<EmailItemModel>();
            return;
        }

        try
        {
            Items = await _emailService.GetInboxAsync(userId, page, pageSize);
        }
        catch (InvalidOperationException)
        {
            // No account/credentials configured yet; show empty inbox
            Items = new List<EmailItemModel>();
        }
    }
}
