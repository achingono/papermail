namespace PaperMail.Core.Entities;

public sealed class EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

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

    public override string ToString() => Value;
    public override int GetHashCode() => Value.ToLowerInvariant().GetHashCode();
    public override bool Equals(object? obj) => obj is EmailAddress other &&
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
}
