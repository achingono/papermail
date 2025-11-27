namespace Papermail.Data.Clients;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

public interface IImapClient
{
    Task<IEnumerable<Email>> FetchEmailsAsync(string username, string accessToken, int skip, int take, CancellationToken ct = default);
    Task SaveDraftAsync(string username, string accessToken, Email draft, CancellationToken ct = default);
    Task<Email?> GetEmailByIdAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
    Task MarkReadAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
    Task DeleteAsync(string username, string accessToken, Guid emailId, CancellationToken ct = default);
}