using Papermail.Data.Models;
using Papermail.Core.Entities;

namespace Papermail.Data.Mappers;

/// <summary>
/// Maps between Email entities and DTOs.
/// </summary>
public static class EmailMapper
{
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

    public static AttachmentModel ToAttachmentModel(Attachment attachment)
    {
        return new AttachmentModel
        {
            FileName = attachment.FileName,
            SizeBytes = attachment.SizeBytes,
            ContentType = attachment.ContentType
        };
    }

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