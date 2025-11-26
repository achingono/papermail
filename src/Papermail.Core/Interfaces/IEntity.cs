using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Papermail.Core.Entities;

/// <summary>
/// Represents the base contract for domain entities that expose a unique identifier.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity instance.
    /// </summary>
    [Key]
    Guid Id { get; set; }
}