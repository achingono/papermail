namespace Papermail.Core.Configuration;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseTls { get; set; } = true;
    public bool TrustCertificates { get; set; } = false;

}