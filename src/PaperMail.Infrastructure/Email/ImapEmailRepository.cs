using Microsoft.Extensions.Options;
using PaperMail.Core.Entities;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Configuration;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Email;

public sealed class ImapEmailRepository : IEmailRepository
{
    private readonly IMailKitWrapper _mailKit;
    private readonly ISmtpWrapper _smtpWrapper;
    private readonly ImapSettings _imapSettings;
    private readonly SmtpSettings _smtpSettings;
    private readonly ITokenStorage _tokenStorage;

    public ImapEmailRepository(
        IMailKitWrapper mailKit, 
        ISmtpWrapper smtpWrapper,
        IOptions<ImapSettings> imapSettings, 
        IOptions<SmtpSettings> smtpSettings,
        ITokenStorage tokenStorage)
    {
        _mailKit = mailKit ?? throw new ArgumentNullException(nameof(mailKit));
        _smtpWrapper = smtpWrapper ?? throw new ArgumentNullException(nameof(smtpWrapper));
        _imapSettings = imapSettings?.Value ?? throw new ArgumentNullException(nameof(imapSettings));
        _smtpSettings = smtpSettings?.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
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
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
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

    public async Task SaveDraftAsync(EmailEntity draft, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId
        };

        await _mailKit.SaveDraftAsync(settingsWithUser, accessToken, draft, ct);
    }

    public async Task SendEmailAsync(EmailEntity email, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var settingsWithUser = new SmtpSettings
        {
            Host = _smtpSettings.Host,
            Port = _smtpSettings.Port,
            UseTls = _smtpSettings.UseTls,
            Username = userId,
            Password = _smtpSettings.Password // include password for fallback auth
        };

        await _smtpWrapper.SendEmailAsync(settingsWithUser, accessToken, email, ct);
    }
}
