namespace Papermail.Web.Configuration;

/// <summary>
/// Represents the settings required for Facebook authentication.
/// </summary>
public class FacebookSettings
{
    /// <summary>
    /// The Facebook App ID used for authentication.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// The Facebook App Secret used to secure authentication requests.
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// The valid issuer for the Facebook token, typically the Facebook URL.
    /// </summary>
    public string ValidIssuer { get; set; } = "https://www.facebook.com";
}
