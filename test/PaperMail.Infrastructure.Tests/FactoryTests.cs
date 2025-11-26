using FluentAssertions;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using PaperMail.Infrastructure.Email;

namespace PaperMail.Infrastructure.Tests;

public class ImapClientFactoryTests
{
    [Fact]
    public void CreateClient_ShouldReturnImapClient()
    {
        // Arrange
        var factory = new ImapClientFactory();

        // Act
        var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IImapClient>();
    }

    [Fact]
    public void CreateClient_MultipleCalls_ShouldReturnNewInstances()
    {
        // Arrange
        var factory = new ImapClientFactory();

        // Act
        var client1 = factory.CreateClient();
        var client2 = factory.CreateClient();

        // Assert
        client1.Should().NotBeSameAs(client2);
    }
}

public class SmtpClientFactoryTests
{
    [Fact]
    public void CreateClient_ShouldReturnSmtpClient()
    {
        // Arrange
        var factory = new SmtpClientFactory();

        // Act
        var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<ISmtpClient>();
    }

    [Fact]
    public void CreateClient_MultipleCalls_ShouldReturnNewInstances()
    {
        // Arrange
        var factory = new SmtpClientFactory();

        // Act
        var client1 = factory.CreateClient();
        var client2 = factory.CreateClient();

        // Assert
        client1.Should().NotBeSameAs(client2);
    }
}
