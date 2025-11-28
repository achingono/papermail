using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Services;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for the Junk folder, lists recent emails.
/// </summary>
[Authorize]
public class JunkModel : PageModel
{
    private readonly IEmailService _emailService;

    /// <summary>
    /// The items to render in the junk list.
    /// </summary>
    public List<EmailItemModel> Items { get; private set; } = new();
    
    public string FolderName => "Junk Mail";
    public int CurrentPage { get; private set; }
    public int PageSize { get; private set; }
    public string SortDirection { get; private set; } = "desc";
    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public JunkModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task OnGetAsync(int page = 0, int pageSize = 50, string sort = "desc")
    {
        CurrentPage = page;
        PageSize = pageSize;
        SortDirection = sort;
        
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Not authenticated or no user ID available
            Items = new List<EmailItemModel>();
            TotalCount = 0;
            return;
        }

        try
        {
            Items = await _emailService.GetJunkAsync(userId, page, pageSize);
            TotalCount = await _emailService.GetJunkCountAsync(userId);
        }
        catch (InvalidOperationException)
        {
            // No account/credentials configured yet; show empty junk
            Items = new List<EmailItemModel>();
            TotalCount = 0;
        }
    }
}
