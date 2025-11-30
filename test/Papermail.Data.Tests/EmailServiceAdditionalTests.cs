using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

public class EmailServiceAdditionalTests
{
    private readonly Mock<IEmailRepository> _mockRepository = new();
    private readonly Mock<ILogger<EmailService>> _mockLogger = new();
    private readonly FakeDistributedCache _cache = new();
    private readonly DataContext _context;
    private readonly EmailService _service;
    private readonly string _userId = "extra-user";

    public EmailServiceAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _service = new EmailService(_mockRepository.Object, _context, _mockLogger.Object, _cache);
        AddAccount(_userId, "extra@test.com");
    }

    [Fact]
    public async Task GetArchiveAsync_CachesAndInvalidatesOnMove()
    {
        var calls = 0;
        _mockRepository.Setup(r => r.GetArchiveAsync(_userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return new List<Email> { CreateEmail("Arch") }; });
        _mockRepository.Setup(r => r.MoveToArchiveAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.GetArchiveAsync(_userId); // cache fill
        await _service.GetArchiveAsync(_userId); // cached
        await _service.MoveToArchiveAsync(Guid.NewGuid(), _userId); // invalidate
        await _service.GetArchiveAsync(_userId); // fetch again

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetDeletedAsync_Caches()
    {
        var calls = 0;
        _mockRepository.Setup(r => r.GetDeletedAsync(_userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return new List<Email> { CreateEmail("Del") }; });
        await _service.GetDeletedAsync(_userId);
        await _service.GetDeletedAsync(_userId);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetJunkAsync_Caches()
    {
        var calls = 0;
        _mockRepository.Setup(r => r.GetJunkAsync(_userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { calls++; return new List<Email> { CreateEmail("Junk") }; });
        await _service.GetJunkAsync(_userId);
        await _service.GetJunkAsync(_userId);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MoveToJunkAsync_InvalidatesJunkCache()
    {
        var listCalls = 0;
        _mockRepository.Setup(r => r.GetJunkAsync(_userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { listCalls++; return new List<Email> { CreateEmail("Junk") }; });
        _mockRepository.Setup(r => r.MoveToJunkAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await _service.GetJunkAsync(_userId);
        await _service.GetJunkAsync(_userId); // cached
        await _service.MoveToJunkAsync(Guid.NewGuid(), _userId); // invalidate
        await _service.GetJunkAsync(_userId); // fetch again
        Assert.Equal(2, listCalls);
    }

    [Fact]
    public async Task CountCaching_AllFolders()
    {
        int sentCalls = 0, draftsCalls = 0, deletedCalls = 0, archiveCalls = 0, junkCalls = 0;
        _mockRepository.Setup(r => r.GetSentCountAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(() => { sentCalls++; return 5; });
        _mockRepository.Setup(r => r.GetDraftsCountAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(() => { draftsCalls++; return 6; });
        _mockRepository.Setup(r => r.GetDeletedCountAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(() => { deletedCalls++; return 7; });
        _mockRepository.Setup(r => r.GetArchiveCountAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(() => { archiveCalls++; return 8; });
        _mockRepository.Setup(r => r.GetJunkCountAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(() => { junkCalls++; return 9; });

        // First calls populate cache
        await _service.GetSentCountAsync(_userId);
        await _service.GetDraftsCountAsync(_userId);
        await _service.GetDeletedCountAsync(_userId);
        await _service.GetArchiveCountAsync(_userId);
        await _service.GetJunkCountAsync(_userId);
        // Second calls should come from cache
        await _service.GetSentCountAsync(_userId);
        await _service.GetDraftsCountAsync(_userId);
        await _service.GetDeletedCountAsync(_userId);
        await _service.GetArchiveCountAsync(_userId);
        await _service.GetJunkCountAsync(_userId);

        Assert.Equal(1, sentCalls);
        Assert.Equal(1, draftsCalls);
        Assert.Equal(1, deletedCalls);
        Assert.Equal(1, archiveCalls);
        Assert.Equal(1, junkCalls);
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
