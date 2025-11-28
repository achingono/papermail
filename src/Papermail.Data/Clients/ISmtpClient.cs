
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Data.Clients;

/// <summary>
/// Defines SMTP client operations for sending email messages.
/// </summary>
public interface ISmtpClient
{
    /// <summary>
    /// Sends an email message using the SMTP protocol.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="email">The email message to send.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SendEmailAsync(string username, string accessToken, Email email, CancellationToken ct = default);
}