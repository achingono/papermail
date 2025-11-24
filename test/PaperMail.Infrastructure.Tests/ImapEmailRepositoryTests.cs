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
    private readonly Mock<ITokenStorage> _tokenStorageMock;
    private readonly ImapSettings _settings;
    private readonly ImapEmailRepository _sut;

    public ImapEmailRepositoryTests()
    {
        _mailKitMock = new Mock<IMailKitWrapper>();
        _tokenStorageMock = new Mock<ITokenStorage>();
        _settings = new ImapSettings { Host = "imap.example.com", Port = 993, UseSsl = true };
        _sut = new ImapEmailRepository(_mailKitMock.Object, _settings, _tokenStorageMock.Object);
    }

    [Fact]
    public async Task GetInboxAsync_ShouldFetchEmailsViaMailKit()
    {
        var accessToken = "test-token";
        _tokenStorageMock.Setup(x => x.GetTokenAsync("current-user", It.IsAny<CancellationToken>()))
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

        _mailKitMock.Setup(x => x.FetchEmailsAsync(_settings, accessToken, 0, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmails);

        var result = await _sut.GetInboxAsync(0, 25);

        result.Should().HaveCount(1);
        result.First().Subject.Should().Be("Test Subject");
        _mailKitMock.Verify(x => x.FetchEmailsAsync(_settings, accessToken, 0, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInboxAsync_NoToken_ShouldThrow()
    {
        _tokenStorageMock.Setup(x => x.GetTokenAsync("current-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.GetInboxAsync(0, 25);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    [Fact]
    public async Task GetInboxAsync_Pagination_ShouldCalculateSkip()
    {
        var accessToken = "token";
        _tokenStorageMock.Setup(x => x.GetTokenAsync("current-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        _mailKitMock.Setup(x => x.FetchEmailsAsync(_settings, accessToken, 50, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailEntity>());

        await _sut.GetInboxAsync(page: 2, pageSize: 25);

        _mailKitMock.Verify(x => x.FetchEmailsAsync(_settings, accessToken, 50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
