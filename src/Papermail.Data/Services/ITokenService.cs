namespace Papermail.Data.Services;

public interface ITokenService
{
    Task<(string? Username, string? AccessToken)> GetCredentialsAsync(string userId, CancellationToken ct = default);
    Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default);
    Task<string?> GetRefreshTokenAsync(string userId, CancellationToken ct = default);
    string ProtectToken(string token);
}