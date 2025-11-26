using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents an email message with immutable properties.
/// This is a value object that encapsulates email data and behavior.
/// </summary>
public sealed class Email
{
    /// <summary>
    /// Gets the unique identifier for the email.
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// Gets the sender's email address.
    /// </summary>
    public EmailAddress From { get; }
    
    /// <summary>
    /// Gets the collection of recipient email addresses.
    /// </summary>
    public IReadOnlyCollection<EmailAddress> To { get; }
    
    /// <summary>
    /// Gets the email subject.
    /// </summary>
    public string Subject { get; }
    
    /// <summary>
    /// Gets the plain text body of the email.
    /// </summary>
    public string BodyPlain { get; }
    
    /// <summary>
    /// Gets the HTML body of the email, if available.
    /// </summary>
    public string? BodyHtml { get; }
    
    /// <summary>
    /// Gets the date and time when the email was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; }
    
    /// <summary>
    /// Gets a value indicating whether the email has been read.
    /// </summary>
    public bool IsRead { get; private set; }
    
    /// <summary>
    /// Gets the collection of attachments associated with this email.
    /// </summary>
    public IReadOnlyCollection<Attachment> Attachments { get; }

    private Email(
        Guid? id,
        EmailAddress from,
        IEnumerable<EmailAddress> to,
        string subject,
        string bodyPlain,
        string? bodyHtml,
        DateTimeOffset receivedAt,
        IEnumerable<Attachment>? attachments)
    {
        Id = id ?? Guid.NewGuid();
        From = from;
        To = new ReadOnlyCollection<EmailAddress>(to.ToList());
        Subject = subject;
        BodyPlain = bodyPlain;
        BodyHtml = bodyHtml;
        ReceivedAt = receivedAt;
        Attachments = new ReadOnlyCollection<Attachment>((attachments ?? Array.Empty<Attachment>()).ToList());
    }

    /// <summary>
    /// Creates a new email instance with a generated unique identifier.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="to">The collection of recipient email addresses.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="bodyPlain">The plain text body of the email.</param>
    /// <param name="bodyHtml">The HTML body of the email (optional).</param>
    /// <param name="receivedAt">The date and time when the email was received.</param>
    /// <param name="attachments">The collection of email attachments (optional).</param>
    /// <returns>A new email instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when no recipients are provided.</exception>
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
        return new Email(null, from, to, subject.Trim(), bodyPlain, bodyHtml, receivedAt, attachments);
    }

    /// <summary>
    /// Creates a new email instance with a specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the email.</param>
    /// <param name="from">The sender's email address.</param>
    /// <param name="to">The collection of recipient email addresses.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="bodyPlain">The plain text body of the email.</param>
    /// <param name="bodyHtml">The HTML body of the email (optional).</param>
    /// <param name="receivedAt">The date and time when the email was received.</param>
    /// <param name="attachments">The collection of email attachments (optional).</param>
    /// <returns>A new email instance with the specified identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when no recipients are provided.</exception>
    public static Email CreateWithId(
        Guid id,
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
        return new Email(id, from, to, subject.Trim(), bodyPlain, bodyHtml, receivedAt, attachments);
    }

    /// <summary>
    /// Marks the email as read.
    /// </summary>
    public void MarkRead() => IsRead = true;
    
    /// <summary>
    /// Marks the email as unread.
    /// </summary>
    public void MarkUnread() => IsRead = false;
}