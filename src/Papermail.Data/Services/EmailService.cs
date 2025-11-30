using Papermail.Data.Mappers;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Papermail.Data.Services;

/// <summary>
/// Email service implementation.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailRepository _emailRepository;
    private readonly DataContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IDistributedCache _cache;
    private const int CacheSeconds = 60; // base absolute expiration

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="emailRepository">The email repository for data access.</param>
    /// <param name="context">The database context for account lookup.</param>
    /// <param name="logger">The logger for logging email operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailRepository, context, or logger is null.</exception>
    public EmailService(IEmailRepository emailRepository, DataContext context, ILogger<EmailService> logger, IDistributedCache cache)
    {
        _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private async Task<int> GetUserVersionAsync(string userId)
    {
        var key = $"email:{userId}:version";
        var existing = await _cache.GetStringAsync(key);
        if (int.TryParse(existing, out var v)) return v;
        // initialize
        await _cache.SetStringAsync(key, "1");
        return 1;
    }

    private async Task IncrementUserVersionAsync(string userId)
    {
        var key = $"email:{userId}:version";
        var existing = await _cache.GetStringAsync(key);
        var v = 1;
        if (int.TryParse(existing, out var parsed)) v = parsed + 1;
        await _cache.SetStringAsync(key, v.ToString());
    }

    private static string BuildKey(string userId, string category, int version, params object[] parts)
        => $"email:{userId}:{category}:v{version}:{string.Join(':', parts)}";

    private static DistributedCacheEntryOptions CacheEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheSeconds),
        SlidingExpiration = TimeSpan.FromSeconds(CacheSeconds / 2)
    };

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

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "inbox", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetInboxAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
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

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "sent", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetSentAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
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

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "drafts", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetDraftsAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
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
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "detail", version, emailId);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<EmailModel>(cached);
        }
        var email = await _emailRepository.GetByIdAsync(emailId, userId);
        if (email == null)
        {
            _logger.LogWarning("Email {EmailId} not found for user {UserId}", emailId, userId);
        }
        var dto = email == null ? null : EmailMapper.ToDetailDto(email);
        if (dto != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), CacheEntryOptions());
        }
        return dto;
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
        await IncrementUserVersionAsync(userId); // invalidate cache by version bump
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
        await IncrementUserVersionAsync(userId);
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
        await IncrementUserVersionAsync(userId);
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
        await IncrementUserVersionAsync(userId);
        _logger.LogInformation("Deleted email {EmailId} for user {UserId}", emailId, userId);
    }

    public async Task<List<EmailItemModel>> GetDeletedAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetDeletedAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetDeletedAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetDeletedAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetDeletedAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "deleted", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetDeletedAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
        _logger.LogInformation("Retrieved {Count} deleted emails for user {UserId}", result.Count, userId);
        return result;
    }

    public async Task<List<EmailItemModel>> GetArchiveAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetArchiveAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetArchiveAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetArchiveAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetArchiveAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "archive", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetArchiveAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
        _logger.LogInformation("Retrieved {Count} archive emails for user {UserId}", result.Count, userId);
        return result;
    }

    public async Task<List<EmailItemModel>> GetJunkAsync(string userId, int page = 0, int pageSize = 50)
    {
        _logger.LogDebug("GetJunkAsync called for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetJunkAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (page < 0)
        {
            _logger.LogWarning("GetJunkAsync called with negative page {Page}", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            _logger.LogWarning("GetJunkAsync called with invalid pageSize {PageSize}", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");
        }

        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "junk", version, page, pageSize);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<EmailItemModel>>(cached) ?? new List<EmailItemModel>();
        }
        var emails = await _emailRepository.GetJunkAsync(userId, page, pageSize);
        var result = emails.Select(EmailMapper.ToListItemDto).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), CacheEntryOptions());
        _logger.LogInformation("Retrieved {Count} junk emails for user {UserId}", result.Count, userId);
        return result;
    }    

    public async Task<int> GetInboxCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "inbox");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetInboxCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task<int> GetSentCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "sent");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetSentCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task<int> GetDraftsCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "drafts");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetDraftsCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task<int> GetDeletedCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "deleted");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetDeletedCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task<int> GetArchiveCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "archive");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetArchiveCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task<int> GetJunkCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        var version = await GetUserVersionAsync(userId);
        var cacheKey = BuildKey(userId, "count", version, "junk");
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null && int.TryParse(cached, out var cachedCount)) return cachedCount;
        var count = await _emailRepository.GetJunkCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), CacheEntryOptions());
        return count;
    }

    public async Task MoveToArchiveAsync(Guid emailId, string userId)
    {
        _logger.LogDebug("MoveToArchiveAsync called for emailId {EmailId}, userId {UserId}", emailId, userId);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("MoveToArchiveAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        await _emailRepository.MoveToArchiveAsync(emailId, userId);
        await IncrementUserVersionAsync(userId);
        _logger.LogInformation("Moved email {EmailId} to Archive for user {UserId}", emailId, userId);
    }

    public async Task MoveToJunkAsync(Guid emailId, string userId)
    {
        _logger.LogDebug("MoveToJunkAsync called for emailId {EmailId}, userId {UserId}", emailId, userId);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("MoveToJunkAsync called with null or empty userId");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        await _emailRepository.MoveToJunkAsync(emailId, userId);
        await IncrementUserVersionAsync(userId);
        _logger.LogInformation("Moved email {EmailId} to Junk for user {UserId}", emailId, userId);
    }
}