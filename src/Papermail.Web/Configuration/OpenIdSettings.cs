namespace Papermail.Web.Configuration;

/// <summary>
/// Represents the settings required for OpenID Connect authentication.
/// </summary>
public class OpenIdSettings
{
    /// <summary>
    /// Gets or sets the OpenID Connect authority URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenID Connect client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenID Connect client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS is required for metadata retrieval.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
