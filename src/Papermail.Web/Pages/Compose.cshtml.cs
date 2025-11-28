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

    public async Task<IActionResult> OnGetAsync(Guid? replyTo, Guid? replyAll, Guid? forward)
    {
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            ErrorMessage = "User not authenticated";
            return Page();
        }

        // Determine action and load source email if provided
        var sourceId = replyTo ?? replyAll ?? forward;
        if (sourceId.HasValue)
        {
            var source = await _emailService.GetEmailByIdAsync(sourceId.Value, userId);
            if (source == null)
            {
                ErrorMessage = "Original email not found";
                return Page();
            }

            if (forward.HasValue)
            {
                // Forward: empty To, Subject = Fwd:, Body = original content quoted
                To = string.Empty;
                Subject = $"Fwd: {source.Subject}";
                Body = BuildForwardBody(source);
            }
            else
            {
                // Reply / Reply All
                var toList = new List<string>();
                if (source.From != null && !string.IsNullOrWhiteSpace(source.From.Address))
                {
                    toList.Add(source.From.Address);
                }

                // ReplyAll adds original To and Cc (excluding current user to avoid loops)
                if (replyAll.HasValue)
                {
                    void addAddresses(IEnumerable<Papermail.Data.Models.EmailAddressModel>? addrs)
                    {
                        if (addrs == null) return;
                        foreach (var a in addrs)
                        {
                            var addr = a.Address;
                            if (!string.IsNullOrWhiteSpace(addr) && !string.Equals(addr, userId, StringComparison.OrdinalIgnoreCase))
                                toList.Add(addr);
                        }
                    }
                    addAddresses(source.To);
                    addAddresses(source.Cc);
                }

                // De-duplicate
                To = string.Join(", ", toList.Distinct(StringComparer.OrdinalIgnoreCase));
                Subject = source.Subject?.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) == true
                    ? source.Subject!
                    : $"Re: {source.Subject}";
                Body = BuildReplyBody(source);
            }
        }

        return Page();
    }

    private string BuildReplyBody(Papermail.Data.Models.EmailModel source)
    {
        var from = source.From?.Address ?? "";
        var date = source.Date.ToString("yyyy-MM-dd HH:mm");
        var subj = source.Subject ?? "";
        var header = $"\n\nOn {date}, {from} wrote:\n";
        var content = !string.IsNullOrWhiteSpace(source.Body)
            ? source.Body
            : (!string.IsNullOrWhiteSpace(source.HtmlBody) ? StripHtml(source.HtmlBody) : "");
        var quoted = string.Join("\n", content.Split('\n').Select(l => "> " + l));
        return header + quoted;
    }

    private string BuildForwardBody(Papermail.Data.Models.EmailModel source)
    {
        var from = source.From?.Address ?? "";
        var to = string.Join(", ", (source.To ?? new List<Papermail.Data.Models.EmailAddressModel>()).Select(a => a.Address));
        var cc = string.Join(", ", (source.Cc ?? new List<Papermail.Data.Models.EmailAddressModel>()).Select(a => a.Address));
        var date = source.Date.ToString("yyyy-MM-dd HH:mm");
        var subj = source.Subject ?? "";
        var header = $"\n\n---------- Forwarded message ---------\nFrom: {from}\nDate: {date}\nSubject: {subj}\nTo: {to}\n" + (string.IsNullOrEmpty(cc) ? string.Empty : $"Cc: {cc}\n") + "\n";
        var content = !string.IsNullOrWhiteSpace(source.Body)
            ? source.Body
            : (!string.IsNullOrWhiteSpace(source.HtmlBody) ? StripHtml(source.HtmlBody) : "");
        return header + content;
    }

    // Minimal HTML stripper for quoting; avoids bringing HTML tags into plain text compose
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        // Replace common breaks with newlines
        html = html.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        // Remove tags
        var withoutTags = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);
        // Decode entities
        return System.Net.WebUtility.HtmlDecode(withoutTags);
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
