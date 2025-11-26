namespace Papermail.Web.Configuration;

/// <summary>
/// Represents the settings required for Microsoft authentication.
/// </summary>
public class MicrosoftSettings
{
    /// <summary>
    /// The Microsoft Client ID used for authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Microsoft Client Secret used to secure authentication requests.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The valid issuer for the Microsoft token, typically the Microsoft accounts URL.
    /// </summary>
    public string ValidIssuer { get; set; } = "https://login.microsoftonline.com/common/v2.0";

}
