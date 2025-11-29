using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents a domain mapped to an email service provider (e.g. hotmail.com -> Outlook provider).
/// </summary>
public class Domain : IEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid domain format")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid ProviderId { get; set; }

    [ForeignKey(nameof(ProviderId))]
    public virtual Provider Provider { get; set; } = default!;
}
