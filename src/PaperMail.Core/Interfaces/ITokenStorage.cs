namespace PaperMail.Core.Interfaces;

public interface ITokenStorage
{
    Task StoreTokenAsync(string userId, string refreshToken, CancellationToken ct = default);
    Task<string?> GetTokenAsync(string userId, CancellationToken ct = default);
    Task RevokeTokenAsync(string userId, CancellationToken ct = default);
}
