using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Services;
using System.ComponentModel.DataAnnotations;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for composing and sending emails.
/// </summary>
[Authorize]
public class ComposeModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ComposeModel> _logger;

    [BindProperty]
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string To { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(200, ErrorMessage = "Subject cannot be longer than 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Message body is required")]
    public string Body { get; set; } = string.Empty;

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public ComposeModel(IEmailService emailService, ILogger<ComposeModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public void OnGet()
    {
        // Just render the form
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            ErrorMessage = "User not authenticated";
            return Page();
        }

        var draft = new DraftModel
        {
            To = new List<string> { To },
            Subject = Subject,
            BodyPlain = Body
        };

        try
        {
            if (action == "send")
            {
                await _emailService.SendEmailAsync(draft, userId);
                Success = true;
                _logger.LogInformation("Email sent successfully from {UserId} to {To}", userId, To);
                
                // Clear form
                To = string.Empty;
                Subject = string.Empty;
                Body = string.Empty;
                ModelState.Clear();
            }
            else if (action == "draft")
            {
                await _emailService.SaveDraftAsync(draft, userId);
                Success = true;
                ErrorMessage = "Draft saved successfully";
                _logger.LogInformation("Draft saved for {UserId}", userId);
            }

            return Page();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to send email for user {UserId}", userId);
            ErrorMessage = "Unable to send email. Please ensure your account is properly configured.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email for user {UserId}", userId);
            ErrorMessage = "An error occurred while sending the email. Please try again.";
            return Page();
        }
    }
}
