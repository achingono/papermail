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
    public string? ComposeMode { get; private set; } // "reply", "replyall", "forward"

    public async Task<IActionResult> OnGetAsync(Guid? reply, Guid? replyall, Guid? forward)
    {
        try
        {
            // Get userId from session
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/OAuth/Login");
            }

            // Handle reply
            if (reply.HasValue)
            {
                var originalEmail = await _emailService.GetEmailByIdAsync(reply.Value, userId);
                if (originalEmail != null)
                {
                    ComposeMode = "reply";
                    EmailRequest.To = new List<string> { originalEmail.From };
                    EmailRequest.Subject = originalEmail.Subject.StartsWith("Re: ") 
                        ? originalEmail.Subject 
                        : $"Re: {originalEmail.Subject}";
                    EmailRequest.BodyPlain = $"\n\n---\nOn {originalEmail.ReceivedAt:yyyy-MM-dd HH:mm}, {originalEmail.From} wrote:\n{originalEmail.BodyPlain}";
                }
            }
            // Handle reply all
            else if (replyall.HasValue)
            {
                var originalEmail = await _emailService.GetEmailByIdAsync(replyall.Value, userId);
                if (originalEmail != null)
                {
                    ComposeMode = "replyall";
                    // Include original sender + all original recipients
                    var recipients = new List<string> { originalEmail.From };
                    recipients.AddRange(originalEmail.To);
                    EmailRequest.To = recipients.Distinct().ToList();
                    EmailRequest.Subject = originalEmail.Subject.StartsWith("Re: ") 
                        ? originalEmail.Subject 
                        : $"Re: {originalEmail.Subject}";
                    EmailRequest.BodyPlain = $"\n\n---\nOn {originalEmail.ReceivedAt:yyyy-MM-dd HH:mm}, {originalEmail.From} wrote:\n{originalEmail.BodyPlain}";
                }
            }
            // Handle forward
            else if (forward.HasValue)
            {
                var originalEmail = await _emailService.GetEmailByIdAsync(forward.Value, userId);
                if (originalEmail != null)
                {
                    ComposeMode = "forward";
                    EmailRequest.Subject = originalEmail.Subject.StartsWith("Fwd: ") 
                        ? originalEmail.Subject 
                        : $"Fwd: {originalEmail.Subject}";
                    EmailRequest.BodyPlain = $"\n\n---\nForwarded message:\nFrom: {originalEmail.From}\nDate: {originalEmail.ReceivedAt:yyyy-MM-dd HH:mm}\nSubject: {originalEmail.Subject}\nTo: {string.Join(", ", originalEmail.To)}\n\n{originalEmail.BodyPlain}";
                }
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing compose form");
            ErrorMessage = "An error occurred while loading the email.";
            return Page();
        }
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
