namespace Papermail.Data.Models;

/// <summary>
/// DTO for detailed email view.
/// Includes full email content and all attachments.
/// </summary>
public class EmailModel
{
    public Guid Id { get; set; }
    public EmailAddressModel From { get; set; } = new();
    public List<EmailAddressModel> To { get; set; } = new();
    public List<EmailAddressModel> Cc { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsRead { get; set; }
    public List<AttachmentModel> Attachments { get; set; } = new();
}