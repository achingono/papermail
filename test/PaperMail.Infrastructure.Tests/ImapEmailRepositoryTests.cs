using FluentAssertions;
using Moq;
using PaperMail.Core.Entities;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Tests;

public class ImapEmailRepositoryTests
{
    private readonly Mock<IMailKitWrapper> _mailKitMock;
    private readonly Mock<ISmtpWrapper> _smtpWrapperMock;
    private readonly Mock<ITokenStorage> _tokenStorageMock;
    private readonly ImapSettings _imapSettings;
    private readonly SmtpSettings _smtpSettings;
    private readonly ImapEmailRepository _sut;

    public ImapEmailRepositoryTests()
    {
        _mailKitMock = new Mock<IMailKitWrapper>();
        _smtpWrapperMock = new Mock<ISmtpWrapper>();
        _tokenStorageMock = new Mock<ITokenStorage>();
        _imapSettings = new ImapSettings { Host = "imap.example.com", Port = 993, UseSsl = true, Username = "test@example.com", Password = "pass" };
        _smtpSettings = new SmtpSettings { Host = "smtp.example.com", Port = 587, UseTls = true, Username = "test@example.com", Password = "pass" };
        _sut = new ImapEmailRepository(
            _mailKitMock.Object,
            _smtpWrapperMock.Object,
            Microsoft.Extensions.Options.Options.Create(_imapSettings),
            Microsoft.Extensions.Options.Options.Create(_smtpSettings),
            _tokenStorageMock.Object);
    }

    [Fact]
    public async Task GetInboxAsync_ShouldFetchEmailsViaMailKit()
    {
        var accessToken = "test-token";
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        var expectedEmails = new List<EmailEntity>
        {
            EmailEntity.Create(
                EmailAddress.Create("from@example.com"),
                new[] { EmailAddress.Create("to@example.com") },
                "Test Subject",
                "Plain body",
                null,
                DateTimeOffset.UtcNow
            )
        };

        _mailKitMock.Setup(x => x.FetchEmailsAsync(It.IsAny<ImapSettings>(), accessToken, 0, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmails);

        var result = await _sut.GetInboxAsync("user@example.com", 0, 25);

        result.Should().HaveCount(1);
        result.First().Subject.Should().Be("Test Subject");
        _mailKitMock.Verify(x => x.FetchEmailsAsync(It.IsAny<ImapSettings>(), accessToken, 0, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_NoToken_ShouldThrow()
    {
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.GetInboxAsync("user@example.com", 0, 25);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task GetInboxAsync_Pagination_ShouldCalculateSkip()
    {
        var accessToken = "token";
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.FetchEmailsAsync(It.IsAny<ImapSettings>(), accessToken, 50, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailEntity>());

        await _sut.GetInboxAsync("user@example.com", page: 2, pageSize: 25);

        _mailKitMock.Verify(x => x.FetchEmailsAsync(It.IsAny<ImapSettings>(), accessToken, 50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_AuthenticationException_ShouldReturnEmpty()
    {
        var accessToken = "test-token";
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.FetchEmailsAsync(It.IsAny<ImapSettings>(), accessToken, 0, 25, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Security.Authentication.AuthenticationException("Auth failed"));

        var result = await _sut.GetInboxAsync("user@example.com", 0, 25);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidToken_ShouldReturnEmail()
    {
        var emailId = Guid.NewGuid();
        var accessToken = "test-token";
        var expectedEmail = EmailEntity.CreateWithId(
            emailId,
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Test Subject",
            "Plain body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.GetEmailByIdAsync(It.IsAny<ImapSettings>(), accessToken, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmail);

        var result = await _sut.GetByIdAsync(emailId, "user@example.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(emailId);
        result.Subject.Should().Be("Test Subject");
        _mailKitMock.Verify(x => x.GetEmailByIdAsync(It.IsAny<ImapSettings>(), accessToken, emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NoToken_ShouldThrow()
    {
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.GetByIdAsync(Guid.NewGuid(), "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task GetByIdAsync_AuthenticationException_ShouldReturnNull()
    {
        var accessToken = "test-token";
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.GetEmailByIdAsync(It.IsAny<ImapSettings>(), accessToken, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Security.Authentication.AuthenticationException("Auth failed"));

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), "user@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkReadAsync_WithValidToken_ShouldCallMailKit()
    {
        var emailId = Guid.NewGuid();
        var accessToken = "test-token";

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        await _sut.MarkReadAsync(emailId, "user@example.com");

        _mailKitMock.Verify(x => x.MarkReadAsync(It.IsAny<ImapSettings>(), accessToken, emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkReadAsync_NoToken_ShouldThrow()
    {
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.MarkReadAsync(Guid.NewGuid(), "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task DeleteAsync_WithValidToken_ShouldCallMailKit()
    {
        var emailId = Guid.NewGuid();
        var accessToken = "test-token";

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        await _sut.DeleteAsync(emailId, "user@example.com");

        _mailKitMock.Verify(x => x.DeleteAsync(It.IsAny<ImapSettings>(), accessToken, emailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NoToken_ShouldThrow()
    {
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.DeleteAsync(Guid.NewGuid(), "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task SaveDraftAsync_WithValidToken_ShouldCallMailKit()
    {
        var accessToken = "test-token";
        var draft = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Draft Subject",
            "Draft body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        await _sut.SaveDraftAsync(draft, "user@example.com");

        _mailKitMock.Verify(x => x.SaveDraftAsync(It.IsAny<ImapSettings>(), accessToken, draft, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraftAsync_NoToken_ShouldThrow()
    {
        var draft = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Draft Subject",
            "Draft body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.SaveDraftAsync(draft, "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task SendEmailAsync_WithValidToken_ShouldCallSmtpWrapper()
    {
        var accessToken = "test-token";
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Email Subject",
            "Email body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        await _sut.SendEmailAsync(email, "user@example.com");

        _smtpWrapperMock.Verify(x => x.SendEmailAsync(It.IsAny<SmtpSettings>(), accessToken, email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_NoToken_ShouldThrow()
    {
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Email Subject",
            "Email body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.SendEmailAsync(email, "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task GetInboxAsync_UsesImapPasswordFallback_WhenConfigured()
    {
        var accessToken = "test-token";
        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.FetchEmailsAsync(
                It.Is<ImapSettings>(s => s.Password == "pass"),
                accessToken, 0, 25,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailEntity>());

        await _sut.GetInboxAsync("user@example.com", 0, 25);

        _mailKitMock.Verify(x => x.FetchEmailsAsync(
            It.Is<ImapSettings>(s => s.Password == "pass"),
            accessToken, 0, 25,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_UsesSmtpSettings_WithUserId()
    {
        var accessToken = "test-token";
        var email = EmailEntity.Create(
            EmailAddress.Create("from@example.com"),
            new[] { EmailAddress.Create("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        _tokenStorageMock.Setup(x => x.GetAccessTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        await _sut.SendEmailAsync(email, "user@example.com");

        _smtpWrapperMock.Verify(x => x.SendEmailAsync(
            It.Is<SmtpSettings>(s => s.Username == "user@example.com" && s.Host == "smtp.example.com"),
            accessToken,
            email,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
