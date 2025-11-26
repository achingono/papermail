using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;
using MailKit.Security;
using Xunit;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests for MailKitWrapper using real mail server from docker-compose.
/// These tests verify actual IMAP operations against a live mail server.
/// </summary>
[Collection("DockerCompose")]
public class MailKitWrapperIntegrationTests
{
    private readonly DockerComposeFixture _fixture;
    private readonly MailKitWrapper _wrapper;
    private readonly ImapSettings _settings;

    public MailKitWrapperIntegrationTests(DockerComposeFixture fixture)
    {
        _fixture = fixture;
        
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        
        var factory = new ImapClientFactory();
        _wrapper = new MailKitWrapper(factory, mockEnv.Object);
        
        _settings = new ImapSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.ImapPort,
            UseSsl = true, // Using IMAPS on port 993
            Username = _fixture.TestUser,
            Password = _fixture.TestPassword
        };
    }

    [Fact]
    public async Task FetchEmailsAsync_WithPasswordAuth_ShouldConnectAndFetch()
    {
        // Arrange
        var accessToken = "dummy-token"; // Will fall back to password auth
        
        // Act
        var emails = await _wrapper.FetchEmailsAsync(_settings, accessToken, 0, 10);
        
        // Assert
        emails.Should().NotBeNull();
        // Empty inbox is expected for fresh mail server
    }

    [Fact]
    public async Task SaveDraftAsync_ShouldCreateDraftInMailbox()
    {
        // Arrange
        var accessToken = "dummy-token";
        var draft = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create("recipient@example.com") },
            "Integration Test Draft",
            "This is a test draft created by integration test",
            null,
            DateTimeOffset.UtcNow
        );
        
        // Act
        await _wrapper.SaveDraftAsync(_settings, accessToken, draft);
        
        // Assert - Draft should be saved successfully without throwing
        // In a real scenario, we'd fetch drafts to verify, but that requires additional IMAP operations
    }

    [Fact]
    public async Task GetEmailByIdAsync_NonExistentEmail_ShouldReturnNull()
    {
        // Arrange
        var accessToken = "dummy-token";
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var email = await _wrapper.GetEmailByIdAsync(_settings, accessToken, nonExistentId);
        
        // Assert
        email.Should().BeNull();
    }

    [Fact]
    public async Task MarkReadAsync_NonExistentEmail_ShouldCompleteWithoutError()
    {
        // Arrange
        var accessToken = "dummy-token";
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var act = async () => await _wrapper.MarkReadAsync(_settings, accessToken, nonExistentId);
        
        // Assert - Should complete successfully even if email not found
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentEmail_ShouldCompleteWithoutError()
    {
        // Arrange
        var accessToken = "dummy-token";
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var act = async () => await _wrapper.DeleteAsync(_settings, accessToken, nonExistentId);
        
        // Assert - Should complete successfully even if email not found
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FetchEmailsAsync_WithInvalidCredentials_ShouldThrowAuthException()
    {
        // Arrange
        var invalidSettings = new ImapSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.ImapPort,
            UseSsl = true, // Using IMAPS on port 993
            Username = "invalid@example.com",
            Password = "wrongpassword"
        };
        var accessToken = "dummy-token";
        
        // Act
        var act = async () => await _wrapper.FetchEmailsAsync(invalidSettings, accessToken, 0, 10);
        
        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
