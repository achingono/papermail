using PaperMail.Application.DTOs;
using PaperMail.Core.Entities;

namespace PaperMail.Application.Mappers;

/// <summary>
/// Maps between Email entities and DTOs.
/// </summary>
public static class EmailMapper
{
    public static EmailListItemDto ToListItemDto(Email email)
    {
        return new EmailListItemDto
        {
            Id = email.Id,
            From = email.From.Value,
            Subject = email.Subject,
            ReceivedAt = email.ReceivedAt.DateTime,
            IsRead = email.IsRead,
            HasAttachments = email.Attachments.Any()
        };
    }

    public static EmailDetailDto ToDetailDto(Email email)
    {
        return new EmailDetailDto
        {
            Id = email.Id,
            From = email.From.Value,
            To = email.To.Select(e => e.Value).ToList(),
            Subject = email.Subject,
            BodyPlain = email.BodyPlain,
            BodyHtml = email.BodyHtml,
            ReceivedAt = email.ReceivedAt.DateTime,
            IsRead = email.IsRead,
            Attachments = email.Attachments.Select(ToAttachmentDto).ToList()
        };
    }

    public static AttachmentDto ToAttachmentDto(Attachment attachment)
    {
        return new AttachmentDto
        {
            FileName = attachment.FileName,
            SizeBytes = attachment.SizeBytes,
            ContentType = attachment.ContentType
        };
    }

    public static Email ToEntity(ComposeEmailRequest request, string fromAddress)
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
