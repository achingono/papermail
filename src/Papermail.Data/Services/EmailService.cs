using Papermail.Data.Mappers;
using Papermail.Data.Models;
using Papermail.Data.Repositories;

namespace Papermail.Data.Services;

/// <summary>
/// Email service implementation.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailRepository _emailRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="emailRepository">The email repository for data access.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailRepository is null.</exception>
    public EmailService(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
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
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        if (page < 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be non-negative");

        if (pageSize <= 0 || pageSize > 200)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 200");

        var emails = await _emailRepository.GetInboxAsync(userId, page, pageSize);
        return emails.Select(EmailMapper.ToListItemDto).ToList();
    }

    /// <summary>
    /// Retrieves a single email by its unique identifier.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The email details if found; otherwise, null.</returns>
    public async Task<EmailModel?> GetEmailByIdAsync(Guid emailId, string userId)
    {
        var email = await _emailRepository.GetByIdAsync(emailId, userId);
        return email == null ? null : EmailMapper.ToDetailDto(email);
    }

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email to mark as read.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    public async Task MarkAsReadAsync(Guid emailId, string userId)
    {
        await _emailRepository.MarkReadAsync(emailId, userId);
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
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        var email = EmailMapper.ToEntity(request, userId);
        await _emailRepository.SaveDraftAsync(email, userId);
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
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        var email = EmailMapper.ToEntity(request, userId);
        await _emailRepository.SendEmailAsync(email, userId);
        return email.Id;
    }

    /// <summary>
    /// Deletes an email by its unique identifier.
    /// </summary>
    /// <param name="emailId">The unique identifier of the email to delete.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    public async Task DeleteEmailAsync(Guid emailId, string userId)
    {
        await _emailRepository.DeleteAsync(emailId, userId);
    }
}