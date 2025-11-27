namespace Papermail.Data.Models;

/// <summary>
/// Request DTO for composing a new email.
/// </summary>
public class DraftModel
{
    public List<string> To { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string? BodyPlain { get; set; }
    public string? BodyHtml { get; set; }
}