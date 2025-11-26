using PaperMail.Application.DTOs;
using PaperMail.Application.Mappers;
using PaperMail.Core.Interfaces;

namespace PaperMail.Application.Services;

/// <summary>
/// Email service implementation.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailRepository _emailRepository;

    public EmailService(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
    }

    public async Task<List<EmailListItemDto>> GetInboxAsync(string userId, int page = 0, int pageSize = 50)
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

    public async Task<EmailDetailDto?> GetEmailByIdAsync(Guid emailId, string userId)
    {
        var email = await _emailRepository.GetByIdAsync(emailId, userId);
        return email == null ? null : EmailMapper.ToDetailDto(email);
    }

    public async Task MarkAsReadAsync(Guid emailId, string userId)
    {
        await _emailRepository.MarkReadAsync(emailId, userId);
    }

    public async Task<Guid> SaveDraftAsync(ComposeEmailRequest request, string userId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        var email = EmailMapper.ToEntity(request, userId);
        await _emailRepository.SaveDraftAsync(email, userId);
        return email.Id;
    }

    public async Task<Guid> SendEmailAsync(ComposeEmailRequest request, string userId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));

        var email = EmailMapper.ToEntity(request, userId);
        await _emailRepository.SendEmailAsync(email, userId);
        return email.Id;
    }

    public async Task DeleteEmailAsync(Guid emailId, string userId)
    {
        await _emailRepository.DeleteAsync(emailId, userId);
    }
}
