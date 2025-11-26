using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;
using System.Security.Authentication;
using Xunit;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests for SmtpWrapper using real mail server from docker-compose.
/// These tests verify actual SMTP operations against a live mail server.
/// </summary>
[Collection("DockerCompose")]
public class SmtpWrapperIntegrationTests
{
    private readonly DockerComposeFixture _fixture;
    private readonly SmtpWrapper _wrapper;
    private readonly SmtpSettings _settings;

    public SmtpWrapperIntegrationTests(DockerComposeFixture fixture)
    {
        _fixture = fixture;
        
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        
        var factory = new SmtpClientFactory();
        _wrapper = new SmtpWrapper(factory, mockEnv.Object);
        
        _settings = new SmtpSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.SmtpPort,
            UseTls = true, // Using STARTTLS on port 587
            Username = _fixture.TestUser,
            Password = _fixture.TestPassword
        };
    }

    [Fact(Skip = "Requires docker-compose mail server to be running")]
    public async Task SendEmailAsync_WithPasswordAuth_ShouldSendEmail()
    {
        // Arrange
        var accessToken = "dummy-token"; // Will fall back to password auth
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create("recipient@example.com") },
            "Integration Test Email",
            "This is a test email sent by integration test.",
            "<html><body><p>This is a test email sent by integration test.</p></body></html>",
            DateTimeOffset.UtcNow
        );
        
        // Act
        var act = async () => await _wrapper.SendEmailAsync(_settings, accessToken, email);
        
        // Assert - Should complete successfully without throwing
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires docker-compose mail server to be running")]
    public async Task SendEmailAsync_MultipleRecipients_ShouldSendToAll()
    {
        // Arrange
        var accessToken = "dummy-token";
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { 
                EmailAddress.Create("recipient1@example.com"),
                EmailAddress.Create("recipient2@example.com"),
                EmailAddress.Create("recipient3@example.com")
            },
            "Integration Test - Multiple Recipients",
            "This email is sent to multiple recipients.",
            null,
            DateTimeOffset.UtcNow
        );
        
        // Act
        var act = async () => await _wrapper.SendEmailAsync(_settings, accessToken, email);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires docker-compose mail server to be running")]
    public async Task SendEmailAsync_WithAttachments_ShouldIncludeAttachments()
    {
        // Arrange
        var accessToken = "dummy-token";
        var attachments = new List<Attachment>
        {
            new Attachment("test-document.txt", 1024, "text/plain"),
            new Attachment("test-image.png", 2048, "image/png")
        };
        
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create("recipient@example.com") },
            "Integration Test - With Attachments",
            "This email includes test attachments.",
            null,
            DateTimeOffset.UtcNow,
            attachments
        );
        
        // Act
        var act = async () => await _wrapper.SendEmailAsync(_settings, accessToken, email);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires docker-compose mail server to be running")]
    public async Task SendEmailAsync_WithInvalidCredentials_ShouldThrowAuthException()
    {
        // Arrange
        var invalidSettings = new SmtpSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.SmtpPort,
            UseTls = true,
            Username = "invalid@example.com",
            Password = "wrongpassword"
        };
        var accessToken = "dummy-token";
        var email = EmailEntity.Create(
            EmailAddress.Create("sender@example.com"),
            new[] { EmailAddress.Create("recipient@example.com") },
            "Test",
            "Test body",
            null,
            DateTimeOffset.UtcNow
        );
        
        // Act
        var act = async () => await _wrapper.SendEmailAsync(invalidSettings, accessToken, email);
        
        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact(Skip = "Requires docker-compose mail server to be running")]
    public async Task SendEmailAsync_PlainTextOnly_ShouldSend()
    {
        // Arrange
        var accessToken = "dummy-token";
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create("recipient@example.com") },
            "Plain Text Email",
            "This is plain text only email without HTML.",
            null, // No HTML body
            DateTimeOffset.UtcNow
        );
        
        // Act
        var act = async () => await _wrapper.SendEmailAsync(_settings, accessToken, email);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
}
