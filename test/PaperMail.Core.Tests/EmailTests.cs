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
}