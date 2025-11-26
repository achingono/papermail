using PaperMail.Core.Entities;

namespace PaperMail.Core.Interfaces;

public interface IEmailRepository
{
    Task<Email?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<Email>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default);
    Task SaveDraftAsync(Email draft, string userId, CancellationToken ct = default);
    Task SendEmailAsync(Email email, string userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string userId, CancellationToken ct = default);
}