using MailKit.Net.Smtp;
using MailKit.Security;
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

    public SmtpWrapper() : this(new SmtpClientFactory())
    {
    }

    public SmtpWrapper(ISmtpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task SendEmailAsync(
        SmtpSettings settings,
        string accessToken,
        EmailEntity email,
        CancellationToken ct = default)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required", nameof(accessToken));
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new ArgumentException("SMTP username is required", nameof(settings.Username));
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        using var client = _clientFactory.CreateClient();
        
        // Connect to SMTP server
        await client.ConnectAsync(settings.Host, settings.Port, 
            settings.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

        // Authenticate using XOAUTH2
        var oauth2 = new SaslMechanismOAuth2(settings.Username, accessToken);
        await client.AuthenticateAsync(oauth2, ct);

        // Create and send message
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
