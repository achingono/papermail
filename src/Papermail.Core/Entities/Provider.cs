using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents an email service provider (e.g., Microsoft, Google, Yahoo).
/// Providers can have multiple accounts associated with them.
/// </summary>
public class Provider: IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the email service provider.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of accounts associated with this provider.
    /// </summary>
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}