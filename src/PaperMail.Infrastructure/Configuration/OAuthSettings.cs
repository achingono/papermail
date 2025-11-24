namespace PaperMail.Infrastructure.Configuration;

public sealed class OAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string RedirectUri { get; set; } = string.Empty;
}
