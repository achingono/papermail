using PaperMail.Application.DTOs;

namespace PaperMail.Application.Services;

/// <summary>
/// Application service for email operations.
/// Orchestrates repository calls and DTO mapping.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Retrieves inbox emails with pagination.
    /// </summary>
    Task<List<EmailListItemDto>> GetInboxAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves a single email by ID.
    /// </summary>
    Task<EmailDetailDto?> GetEmailByIdAsync(Guid emailId, string userId);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    Task MarkAsReadAsync(Guid emailId, string userId);

    /// <summary>
    /// Saves a draft email.
    /// </summary>
    Task<Guid> SaveDraftAsync(ComposeEmailRequest request, string userId);

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    Task<Guid> SendEmailAsync(ComposeEmailRequest request, string userId);

    /// <summary>
    /// Deletes an email by ID.
    /// </summary>
    Task DeleteEmailAsync(Guid emailId, string userId);
}
