using Papermail.Core.Configuration;
using Xunit;

namespace Papermail.Core.Tests;

public class SmtpSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = new SmtpSettings();
        Assert.Equal(string.Empty, settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.True(settings.UseTls);
        Assert.False(settings.TrustCertificates);
        Assert.Null(settings.Username);
        Assert.Null(settings.Password);
    }

    [Fact]
    public void SetProperties_UpdatesValues()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.example.com",
            Port = 25,
            UseTls = false,
            TrustCertificates = true,
            Username = "user",
            Password = "pass"
        };
        Assert.Equal("smtp.example.com", settings.Host);
        Assert.Equal(25, settings.Port);
        Assert.False(settings.UseTls);
        Assert.True(settings.TrustCertificates);
        Assert.Equal("user", settings.Username);
        Assert.Equal("pass", settings.Password);
    }
}
