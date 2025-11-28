using System;
using System.Collections.Generic;
using System.Linq;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class EmailTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsEmail()
    {
        var from = EmailAddress.Create("sender@test.com");
        var to = new[] { EmailAddress.Create("recipient@test.com") };
        var subject = "Test Subject";
        var bodyPlain = "Hello";
        var receivedAt = DateTimeOffset.UtcNow;
        var email = Email.Create(from, to, subject, bodyPlain, null, receivedAt);
        Assert.Equal(from, email.From);
        Assert.Equal(subject, email.Subject);
        Assert.Equal(bodyPlain, email.BodyPlain);
        Assert.Equal(receivedAt, email.ReceivedAt);
        Assert.False(email.IsRead);
    }

    [Fact]
    public void Create_NullFrom_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Email.Create(null!, new[] { EmailAddress.Create("a@b.com") }, "subj", "body", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_EmptyRecipients_ThrowsArgumentException()
    {
        var from = EmailAddress.Create("sender@test.com");
        Assert.Throws<ArgumentException>(() => Email.Create(from, Array.Empty<EmailAddress>(), "subj", "body", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_NullSubject_ThrowsArgumentNullException()
    {
        var from = EmailAddress.Create("sender@test.com");
        var to = new[] { EmailAddress.Create("recipient@test.com") };
        Assert.Throws<ArgumentNullException>(() => Email.Create(from, to, null!, "body", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_NullBodyPlain_ThrowsArgumentNullException()
    {
        var from = EmailAddress.Create("sender@test.com");
        var to = new[] { EmailAddress.Create("recipient@test.com") };
        Assert.Throws<ArgumentNullException>(() => Email.Create(from, to, "subj", null!, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkRead_SetsIsReadTrue()
    {
        var email = Email.Create(EmailAddress.Create("a@b.com"), new[] { EmailAddress.Create("c@d.com") }, "subj", "body", null, DateTimeOffset.UtcNow);
        email.MarkRead();
        Assert.True(email.IsRead);
    }

    [Fact]
    public void MarkUnread_SetsIsReadFalse()
    {
        var email = Email.Create(EmailAddress.Create("a@b.com"), new[] { EmailAddress.Create("c@d.com") }, "subj", "body", null, DateTimeOffset.UtcNow);
        email.MarkRead();
        email.MarkUnread();
        Assert.False(email.IsRead);
    }

    [Fact]
    public void CreateWithId_SetsId()
    {
        var id = Guid.NewGuid();
        var email = Email.CreateWithId(id, EmailAddress.Create("a@b.com"), new[] { EmailAddress.Create("c@d.com") }, "subj", "body", null, DateTimeOffset.UtcNow);
        Assert.Equal(id, email.Id);
    }
}
