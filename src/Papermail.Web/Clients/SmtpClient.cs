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
/// Provides SMTP client functionality for sending email messages.
/// Implements OAuth2 authentication for secure access to email servers.
/// </summary>
public class SmtpClient : Papermail.Data.Clients.ISmtpClient
{
    private readonly MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
    private readonly SmtpSettings settings;
    private readonly ILogger<SmtpClient> logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpClient"/> class.
    /// </summary>
    /// <param name="options">The SMTP configuration settings.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public SmtpClient(IOptions<SmtpSettings> options, ILogger<SmtpClient> logger)
    {
        settings = options.Value;
        this.logger = logger;
        if (settings.TrustCertificates)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
    }

    /// <summary>
    /// Connects to the SMTP server and authenticates using OAuth2.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown when access token is empty.</exception>
    /// <exception cref="AuthenticationException">Thrown when server doesn't support XOAUTH2.</exception>
    private async Task ConnectAndAuthenticateAsync(string username, string accessToken, CancellationToken ct)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));

        if (!client.IsConnected)
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseTls, ct);
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

    /// <summary>
    /// Sends an email message using the SMTP protocol.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="email">The email message to send.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task SendEmailAsync(string username, string accessToken, Email email, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var message = CreateMimeMessage(email);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
    
    /// <summary>
    /// Creates a MimeMessage from an Email entity, converting the domain model to MIME format.
    /// </summary>
    /// <param name="email">The email entity to convert.</param>
    /// <returns>A MimeMessage ready to be sent via SMTP.</returns>
    internal static MimeMessage CreateMimeMessage(Email email)
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

}