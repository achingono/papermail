namespace Papermail.Core.Configuration;

/// <summary>
/// Represents configuration settings for SMTP email protocol connections.
/// </summary>
public sealed class SmtpSettings
{
    /// <summary>
    /// Gets or sets the SMTP server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the SMTP server port number. Default is 587 for STARTTLS.
    /// </summary>
    public int Port { get; set; } = 587;
    
    /// <summary>
    /// Gets or sets a value indicating whether to use TLS encryption. Default is true.
    /// </summary>
    public bool UseTls { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to trust invalid or self-signed certificates. Default is false.
    /// </summary>
    public bool TrustCertificates { get; set; } = false;
}