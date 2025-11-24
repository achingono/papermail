using FluentAssertions;
using PaperMail.Infrastructure.Configuration;

namespace PaperMail.Infrastructure.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ImapSettings_ShouldHaveDefaults()
    {
        var settings = new ImapSettings();
        settings.Port.Should().Be(993);
        settings.UseSsl.Should().BeTrue();
        settings.Host.Should().BeEmpty();
    }

    [Fact]
    public void SmtpSettings_ShouldHaveDefaults()
    {
        var settings = new SmtpSettings();
        settings.Port.Should().Be(587);
        settings.UseTls.Should().BeTrue();
        settings.Host.Should().BeEmpty();
    }

    [Fact]
    public void OAuthSettings_ShouldHaveDefaults()
    {
        var settings = new OAuthSettings();
        settings.ClientId.Should().BeEmpty();
        settings.ClientSecret.Should().BeEmpty();
        settings.Scopes.Should().BeEmpty();
        settings.RedirectUri.Should().BeEmpty();
    }
}
