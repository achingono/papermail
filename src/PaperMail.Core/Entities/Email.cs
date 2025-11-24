using System.Collections.ObjectModel;

namespace PaperMail.Core.Entities;

public sealed class Email
{
    public Guid Id { get; } = Guid.NewGuid();
    public EmailAddress From { get; }
    public IReadOnlyCollection<EmailAddress> To { get; }
    public string Subject { get; }
    public string BodyPlain { get; }
    public string? BodyHtml { get; }
    public DateTimeOffset ReceivedAt { get; }
    public bool IsRead { get; private set; }
    public IReadOnlyCollection<Attachment> Attachments { get; }

    private Email(
        EmailAddress from,
        IEnumerable<EmailAddress> to,
        string subject,
        string bodyPlain,
        string? bodyHtml,
        DateTimeOffset receivedAt,
        IEnumerable<Attachment>? attachments)
    {
        From = from;
        To = new ReadOnlyCollection<EmailAddress>(to.ToList());
        Subject = subject;
        BodyPlain = bodyPlain;
        BodyHtml = bodyHtml;
        ReceivedAt = receivedAt;
        Attachments = new ReadOnlyCollection<Attachment>((attachments ?? Array.Empty<Attachment>()).ToList());
    }

    public static Email Create(
        EmailAddress from,
        IEnumerable<EmailAddress> to,
        string subject,
        string bodyPlain,
        string? bodyHtml,
        DateTimeOffset receivedAt,
        IEnumerable<Attachment>? attachments = null)
    {
        if (from is null) throw new ArgumentNullException(nameof(from));
        if (to is null || !to.Any()) throw new ArgumentException("At least one recipient required", nameof(to));
        if (subject is null) throw new ArgumentNullException(nameof(subject));
        if (bodyPlain is null) throw new ArgumentNullException(nameof(bodyPlain));
        return new Email(from, to, subject.Trim(), bodyPlain, bodyHtml, receivedAt, attachments);
    }

    public void MarkRead() => IsRead = true;
    public void MarkUnread() => IsRead = false;
}