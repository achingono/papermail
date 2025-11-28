using Papermail.Core.Entities;
using Papermail.Data.Mappers;
using Papermail.Data.Models;

namespace Papermail.Data.Tests;

public class EmailMapperTests
{
    [Fact]
    public void ToListItemDto_MapsEmailToListItem()
    {
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Test Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        var result = EmailMapper.ToListItemDto(email);

        Assert.Equal(email.Id, result.Id);
        Assert.Equal("sender@test.com", result.From);
        Assert.Equal("Test Subject", result.Subject);
        Assert.False(result.IsRead);
        Assert.False(result.HasAttachments);
    }

    [Fact]
    public void ToListItemDto_WithAttachments_SetsHasAttachmentsTrue()
    {
        var attachments = new[] { new Attachment("file.txt", 100, "text/plain") };
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("recipient@test.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow,
            attachments
        );

        var result = EmailMapper.ToListItemDto(email);

        Assert.True(result.HasAttachments);
    }

    [Fact]
    public void ToDetailDto_MapsEmailToDetailModel()
    {
        var email = Email.Create(
            EmailAddress.Create("sender@test.com"),
            new[] { EmailAddress.Create("r1@test.com"), EmailAddress.Create("r2@test.com") },
            "Subject",
            "Plain body",
            "<p>HTML body</p>",
            DateTimeOffset.UtcNow
        );

        var result = EmailMapper.ToDetailDto(email);

        Assert.Equal(email.Id, result.Id);
        Assert.Equal("sender@test.com", result.From);
        Assert.Equal(2, result.To.Count);
        Assert.Contains("r1@test.com", result.To);
        Assert.Contains("r2@test.com", result.To);
        Assert.Equal("Subject", result.Subject);
        Assert.Equal("Plain body", result.BodyPlain);
        Assert.Equal("<p>HTML body</p>", result.BodyHtml);
    }

    [Fact]
    public void ToAttachmentModel_MapsAttachment()
    {
        var attachment = new Attachment("document.pdf", 2048, "application/pdf");

        var result = EmailMapper.ToAttachmentModel(attachment);

        Assert.Equal("document.pdf", result.FileName);
        Assert.Equal(2048, result.SizeBytes);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public void ToEntity_MapsDraftToEmail()
    {
        var draft = new DraftModel
        {
            To = new List<string> { "recipient@test.com" },
            Subject = "Draft Subject",
            BodyPlain = "Draft body",
            BodyHtml = "<p>Draft HTML</p>"
        };

        var result = EmailMapper.ToEntity(draft, "sender@test.com");

        Assert.Equal("sender@test.com", result.From.Value);
        Assert.Single(result.To);
        Assert.Equal("recipient@test.com", result.To.First().Value);
        Assert.Equal("Draft Subject", result.Subject);
        Assert.Equal("Draft body", result.BodyPlain);
        Assert.Equal("<p>Draft HTML</p>", result.BodyHtml);
    }
}
