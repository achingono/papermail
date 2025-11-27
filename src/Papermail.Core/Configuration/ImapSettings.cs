namespace Papermail.Core.Configuration;

public sealed class ImapSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public bool TrustCertificates { get; set; } = false;
}