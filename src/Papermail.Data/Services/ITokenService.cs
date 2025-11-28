namespace Papermail.Data.Services;

/// <summary>
/// Defines token management operations for OAuth token encryption and retrieval.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Retrieves the username and decrypted access token for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A tuple containing the username and access token if valid; otherwise, (null, null).</returns>
    Task<(string? Username, string? AccessToken)> GetCredentialsAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves the decrypted access token for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The decrypted access token if valid; otherwise, null.</returns>
    Task<string?> GetAccessTokenAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves the decrypted refresh token for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The decrypted refresh token if found; otherwise, null.</returns>
    Task<string?> GetRefreshTokenAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Encrypts a token using Data Protection for secure storage.
    /// </summary>
    /// <param name="token">The token to encrypt.</param>
    /// <returns>The encrypted token string.</returns>
    string ProtectToken(string token);
}