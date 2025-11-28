using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Models;
using Papermail.Data.Repositories;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

/// <summary>
/// Unit tests for EmailService to verify business logic and error handling.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IEmailRepository> _mockRepository;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<DataContext> _mockContext;
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _mockRepository = new Mock<IEmailRepository>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _mockContext = new Mock<DataContext>(options);
        
        _service = new EmailService(_mockRepository.Object, _mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetInboxAsync_WithValidParameters_ReturnsEmailList()
    {
        // Arrange
        var userId = "test-user";
        var emails = new List<Email>
        {
            new Email 
            { 
                Id = Guid.NewGuid(), 
                Subject = "Test Email", 
                From = new EmailAddress { Address = "sender@test.com" } 
            }
        };
        _mockRepository.Setup(r => r.GetInboxAsync(userId, 0, 50, default))
            .ReturnsAsync(emails);

        // Act
        var result = await _service.GetInboxAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test Email", result[0].Subject);
        _mockRepository.Verify(r => r.GetInboxAsync(userId, 0, 50, default), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetInboxAsync(null!));
    }

    [Fact]
    public async Task GetInboxAsync_WithNegativePage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetInboxAsync("user", page: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetInboxAsync_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetInboxAsync("user", pageSize: pageSize));
    }

    [Fact]
    public async Task GetSentAsync_WithValidParameters_ReturnsSentEmailList()
    {
        // Arrange
        var userId = "test-user";
        var emails = new List<Email>
        {
            new Email 
            { 
                Id = Guid.NewGuid(), 
                Subject = "Sent Email", 
                To = new List<EmailAddress> 
                { 
                    new EmailAddress { Address = "recipient@test.com" } 
                } 
            }
        };
        _mockRepository.Setup(r => r.GetSentAsync(userId, 0, 50, default))
            .ReturnsAsync(emails);

        // Act
        var result = await _service.GetSentAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Sent Email", result[0].Subject);
    }

    [Fact]
    public async Task GetDraftsAsync_WithValidParameters_ReturnsDraftEmailList()
    {
        // Arrange
        var userId = "test-user";
        var emails = new List<Email>
        {
            new Email 
            { 
                Id = Guid.NewGuid(), 
                Subject = "Draft Email", 
                IsDraft = true 
            }
        };
        _mockRepository.Setup(r => r.GetDraftsAsync(userId, 0, 50, default))
            .ReturnsAsync(emails);

        // Act
        var result = await _service.GetDraftsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Draft Email", result[0].Subject);
    }

    [Fact]
    public async Task MarkAsReadAsync_ValidEmailId_CallsRepository()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var userId = "test-user";
        _mockRepository.Setup(r => r.MarkReadAsync(emailId, userId, default))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsReadAsync(emailId, userId);

        // Assert
        _mockRepository.Verify(r => r.MarkReadAsync(emailId, userId, default), Times.Once);
    }
}
