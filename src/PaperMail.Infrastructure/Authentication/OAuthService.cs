using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Configuration;

namespace PaperMail.Infrastructure.Authentication;

/// <summary>
/// Service for managing OAuth 2.0 authentication flow.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Generates authorization URL with PKCE parameters.
    /// </summary>
    (string AuthUrl, string CodeVerifier, string State) GetAuthorizationUrl();

    /// <summary>
    /// Exchanges authorization code for access/refresh tokens.
    /// </summary>
    Task<OAuthTokenResponse> ExchangeCodeForTokensAsync(string code, string codeVerifier, CancellationToken ct = default);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    Task<OAuthTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Stores tokens for a user.
    /// </summary>
    Task StoreUserTokensAsync(string userId, OAuthTokenResponse tokens, CancellationToken ct = default);

    /// <summary>
    /// Retrieves stored refresh token for a user.
    /// </summary>
    Task<string?> GetUserRefreshTokenAsync(string userId, CancellationToken ct = default);
}

public class OAuthService : IOAuthService
{
    private readonly OAuthSettings _settings;
    private readonly ITokenStorage _tokenStorage;
    private readonly IHttpClientFactory _httpClientFactory;

    public OAuthService(
        IOptions<OAuthSettings> settings,
        ITokenStorage tokenStorage,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _tokenStorage = tokenStorage;
        _httpClientFactory = httpClientFactory;
    }

    public (string AuthUrl, string CodeVerifier, string State) GetAuthorizationUrl()
    {
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');

        var scopes = string.Join(" ", _settings.Scopes);
        var baseAuth = _settings.AuthorizationEndpoint.TrimEnd('?');
        var authUrl = baseAuth + "?" +
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&code_challenge={codeChallenge}" +
            $"&code_challenge_method=S256" +
            $"&state={state}";

        return (authUrl, codeVerifier, state);
    }

    public async Task<OAuthTokenResponse> ExchangeCodeForTokensAsync(
        string code, 
        string codeVerifier, 
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _settings.RedirectUri
        };

        var response = await client.PostAsync(
            _settings.TokenEndpoint,
            new FormUrlEncodedContent(requestBody),
            ct);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<OAuthTokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize token response");
    }

    public async Task<OAuthTokenResponse> RefreshAccessTokenAsync(
        string refreshToken, 
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        var response = await client.PostAsync(
            _settings.TokenEndpoint,
            new FormUrlEncodedContent(requestBody),
            ct);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        // Refresh tokens don't return a new refresh token, preserve the old one
        if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            tokenResponse.RefreshToken = refreshToken;
        }

        return tokenResponse;
    }

    public async Task StoreUserTokensAsync(
        string userId, 
        OAuthTokenResponse tokens, 
        CancellationToken ct = default)
    {
        // Store access token with expiration
        await _tokenStorage.StoreAccessTokenAsync(userId, tokens.AccessToken, tokens.ExpiresIn, ct);
        
        // Store refresh token if available
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            await _tokenStorage.StoreTokenAsync(userId, tokens.RefreshToken, ct);
        }
    }

    public async Task<string?> GetUserRefreshTokenAsync(string userId, CancellationToken ct = default)
    {
        return await _tokenStorage.GetTokenAsync(userId, ct);
    }
}

public class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string? Scope { get; set; }

    // JSON property names for deserialization
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessTokenJson { set => AccessToken = value; }

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshTokenJson { set => RefreshToken = value; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresInJson { set => ExpiresIn = value; }

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenTypeJson { set => TokenType = value; }

    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string? ScopeJson { set => Scope = value; }
}
