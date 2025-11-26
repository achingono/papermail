using FluentAssertions;
using Moq;
using PaperMail.Application.DTOs;
using PaperMail.Application.Services;
using PaperMail.Core.Entities;
using PaperMail.Core.Interfaces;

namespace PaperMail.Application.Tests;

public class EmailServiceTests
{
    private readonly Mock<IEmailRepository> _repositoryMock;
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _repositoryMock = new Mock<IEmailRepository>();
        _service = new EmailService(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("emailRepository");
    }

    [Fact]
    public async Task GetInboxAsync_ValidParameters_ReturnsMappedDtos()
    {
        // Arrange
        var from = EmailAddress.Create("sender@example.com");
        var to = new[] { EmailAddress.Create("recipient@example.com") };
        var emails = new[]
        {
            Email.Create(from, to, "Email 1", "Body 1", null, DateTimeOffset.UtcNow),
            Email.Create(from, to, "Email 2", "Body 2", null, DateTimeOffset.UtcNow)
        };

        _repositoryMock
            .Setup(r => r.GetInboxAsync("user123", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        // Act
        var result = await _service.GetInboxAsync("user123", 0, 50);

        // Assert
        result.Should().HaveCount(2);
        result[0].Subject.Should().Be("Email 1");
        result[1].Subject.Should().Be("Email 2");
        _repositoryMock.Verify(r => r.GetInboxAsync("user123", 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = async () => await _service.GetInboxAsync("", 0, 50);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("userId");
    }

    [Fact]
    public async Task GetInboxAsync_NegativePage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _service.GetInboxAsync("user123", -1, 50);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("page");
    }

    [Fact]
    public async Task GetInboxAsync_PageSizeZero_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _service.GetInboxAsync("user123", 0, 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize");
    }

    [Fact]
    public async Task GetInboxAsync_PageSizeTooLarge_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _service.GetInboxAsync("user123", 0, 201);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize");
    }

    [Fact]
    public async Task GetEmailByIdAsync_ExistingEmail_ReturnsMappedDto()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var from = EmailAddress.Create("sender@example.com");
        var to = new[] { EmailAddress.Create("recipient@example.com") };
        var email = Email.Create(from, to, "Test Subject", "Body", "<p>HTML</p>", DateTimeOffset.UtcNow);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(emailId, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        // Act
        var result = await _service.GetEmailByIdAsync(emailId, "user123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(email.Id);
        result.Subject.Should().Be("Test Subject");
        result.From.Should().Be("sender@example.com");
        result.BodyPlain.Should().Be("Body");
        result.BodyHtml.Should().Be("<p>HTML</p>");
    }

    [Fact]
    public async Task GetEmailByIdAsync_NonExistingEmail_ReturnsNull()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(emailId, "user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Email?)null);

        // Act
        var result = await _service.GetEmailByIdAsync(emailId, "user123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_ValidId_CallsRepository()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.MarkReadAsync(emailId, "user123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsReadAsync(emailId, "user123");

        // Assert
        _repositoryMock.Verify(r => r.MarkReadAsync(emailId, "user123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_ValidRequest_ReturnsEmailId()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Draft Subject",
            BodyPlain = "Draft content"
        };

        _repositoryMock
            .Setup(r => r.SaveDraftAsync(It.IsAny<Email>(), "sender@example.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SaveDraftAsync(request, "sender@example.com");

        // Assert
        result.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.SaveDraftAsync(It.Is<Email>(e => 
            e.Subject == "Draft Subject" && 
            e.From.Value == "sender@example.com"
        ), "sender@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.SaveDraftAsync(null!, "sender@example.com");
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SaveDraftAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = "Body"
        };

        // Act & Assert
        var act = async () => await _service.SaveDraftAsync(request, "");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("userId");
    }

    [Fact]
    public async Task GetInboxAsync_WithPagination_PassesCorrectParameters()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetInboxAsync("user123", 2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Email>());

        // Act
        await _service.GetInboxAsync("user123", page: 2, pageSize: 25);

        // Assert
        _repositoryMock.Verify(r => r.GetInboxAsync("user123", 2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
