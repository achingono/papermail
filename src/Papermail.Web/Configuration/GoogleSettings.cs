namespace Papermail.Web.Configuration;

/// <summary>
/// Represents the settings required for Google authentication.
/// </summary>
public class GoogleSettings
{
    /// <summary>
    /// The Google Client ID used for authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Google Client Secret used to secure authentication requests.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The valid issuer for the Google token, typically the Google accounts URL.
    /// </summary>
    public string ValidIssuer { get; set; } = "https://accounts.google.com";
}
