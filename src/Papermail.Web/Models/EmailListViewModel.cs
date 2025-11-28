using Papermail.Data.Models;

namespace Papermail.Web.Models;

public class EmailListViewModel
{
    public List<EmailItemModel> Items { get; set; } = new();
    public string FolderName { get; set; } = string.Empty;
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public string SortDirection { get; set; } = "desc";
    public int TotalPages { get; set; }
    public string PageUrl { get; set; } = string.Empty;
}
