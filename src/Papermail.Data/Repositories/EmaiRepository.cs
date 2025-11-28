using Papermail.Data.Repositories;
using System.Security.Authentication;
using EmailEntity = Papermail.Core.Entities.Email;
using Papermail.Data.Clients;
using Papermail.Data.Services;
using Microsoft.Extensions.Logging;

namespace Papermail.Data.Repositories;

/// <summary>
/// Repository implementation for email operations, orchestrating IMAP and SMTP client calls.
/// </summary>
public sealed class EmailRepository : IEmailRepository
{
    private readonly IImapClient _imapClient;
    private readonly ISmtpClient _smtpClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<EmailRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailRepository"/> class.
    /// </summary>
    /// <param name="imapClient">The IMAP client for email fetching operations.</param>
    /// <param name="smtpClient">The SMTP client for email sending operations.</param>
    /// <param name="tokenService">The token service for retrieving OAuth credentials.</param>
    /// <param name="logger">The logger for logging warnings and errors.</param>
    public EmailRepository(
        IImapClient imapClient,
        ISmtpClient smtpClient,
        ITokenService tokenService,
        ILogger<EmailRepository> logger)
    {
        _imapClient = imapClient ?? throw new ArgumentNullException(nameof(imapClient));
        _smtpClient = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a specific email by its unique identifier via IMAP.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The email if found; otherwise, null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<EmailEntity?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        try
        {
            return await _imapClient.GetEmailByIdAsync(credentials.Username, credentials.AccessToken, id, ct);
        }
        catch (AuthenticationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves inbox emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            // Authentication failed even after fallback; surface empty inbox rather than hard failure.
            return Array.Empty<EmailEntity>();
        }
    }

    /// <summary>
    /// Retrieves sent emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetSentAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchSentEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            return Array.Empty<EmailEntity>();
        }
    }

    /// <summary>
    /// Retrieves draft emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetDraftsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchDraftEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            return Array.Empty<EmailEntity>();
        }
    }

    /// <summary>
    /// Marks an email as read via IMAP.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.MarkReadAsync(credentials.Username, credentials.AccessToken, id, ct);
    }

    /// <summary>
    /// Saves an email as a draft via IMAP.
    /// </summary>
    /// <param name="draft">The email to save as a draft.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task SaveDraftAsync(EmailEntity draft, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.SaveDraftAsync(credentials.Username, credentials.AccessToken, draft, ct);
    }

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    /// <param name="email">The email to send.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task SendEmailAsync(EmailEntity email, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        // Send via SMTP
        await _smtpClient.SendEmailAsync(credentials.Username, credentials.AccessToken, email, ct);
        
        // Save to Sent folder via IMAP after successful send
        try
        {
            await _imapClient.SaveToSentAsync(credentials.Username, credentials.AccessToken, email, ct);
        }
        catch (Exception ex)
        {
            // Log but don't fail the send operation if saving to Sent fails
            _logger.LogWarning(ex, "Failed to save email to Sent folder for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Deletes an email via IMAP.
    /// </summary>
    /// <param name="id">The unique identifier of the email.</param>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.DeleteAsync(credentials.Username, credentials.AccessToken, id, ct);
    }


    /// <summary>
    /// Retrieves junk emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetJunkAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchJunkEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            return Array.Empty<EmailEntity>();
        }
    }

    /// <summary>
    /// Retrieves archived emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetArchiveAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchArchiveEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            return Array.Empty<EmailEntity>();
        }
    }

    /// <summary>
    /// Retrieves deleted emails with pagination via IMAP.
    /// </summary>
    /// <param name="userId">The user ID for authentication.</param>
    /// <param name="page">The page number (0-based).</param>
    /// <param name="pageSize">The number of emails per page.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only collection of emails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are unavailable.</exception>
    public async Task<IReadOnlyCollection<EmailEntity>> GetDeletedAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchDeletedEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            return Array.Empty<EmailEntity>();
        }
    }

    public async Task<int> GetInboxCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        try
        {
            return await _imapClient.GetInboxCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task<int> GetSentCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        try
        {
            return await _imapClient.GetSentCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task<int> GetDraftsCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        try
        {
            return await _imapClient.GetDraftsCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task<int> GetDeletedCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        try
        {
            return await _imapClient.GetDeletedCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task<int> GetJunkCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        try
        {
            return await _imapClient.GetJunkCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task<int> GetArchiveCountAsync(string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        try
        {
            return await _imapClient.GetArchiveCountAsync(credentials.Username, credentials.AccessToken, ct);
        }
        catch (AuthenticationException)
        {
            return 0;
        }
    }

    public async Task MoveToArchiveAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        
        await _imapClient.MoveToArchiveAsync(credentials.Username, credentials.AccessToken, id, ct);
    }

    public async Task MoveToJunkAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);
        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");
        
        await _imapClient.MoveToJunkAsync(credentials.Username, credentials.AccessToken, id, ct);
    }
}