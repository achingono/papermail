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

public sealed class MailKitWrapper : IMailKitWrapper
{
    public async Task<IEnumerable<EmailEntity>> FetchEmailsAsync(
        ImapSettings settings,
        string accessToken,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        using var client = new ImapClient();
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

    private static EmailEntity MapToEmail(MimeMessage message)
    {
        var from = EmailAddress.Create(message.From.Mailboxes.First().Address);
        var to = message.To.Mailboxes.Select(m => EmailAddress.Create(m.Address));
        var attachments = message.Attachments.OfType<MimePart>()
            .Select(a => new Attachment(a.FileName ?? "unknown", a.Content?.Stream.Length ?? 0, a.ContentType.MimeType))
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
