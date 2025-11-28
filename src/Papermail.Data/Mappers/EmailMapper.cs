using Papermail.Data.Models;
using Papermail.Core.Entities;

namespace Papermail.Data.Mappers;

/// <summary>
/// Maps between Email entities and DTOs.
/// </summary>
public static class EmailMapper
{
    /// <summary>
    /// Maps an Email entity to a list item DTO for inbox display.
    /// </summary>
    /// <param name="email">The email entity to map.</param>
    /// <returns>An email list item DTO.</returns>
    public static EmailItemModel ToListItemDto(Email email)
    {
        return new EmailItemModel
        {
            Id = email.Id,
            From = email.From.Value,
            Subject = email.Subject,
            ReceivedAt = email.ReceivedAt.DateTime,
            IsRead = email.IsRead,
            HasAttachments = email.Attachments.Any()
        };
    }

    /// <summary>
    /// Maps an Email entity to a detailed DTO for full email view.
    /// </summary>
    /// <param name="email">The email entity to map.</param>
    /// <returns>A detailed email DTO.</returns>
    public static EmailModel ToDetailDto(Email email)
    {
        return new EmailModel
        {
            Id = email.Id,
            From = email.From.Value,
            To = email.To.Select(e => e.Value).ToList(),
            Subject = email.Subject,
            BodyPlain = email.BodyPlain,
            BodyHtml = email.BodyHtml,
            ReceivedAt = email.ReceivedAt.DateTime,
            IsRead = email.IsRead,
            Attachments = email.Attachments.Select(ToAttachmentModel).ToList()
        };
    }

    /// <summary>
    /// Maps an Attachment entity to an attachment DTO.
    /// </summary>
    /// <param name="attachment">The attachment entity to map.</param>
    /// <returns>An attachment DTO.</returns>
    public static AttachmentModel ToAttachmentModel(Attachment attachment)
    {
        return new AttachmentModel
        {
            FileName = attachment.FileName,
            SizeBytes = attachment.SizeBytes,
            ContentType = attachment.ContentType
        };
    }

    /// <summary>
    /// Maps a draft DTO to an Email entity for sending or saving.
    /// </summary>
    /// <param name="request">The draft DTO containing email data.</param>
    /// <param name="fromAddress">The sender's email address.</param>
    /// <returns>An Email entity.</returns>
    public static Email ToEntity(DraftModel request, string fromAddress)
    {
        var from = EmailAddress.Create(fromAddress);
        var to = request.To.Select(EmailAddress.Create).ToList();

        return Email.Create(
            from,
            to,
            request.Subject,
            request.BodyPlain ?? string.Empty,
            request.BodyHtml,
            DateTimeOffset.UtcNow
        );
    }
}