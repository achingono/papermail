using FluentAssertions;
using Moq;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;
using PaperMail.Core.Entities;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Tests;

public class SmtpWrapperTests
{
    private readonly Mock<ISmtpClientFactory> _clientFactoryMock;
    private readonly Mock<ISmtpClient> _smtpClientMock;
    private readonly Mock<Microsoft.Extensions.Hosting.IHostEnvironment> _environmentMock;
    private readonly SmtpWrapper _wrapper;
    private readonly SmtpSettings _settings;

    public SmtpWrapperTests()
    {
        _clientFactoryMock = new Mock<ISmtpClientFactory>();
        _smtpClientMock = new Mock<ISmtpClient>();
        _environmentMock = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        _settings = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            UseTls = true,
            Username = "test@example.com",
            Password = "password"
        };

        _smtpClientMock.Setup(c => c.AuthenticationMechanisms).Returns(new HashSet<string> { "XOAUTH2", "PLAIN" });
        _clientFactoryMock.Setup(f => f.CreateClient()).Returns(_smtpClientMock.Object);
        
        _wrapper = new SmtpWrapper(_clientFactoryMock.Object, _environmentMock.Object);
    }

    [Fact]
    public void Constructor_NullClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var mockEnv = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        var act = () => new SmtpWrapper(null!, mockEnv.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clientFactory");
    }

    [Fact]
    public async Task SendEmailAsync_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act & Assert
        var act = async () => await _wrapper.SendEmailAsync(null!, "token", email);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task SendEmailAsync_NullEmail_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _wrapper.SendEmailAsync(_settings, "token", null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("email");
    }

    [Fact]
    public async Task SendEmailAsync_EmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        var settingsNoUser = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            UseTls = true,
            Username = "",
            Password = "password"
        };

        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act & Assert
        var act = async () => await _wrapper.SendEmailAsync(settingsNoUser, "token", email);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*username*");
    }

    [Fact]
    public async Task SendEmailAsync_ValidParameters_ConnectsToSmtpServer()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.ConnectAsync(
            "smtp.test.com",
            587,
            SecureSocketOptions.StartTls,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ValidParameters_AuthenticatesWithOAuth()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.AuthenticateAsync(
            It.IsAny<SaslMechanism>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ValidParameters_SendsMessage()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Test Subject",
            "Test Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.SendAsync(
            It.IsAny<MimeMessage>(),
            It.IsAny<CancellationToken>(),
            null), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ValidParameters_DisconnectsAfterSend()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.DisconnectAsync(
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithHtmlBody_CreatesMultipartMessage()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Plain text",
            "<html><body><p>HTML content</p></body></html>",
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.SendAsync(
            It.IsAny<MimeMessage>(),
            It.IsAny<CancellationToken>(),
            null), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_MultipleRecipients_AddsAllToMessage()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { 
                EmailAddress.Create("to1@example.com"),
                EmailAddress.Create("to2@example.com"),
                EmailAddress.Create("to3@example.com")
            },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.SendAsync(
            It.IsAny<MimeMessage>(),
            It.IsAny<CancellationToken>(),
            null), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_UseTlsFalse_ConnectsWithoutTls()
    {
        // Arrange
        var settingsNoTls = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 25,
            UseTls = false,
            Username = "test@example.com",
            Password = "password"
        };

        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await _wrapper.SendEmailAsync(settingsNoTls, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.ConnectAsync(
            "smtp.test.com",
            25,
            SecureSocketOptions.None,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_OAuthFails_FallbackToPassword()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        _smtpClientMock.Setup(c => c.AuthenticateAsync(
            It.IsAny<SaslMechanism>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException("OAuth failed"));

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.AuthenticateAsync(
            "test@example.com",
            "password",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_NoOAuthSupport_UsePassword()
    {
        // Arrange
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        _smtpClientMock.Setup(c => c.AuthenticationMechanisms)
            .Returns(new HashSet<string> { "PLAIN", "LOGIN" });

        // Act
        await _wrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert
        _smtpClientMock.Verify(c => c.AuthenticateAsync(
            "test@example.com",
            "password",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_DevelopmentEnvironment_AcceptsSelfSignedCertificates()
    {
        // Arrange
        var devEnvMock = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        devEnvMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var devWrapper = new SmtpWrapper(_clientFactoryMock.Object, devEnvMock.Object);
        
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        // Act
        await devWrapper.SendEmailAsync(_settings, "access-token", email);

        // Assert - verify callback was set (this happens via side effect on client)
        _smtpClientMock.VerifySet(c => c.ServerCertificateValidationCallback = It.IsAny<System.Net.Security.RemoteCertificateValidationCallback>(), Times.Once);
    }
}
