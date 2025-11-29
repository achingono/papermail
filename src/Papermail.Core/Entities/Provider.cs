using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Papermail.Core.Configuration;

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
    /// Gets or sets the IMAP settings for the provider.
    /// </summary>
    public ImapSettings Imap { get; set; } = new ImapSettings();

    /// <summary>
    /// Gets or sets the SMTP settings for the provider.
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new SmtpSettings();

    /// <summary>
    /// Collection of domains that map to this provider (e.g. hotmail.com, live.com).
    /// </summary>
    public virtual ICollection<Domain> Domains { get; set; } = new List<Domain>();

    /// <summary>
    /// Gets or sets the collection of accounts associated with this provider.
    /// </summary>
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}