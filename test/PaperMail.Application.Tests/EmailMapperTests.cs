using FluentAssertions;
using PaperMail.Application.Mappers;
using PaperMail.Core.Entities;

namespace PaperMail.Application.Tests;

public class EmailMapperTests
{
    [Fact]
    public void ToListItemDto_MapsAllProperties()
    {
        // Arrange
        var from = EmailAddress.Create("sender@example.com");
        var to = new[] { EmailAddress.Create("recipient@example.com") };
        var attachment = new Attachment("file.pdf", 1024, "application/pdf");
        var receivedAt = DateTimeOffset.UtcNow.AddHours(-1);
        
        var email = Email.Create(from, to, "Test Subject", "Body", "<p>Body</p>", receivedAt, new[] { attachment });
        email.MarkRead();

        // Act
        var dto = EmailMapper.ToListItemDto(email);

        // Assert
        dto.Id.Should().Be(email.Id);
        dto.From.Should().Be("sender@example.com");
        dto.Subject.Should().Be("Test Subject");
        dto.ReceivedAt.Should().Be(receivedAt.DateTime);
        dto.IsRead.Should().BeTrue();
        dto.HasAttachments.Should().BeTrue();
    }

    [Fact]
    public void ToListItemDto_EmailWithoutAttachments_HasAttachmentsFalse()
    {
        // Arrange
        var from = EmailAddress.Create("sender@example.com");
        var to = new[] { EmailAddress.Create("recipient@example.com") };
        
        var email = Email.Create(from, to, "Subject", "Body", null, DateTimeOffset.UtcNow);

        // Act
        var dto = EmailMapper.ToListItemDto(email);

        // Assert
        dto.HasAttachments.Should().BeFalse();
    }

    [Fact]
    public void ToDetailDto_MapsAllProperties()
    {
        // Arrange
        var from = EmailAddress.Create("sender@example.com");
        var to = new[] 
        { 
            EmailAddress.Create("recipient1@example.com"),
            EmailAddress.Create("recipient2@example.com")
        };
        var attachment = new Attachment("report.pdf", 2048, "application/pdf");
        var receivedAt = DateTimeOffset.UtcNow.AddDays(-1);
        
        var email = Email.Create(
            from, to, "Detailed Subject", "Plain body", "<html>Rich body</html>", 
            receivedAt, new[] { attachment });
        email.MarkRead();

        // Act
        var dto = EmailMapper.ToDetailDto(email);

        // Assert
        dto.Id.Should().Be(email.Id);
        dto.From.Should().Be("sender@example.com");
        dto.To.Should().HaveCount(2);
        dto.To.Should().Contain("recipient1@example.com");
        dto.To.Should().Contain("recipient2@example.com");
        dto.Subject.Should().Be("Detailed Subject");
        dto.BodyPlain.Should().Be("Plain body");
        dto.BodyHtml.Should().Be("<html>Rich body</html>");
        dto.ReceivedAt.Should().Be(receivedAt.DateTime);
        dto.IsRead.Should().BeTrue();
        dto.Attachments.Should().HaveCount(1);
        dto.Attachments[0].FileName.Should().Be("report.pdf");
        dto.Attachments[0].SizeBytes.Should().Be(2048);
        dto.Attachments[0].ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public void ToAttachmentDto_MapsAllProperties()
    {
        // Arrange
        var attachment = new Attachment("document.docx", 4096, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        // Act
        var dto = EmailMapper.ToAttachmentDto(attachment);

        // Assert
        dto.FileName.Should().Be("document.docx");
        dto.SizeBytes.Should().Be(4096);
        dto.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    [Fact]
    public void ToEntity_CreatesValidEmail()
    {
        // Arrange
        var request = new DTOs.ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "New Email",
            BodyPlain = "Plain content",
            BodyHtml = "<p>Rich content</p>"
        };

        // Act
        var email = EmailMapper.ToEntity(request, "sender@example.com");

        // Assert
        email.Should().NotBeNull();
        email.From.Value.Should().Be("sender@example.com");
        email.To.Should().HaveCount(1);
        email.To.First().Value.Should().Be("recipient@example.com");
        email.Subject.Should().Be("New Email");
        email.BodyPlain.Should().Be("Plain content");
        email.BodyHtml.Should().Be("<p>Rich content</p>");
        email.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToEntity_WithMultipleRecipients_CreatesEmailWithAllRecipients()
    {
        // Arrange
        var request = new DTOs.ComposeEmailRequest
        {
            To = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" },
            Subject = "Group Email",
            BodyPlain = "Message for all"
        };

        // Act
        var email = EmailMapper.ToEntity(request, "sender@example.com");

        // Assert
        email.To.Should().HaveCount(3);
        email.To.Select(e => e.Value).Should().Contain(new[] 
        { 
            "user1@example.com", 
            "user2@example.com", 
            "user3@example.com" 
        });
    }

    [Fact]
    public void ToEntity_WithNullBodyPlain_UsesEmptyString()
    {
        // Arrange
        var request = new DTOs.ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "HTML Only",
            BodyPlain = null,
            BodyHtml = "<p>HTML content</p>"
        };

        // Act
        var email = EmailMapper.ToEntity(request, "sender@example.com");

        // Assert
        email.BodyPlain.Should().Be(string.Empty);
        email.BodyHtml.Should().Be("<p>HTML content</p>");
    }
}
