namespace PaperMail.Infrastructure.Configuration;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    // Plain password used only in development fallback when XOAUTH2 is unavailable.
    public string Password { get; set; } = string.Empty;
}
