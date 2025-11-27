using System.Configuration;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Web.Clients;

public class ImapClient : Papermail.Data.Clients.IImapClient
{
    private readonly MailKit.Net.Imap.ImapClient client = new MailKit.Net.Imap.ImapClient();
    private readonly ImapSettings settings;
    private readonly ILogger<ImapClient> logger;

    public ImapClient(IOptions<ImapSettings> options, ILogger<ImapClient> logger)
    {
        settings = options.Value;
        this.logger = logger;
        if (settings.TrustCertificates)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
    }

    private async Task ConnectAndAuthenticateAsync(string username, string accessToken, CancellationToken ct)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));

        if (!client.IsConnected)
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, ct);
        }

        var mechanisms = client.AuthenticationMechanisms;

        if (!mechanisms.Contains("XOAUTH2", StringComparer.OrdinalIgnoreCase))
        {
            throw new AuthenticationException("Server does not support XOAUTH2 authentication.");
        }

        if (!client.IsAuthenticated)
        {
            var oauth2 = new SaslMechanismOAuth2(username, accessToken);
            await client.AuthenticateAsync(oauth2, ct);
        }
    }

    public async Task DeleteAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            // Mark as deleted and expunge immediately
            await inbox.AddFlagsAsync(index.Value, MessageFlags.Deleted, true, ct);
            await inbox.ExpungeAsync(ct);
        }

        await client.DisconnectAsync(true, ct);
    }

    public async Task<IEnumerable<Email>> FetchEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        var emails = new List<Email>();
        var messageCount = Math.Min(take, inbox.Count - skip);

        for (var i = skip; i < skip + messageCount; i++)
        {
            var message = await inbox.GetMessageAsync(i, ct);
            emails.Add(MapToEmail(message));
        }

        await client.DisconnectAsync(true, ct);
        return emails;
    }

    public async Task<Email?> GetEmailByIdAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        var message = await FindMessageByIdAsync(inbox, emailId, ct);

        await client.DisconnectAsync(true, ct);

        return message != null ? MapToEmail(message) : null;
    }

    public async Task MarkReadAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            await inbox.AddFlagsAsync(index.Value, MessageFlags.Seen, true, ct);
        }

        await client.DisconnectAsync(true, ct);
    }

    public async Task SaveDraftAsync(string username, string accessToken, Email draft, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        // Get or create Drafts folder
        var drafts = client.GetFolder(MailKit.SpecialFolder.Drafts);
        await drafts.OpenAsync(FolderAccess.ReadWrite, ct);

        // Create MimeMessage from EmailEntity
        var message = SmtpClient.CreateMimeMessage(draft);

        // Append to Drafts folder
        await drafts.AppendAsync(message, MessageFlags.Draft, ct);

        await client.DisconnectAsync(true, ct);
    }

    public static Email MapToEmail(MimeMessage message)
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

        // Use Message-ID to create deterministic GUID
        var messageId = message.MessageId ?? Guid.NewGuid().ToString();
        var emailId = CreateDeterministicGuid(messageId);

        return Email.CreateWithId(
            emailId,
            from,
            to,
            message.Subject ?? "(no subject)",
            message.TextBody ?? message.HtmlBody ?? string.Empty,
            message.HtmlBody,
            message.Date,
            attachments
        );
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    /// <summary>
    /// Finds a message by its GUID (derived from Message-ID) using batch IMAP FETCH.
    /// Uses IMAP FETCH with MessageSummaryItems.Envelope to retrieve only headers in batches,
    /// significantly reducing network traffic compared to fetching full messages.
    /// </summary>
    private static async Task<MimeMessage?> FindMessageByIdAsync(IMailFolder folder, Guid emailId, CancellationToken ct)
    {
        await folder.OpenAsync(FolderAccess.ReadWrite, ct);

        if (folder.Count == 0)
            return null;

        // Batch fetch headers for better performance
        // Process in chunks to avoid overwhelming the server
        const int batchSize = 100;

        for (int offset = 0; offset < folder.Count; offset += batchSize)
        {
            int count = Math.Min(batchSize, folder.Count - offset);
            var indexes = Enumerable.Range(offset, count).ToList();

            // Fetch headers in batch using IMAP FETCH command
            var headersList = await folder.FetchAsync(indexes, MessageSummaryItems.Envelope, ct);

            foreach (var summary in headersList)
            {
                var messageId = summary.Envelope.MessageId ?? string.Empty;
                var messageGuid = CreateDeterministicGuid(messageId);

                if (messageGuid == emailId)
                {
                    // Found the match, fetch the full message
                    return await folder.GetMessageAsync(summary.Index, ct);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a message index by its GUID (derived from Message-ID) using batch IMAP FETCH.
    /// Uses IMAP FETCH with MessageSummaryItems.Envelope to retrieve only headers in batches,
    /// enabling efficient message flag operations (mark read, delete) without fetching full content.
    /// </summary>
    private static async Task<int?> FindMessageIndexByIdAsync(IMailFolder folder, Guid emailId, CancellationToken ct)
    {
        await folder.OpenAsync(FolderAccess.ReadWrite, ct);

        if (folder.Count == 0)
            return null;

        // Batch fetch headers for better performance
        // Process in chunks to avoid overwhelming the server
        const int batchSize = 100;

        for (int offset = 0; offset < folder.Count; offset += batchSize)
        {
            int count = Math.Min(batchSize, folder.Count - offset);
            var indexes = Enumerable.Range(offset, count).ToList();

            // Fetch headers in batch using IMAP FETCH command
            var headersList = await folder.FetchAsync(indexes, MessageSummaryItems.Envelope, ct);

            foreach (var summary in headersList)
            {
                var messageId = summary.Envelope.MessageId ?? string.Empty;
                var messageGuid = CreateDeterministicGuid(messageId);

                if (messageGuid == emailId)
                {
                    return summary.Index;
                }
            }
        }

        return null;
    }
}