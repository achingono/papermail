using PaperMail.Application.DTOs;
using PaperMail.Core.Entities;

namespace PaperMail.Application.Validators;

/// <summary>
/// Validates email composition requests.
/// </summary>
public static class ComposeValidator
{
    public const int MaxSubjectLength = 200;
    public const int MaxBodyLength = 1_000_000; // 1MB plain text

    public static ValidationResult Validate(ComposeEmailRequest request)
    {
        var errors = new List<string>();

        // Validate recipients
        if (request.To == null || !request.To.Any())
        {
            errors.Add("At least one recipient is required");
        }
        else
        {
            foreach (var email in request.To)
            {
                try
                {
                    EmailAddress.Create(email);
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"Invalid email address '{email}': {ex.Message}");
                }
            }
        }

        // Validate subject
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            errors.Add("Subject is required");
        }
        else if (request.Subject.Length > MaxSubjectLength)
        {
            errors.Add($"Subject exceeds maximum length of {MaxSubjectLength} characters");
        }

        // Validate body
        if (string.IsNullOrWhiteSpace(request.BodyPlain) && string.IsNullOrWhiteSpace(request.BodyHtml))
        {
            errors.Add("Email body is required");
        }

        if (request.BodyPlain?.Length > MaxBodyLength)
        {
            errors.Add($"Plain text body exceeds maximum length of {MaxBodyLength} characters");
        }

        if (request.BodyHtml?.Length > MaxBodyLength)
        {
            errors.Add($"HTML body exceeds maximum length of {MaxBodyLength} characters");
        }

        return new ValidationResult(errors);
    }
}

public class ValidationResult
{
    public ValidationResult(List<string> errors)
    {
        Errors = errors;
    }

    public List<string> Errors { get; }
    public bool IsValid => !Errors.Any();
}
