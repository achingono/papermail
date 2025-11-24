using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Email;

public interface IMailKitWrapper
{
    Task<IEnumerable<EmailEntity>> FetchEmailsAsync(ImapSettings settings, string accessToken, int skip, int take, CancellationToken ct = default);
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

    public MailKitWrapper() : this(new ImapClientFactory())
    {
    }

    public MailKitWrapper(IImapClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
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
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero");

        using var client = _clientFactory.CreateClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, ct);
        
        var oauth2 = new SaslMechanismOAuth2(accessToken, accessToken);
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
