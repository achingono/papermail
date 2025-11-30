using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data.Models;
using Papermail.Data.Services;
using Papermail.Web.Services;
using System.Security.Claims;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for the Sent Items folder.
/// </summary>
[Authorize]
public class SentModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly IEmailPrefetchQueue _prefetchQueue;

    public SentModel(IEmailService emailService, IEmailPrefetchQueue prefetchQueue)
    {
        _emailService = emailService;
        _prefetchQueue = prefetchQueue;
    }

    public List<EmailItemModel> Items { get; private set; } = new();
    
    public string FolderName => "Sent Items";
    public int CurrentPage { get; private set; }
    public int PageSize { get; private set; }
    public string SortDirection { get; private set; } = "desc";
    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync(int page = 0, int pageSize = 50, string sort = "desc")
    {
        CurrentPage = page;
        PageSize = pageSize;
        SortDirection = sort;
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            Items = await _emailService.GetSentAsync(userId, page, pageSize);
            TotalCount = await _emailService.GetSentCountAsync(userId);
            var nextPage = page + 1;
            if (nextPage < TotalPages)
                _prefetchQueue.Enqueue(userId, "sent", nextPage, 2, pageSize);
        }
        else
        {
            TotalCount = 0;
        }
    }
}
