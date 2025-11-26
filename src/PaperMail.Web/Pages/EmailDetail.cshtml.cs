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
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user session found, redirecting to login");
                return RedirectToPage("/oauth/login");
            }

            Email = await _emailService.GetEmailByIdAsync(id, userId);
            
            if (Email == null)
            {
                return NotFound();
            }

            // Mark as read when viewing
            if (!Email.IsRead)
            {
                await _emailService.MarkAsReadAsync(id, userId);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email {EmailId}", id);
            return RedirectToPage("/Error");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid emailId)
    {
        try
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user session found, redirecting to login");
                return RedirectToPage("/oauth/login");
            }

            await _emailService.DeleteEmailAsync(emailId, userId);
            _logger.LogInformation("Email {EmailId} deleted by user {UserId}", emailId, userId);
            return RedirectToPage("/Inbox");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email {EmailId}", emailId);
            return RedirectToPage("/Error");
        }
    }
}
