using FluentAssertions;
using PaperMail.Application.DTOs;
using PaperMail.Application.Validators;

namespace PaperMail.Application.Tests;

public class ComposeValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Valid Subject",
            BodyPlain = "Valid body content"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NoRecipients_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string>(),
            Subject = "Subject",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("At least one recipient is required");
    }

    [Fact]
    public void Validate_NullRecipients_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = null!,
            Subject = "Subject",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("At least one recipient is required");
    }

    [Fact]
    public void Validate_InvalidEmailAddress_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "not-an-email" },
            Subject = "Subject",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email address 'not-an-email'"));
    }

    [Fact]
    public void Validate_MultipleInvalidEmails_ReturnsMultipleErrors()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "invalid1", "@invalid2", "invalid3@" },
            Subject = "Subject",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Validate_EmptySubject_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Subject is required");
    }

    [Fact]
    public void Validate_WhitespaceSubject_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "   ",
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Subject is required");
    }

    [Fact]
    public void Validate_SubjectTooLong_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = new string('A', ComposeValidator.MaxSubjectLength + 1),
            BodyPlain = "Body"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain($"Subject exceeds maximum length of {ComposeValidator.MaxSubjectLength} characters");
    }

    [Fact]
    public void Validate_NoBody_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = null,
            BodyHtml = null
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Email body is required");
    }

    [Fact]
    public void Validate_EmptyBodyPlainAndHtml_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = "",
            BodyHtml = "   "
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Email body is required");
    }

    [Fact]
    public void Validate_OnlyHtmlBody_IsValid()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = null,
            BodyHtml = "<p>HTML content</p>"
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BodyPlainTooLong_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = new string('X', ComposeValidator.MaxBodyLength + 1)
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain($"Plain text body exceeds maximum length of {ComposeValidator.MaxBodyLength} characters");
    }

    [Fact]
    public void Validate_BodyHtmlTooLong_ReturnsError()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string> { "recipient@example.com" },
            Subject = "Subject",
            BodyPlain = "Plain",
            BodyHtml = new string('Y', ComposeValidator.MaxBodyLength + 1)
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain($"HTML body exceeds maximum length of {ComposeValidator.MaxBodyLength} characters");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new ComposeEmailRequest
        {
            To = new List<string>(),
            Subject = "",
            BodyPlain = null
        };

        // Act
        var result = ComposeValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain("At least one recipient is required");
        result.Errors.Should().Contain("Subject is required");
        result.Errors.Should().Contain("Email body is required");
    }
}
