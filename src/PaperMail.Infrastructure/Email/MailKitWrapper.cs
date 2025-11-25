using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using MimeKit;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Email;

public interface IMailKitWrapper
{
    Task<IEnumerable<EmailEntity>> FetchEmailsAsync(ImapSettings settings, string accessToken, int skip, int take, CancellationToken ct = default);
    Task SaveDraftAsync(ImapSettings settings, string accessToken, EmailEntity draft, CancellationToken ct = default);
}

public interface IImapClientFactory
{
    IImapClient CreateClient();
}

public class ImapClientFactory : IImapClientFactory
{
    public IImapClient CreateClient() => new ImapClient();
}

public sealed class MailKitWrapper : IMailKitWrapper
{
    private readonly IImapClientFactory _clientFactory;
    private readonly bool _isDevelopment;

    public MailKitWrapper(IImapClientFactory clientFactory, IHostEnvironment environment)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _isDevelopment = environment?.IsDevelopment() ?? false;
    }

    public async Task<IEnumerable<EmailEntity>> FetchEmailsAsync(
        ImapSettings settings,
        string accessToken,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new ArgumentException("IMAP username is required", nameof(settings.Username));
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero");

        using var client = _clientFactory.CreateClient();
        
        // Accept self-signed certificates only in development environment
        if (_isDevelopment)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
        
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, ct);

        // Use XOAUTH2 with username and access token
        var oauth2 = new MailKit.Security.SaslMechanismOAuth2(settings.Username, accessToken);
        await client.AuthenticateAsync(oauth2, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        var emails = new List<EmailEntity>();
        var messageCount = Math.Min(take, inbox.Count - skip);

        for (var i = skip; i < skip + messageCount; i++)
        {
            var message = await inbox.GetMessageAsync(i, ct);
            emails.Add(MapToEmail(message));
        }

        await client.DisconnectAsync(true, ct);
        return emails;
    }

    public async Task SaveDraftAsync(
        ImapSettings settings,
        string accessToken,
        EmailEntity draft,
        CancellationToken ct = default)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new ArgumentException("IMAP username is required", nameof(settings.Username));
        if (draft == null)
            throw new ArgumentNullException(nameof(draft));

        using var client = _clientFactory.CreateClient();
        
        // Accept self-signed certificates only in development environment
        if (_isDevelopment)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
        
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, ct);

        var oauth2 = new MailKit.Security.SaslMechanismOAuth2(settings.Username, accessToken);
        await client.AuthenticateAsync(oauth2, ct);

        // Get or create Drafts folder
        var drafts = client.GetFolder(MailKit.SpecialFolder.Drafts);
        await drafts.OpenAsync(FolderAccess.ReadWrite, ct);

        // Create MimeMessage from EmailEntity
        var message = CreateMimeMessage(draft);

        // Append to Drafts folder
        await drafts.AppendAsync(message, MessageFlags.Draft, ct);

        await client.DisconnectAsync(true, ct);
    }

    private static MimeMessage CreateMimeMessage(EmailEntity email)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(string.Empty, email.From.Value));
        
        foreach (var recipient in email.To)
        {
            message.To.Add(new MailboxAddress(string.Empty, recipient.Value));
        }

        message.Subject = email.Subject;

        var builder = new BodyBuilder();
        if (!string.IsNullOrEmpty(email.BodyHtml))
        {
            builder.HtmlBody = email.BodyHtml;
        }
        if (!string.IsNullOrEmpty(email.BodyPlain))
        {
            builder.TextBody = email.BodyPlain;
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    public static EmailEntity MapToEmail(MimeMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var fromMailbox = message.From.Mailboxes.FirstOrDefault();
        if (fromMailbox == null)
            throw new InvalidOperationException("Email must have a sender");

        var from = EmailAddress.Create(fromMailbox.Address);
        var to = message.To.Mailboxes.Select(m => EmailAddress.Create(m.Address)).ToList();
        
        if (!to.Any())
        {
            // If no To recipients, create a placeholder
            to.Add(EmailAddress.Create("undisclosed-recipients@example.com"));
        }

        var attachments = message.Attachments.OfType<MimePart>()
            .Select(a => new Attachment(
                a.FileName ?? "unknown", 
                a.Content?.Stream.Length ?? 0, 
                a.ContentType.MimeType))
            .ToList();

        return EmailEntity.Create(
            from,
            to,
            message.Subject ?? "(no subject)",
            message.TextBody ?? message.HtmlBody ?? string.Empty,
            message.HtmlBody,
            message.Date,
            attachments
        );
    }
}
