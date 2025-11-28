namespace Papermail.Core.Configuration;

/// <summary>
/// Represents configuration settings for IMAP email protocol connections.
/// </summary>
public sealed class ImapSettings
{
    /// <summary>
    /// Gets or sets the IMAP server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the IMAP server port number. Default is 993 for SSL/TLS.
    /// </summary>
    public int Port { get; set; } = 993;
    
    /// <summary>
    /// Gets or sets a value indicating whether to use SSL/TLS encryption. Default is true.
    /// </summary>
    public bool UseSsl { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to trust invalid or self-signed certificates. Default is false.
    /// </summary>
    public bool TrustCertificates { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the username for IMAP authentication (used as fallback when OAuth is unavailable).
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Gets or sets the password for IMAP authentication (used as fallback when OAuth is unavailable).
    /// </summary>
    public string? Password { get; set; }
}