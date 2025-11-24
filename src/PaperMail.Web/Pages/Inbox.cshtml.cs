using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Application.DTOs;
using PaperMail.Application.Services;

namespace PaperMail.Web.Pages;

public class InboxModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<InboxModel> _logger;
    private const int PageSize = 50;

    public InboxModel(IEmailService emailService, ILogger<InboxModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public List<EmailListItemDto> Emails { get; private set; } = new();
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }

    public async Task OnGetAsync(int page = 0)
    {
        try
        {
            // TODO: Get actual user ID from authentication
            var userId = "current-user";
            
            CurrentPage = Math.Max(0, page);
            Emails = await _emailService.GetInboxAsync(userId, CurrentPage, PageSize);
            
            // TODO: Get total count from repository for accurate pagination
            TotalPages = Emails.Count == PageSize ? CurrentPage + 2 : CurrentPage + 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inbox");
            Emails = new List<EmailListItemDto>();
        }
    }
}
