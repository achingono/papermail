using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Services;
using Papermail.Web.Services;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for the Inbox folder, lists recent emails.
/// </summary>
[Authorize]
public class DeletedModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly IEmailPrefetchQueue _prefetchQueue;

    /// <summary>
    /// The items to render in the inbox list.
    /// </summary>
    public List<EmailItemModel> Items { get; private set; } = new();
    
    public string FolderName => "Deleted";
    public int CurrentPage { get; private set; }
    public int PageSize { get; private set; }
    public string SortDirection { get; private set; } = "desc";
    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public DeletedModel(IEmailService emailService, IEmailPrefetchQueue prefetchQueue)
    {
        _emailService = emailService;
        _prefetchQueue = prefetchQueue;
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
            Items = await _emailService.GetDeletedAsync(userId, page, pageSize);
            TotalCount = await _emailService.GetDeletedCountAsync(userId);
            var nextPage = page + 1;
            if (nextPage < TotalPages)
                _prefetchQueue.Enqueue(userId, "deleted", nextPage, 1, pageSize);
        }
        catch (InvalidOperationException)
        {
            // No account/credentials configured yet; show empty inbox
            Items = new List<EmailItemModel>();
            TotalCount = 0;
        }
    }
}
