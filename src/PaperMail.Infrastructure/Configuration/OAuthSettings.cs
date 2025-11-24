namespace PaperMail.Infrastructure.Configuration;

public sealed class OAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Authorization endpoint (e.g. https://accounts.google.com/o/oauth2/v2/auth or http://oidc:8080/authorize).
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";

    /// <summary>
    /// Token endpoint (e.g. https://oauth2.googleapis.com/token or http://oidc:8080/token).
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
}
