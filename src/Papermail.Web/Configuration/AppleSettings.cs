namespace Papermail.Web.Configuration;

/// <summary>
/// Represents the settings required for Apple authentication.
/// </summary>
public class AppleSettings
{
    /// <summary>
    /// The Apple Client ID used for authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Apple Key ID used for signing authentication requests.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// The Apple Team ID associated with the application.
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// The valid issuer for the Apple token, typically the Apple ID URL.
    /// </summary>
    public string ValidIssuer { get; set; } = "https://appleid.apple.com";
}
