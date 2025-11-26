using Microsoft.Extensions.Options;
using PaperMail.Core.Entities;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Configuration;
using System.Security.Authentication;
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

    public async Task<EmailEntity?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var password = string.IsNullOrWhiteSpace(_imapSettings.Password) ? _smtpSettings.Password : _imapSettings.Password;
        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId,
            Password = password
        };

        try
        {
            return await _mailKit.GetEmailByIdAsync(settingsWithUser, accessToken, id, ct);
        }
        catch (AuthenticationException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyCollection<EmailEntity>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        // Use userId from session as IMAP username
        var password = string.IsNullOrWhiteSpace(_imapSettings.Password) ? _smtpSettings.Password : _imapSettings.Password;
        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId,
            Password = password
        };

        var skip = page * pageSize;
        try
        {
            var emails = await _mailKit.FetchEmailsAsync(settingsWithUser, accessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            // Authentication failed even after fallback; surface empty inbox rather than hard failure.
            return Array.Empty<EmailEntity>();
        }
    }

    public async Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var password = string.IsNullOrWhiteSpace(_imapSettings.Password) ? _smtpSettings.Password : _imapSettings.Password;
        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId,
            Password = password
        };

        await _mailKit.MarkReadAsync(settingsWithUser, accessToken, id, ct);
    }

    public async Task SaveDraftAsync(EmailEntity draft, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var password = string.IsNullOrWhiteSpace(_imapSettings.Password) ? _smtpSettings.Password : _imapSettings.Password;
        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId,
            Password = password
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

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("No access token available");

        var password = string.IsNullOrWhiteSpace(_imapSettings.Password) ? _smtpSettings.Password : _imapSettings.Password;
        var settingsWithUser = new ImapSettings
        {
            Host = _imapSettings.Host,
            Port = _imapSettings.Port,
            UseSsl = _imapSettings.UseSsl,
            Username = userId,
            Password = password
        };

        await _mailKit.DeleteAsync(settingsWithUser, accessToken, id, ct);
    }
}
