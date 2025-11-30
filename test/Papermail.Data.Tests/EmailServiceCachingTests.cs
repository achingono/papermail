using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Papermail.Data.Services;
using System.Text.Json;

namespace Papermail.Data.Tests;

public class EmailServiceCachingTests
{
    private readonly Mock<IEmailRepository> _mockRepository = new();
    private readonly Mock<ILogger<EmailService>> _mockLogger = new();
    private readonly FakeDistributedCache _cache = new();
    private readonly DataContext _context;
    private readonly EmailService _service;

    public EmailServiceCachingTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _service = new EmailService(_mockRepository.Object, _context, _mockLogger.Object, _cache);
    }

    [Fact]
    public async Task GetInboxAsync_CachesResult_AndReturnsFromCache()
    {
        var userId = "user-1";
        var calls = 0;
        _mockRepository.Setup(r => r.GetInboxAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return new List<Email> { CreateEmail("Inbox 1") }; });

        var first = await _service.GetInboxAsync(userId);
        var second = await _service.GetInboxAsync(userId); // should hit cache

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(1, calls); // repository only called once
    }

    [Fact]
    public async Task GetInboxAsync_InvalidatedAfterSendEmailAsync()
    {
        var userId = "user-2";
        AddAccount(userId, "user2@test.com");
        var calls = 0;
        _mockRepository.Setup(r => r.GetInboxAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return new List<Email> { CreateEmail("Inbox 2") }; });
        _mockRepository.Setup(r => r.SendEmailAsync(It.IsAny<Email>(), userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.GetInboxAsync(userId); // populate cache
        var draft = new DraftModel { To = ["r@test.com"], Subject = "s", BodyPlain = "b" };
        await _service.SendEmailAsync(draft, userId); // bump version
        await _service.GetInboxAsync(userId); // new version -> repository again

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetInboxCountAsync_Caches_AndInvalidates()
    {
        var userId = "user-3";
        var countCalls = 0;
        _mockRepository.Setup(r => r.GetInboxCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { countCalls++; return 42; });
        _mockRepository.Setup(r => r.SendEmailAsync(It.IsAny<Email>(), userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AddAccount(userId, "user3@test.com");

        var first = await _service.GetInboxCountAsync(userId);
        var second = await _service.GetInboxCountAsync(userId); // cached
        var draft = new DraftModel { To = ["r@test.com"], Subject = "s", BodyPlain = "b" };
        await _service.SendEmailAsync(draft, userId); // invalidate
        var third = await _service.GetInboxCountAsync(userId);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(42, third);
        Assert.Equal(2, countCalls); // one for first, one after invalidation
    }

    [Fact]
    public async Task GetEmailByIdAsync_CachesDetail_AndInvalidatesOnMarkRead()
    {
        var userId = "user-4";
        var emailId = Guid.NewGuid();
        var calls = 0;
        _mockRepository.Setup(r => r.GetByIdAsync(emailId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return CreateEmail("Detail"); });
        _mockRepository.Setup(r => r.MarkReadAsync(emailId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var first = await _service.GetEmailByIdAsync(emailId, userId);
        var second = await _service.GetEmailByIdAsync(emailId, userId); // cached
        await _service.MarkAsReadAsync(emailId, userId); // invalidate
        var third = await _service.GetEmailByIdAsync(emailId, userId); // new version

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.Equal(2, calls); // first and third fetch repository
    }

    private static Email CreateEmail(string subject) => Email.Create(
        EmailAddress.Create("sender@test.com"),
        new[] { EmailAddress.Create("recipient@test.com") },
        subject,
        "Body",
        null,
        DateTimeOffset.UtcNow);

    private void AddAccount(string userId, string emailAddress)
    {
        var provider = new Provider { Name = "Test" };
        var account = new Account
        {
            UserId = userId,
            EmailAddress = emailAddress,
            Provider = provider,
            IsActive = true
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
    }
}
