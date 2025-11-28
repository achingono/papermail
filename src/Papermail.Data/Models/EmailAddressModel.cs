namespace Papermail.Data.Models;

/// <summary>
/// DTO for email address with name and address.
/// </summary>
public class EmailAddressModel
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
