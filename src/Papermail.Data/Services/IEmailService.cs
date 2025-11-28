using Papermail.Data.Models;

namespace Papermail.Data.Services;

/// <summary>
/// Application service for email operations.
/// Orchestrates repository calls and DTO mapping.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Retrieves inbox emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetInboxAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves sent emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetSentAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetDraftsAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetDeletedAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetArchiveAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    Task<List<EmailItemModel>> GetJunkAsync(string userId, int page = 0, int pageSize = 50);

    /// <summary>
    /// Retrieves total counts for paging in various folders.
    /// </summary>
    Task<int> GetInboxCountAsync(string userId);
    Task<int> GetSentCountAsync(string userId);
    Task<int> GetDraftsCountAsync(string userId);
    Task<int> GetDeletedCountAsync(string userId);
    Task<int> GetArchiveCountAsync(string userId);
    Task<int> GetJunkCountAsync(string userId);

    /// <summary>
    /// Retrieves a single email by ID.
    /// </summary>
    Task<EmailModel?> GetEmailByIdAsync(Guid emailId, string userId);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    Task MarkAsReadAsync(Guid emailId, string userId);

    /// <summary>
    /// Saves a draft email.
    /// </summary>
    Task<Guid> SaveDraftAsync(DraftModel request, string userId);

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    Task<Guid> SendEmailAsync(DraftModel request, string userId);

    /// <summary>
    /// Deletes an email by ID.
    /// </summary>
    Task DeleteEmailAsync(Guid emailId, string userId);

    /// <summary>
    /// Moves an email to the Archive folder.
    /// </summary>
    Task MoveToArchiveAsync(Guid emailId, string userId);

    /// <summary>
    /// Moves an email to the Junk folder.
    /// </summary>
    Task MoveToJunkAsync(Guid emailId, string userId);
}