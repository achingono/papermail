using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

public class EmailServiceTests
{
    private readonly Mock<IEmailRepository> _mockRepository;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly DataContext _context;
    private readonly EmailService _service;
    private readonly FakeDistributedCache _cache;

    public EmailServiceTests()
    {
        _mockRepository = new Mock<IEmailRepository>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        _cache = new FakeDistributedCache();
        
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        
        _service = new EmailService(_mockRepository.Object, _context, _mockLogger.Object, _cache);
    }

    [Fact]
    public async Task GetInboxAsync_WithValidParameters_ReturnsEmailList()
    {
        var userId = "test-user";
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Test Email",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        _mockRepository.Setup(r => r.GetInboxAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Email> { email });

        var result = await _service.GetInboxAsync(userId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test Email", result[0].Subject);
    }

    [Fact]
    public async Task GetInboxAsync_WithNullUserId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetInboxAsync(null!));
    }

    [Fact]
    public async Task GetInboxAsync_WithNegativePage_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetInboxAsync("user", page: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetInboxAsync_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetInboxAsync("user", pageSize: pageSize));
    }

    [Fact]
    public async Task GetSentAsync_WithValidParameters_ReturnsSentEmailList()
    {
        var userId = "test-user";
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Sent Email",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        _mockRepository.Setup(r => r.GetSentAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Email> { email });

        var result = await _service.GetSentAsync(userId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Sent Email", result[0].Subject);
    }

    [Fact]
    public async Task GetDraftsAsync_WithValidParameters_ReturnsDraftEmailList()
    {
        var userId = "test-user";
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Draft Email",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        _mockRepository.Setup(r => r.GetDraftsAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Email> { email });

        var result = await _service.GetDraftsAsync(userId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Draft Email", result[0].Subject);
    }

    [Fact]
    public async Task GetEmailByIdAsync_WhenFound_ReturnsEmail()
    {
        var emailId = Guid.NewGuid();
        var userId = "test-user";
        var email = Email.CreateWithId(
            emailId,
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        _mockRepository.Setup(r => r.GetByIdAsync(emailId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var result = await _service.GetEmailByIdAsync(emailId, userId);

        Assert.NotNull(result);
        Assert.Equal(emailId, result.Id);
        Assert.Equal("Subject", result.Subject);
    }

    [Fact]
    public async Task GetEmailByIdAsync_WhenNotFound_ReturnsNull()
    {
        var emailId = Guid.NewGuid();
        var userId = "test-user";
        _mockRepository.Setup(r => r.GetByIdAsync(emailId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        var result = await _service.GetEmailByIdAsync(emailId, userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_ValidEmailId_CallsRepository()
    {
        var emailId = Guid.NewGuid();
        var userId = "test-user";

        await _service.MarkAsReadAsync(emailId, userId);

        _mockRepository.Verify(r => r.MarkReadAsync(emailId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_WithValidData_ReturnsDraftId()
    {
        var userId = "test-user";
        var provider = new Provider { Name = "Test" };
        var account = new Account 
        { 
            UserId = userId, 
            EmailAddress = "user@test.com",
            Provider = provider,
            IsActive = true
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var draft = new DraftModel
        {
            To = new List<string> { "recipient@test.com" },
            Subject = "Draft",
            BodyPlain = "Body"
        };

        var result = await _service.SaveDraftAsync(draft, userId);

        Assert.NotEqual(Guid.Empty, result);
        _mockRepository.Verify(r => r.SaveDraftAsync(It.IsAny<Email>(), userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.SaveDraftAsync(null!, "user"));
    }

    [Fact]
    public async Task SaveDraftAsync_WithNoAccount_ThrowsInvalidOperationException()
    {
        var draft = new DraftModel { To = new List<string> { "test@test.com" } };
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SaveDraftAsync(draft, "nonexistent-user"));
    }

    [Fact]
    public async Task SendEmailAsync_WithValidData_ReturnsEmailId()
    {
        var userId = "test-user";
        var provider = new Provider { Name = "Test" };
        var account = new Account 
        { 
            UserId = userId, 
            EmailAddress = "user@test.com",
            Provider = provider,
            IsActive = true
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var draft = new DraftModel
        {
            To = new List<string> { "recipient@test.com" },
            Subject = "Email",
            BodyPlain = "Body"
        };

        var result = await _service.SendEmailAsync(draft, userId);

        Assert.NotEqual(Guid.Empty, result);
        _mockRepository.Verify(r => r.SendEmailAsync(It.IsAny<Email>(), userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEmailAsync_ValidEmailId_CallsRepository()
    {
        var emailId = Guid.NewGuid();
        var userId = "test-user";

        await _service.DeleteEmailAsync(emailId, userId);

        _mockRepository.Verify(r => r.DeleteAsync(emailId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSentAsync_WithNullUserId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetSentAsync(null!));
    }

    [Fact]
    public async Task GetSentAsync_WithNegativePage_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetSentAsync("user", page: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetSentAsync_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetSentAsync("user", pageSize: pageSize));
    }

    [Fact]
    public async Task GetDraftsAsync_WithNullUserId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetDraftsAsync(null!));
    }

    [Fact]
    public async Task GetDraftsAsync_WithNegativePage_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetDraftsAsync("user", page: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetDraftsAsync_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetDraftsAsync("user", pageSize: pageSize));
    }

    [Fact]
    public async Task SaveDraftAsync_WithNullUserId_ThrowsArgumentException()
    {
        var draft = new DraftModel { To = new List<string> { "test@test.com" } };
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SaveDraftAsync(draft, null!));
    }

    [Fact]
    public async Task SendEmailAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.SendEmailAsync(null!, "user"));
    }

    [Fact]
    public async Task SendEmailAsync_WithNullUserId_ThrowsArgumentException()
    {
        var draft = new DraftModel { To = new List<string> { "test@test.com" } };
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SendEmailAsync(draft, null!));
    }

    [Fact]
    public async Task SendEmailAsync_WithNoAccount_ThrowsInvalidOperationException()
    {
        var draft = new DraftModel { To = new List<string> { "test@test.com" } };
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SendEmailAsync(draft, "nonexistent-user"));
    }
}
