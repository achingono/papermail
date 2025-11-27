using System.Configuration;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Web.Clients;

public class SmtpClient : Papermail.Data.Clients.ISmtpClient
{
    private readonly MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
    private readonly SmtpSettings settings;
    private readonly ILogger<SmtpClient> logger;
    public SmtpClient(IOptions<SmtpSettings> options, ILogger<SmtpClient> logger)
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

    public async Task SendEmailAsync(string username, string accessToken, Email email, CancellationToken ct = default)
    {
        await ConnectAndAuthenticateAsync(username, accessToken, ct);
        var message = CreateMimeMessage(email);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
    
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