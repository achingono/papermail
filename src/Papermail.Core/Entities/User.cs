using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents a user in the Papermail system.
/// A user can have multiple email accounts from different providers.
/// </summary>
public class User : IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [Required,
    DataType(DataType.Text),
    StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [Required,
    StringLength(100),
    DataType(DataType.Text)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [StringLength(255)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's primary email address.
    /// </summary>
    [Required,
    StringLength(256),
    DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of email accounts associated with this user.
    /// </summary>
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}