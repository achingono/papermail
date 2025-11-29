using System.Configuration;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Web.Clients;

/// <summary>
/// Provides IMAP client functionality for fetching, managing, and manipulating email messages.
/// Implements OAuth2 authentication for secure access to email servers.
/// </summary>
public class ImapClient : Papermail.Data.Clients.IImapClient
{
    private readonly MailKit.Net.Imap.ImapClient client = new MailKit.Net.Imap.ImapClient();
    private readonly ImapSettings settings;
    private readonly ILogger<ImapClient> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImapClient"/> class.
    /// </summary>
    /// <param name="options">The IMAP configuration settings.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public ImapClient(ImapSettings settings, ILogger<ImapClient> logger)
    {
        this.settings = settings;
        this.logger = logger;
        if (settings.TrustCertificates)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
    }

    /// <summary>
    /// Connects to the IMAP server and authenticates using OAuth2.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown when access token is empty.</exception>
    /// <exception cref="AuthenticationException">Thrown when server doesn't support XOAUTH2.</exception>
    private async Task ConnectAndAuthenticateAsync(string username, string accessToken, CancellationToken ct)
    {
        logger.LogDebug("ConnectAndAuthenticateAsync called for user {Username}", username);
        
        if (settings == null)
        {
            logger.LogError("IMAP settings are null");
            throw new ArgumentNullException(nameof(settings));
        }
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("ConnectAndAuthenticateAsync called with empty access token for user {Username}", username);
            throw new ArgumentException("Access token or password is required", nameof(accessToken));
        }

        if (!client.IsConnected)
        {
            // For port 143, use STARTTLS; for port 993, use SSL on connect
            var secureSocketOptions = settings.Port == 993 
                ? SecureSocketOptions.SslOnConnect 
                : (settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            
            logger.LogInformation("Connecting to IMAP server {Host}:{Port} with {SecurityMode}", 
                settings.Host, settings.Port, secureSocketOptions);
            await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions, ct);
            logger.LogInformation("Successfully connected to IMAP server {Host}:{Port}", settings.Host, settings.Port);
        }

        if (!client.IsAuthenticated)
        {
            var mechanisms = client.AuthenticationMechanisms;
            logger.LogDebug("Available IMAP authentication mechanisms: {Mechanisms}", string.Join(", ", mechanisms));
            
            // Try OAuth2 first if supported
            if (mechanisms.Contains("XOAUTH2", StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    logger.LogDebug("Attempting OAuth2 authentication for user {Username}", username);
                    var oauth2 = new SaslMechanismOAuth2(username, accessToken);
                    await client.AuthenticateAsync(oauth2, ct);
                    logger.LogInformation("Successfully authenticated via OAuth2 for user {Username}", username);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "OAuth2 authentication failed for user {Username}, falling back to basic auth", username);
                }
            }
            
