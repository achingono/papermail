using FluentAssertions;
using PaperMail.Core.Entities;

namespace PaperMail.Core.Tests;

public class EmailTests
{
    private static EmailAddress Addr(string v) => EmailAddress.Create(v);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var email = Email.Create(
            Addr("from@example.com"),
            new[] { Addr("to@example.com") },
            "Subject",
            "Plain body",
            "<p>HTML body</p>",
            DateTimeOffset.UtcNow
        );

        email.From.Value.Should().Be("from@example.com");
        email.To.Should().HaveCount(1);
        email.Subject.Should().Be("Subject");
        email.BodyHtml.Should().Be("<p>HTML body</p>");
        email.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkRead_ShouldSetIsReadTrue()
    {
        var email = Email.Create(
            Addr("from@example.com"),
            new[] { Addr("to@example.com") },
            "Subject",
            "Plain body",
            null,
            DateTimeOffset.UtcNow
        );
        email.MarkRead();
        email.IsRead.Should().BeTrue();
    }

    [Fact]
    public void Create_NoRecipients_ShouldThrow()
    {
        Action act = () => Email.Create(
            Addr("from@example.com"),
            Array.Empty<EmailAddress>(),
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithId_ShouldUseProvidedId()
    {
        var id = Guid.NewGuid();
        var email = Email.CreateWithId(
            id,
            Addr("from@example.com"),
            new[] { Addr("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        email.Id.Should().Be(id);
    }

    [Fact]
    public void CreateWithId_WithAttachments_ShouldSetAttachments()
    {
        var id = Guid.NewGuid();
        var attachments = new List<Attachment>
        {
            new Attachment("file1.pdf", 1024, "application/pdf"),
            new Attachment("file2.txt", 512, "text/plain")
        };

        var email = Email.CreateWithId(
            id,
            Addr("from@example.com"),
            new[] { Addr("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow,
            attachments
        );

        email.Attachments.Should().HaveCount(2);
        email.Attachments.Should().Contain(a => a.FileName == "file1.pdf");
    }

    [Fact]
    public void Create_WithAttachments_ShouldSetAttachments()
    {
        var attachments = new List<Attachment>
        {
            new Attachment("doc.docx", 2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        };

        var email = Email.Create(
            Addr("from@example.com"),
            new[] { Addr("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow,
            attachments
        );

        email.Attachments.Should().HaveCount(1);
        email.Attachments.First().FileName.Should().Be("doc.docx");
        email.Attachments.First().SizeBytes.Should().Be(2048);
    }

    [Fact]
    public void Create_NullFrom_ShouldThrow()
    {
        Action act = () => Email.Create(
            null!,
            new[] { Addr("to@example.com") },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullTo_ShouldThrow()
    {
        Action act = () => Email.Create(
            Addr("from@example.com"),
            null!,
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );
        act.Should().Throw<ArgumentException>().WithParameterName("to");
    }

    [Fact]
    public void Create_MultipleRecipients_ShouldStoreAll()
    {
        var email = Email.Create(
            Addr("from@example.com"),
            new[] { 
                Addr("to1@example.com"),
                Addr("to2@example.com"),
                Addr("to3@example.com")
            },
            "Subject",
            "Body",
            null,
            DateTimeOffset.UtcNow
        );

        email.To.Should().HaveCount(3);
        email.To.Select(t => t.Value).Should().Contain(new[] { 
            "to1@example.com", 
            "to2@example.com", 
            "to3@example.com" 
        });
    }
}