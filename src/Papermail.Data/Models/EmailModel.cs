namespace Papermail.Data.Models;

/// <summary>
/// DTO for detailed email view.
/// Includes full email content and all attachments.
/// </summary>
public class EmailModel
{
    public Guid Id { get; set; }
    public string From { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string? BodyPlain { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public List<AttachmentModel> Attachments { get; set; } = new();
}