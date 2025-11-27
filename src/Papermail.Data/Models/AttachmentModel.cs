namespace Papermail.Data.Models;

/// <summary>
/// DTO for email attachment metadata.
/// </summary>
public class AttachmentModel
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}