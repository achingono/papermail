using Papermail.Data.Mappers;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Papermail.Data.Services;

/// <summary>
/// Email service implementation.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailRepository _emailRepository;
    private readonly DataContext _context;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="emailRepository">The email repository for data access.</param>
    /// <param name="context">The database context for account lookup.</param>
    /// <param name="logger">The logger for logging email operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailRepository, context, or logger is null.</exception>
    public EmailService(IEmailRepository emailRepository, DataContext context, ILogger<EmailService> logger)
    {
        _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves inbox emails with pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="page">The page number (0-based). Default is 0.</param>
    /// <param name="pageSize">The number of emails per page. Default is 50, maximum is 200.</param>
    /// <returns>A list of email items for display in the inbox.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page is negative or pageSize is out of valid range.</exception>
    public async Task<List<EmailItemModel>> GetInboxAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetInboxAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetInboxAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetInboxAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetInboxAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var emails = await _emailRepository.GetInboxAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        _logger.LogInformation("Retrieved {Count} inbox emails for user {UserId}", result.Count, userId);
        return result;
    }

    /// <summary>
    /// Retrieves sent emails with pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="page">The page number (0-based). Default is 0.</param>
    /// <param name="pageSize">The number of emails per page. Default is 50. Maximum is 200.</param>
    /// <returns>A list of sent email items.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page is negative or pageSize is out of valid range.</exception>
    public async Task<List<EmailItemModel>> GetSentAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetSentAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetSentAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetSentAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetSentAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var emails = await _emailRepository.GetSentAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        _logger.LogInformation("Retrieved {Count} sent emails for user {UserId}", result.Count, userId);
        return result;
    }

    /// <summary>
    /// Retrieves draft emails with pagination.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="page">The page number (0-based). Default is 0.</param>
    /// <param name="pageSize">The number of emails per page. Default is 50. Maximum is 200.</param>
    /// <returns>A list of draft email items.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page is negative or pageSize is out of valid range.</exception>
    public async Task<List<EmailItemModel>> GetDraftsAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetDraftsAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetDraftsAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetDraftsAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetDraftsAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var emails = await _emailRepository.GetDraftsAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        _logger.LogInformation("Retrieved {Count} draft emails for user {UserId}", result.Count, userId);
        return result;
    }

    /// <summary>
    /// Retrieves a single email by its unique identifier.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The email details if found; otherwise, null.</returns>
    public async Task<EmailModel?> GetEmailByIdAsync(Guid emailId, string userId)
    {
        _logger.LogDebug("GetEmailByIdAsync called for emailId {EmailId}, userId {UserId}", emailId, userId);
        var email = await _emailRepository.GetByIdAsync(emailId, userId);
        if (email == null)
        {
            _logger.LogWarning("Email {EmailId} not found for user {UserId}", emailId, userId);
        }
        return email == null ? null : EmailMapper.ToDetailDto(email);
    }

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email to mark as read.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    public async Task MarkAsReadAsync(Guid emailId, string userId)
    {
        _logger.LogDebug("MarkAsReadAsync called for emailId {EmailId}, userId {UserId}", emailId, userId);
        await _emailRepository.MarkReadAsync(emailId, userId);
        _logger.LogInformation("Marked email {EmailId} as read for user {UserId}", emailId, userId);
    }

    /// <summary>
    /// Saves a draft email.
    /// </summary>
    /// <param name="request">The draft email data.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The unique identifier of the saved draft.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
    public async Task<Guid> SaveDraftAsync(DraftModel request, string userId)
    {
        _logger.LogDebug("SaveDraftAsync called for user {UserId}", userId);
        
        if (request == null)
        {
            _logger.LogWarning("SaveDraftAsync called with null request");
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SaveDraftAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        // Get the user's email address from their account
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);
        
        if (account == null)
        {
            _logger.LogError("SaveDraftAsync: No active account found for user {UserId}", userId);
            throw new InvalidOperationException("No active account found for user");
        }

        var email = EmailMapper.ToEntity(request, account.EmailAddress);
        await _emailRepository.SaveDraftAsync(email, userId);
        _logger.LogInformation("Saved draft email {EmailId} for user {UserId}", email.Id, userId);
        return email.Id;
    }

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    /// <param name="request">The email data to send.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The unique identifier of the sent email.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
    public async Task<Guid> SendEmailAsync(DraftModel request, string userId)
    {
        _logger.LogDebug("SendEmailAsync called for user {UserId} to {Recipients}", userId, string.Join(", ", request?.To ?? new List<string>()));
        
        if (request == null)
        {
            _logger.LogWarning("SendEmailAsync called with null request");
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SendEmailAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        // Get the user's email address from their account
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);
        
        if (account == null)
        {
            _logger.LogError("SendEmailAsync: No active account found for user {UserId}", userId);
            throw new InvalidOperationException("No active account found for user");
        }

        var email = EmailMapper.ToEntity(request, account.EmailAddress);
        await _emailRepository.SendEmailAsync(email, userId);
        _logger.LogInformation("Sent email {EmailId} from user {UserId} to {Recipients}", email.Id, userId, string.Join(", ", request.To));
        return email.Id;
    }

    /// <summary>
    /// Deletes an email by its unique identifier.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email to delete.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    public async Task DeleteEmailAsync(Guid emailId, string userId)
    {
        _logger.LogDebug("DeleteEmailAsync called for emailId {EmailId}, userId {UserId}", emailId, userId);
        await _emailRepository.DeleteAsync(emailId, userId);
        _logger.LogInformation("Deleted email {EmailId} for user {UserId}", emailId, userId);
    }
}