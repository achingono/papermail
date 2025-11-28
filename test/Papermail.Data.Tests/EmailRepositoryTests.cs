using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data.Clients;
using Papermail.Data.Repositories;
using Papermail.Data.Services;
using System.Security.Authentication;
using Xunit;

namespace Papermail.Data.Tests;

public class EmailRepositoryTests
{
    private readonly Mock<IImapClient> _mockImapClient;
    private readonly Mock<ISmtpClient> _mockSmtpClient;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<EmailRepository>> _mockLogger;
    private readonly EmailRepository _repository;

    public EmailRepositoryTests()
    {
        _mockImapClient = new Mock<IImapClient>();
        _mockSmtpClient = new Mock<ISmtpClient>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<EmailRepository>>();

        _repository = new EmailRepository(
            _mockImapClient.Object,
            _mockSmtpClient.Object,
            _mockTokenService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WithValidCredentials_ReturnsEmail()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();
        var expectedEmail = CreateTestEmail();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.GetEmailByIdAsync("user@test.com", "access-token", emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmail);

        // Act
        var result = await _repository.GetByIdAsync(emailId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEmail.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "access-token"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.GetByIdAsync(emailId, userId)
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", (string?)null));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.GetByIdAsync(emailId, userId)
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenAuthenticationFails_ReturnsNull()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.GetEmailByIdAsync("user@test.com", "access-token", emailId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetByIdAsync(emailId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInboxAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var userId = "user-123";
        var expectedEmails = new[] { CreateTestEmail(), CreateTestEmail() };

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmails);

        // Act
        var result = await _repository.GetInboxAsync(userId, 0, 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetInboxAsync_WhenAuthenticationFails_ReturnsEmptyCollection()
    {
        // Arrange
        var userId = "user-123";

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetInboxAsync(userId, 0, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSentAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var userId = "user-123";
        var expectedEmails = new[] { CreateTestEmail(), CreateTestEmail() };

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchSentEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmails);

        // Act
        var result = await _repository.GetSentAsync(userId, 0, 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSentAsync_WhenAuthenticationFails_ReturnsEmptyCollection()
    {
        // Arrange
        var userId = "user-123";

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchSentEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetSentAsync(userId, 0, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDraftsAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var userId = "user-123";
        var expectedEmails = new[] { CreateTestEmail(), CreateTestEmail() };

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchDraftEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmails);

        // Act
        var result = await _repository.GetDraftsAsync(userId, 0, 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDraftsAsync_WhenAuthenticationFails_ReturnsEmptyCollection()
    {
        // Arrange
        var userId = "user-123";

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.FetchDraftEmailsAsync("user@test.com", "access-token", 0, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetDraftsAsync(userId, 0, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task MarkReadAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        // Act
        await _repository.MarkReadAsync(emailId, userId);

        // Assert
        _mockImapClient.Verify(
            c => c.MarkReadAsync("user@test.com", "access-token", emailId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveDraftAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var userId = "user-123";
        var draft = CreateTestEmail();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        // Act
        await _repository.SaveDraftAsync(draft, userId);

        // Assert
        _mockImapClient.Verify(
            c => c.SaveDraftAsync("user@test.com", "access-token", draft, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_WithValidCredentials_SendsAndSavesToSent()
    {
        // Arrange
        var userId = "user-123";
        var email = CreateTestEmail();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        // Act
        await _repository.SendEmailAsync(email, userId);

        // Assert
        _mockSmtpClient.Verify(
            c => c.SendEmailAsync("user@test.com", "access-token", email, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockImapClient.Verify(
            c => c.SaveToSentAsync("user@test.com", "access-token", email, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_WhenSaveToSentFails_LogsWarningButDoesNotThrow()
    {
        // Arrange
        var userId = "user-123";
        var email = CreateTestEmail();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        _mockImapClient
            .Setup(c => c.SaveToSentAsync("user@test.com", "access-token", email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save to Sent failed"));

        // Act & Assert
        await _repository.SendEmailAsync(email, userId);

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to save email to Sent folder")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();

        _mockTokenService
            .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user@test.com", "access-token"));

        // Act
        await _repository.DeleteAsync(emailId, userId);

        // Assert
        _mockImapClient.Verify(
            c => c.DeleteAsync("user@test.com", "access-token", emailId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void Constructor_WithNullImapClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailRepository(
            null!,
            _mockSmtpClient.Object,
            _mockTokenService.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullSmtpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailRepository(
            _mockImapClient.Object,
            null!,
            _mockTokenService.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullTokenService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailRepository(
            _mockImapClient.Object,
            _mockSmtpClient.Object,
            null!,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailRepository(
            _mockImapClient.Object,
            _mockSmtpClient.Object,
            _mockTokenService.Object,
            null!
        ));
    }

    private static Email CreateTestEmail()
    {
        var from = EmailAddress.Create("sender@test.com");
        var to = new[] { EmailAddress.Create("recipient@test.com") };
        return Email.Create(from, to, "Test Subject", "Test Body", "<p>Test Body</p>", DateTimeOffset.UtcNow);
    }
}
