namespace Papermail.Data.Clients;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

/// <summary>
/// Defines IMAP client operations for fetching and managing email messages.
/// </summary>
public interface IImapClient
{
    /// <summary>
    /// Fetches a range of email messages from the inbox.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default);
    
    /// <summary>
    /// Fetches a range of email messages from the Sent folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchSentEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default);
    
    /// <summary>
    /// Fetches a range of email messages from the Drafts folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchDraftEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default);
    
    /// <summary>
    /// Saves an email message as a draft in the Drafts folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="draft">The email message to save as a draft.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SaveDraftAsync(string username, string accessToken, Email draft, CancellationToken ct = default);
    
    /// <summary>
    /// Saves an email message to the Sent folder after successful SMTP send.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="email">The email message to save to Sent folder.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SaveToSentAsync(string username, string accessToken, Email email, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves a specific email message by its unique identifier.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The email message if found; otherwise, null.</returns>
    Task<Email?> GetEmailByIdAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
    
    /// <summary>
    /// Marks an email message as read by setting the Seen flag.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to mark as read.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task MarkReadAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes an email message by marking it as deleted and expunging it.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to delete.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task DeleteAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
    
    /// <summary>
    /// Fetches a range of email messages from the Archive folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchArchiveEmailsAsync(string username, string accessToken, int skip, int pageSize, CancellationToken ct);
    
    /// <summary>
    /// Fetches a range of email messages from the Deleted folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchDeletedEmailsAsync(string username, string accessToken, int skip, int pageSize, CancellationToken ct);
    
    /// <summary>
    /// Fetches a range of email messages from the Junk folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    Task<IEnumerable<Email>> FetchJunkEmailsAsync(string username, string accessToken, int skip, int pageSize, CancellationToken ct);

    /// <summary>
    /// Gets total message count in the Inbox folder.
    /// </summary>
    Task<int> GetInboxCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Sent folder.
    /// </summary>
    Task<int> GetSentCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Drafts folder.
    /// </summary>
    Task<int> GetDraftsCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Deleted folder.
    /// </summary>
    Task<int> GetDeletedCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Junk folder.
    /// </summary>
    Task<int> GetJunkCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Gets total message count in the Archive folder.
    /// </summary>
    Task<int> GetArchiveCountAsync(string username, string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Moves an email to the Archive folder.
    /// </summary>
    Task MoveToArchiveAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);

    /// <summary>
    /// Moves an email to the Junk folder.
    /// </summary>
    Task MoveToJunkAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
}