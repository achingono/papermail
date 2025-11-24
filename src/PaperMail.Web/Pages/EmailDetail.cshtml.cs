using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Application.DTOs;
using PaperMail.Application.Services;

namespace PaperMail.Web.Pages;

public class EmailDetailModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailDetailModel> _logger;

    public EmailDetailModel(IEmailService emailService, ILogger<EmailDetailModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public EmailDetailDto? Email { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Email = await _emailService.GetEmailByIdAsync(id);
            
            if (Email == null)
            {
                return NotFound();
            }

            // Mark as read when viewing
            if (!Email.IsRead)
            {
                await _emailService.MarkAsReadAsync(id);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email {EmailId}", id);
            return RedirectToPage("/Error");
        }
    }
}
