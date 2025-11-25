using Microsoft.Extensions.Options;
using PaperMail.Core.Entities;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Configuration;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Email;

public sealed class ImapEmailRepository : IEmailRepository
{
    private readonly IMailKitWrapper _mailKit;
    private readonly ImapSettings _settings;
    private readonly ITokenStorage _tokenStorage;

    public ImapEmailRepository(IMailKitWrapper mailKit, IOptions<ImapSettings> settings, ITokenStorage tokenStorage)
    {
        _mailKit = mailKit ?? throw new ArgumentNullException(nameof(mailKit));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
    }

    public Task<EmailEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Placeholder: IMAP doesn't use GUIDs directly, needs mapping
        throw new NotImplementedException("Email by ID requires UID mapping implementation");
    }

    public async Task<IReadOnlyCollection<EmailEntity>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        // Use userId from session as IMAP username
        var settingsWithUser = new ImapSettings
        {
            Host = _settings.Host,
            Port = _settings.Port,
            UseSsl = _settings.UseSsl,
            Username = userId
        };

        var skip = page * pageSize;
        var emails = await _mailKit.FetchEmailsAsync(settingsWithUser, accessToken, skip, pageSize, ct);
        return emails.ToList();
    }

    public Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Mark read requires IMAP flag operation");
    }

    public Task SaveDraftAsync(EmailEntity draft, CancellationToken ct = default)
    {
        throw new NotImplementedException("Save draft requires SMTP/IMAP append operation");
    }
}
