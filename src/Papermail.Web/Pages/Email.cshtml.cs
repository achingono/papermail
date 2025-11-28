using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Papermail.Data;
using Papermail.Data.Services;
using Papermail.Web.Models;

namespace Papermail.Web.Pages;

/// <summary>
/// Page model for viewing email details with action handlers.
/// </summary>
[Authorize]
public class EmailModel : PageModel
{
    private readonly IEmailService _emailService;

    public EmailModel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public EmailDetailViewModel ViewModel { get; set; } = new();
    public string ReturnUrl { get; set; } = "/inbox";

    public async Task<IActionResult> OnGetAsync(Guid id, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/inbox";
        
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            var email = await _emailService.GetEmailByIdAsync(id, userId);
            if (email == null)
            {
                return NotFound();
            }

            ViewModel = new EmailDetailViewModel
            {
                Email = email,
                CanReply = true,
                CanReplyAll = email.To.Count > 1 || email.Cc.Count > 0,
                CanForward = true,
                CanDelete = true,
                CanArchive = true,
                CanMarkAsJunk = true
            };

            // Mark as read
            await _emailService.MarkAsReadAsync(id, userId);

            return Page();
        }
        catch (InvalidOperationException)
        {
            // No account/credentials configured
            return RedirectToPage("/Index");
        }
    }

    public IActionResult OnPostReply(Guid id, string? returnUrl = null)
    {
        // TODO: Redirect to compose page with reply context
        return RedirectToPage("/Compose", new { replyTo = id, returnUrl });
    }

    public IActionResult OnPostReplyAll(Guid id, string? returnUrl = null)
    {
        // TODO: Redirect to compose page with reply-all context
        return RedirectToPage("/Compose", new { replyAll = id, returnUrl });
    }

    public IActionResult OnPostForward(Guid id, string? returnUrl = null)
    {
        // TODO: Redirect to compose page with forward context
        return RedirectToPage("/Compose", new { forward = id, returnUrl });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string? returnUrl = null)
    {
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            await _emailService.DeleteEmailAsync(id, userId);
            return Redirect(returnUrl ?? "/inbox");
        }
        catch (InvalidOperationException)
        {
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id, string? returnUrl = null)
    {
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            await _emailService.MoveToArchiveAsync(id, userId);
            return Redirect(returnUrl ?? "/inbox");
        }
        catch (InvalidOperationException)
        {
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostJunkAsync(Guid id, string? returnUrl = null)
    {
        var userId = User.Id();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        try
        {
            await _emailService.MoveToJunkAsync(id, userId);
            return Redirect(returnUrl ?? "/inbox");
        }
        catch (InvalidOperationException)
        {
            return RedirectToPage("/Index");
        }
    }
}
