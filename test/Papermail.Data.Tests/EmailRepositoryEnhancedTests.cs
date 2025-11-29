using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data.Clients;
using Papermail.Data.Repositories;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

public class EmailRepositoryEnhancedTests
{
    private readonly Mock<IImapClient> _mockImapClient;
    private readonly Mock<ISmtpClient> _mockSmtpClient;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<EmailRepository>> _mockLogger;
    private readonly EmailRepository _repository;

    public EmailRepositoryEnhancedTests()
    {
        _mockImapClient = new Mock<IImapClient>();
        _mockSmtpClient = new Mock<ISmtpClient>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<EmailRepository>>();
        _repository = new EmailRepository(
            _mockImapClient.Object,
            _mockSmtpClient.Object,
            _mockTokenService.Object,
            _mockLogger.Object);
    }

    private void SetupCredentials(string username, string accessToken)
    {
        _mockTokenService.Setup(t => t.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((username, accessToken));
    }

    [Fact]
    public async Task GetByIdAsync_WithValidCredentials_ReturnsEmail()
    {
        // Arrange
        var userId = "user-123";
        var emailId = Guid.NewGuid();
        var expectedEmail = Email.CreateWithId(
            emailId,
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Test Email",
            "Test body",
            null,
            DateTimeOffset.UtcNow);

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetEmailByIdAsync("test@test.com", "access-token", emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmail);

        // Act
        var result = await _repository.GetByIdAsync(emailId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(emailId, result.Id);
        Assert.Equal("Test Email", result.Subject);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupCredentials(null!, "access-token");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.GetByIdAsync(Guid.NewGuid(), "user-123"));
            }

    [Fact]
    public async Task GetByIdAsync_WithoutAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupCredentials("test@test.com", null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _repository.GetByIdAsync(Guid.NewGuid(), "user-123"));
    }

    [Fact]
    public async Task GetByIdAsync_WithAuthenticationException_ReturnsNull()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetEmailByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), "user-123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInboxAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var userId = "user-123";
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Email 1", "Body 1", null, DateTimeOffset.UtcNow),
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Email 2", "Body 2", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetInboxAsync(userId, 0, 50);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetInboxAsync_WithAuthenticationException_ReturnsEmpty()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetInboxAsync("user-123", 0, 50);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSentAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Sent Email 1", "Body", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchSentEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetSentAsync("user-123", 0, 50);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetDraftsAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Draft Email 1", "Body", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchDraftEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetDraftsAsync("user-123", 0, 50);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetJunkAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Junk Email 1", "Body", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchJunkEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetJunkAsync("user-123", 0, 50);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetArchiveAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Archived Email 1", "Body", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchArchiveEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetArchiveAsync("user-123", 0, 50);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetDeletedAsync_WithValidCredentials_ReturnsEmails()
    {
        // Arrange
        var emails = new List<Email>
        {
            Email.CreateWithId(Guid.NewGuid(), EmailAddress.Create("sender@test.com"), new[] { EmailAddress.Create("recipient@test.com") }, "Deleted Email 1", "Body", null, DateTimeOffset.UtcNow)
        };

        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.FetchDeletedEmailsAsync("test@test.com", "access-token", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _repository.GetDeletedAsync("user-123", 0, 50);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task MarkReadAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.MarkReadAsync(emailId, "user-123");

        // Assert
        _mockImapClient.Verify(c => c.MarkReadAsync("test@test.com", "access-token", emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var draft = Email.Create(
            EmailAddress.Create("test@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Draft",
            "Draft body",
            null,
            DateTimeOffset.UtcNow);
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.SaveDraftAsync(draft, "user-123");

        // Assert
        _mockImapClient.Verify(c => c.SaveDraftAsync("test@test.com", "access-token", draft, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithValidCredentials_CallsSmtpAndSavesToSent()
    {
        // Arrange
        var email = Email.Create(
            EmailAddress.Create("test@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Test Send",
            "Test body",
            null,
            DateTimeOffset.UtcNow);
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.SendEmailAsync(email, "user-123");

        // Assert
        _mockSmtpClient.Verify(c => c.SendEmailAsync("test@test.com", "access-token", email, It.IsAny<CancellationToken>()), Times.Once);
        _mockImapClient.Verify(c => c.SaveToSentAsync("test@test.com", "access-token", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WhenSaveToSentFails_LogsWarningButSucceeds()
    {
        // Arrange
        var email = Email.Create(
            EmailAddress.Create("test@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Test Send",
            "Test body",
            null,
            DateTimeOffset.UtcNow);
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.SaveToSentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save to sent failed"));

        // Act
        await _repository.SendEmailAsync(email, "user-123");

        // Assert
        _mockSmtpClient.Verify(c => c.SendEmailAsync("test@test.com", "access-token", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.DeleteAsync(emailId, "user-123");

        // Assert
        _mockImapClient.Verify(c => c.DeleteAsync("test@test.com", "access-token", emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetInboxCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _repository.GetInboxCountAsync("user-123");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetInboxCountAsync_WithAuthenticationException_ReturnsZero()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetInboxCountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _repository.GetInboxCountAsync("user-123");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetSentCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetSentCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _repository.GetSentCountAsync("user-123");

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task GetDraftsCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetDraftsCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _repository.GetDraftsCountAsync("user-123");

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetDeletedCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetDeletedCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _repository.GetDeletedCountAsync("user-123");

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetJunkCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetJunkCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        // Act
        var result = await _repository.GetJunkCountAsync("user-123");

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public async Task GetArchiveCountAsync_WithValidCredentials_ReturnsCount()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        _mockImapClient.Setup(c => c.GetArchiveCountAsync("test@test.com", "access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await _repository.GetArchiveCountAsync("user-123");

        // Assert
        Assert.Equal(15, result);
    }

    [Fact]
    public async Task MoveToArchiveAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.MoveToArchiveAsync(emailId, "user-123");

        // Assert
        _mockImapClient.Verify(c => c.MoveToArchiveAsync("test@test.com", "access-token", emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveToJunkAsync_WithValidCredentials_CallsImapClient()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        SetupCredentials("test@test.com", "access-token");

        // Act
        await _repository.MoveToJunkAsync(emailId, "user-123");

        // Assert
        _mockImapClient.Verify(c => c.MoveToJunkAsync("test@test.com", "access-token", emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_CalculatesPaginationCorrectly()
    {
        // Arrange
        SetupCredentials("test@test.com", "access-token");
        var emails = new List<Email>();
        _mockImapClient.Setup(c => c.FetchEmailsAsync("test@test.com", "access-token", 50, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        await _repository.GetInboxAsync("user-123", 2, 25);

        // Assert
        _mockImapClient.Verify(c => c.FetchEmailsAsync("test@test.com", "access-token", 50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