            // Fall back to basic authentication
            logger.LogDebug("Attempting basic authentication for user {Username}", username);
            await client.AuthenticateAsync(username, accessToken, ct);
            logger.LogInformation("Successfully authenticated via basic auth for user {Username}", username);
        }
    }

    /// <summary>
    /// Deletes an email message by marking it as deleted and expunging it from the mailbox.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to delete.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task DeleteAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        logger.LogDebug("DeleteAsync called for user {Username}, emailId {EmailId}", username, emailId);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            logger.LogDebug("Marking message at index {Index} as deleted", index.Value);
            // Mark as deleted and expunge immediately
            await inbox.AddFlagsAsync(index.Value, MessageFlags.Deleted, true, ct);
            await inbox.ExpungeAsync(ct);
            logger.LogInformation("Successfully deleted email {EmailId} for user {Username}", emailId, username);
        }
        else
        {
            logger.LogWarning("Email {EmailId} not found for user {Username}", emailId, username);
        }

        await client.DisconnectAsync(true, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the inbox.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.All, skip, take, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the Sent folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchSentEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.Sent, skip, take, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the Drafts folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchDraftEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.Drafts, skip, take, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the Archive folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchArchiveEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.Archive, skip, take, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the Sent folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchDeletedEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.Trash, skip, take, ct);
    }

    /// <summary>
    /// Fetches a range of email messages from the Junk folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    public async Task<IEnumerable<Email>> FetchJunkEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default)
    {
        return await FetchEmailsFromFolderAsync(username, accessToken, MailKit.SpecialFolder.Junk, skip, take, ct);
    }

    public async Task<int> GetInboxCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = inbox.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    public async Task<int> GetSentCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var sent = client.GetFolder(MailKit.SpecialFolder.Sent);
        await sent.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = sent.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    public async Task<int> GetDraftsCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var drafts = client.GetFolder(MailKit.SpecialFolder.Drafts);
        await drafts.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = drafts.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    public async Task<int> GetDeletedCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var trash = client.GetFolder(MailKit.SpecialFolder.Trash);
        await trash.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = trash.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    public async Task<int> GetJunkCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var junk = client.GetFolder(MailKit.SpecialFolder.Junk);
        
        // If Junk special folder doesn't exist, try to get it by name or create it
        if (junk == null)
        {
            logger.LogDebug("Junk special folder not found, attempting to get by name");
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            junk = await personal.GetSubfolderAsync("Junk", ct);
            
            if (junk == null)
            {
                logger.LogInformation("Creating Junk folder for user {Username}", username);
                junk = await personal.CreateAsync("Junk", true, ct);
                await junk.SubscribeAsync(ct);
            }
        }
        
        await junk.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = junk.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    public async Task<int> GetArchiveCountAsync(string username, string accessToken, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var archive = client.GetFolder(MailKit.SpecialFolder.Archive);

        // If Archive special folder doesn't exist, try to get it by name or create it
        if (archive == null)
        {
            logger.LogDebug("Archive special folder not found, attempting to get by name");
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            archive = await personal.GetSubfolderAsync("Archive", ct);
            
            if (archive == null)
            {
                logger.LogInformation("Creating Archive folder for user {Username}", username);
                archive = await personal.CreateAsync("Archive", true, ct);
                await archive.SubscribeAsync(ct);
            }
        }        
        
        await archive.OpenAsync(FolderAccess.ReadOnly, ct);
        var count = archive.Count;
        await client.DisconnectAsync(true, ct);
        return count;
    }

    /// <summary>
    /// Fetches a range of email messages from a specific folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="specialFolder">The special folder to fetch from.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The maximum number of messages to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A collection of email messages.</returns>
    private async Task<IEnumerable<Email>> FetchEmailsFromFolderAsync(string username, string accessToken, MailKit.SpecialFolder specialFolder, int skip, int take, CancellationToken ct = default)
    {
        logger.LogDebug("FetchEmailsFromFolderAsync called for user {Username}, folder {Folder}, skip {Skip}, take {Take}", 
            username, specialFolder, skip, take);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var folder = specialFolder == MailKit.SpecialFolder.All 
            ? client.Inbox 
            : client.GetFolder(specialFolder);

        // If special folder doesn't exist, try to get it by name or create it
        if (folder == null)
        {
            logger.LogDebug("{Folder} special folder not found, attempting to get by name", specialFolder);
            var folderName = specialFolder.ToString();
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            folder = await personal.GetSubfolderAsync(folderName, ct);
            
            if (folder == null)
            {
                logger.LogInformation("Creating {Folder} folder for user {Username}", folderName, username);
                folder = await personal.CreateAsync(folderName, true, ct);
                await folder.SubscribeAsync(ct);
            }
        }
        
        logger.LogDebug("Opening folder {FolderName} in ReadOnly mode", folder.FullName);
        await folder.OpenAsync(FolderAccess.ReadOnly, ct);
        logger.LogDebug("Folder {FolderName} contains {Count} total messages", folder.FullName, folder.Count);

        var emails = new List<Email>();
        var messageCount = Math.Min(take, folder.Count - skip);
        logger.LogDebug("Fetching {MessageCount} messages from folder {FolderName}", messageCount, folder.FullName);

        for (var i = skip; i < skip + messageCount; i++)
        {
            var message = await folder.GetMessageAsync(i, ct);
            emails.Add(MapToEmail(message));
        }

        logger.LogInformation("Successfully fetched {Count} emails from folder {FolderName} for user {Username}", 
            emails.Count, folder.FullName, username);
        await client.DisconnectAsync(true, ct);
        return emails;
    }

    /// <summary>
    /// Retrieves a specific email message by its unique identifier.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The email message if found; otherwise, null.</returns>
    public async Task<Email?> GetEmailByIdAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        logger.LogDebug("GetEmailByIdAsync called for user {Username}, emailId {EmailId}", username, emailId);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        var message = await FindMessageByIdAsync(inbox, emailId, ct);

        await client.DisconnectAsync(true, ct);

        if (message != null)
        {
            logger.LogInformation("Successfully retrieved email {EmailId} for user {Username}", emailId, username);
            return MapToEmail(message);
        }
        else
        {
            logger.LogWarning("Email {EmailId} not found for user {Username}", emailId, username);
            return null;
        }
    }

    /// <summary>
    /// Marks an email message as read by setting the Seen flag.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="emailId">The unique identifier of the email to mark as read.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task MarkReadAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        logger.LogDebug("MarkReadAsync called for user {Username}, emailId {EmailId}", username, emailId);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            await inbox.AddFlagsAsync(index.Value, MessageFlags.Seen, true, ct);
            logger.LogInformation("Successfully marked email {EmailId} as read for user {Username}", emailId, username);
        }
        else
        {
            logger.LogWarning("Email {EmailId} not found for user {Username}", emailId, username);
        }

        await client.DisconnectAsync(true, ct);
    }

    /// <summary>
    /// Saves an email message as a draft in the Drafts folder.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="draft">The email message to save as a draft.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task SaveDraftAsync(string username, string accessToken, Email draft, CancellationToken ct = default)
    {
        logger.LogDebug("SaveDraftAsync called for user {Username}, subject: {Subject}", username, draft.Subject);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        // Get or create Drafts folder
        var drafts = client.GetFolder(MailKit.SpecialFolder.Drafts);
        await drafts.OpenAsync(FolderAccess.ReadWrite, ct);

        // Create MimeMessage from EmailEntity
        var message = SmtpClient.CreateMimeMessage(draft);

        // Append to Drafts folder
        await drafts.AppendAsync(message, MessageFlags.Draft, ct);
        logger.LogInformation("Successfully saved draft to Drafts folder for user {Username}, subject: {Subject}", 
            username, draft.Subject);

        await client.DisconnectAsync(true, ct);
    }

    /// <summary>
    /// Saves an email message to the Sent folder after successful SMTP send.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="email">The email message to save to Sent folder.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task SaveToSentAsync(string username, string accessToken, Email email, CancellationToken ct = default)
    {
        logger.LogDebug("SaveToSentAsync called for user {Username}, subject: {Subject}", username, email.Subject);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        // Get or create Sent folder
        var sent = client.GetFolder(MailKit.SpecialFolder.Sent);
        await sent.OpenAsync(FolderAccess.ReadWrite, ct);

        // Create MimeMessage from EmailEntity
        var message = SmtpClient.CreateMimeMessage(email);

        // Append to Sent folder with Seen flag (already read)
        await sent.AppendAsync(message, MessageFlags.Seen, ct);
        logger.LogInformation("Successfully saved email to Sent folder for user {Username}, subject: {Subject}", 
            username, email.Subject);

        await client.DisconnectAsync(true, ct);
    }

    /// <summary>
    /// Maps a MimeMessage to an Email entity, creating a deterministic GUID from the Message-ID.
    /// </summary>
    /// <param name="message">The MIME message to map.</param>
    /// <returns>An Email entity representing the MIME message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when message has no sender.</exception>
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

    /// <summary>
    /// Creates a deterministic GUID from a string input using MD5 hashing.
    /// </summary>
    /// <param name="input">The input string (typically Message-ID).</param>
    /// <returns>A GUID derived from the input string.</returns>
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

    public async Task MoveToArchiveAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        logger.LogDebug("MoveToArchiveAsync called for user {Username}, emailId {EmailId}", username, emailId);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        // Get source folder (inbox)
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            // Get or create Archive folder
            var archive = client.GetFolder(MailKit.SpecialFolder.Archive);
            
            // If Archive special folder doesn't exist, try to get it by name or create it
            if (archive == null)
            {
                logger.LogDebug("Archive special folder not found, attempting to get by name");
                var personal = client.GetFolder(client.PersonalNamespaces[0]);
                archive = await personal.GetSubfolderAsync("Archive", ct);
                
                if (archive == null)
                {
                    logger.LogInformation("Creating Archive folder for user {Username}", username);
                    archive = await personal.CreateAsync("Archive", true, ct);
                    await archive.SubscribeAsync(ct);
                }
            }
            
            logger.LogDebug("Moving message at index {Index} to Archive", index.Value);
            
            // Move the message (copy + delete original)
            await inbox.MoveToAsync(index.Value, archive, ct);
            
            logger.LogInformation("Successfully moved email {EmailId} to Archive for user {Username}", emailId, username);
        }
        else
        {
            logger.LogWarning("Email {EmailId} not found in inbox for user {Username}", emailId, username);
        }

        await client.DisconnectAsync(true, ct);
    }

    public async Task MoveToJunkAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default)
    {
        logger.LogDebug("MoveToJunkAsync called for user {Username}, emailId {EmailId}", username, emailId);
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);

        // Get source folder (inbox)
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, ct);

        var index = await FindMessageIndexByIdAsync(inbox, emailId, ct);

        if (index.HasValue)
        {
            // Get or create Junk folder
            var junk = client.GetFolder(MailKit.SpecialFolder.Junk);
            
            // If Junk special folder doesn't exist, try to get it by name or create it
            if (junk == null)
            {
                logger.LogDebug("Junk special folder not found, attempting to get by name");
                var personal = client.GetFolder(client.PersonalNamespaces[0]);
                junk = await personal.GetSubfolderAsync("Junk", ct);
                
                if (junk == null)
                {
                    logger.LogInformation("Creating Junk folder for user {Username}", username);
                    junk = await personal.CreateAsync("Junk", true, ct);
                    await junk.SubscribeAsync(ct);
                }
            }
            
            logger.LogDebug("Moving message at index {Index} to Junk", index.Value);
            
            // Move the message (copy + delete original)
            await inbox.MoveToAsync(index.Value, junk, ct);
            
            logger.LogInformation("Successfully moved email {EmailId} to Junk for user {Username}", emailId, username);
        }
        else
        {
            logger.LogWarning("Email {EmailId} not found in inbox for user {Username}", emailId, username);
        }

        await client.DisconnectAsync(true, ct);
    }
}