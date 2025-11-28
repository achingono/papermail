using Papermail.Core.Configuration;
using Xunit;

namespace Papermail.Core.Tests;

public class ImapSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = new ImapSettings();
        Assert.Equal(string.Empty, settings.Host);
        Assert.Equal(993, settings.Port);
        Assert.True(settings.UseSsl);
        Assert.False(settings.TrustCertificates);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
    }

    [Fact]
    public void SetProperties_UpdatesValues()
    {
        var settings = new ImapSettings
        {
            Host = "imap.example.com",
            Port = 143,
            UseSsl = false,
            TrustCertificates = true,
            Username = "user",
            Password = "pass"
        };
        Assert.Equal("imap.example.com", settings.Host);
        Assert.Equal(143, settings.Port);
        Assert.False(settings.UseSsl);
        Assert.True(settings.TrustCertificates);
        Assert.Equal("user", settings.Username);
        Assert.Equal("pass", settings.Password);
    }
}
