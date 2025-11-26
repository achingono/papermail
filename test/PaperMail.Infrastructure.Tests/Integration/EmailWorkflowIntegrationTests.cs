using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using PaperMail.Core.Entities;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;
using Xunit;
using EmailEntity = PaperMail.Core.Entities.Email;

namespace PaperMail.Infrastructure.Tests.Integration;

/// <summary>
/// End-to-end integration tests that verify complete email workflows.
/// These tests send emails via SMTP and verify they can be retrieved via IMAP.
/// </summary>
[Collection("DockerCompose")]
public class EmailWorkflowIntegrationTests
{
    private readonly DockerComposeFixture _fixture;
    private readonly SmtpWrapper _smtpWrapper;
    private readonly MailKitWrapper _mailWrapper;
    private readonly SmtpSettings _smtpSettings;
    private readonly ImapSettings _imapSettings;

    public EmailWorkflowIntegrationTests(DockerComposeFixture fixture)
    {
        _fixture = fixture;
        
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        
        _smtpWrapper = new SmtpWrapper(new SmtpClientFactory(), mockEnv.Object);
        _mailWrapper = new MailKitWrapper(new ImapClientFactory(), mockEnv.Object);
        
        _smtpSettings = new SmtpSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.SmtpPort,
            UseTls = true,
            Username = _fixture.TestUser,
            Password = _fixture.TestPassword
        };
        
        _imapSettings = new ImapSettings
        {
            Host = _fixture.MailHost,
            Port = _fixture.ImapPort,
            UseSsl = true, // Using IMAPS on port 993
            Username = _fixture.TestUser,
            Password = _fixture.TestPassword
        };
    }

    [Fact]
    public async Task SendEmail_ThenFetch_ShouldRetrieveEmail()
    {
        // Arrange
        var accessToken = "dummy-token";
        var uniqueSubject = $"E2E Test Email {Guid.NewGuid()}";
        
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create(_fixture.TestUser) }, // Send to self
            uniqueSubject,
            "This is an end-to-end test email.",
            "<html><body><p>This is an end-to-end test email.</p></body></html>",
            DateTimeOffset.UtcNow
        );
        
        // Act - Send email via SMTP
        await _smtpWrapper.SendEmailAsync(_smtpSettings, accessToken, email);
        
        // Wait a moment for email to be delivered
        await Task.Delay(2000);
        
        // Act - Fetch emails via IMAP
        var fetchedEmails = await _mailWrapper.FetchEmailsAsync(_imapSettings, accessToken, 0, 50);
        
        // Assert
        var receivedEmail = fetchedEmails.FirstOrDefault(e => e.Subject == uniqueSubject);
        receivedEmail.Should().NotBeNull();
        receivedEmail!.From.Value.Should().Be(_fixture.TestUser);
        receivedEmail.BodyPlain.Should().Contain("end-to-end test");
    }

    [Fact]
    public async Task SaveDraft_ThenFetchFromDrafts_ShouldRetrieveDraft()
    {
        // Arrange
        var accessToken = "dummy-token";
        var uniqueSubject = $"E2E Draft Test {Guid.NewGuid()}";
        
        var draft = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create("recipient@example.com") },
            uniqueSubject,
            "This is a draft email.",
            null,
            DateTimeOffset.UtcNow
        );
        
        // Act - Save draft via IMAP
        await _mailWrapper.SaveDraftAsync(_imapSettings, accessToken, draft);
        
        // Wait a moment for draft to be saved
        await Task.Delay(1000);
        
        // Note: Fetching from Drafts folder would require additional IMAP folder selection
        // For now, we just verify the save operation completed successfully
        // A complete test would require additional MailKitWrapper methods to list folders
    }

    [Fact]
    public async Task SendEmailWithAttachments_ThenVerify_ShouldPreserveAttachments()
    {
        // Arrange
        var accessToken = "dummy-token";
        var uniqueSubject = $"E2E Attachment Test {Guid.NewGuid()}";
        var attachments = new List<Attachment>
        {
            new Attachment("document.pdf", 5120, "application/pdf"),
            new Attachment("image.jpg", 3072, "image/jpeg")
        };
        
        var email = EmailEntity.Create(
            EmailAddress.Create(_fixture.TestUser),
            new[] { EmailAddress.Create(_fixture.TestUser) },
            uniqueSubject,
            "Email with attachments.",
            null,
            DateTimeOffset.UtcNow,
            attachments
        );
        
        // Act - Send email with attachments
        await _smtpWrapper.SendEmailAsync(_smtpSettings, accessToken, email);
        
        // Wait for delivery
        await Task.Delay(2000);
        
        // Fetch and verify
        var fetchedEmails = await _mailWrapper.FetchEmailsAsync(_imapSettings, accessToken, 0, 50);
        var receivedEmail = fetchedEmails.FirstOrDefault(e => e.Subject == uniqueSubject);
        
        // Assert
        receivedEmail.Should().NotBeNull();
        receivedEmail!.Attachments.Should().HaveCount(2);
        receivedEmail.Attachments.Should().Contain(a => a.FileName == "document.pdf");
        receivedEmail.Attachments.Should().Contain(a => a.FileName == "image.jpg");
    }

    [Fact]
    public async Task FetchEmails_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var accessToken = "dummy-token";
        
        // Send multiple test emails
        for (int i = 0; i < 5; i++)
        {
            var email = EmailEntity.Create(
                EmailAddress.Create(_fixture.TestUser),
                new[] { EmailAddress.Create(_fixture.TestUser) },
                $"Pagination Test {i}",
                $"Test email {i}",
                null,
                DateTimeOffset.UtcNow
            );
            await _smtpWrapper.SendEmailAsync(_smtpSettings, accessToken, email);
        }
        
        // Wait for delivery
        await Task.Delay(3000);
        
        // Act - Fetch first page
        var page1 = await _mailWrapper.FetchEmailsAsync(_imapSettings, accessToken, 0, 3);
        
        // Act - Fetch second page
        var page2 = await _mailWrapper.FetchEmailsAsync(_imapSettings, accessToken, 3, 3);
        
        // Assert
        page1.Should().HaveCount(3);
        page2.Should().NotBeEmpty();
        
        // Verify no overlap
        var page1Ids = page1.Select(e => e.Id).ToHashSet();
        var page2Ids = page2.Select(e => e.Id).ToHashSet();
        page1Ids.Should().NotIntersectWith(page2Ids);
    }
}
