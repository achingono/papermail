using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PaperMail.Application.DTOs;
using PaperMail.Application.Services;
using PaperMail.Application.Validators;

namespace PaperMail.Web.Pages;

public class ComposeModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ComposeModel> _logger;

    public ComposeModel(IEmailService emailService, ILogger<ComposeModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public ComposeEmailRequest EmailRequest { get; set; } = new();

    public List<string> ValidationErrors { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        // Initialize empty form
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        try
        {
            // Get userId from session
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/OAuth/Login");
            }

            // Parse comma-separated recipients from form input
            if (!string.IsNullOrWhiteSpace(EmailRequest.To?.FirstOrDefault()))
            {
                var recipientInput = EmailRequest.To.First();
                EmailRequest.To = recipientInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            // Validate request
            var validationResult = ComposeValidator.Validate(EmailRequest);
            if (!validationResult.IsValid)
            {
                ValidationErrors = validationResult.Errors;
                return Page();
            }

            if (action == "draft")
            {
                // Save draft
                var draftId = await _emailService.SaveDraftAsync(EmailRequest, userId);
                _logger.LogInformation("Draft saved: {DraftId}", draftId);
                return RedirectToPage("/Inbox");
            }
            else if (action == "send")
            {
                // Send email via SMTP
                var emailId = await _emailService.SendEmailAsync(EmailRequest, userId);
                _logger.LogInformation("Email sent: {EmailId}", emailId);
                return RedirectToPage("/Inbox");
            }

            return RedirectToPage("/Inbox");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error composing email");
            ErrorMessage = "An error occurred while processing your request.";
            return Page();
        }
    }
}
