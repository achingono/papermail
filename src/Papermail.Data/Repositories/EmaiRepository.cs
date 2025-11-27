using Papermail.Data.Repositories;
using System.Security.Authentication;
using EmailEntity = Papermail.Core.Entities.Email;
using Papermail.Data.Clients;
using Papermail.Data.Services;

namespace Papermail.Data.Repositories;

public sealed class EmailRepository : IEmailRepository
{
    private readonly IImapClient _imapClient;
    private readonly ISmtpClient _smtpClient;
    private readonly ITokenService _tokenService;

    public EmailRepository(
        IImapClient imapClient,
        ISmtpClient smtpClient,
        ITokenService tokenService)
    {
        _imapClient = imapClient ?? throw new ArgumentNullException(nameof(imapClient));
        _smtpClient = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<EmailEntity?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        try
        {
            return await _imapClient.GetEmailByIdAsync(credentials.Username, credentials.AccessToken, id, ct);
        }
        catch (AuthenticationException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyCollection<EmailEntity>> GetInboxAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        var skip = page * pageSize;
        try
        {
            var emails = await _imapClient.FetchEmailsAsync(credentials.Username, credentials.AccessToken, skip, pageSize, ct);
            return emails.ToList();
        }
        catch (AuthenticationException)
        {
            // Authentication failed even after fallback; surface empty inbox rather than hard failure.
            return Array.Empty<EmailEntity>();
        }
    }

    public async Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.MarkReadAsync(credentials.Username, credentials.AccessToken, id, ct);
    }

    public async Task SaveDraftAsync(EmailEntity draft, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.SaveDraftAsync(credentials.Username, credentials.AccessToken, draft, ct);
    }

    public async Task SendEmailAsync(EmailEntity email, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _smtpClient.SendEmailAsync(credentials.Username, credentials.AccessToken, email, ct);
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var credentials = await _tokenService.GetCredentialsAsync(userId, ct);

        if (credentials.Username == null)
            throw new InvalidOperationException("No username available");
        if (credentials.AccessToken == null)
            throw new InvalidOperationException("No access token available");

        await _imapClient.DeleteAsync(credentials.Username, credentials.AccessToken, id, ct);
    }
}