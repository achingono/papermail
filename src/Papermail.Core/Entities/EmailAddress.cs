namespace Papermail.Core.Entities;

/// <summary>
/// Represents an email address value object with validation.
/// This is an immutable type that ensures email addresses are valid upon creation.
/// </summary>
public sealed class EmailAddress
{
    /// <summary>
    /// Gets the email address string value.
    /// </summary>
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new email address instance after validating the format.
    /// </summary>
    /// <param name="value">The email address string to validate and create.</param>
    /// <returns>A new EmailAddress instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the email address is empty, too long, or has an invalid format.</exception>
    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty", nameof(value));
        if (value.Length > 254)
            throw new ArgumentException("Email address too long", nameof(value));
        // Basic pattern (keep lightweight, no heavy regex for performance)
        if (!value.Contains('@') || value.StartsWith("@") || value.EndsWith("@"))
            throw new ArgumentException("Invalid email address format", nameof(value));
        return new EmailAddress(value.Trim());
    }

    /// <summary>
    /// Returns the email address as a string.
    /// </summary>
    /// <returns>The email address string value.</returns>
    public override string ToString() => Value;
    
    /// <summary>
    /// Returns a hash code for the email address (case-insensitive).
    /// </summary>
    /// <returns>A hash code for the current email address.</returns>
    public override int GetHashCode() => Value.ToLowerInvariant().GetHashCode();
    
    /// <summary>
    /// Determines whether the specified object is equal to the current email address (case-insensitive comparison).
    /// </summary>
    /// <param name="obj">The object to compare with the current email address.</param>
    /// <returns>True if the specified object is equal to the current email address; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is EmailAddress other &&
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
}