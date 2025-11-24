namespace PaperMail.Application.DTOs;

/// <summary>
/// DTO for email attachment metadata.
/// </summary>
public class AttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
