namespace PaperMail.Application.DTOs;

/// <summary>
/// DTO for detailed email view.
/// Includes full email content and all attachments.
/// </summary>
public class EmailDetailDto
{
    public Guid Id { get; set; }
    public string From { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string? BodyPlain { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
}
