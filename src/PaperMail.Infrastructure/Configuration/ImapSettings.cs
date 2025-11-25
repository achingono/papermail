namespace PaperMail.Infrastructure.Configuration;

public sealed class ImapSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    // Plain password used only as a development/testing fallback when XOAUTH2 fails.
    // In production prefer token-based (XOAUTH2) authentication.
    public string Password { get; set; } = string.Empty;
}
