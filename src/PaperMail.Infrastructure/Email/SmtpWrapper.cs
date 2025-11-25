using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using MimeKit;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Email;

public interface ISmtpWrapper
{
    Task SendEmailAsync(SmtpSettings settings, string accessToken, EmailEntity email, CancellationToken ct = default);
}

public interface ISmtpClientFactory
{
    ISmtpClient CreateClient();
}

public class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient CreateClient() => new SmtpClient();
}

public sealed class SmtpWrapper : ISmtpWrapper
{
    private readonly ISmtpClientFactory _clientFactory;
    private readonly bool _isDevelopment;

    public SmtpWrapper(ISmtpClientFactory clientFactory, IHostEnvironment environment)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _isDevelopment = environment?.IsDevelopment() ?? false;
    }

    public async Task SendEmailAsync(
        SmtpSettings settings,
        string accessToken,
        EmailEntity email,
        CancellationToken ct = default)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (email == null)
            throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new ArgumentException("SMTP username is required", nameof(settings.Username));

        using var client = _clientFactory.CreateClient();

        // Accept self-signed certificates only in development environment
        // In production, rely on system's trusted CA certificates
        if (_isDevelopment)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }

        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        var authenticated = false;

        // Determine supported authentication mechanisms after connection (and possible STARTTLS upgrade)
        var mechanisms = client.AuthenticationMechanisms;

        // Prefer XOAUTH2 when both a token is present and server advertises support
        if (!string.IsNullOrWhiteSpace(accessToken) && mechanisms.Contains("XOAUTH2", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var oauth2 = new SaslMechanismOAuth2(settings.Username, accessToken);
                await client.AuthenticateAsync(oauth2, ct);
                authenticated = true;
            }
            catch (AuthenticationException)
            {
                // Will fallback below
            }
        }

        // If not authenticated yet and server supports LOGIN/PLAIN and we have a password, fallback.
        if (!authenticated && !string.IsNullOrWhiteSpace(settings.Password) &&
            (mechanisms.Contains("PLAIN", StringComparer.OrdinalIgnoreCase) || mechanisms.Contains("LOGIN", StringComparer.OrdinalIgnoreCase)))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, ct);
            authenticated = true;
        }

        // If server does not advertise any auth mechanism, proceed unauthenticated (e.g. port 25 relay within container network)
        // This may be rejected by the server depending on relay settings; caller will receive the exception from SendAsync.

        var message = CreateMimeMessage(email);
        await client.SendAsync(message, ct);
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
}
