namespace Papermail.Data.Models;
/// <summary>
/// DTO for email list view (inbox/folder listing).
/// Optimized for E-Ink displays with minimal data transfer.
/// </summary>
public class EmailItemModel
{
    public Guid Id { get; set; }
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }
}