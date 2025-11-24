using FluentAssertions;
using Moq;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;

namespace PaperMail.Infrastructure.Tests;

public class MailKitWrapperTests
{
    private readonly Mock<IImapClientFactory> _clientFactoryMock;
    private readonly Mock<IImapClient> _imapClientMock;
    private readonly Mock<IMailFolder> _inboxMock;
    private readonly MailKitWrapper _wrapper;
    private readonly ImapSettings _settings;

    public MailKitWrapperTests()
    {
        _clientFactoryMock = new Mock<IImapClientFactory>();
        _imapClientMock = new Mock<IImapClient>();
        _inboxMock = new Mock<IMailFolder>();
        
        _settings = new ImapSettings
        {
            Host = "imap.test.com",
            Port = 993,
            UseSsl = true
        };

        _imapClientMock.Setup(c => c.Inbox).Returns(_inboxMock.Object);
        _clientFactoryMock.Setup(f => f.CreateClient()).Returns(_imapClientMock.Object);
        
        _wrapper = new MailKitWrapper(_clientFactoryMock.Object);
    }

    [Fact]
    public void Constructor_NullClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MailKitWrapper(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clientFactory");
    }

    [Fact]
    public async Task FetchEmailsAsync_NullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(null!, "token", 0, 10);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task FetchEmailsAsync_EmptyAccessToken_ThrowsArgumentException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(_settings, "", 0, 10);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("accessToken");
    }

    [Fact]
    public async Task FetchEmailsAsync_NullAccessToken_ThrowsArgumentException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(_settings, null!, 0, 10);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("accessToken");
    }

    [Fact]
    public async Task FetchEmailsAsync_NegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(_settings, "token", -1, 10);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("skip");
    }

    [Fact]
    public async Task FetchEmailsAsync_ZeroTake_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(_settings, "token", 0, 0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("take");
    }

    [Fact]
    public async Task FetchEmailsAsync_NegativeTake_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _wrapper.FetchEmailsAsync(_settings, "token", 0, -5);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("take");
    }

    [Fact]
    public async Task FetchEmailsAsync_ValidParameters_ConnectsToImapServer()
    {
        // Arrange
        _inboxMock.Setup(i => i.Count).Returns(0);

        // Act
        await _wrapper.FetchEmailsAsync(_settings, "access-token", 0, 10);

        // Assert
        _imapClientMock.Verify(c => c.ConnectAsync(
            "imap.test.com",
            993,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_ValidParameters_AuthenticatesWithOAuth()
    {
        // Arrange
        _inboxMock.Setup(i => i.Count).Returns(0);

        // Act
        await _wrapper.FetchEmailsAsync(_settings, "access-token", 0, 10);

        // Assert
        _imapClientMock.Verify(c => c.AuthenticateAsync(
            It.IsAny<MailKit.Security.SaslMechanism>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_ValidParameters_OpensInboxReadOnly()
    {
        // Arrange
        _inboxMock.Setup(i => i.Count).Returns(0);

        // Act
        await _wrapper.FetchEmailsAsync(_settings, "access-token", 0, 10);

        // Assert
        _inboxMock.Verify(i => i.OpenAsync(
            FolderAccess.ReadOnly,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_ValidParameters_DisconnectsAfterFetch()
    {
        // Arrange
        _inboxMock.Setup(i => i.Count).Returns(0);

        // Act
        await _wrapper.FetchEmailsAsync(_settings, "access-token", 0, 10);

        // Assert
        _imapClientMock.Verify(c => c.DisconnectAsync(
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_EmptyInbox_ReturnsEmptyList()
    {
        // Arrange
        _inboxMock.Setup(i => i.Count).Returns(0);

        // Act
        var result = await _wrapper.FetchEmailsAsync(_settings, "token", 0, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapToEmail_ValidMessage_ReturnsEmail()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John Doe", "john@example.com"));
        message.To.Add(new MailboxAddress("Jane Smith", "jane@example.com"));
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test body" };
        message.Date = DateTimeOffset.UtcNow;

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.Should().NotBeNull();
        result.From.Value.Should().Be("john@example.com");
        result.To.Should().HaveCount(1);
        result.To.First().Value.Should().Be("jane@example.com");
        result.Subject.Should().Be("Test Subject");
        result.BodyPlain.Should().Be("Test body");
    }

    [Fact]
    public void MapToEmail_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => MailKitWrapper.MapToEmail(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("message");
    }

    [Fact]
    public void MapToEmail_NoFromAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress("Jane", "jane@example.com"));
        message.Subject = "Test";
        message.Body = new TextPart("plain") { Text = "Body" };

        // Act & Assert
        var act = () => MailKitWrapper.MapToEmail(message);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Email must have a sender");
    }

    [Fact]
    public void MapToEmail_NoToRecipients_AddsPlaceholder()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John", "john@example.com"));
        message.Subject = "Test";
        message.Body = new TextPart("plain") { Text = "Body" };

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.To.Should().HaveCount(1);
        result.To.First().Value.Should().Be("undisclosed-recipients@example.com");
    }

    [Fact]
    public void MapToEmail_MultipleRecipients_MapsAll()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John", "john@example.com"));
        message.To.Add(new MailboxAddress("Jane", "jane@example.com"));
        message.To.Add(new MailboxAddress("Bob", "bob@example.com"));
        message.Subject = "Test";
        message.Body = new TextPart("plain") { Text = "Body" };

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.To.Should().HaveCount(2);
        result.To.Select(t => t.Value).Should().Contain(new[] { "jane@example.com", "bob@example.com" });
    }

    [Fact]
    public void MapToEmail_WithAttachment_MapsAttachment()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John", "john@example.com"));
        message.To.Add(new MailboxAddress("Jane", "jane@example.com"));
        message.Subject = "Test";

        var multipart = new Multipart("mixed");
        multipart.Add(new TextPart("plain") { Text = "Body" });
        
        var attachment = new MimePart("application", "pdf")
        {
            Content = new MimeContent(new MemoryStream(new byte[1024])),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            FileName = "test.pdf"
        };
        multipart.Add(attachment);
        
        message.Body = multipart;

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.Attachments.Should().HaveCount(1);
        result.Attachments.First().FileName.Should().Be("test.pdf");
        result.Attachments.First().SizeBytes.Should().Be(1024);
        result.Attachments.First().ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public void MapToEmail_AttachmentWithoutFileName_UsesPlaceholder()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John", "john@example.com"));
        message.To.Add(new MailboxAddress("Jane", "jane@example.com"));
        message.Subject = "Test";

        var multipart = new Multipart("mixed");
        multipart.Add(new TextPart("plain") { Text = "Body" });
        
        var attachment = new MimePart("image", "png")
        {
            Content = new MimeContent(new MemoryStream(new byte[512])),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment)
        };
        multipart.Add(attachment);
        
        message.Body = multipart;

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.Attachments.Should().HaveCount(1);
        result.Attachments.First().FileName.Should().Be("unknown");
    }

    [Fact]
    public void MapToEmail_PreservesDate()
    {
        // Arrange
        var expectedDate = new DateTimeOffset(2024, 11, 24, 10, 30, 0, TimeSpan.Zero);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("John", "john@example.com"));
        message.To.Add(new MailboxAddress("Jane", "jane@example.com"));
        message.Subject = "Test";
        message.Body = new TextPart("plain") { Text = "Body" };
        message.Date = expectedDate;

        // Act
        var result = MailKitWrapper.MapToEmail(message);

        // Assert
        result.ReceivedAt.Should().Be(expectedDate);
    }
}
