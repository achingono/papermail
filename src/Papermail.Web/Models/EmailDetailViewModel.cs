using Papermail.Data.Models;

namespace Papermail.Web.Models;

/// <summary>
/// View model for email detail page with available actions.
/// </summary>
public class EmailDetailViewModel
{
    public EmailModel Email { get; set; } = null!;
    public bool CanReply { get; set; }
    public bool CanReplyAll { get; set; }
    public bool CanForward { get; set; }
    public bool CanDelete { get; set; }
    public bool CanArchive { get; set; }
    public bool CanMarkAsJunk { get; set; }
}
