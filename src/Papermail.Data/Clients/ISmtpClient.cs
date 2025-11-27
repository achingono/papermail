
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Data.Clients;

public interface ISmtpClient
{
    Task SendEmailAsync(string username, string accessToken, Email email, CancellationToken ct = default);
}