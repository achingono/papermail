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
    /// Connects to the SMTP server and authenticates using OAuth2 or basic auth.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token, or password for basic auth.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown when access token is empty.</exception>
    private async Task ConnectAndAuthenticateAsync(string username, string accessToken, CancellationToken ct)
    {
        logger.LogDebug("ConnectAndAuthenticateAsync called for user {Username}", username);
        
        if (settings == null)
        {
            logger.LogError("SMTP settings are null");
            throw new ArgumentNullException(nameof(settings));
        }
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("ConnectAndAuthenticateAsync called with empty access token for user {Username}", username);
            throw new ArgumentException("Access token or password is required", nameof(accessToken));
        }

        if (!client.IsConnected)
        {
            var secureSocketOptions = settings.UseTls 
                ? SecureSocketOptions.StartTls 
                : SecureSocketOptions.None;
            
            logger.LogInformation("Connecting to SMTP server {Host}:{Port} with {SecurityMode}", 
                settings.Host, settings.Port, secureSocketOptions);
            await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions, ct);
            logger.LogInformation("Successfully connected to SMTP server {Host}:{Port}", settings.Host, settings.Port);
        }

        if (!client.IsAuthenticated)
        {
            var mechanisms = client.AuthenticationMechanisms;
            logger.LogDebug("Available SMTP authentication mechanisms: {Mechanisms}", string.Join(", ", mechanisms));
            
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
    /// Sends an email message using the SMTP protocol.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="email">The email message to send.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public async Task SendEmailAsync(string username, string accessToken, Email email, CancellationToken ct = default)
    {
        logger.LogDebug("SendEmailAsync called for user {Username}, subject: {Subject}, recipients: {Recipients}", 
            username, email.Subject, string.Join(", ", email.To.Select(t => t.Value)));
        
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var message = CreateMimeMessage(email);
        
        logger.LogInformation("Sending email via SMTP for user {Username}, subject: {Subject}", username, email.Subject);
        await client.SendAsync(message, ct);
        logger.LogInformation("Successfully sent email via SMTP for user {Username}, subject: {Subject}", username, email.Subject);
        
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