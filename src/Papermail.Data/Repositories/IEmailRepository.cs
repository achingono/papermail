using Papermail.Core.Entities;

namespace Papermail.Data.Repositories;

/// <summary>
/// Defines repository operations for email management, orchestrating IMAP and SMTP operations.
/// </summary>
public interface IEmailRepository
{
    /// <summary>
    /// Retrieves a specific email by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The email if found; otherwise, null.</returns>
    Task<Email?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves inbox emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves sent emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetSentAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetDraftsAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    
    /// <summary>
    /// Marks an email as read.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Saves an email as a draft.
    /// </summary>
    /// <param name="draft">The email to save as a draft.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SaveDraftAsync(Email draft, string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    /// <param name="email">The email to send.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SendEmailAsync(Email email, string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes an email by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task DeleteAsync(Guid id, string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves junk emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetJunkAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves archived emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetArchiveAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves deleted emails with pagination.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    Task<IReadOnlyCollection<Email>> GetDeletedAsync(string userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Inbox folder.
    /// </summary>
    Task<int> GetInboxCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Sent folder.
    /// </summary>
    Task<int> GetSentCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Drafts folder.
    /// </summary>
    Task<int> GetDraftsCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Deleted folder.
    /// </summary>
    Task<int> GetDeletedCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Junk folder.
    /// </summary>
    Task<int> GetJunkCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Archive folder.
    /// </summary>
    Task<int> GetArchiveCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Moves an email to the Archive folder.
    /// </summary>
    Task MoveToArchiveAsync(Guid id, string userId, CancellationToken ct = default);

    /// <summary>
    /// Moves an email to the Junk folder.
    /// </summary>
    Task MoveToJunkAsync(Guid id, string userId, CancellationToken ct = default);
}