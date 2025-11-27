using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents an email account associated with a provider (e.g., Microsoft, Google).
/// Contains OAuth tokens and configuration for accessing external email services.
/// </summary>
public class Account: IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; // Entra sub

    [Required]
    public Guid ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty; // encrypted
    public string? AccessToken { get; set; } // optional, short-lived
    public DateTimeOffset? ExpiresAt { get; set; }
    public virtual ICollection<string> Scopes { get; set; } = new List<string>();
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
