using FluentValidation.TestHelper;
using Papermail.Core.Entities;
using Papermail.Core.Validation;

namespace Papermail.Core.Tests;

/// <summary>
/// Unit tests for AccountValidator to ensure validation rules are correctly enforced.
/// </summary>
public class AccountValidatorTests
{
    private readonly AccountValidator _validator;

    public AccountValidatorTests()
    {
        _validator = new AccountValidator();
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        var account = new Account { UserId = string.Empty };
        var result = _validator.TestValidate(account);
        result.ShouldHaveValidationErrorFor(a => a.UserId)
            .WithErrorMessage("'User ID' must not be empty.");
    }

    [Fact]
    public void Should_Have_Error_When_EmailAddress_Is_Empty()
    {
        // Arrange
        var account = new Account 
        { 
            UserId = "test-user-id", 
            EmailAddress = string.Empty 
        };

        // Act
        var result = _validator.TestValidate(account);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.EmailAddress);
    }

    [Fact]
    public void Should_Have_Error_When_EmailAddress_Is_Invalid()
    {
        var account = new Account 
        { 
            UserId = "test-user-id", 
            EmailAddress = "not-an-email" 
        };
        var result = _validator.TestValidate(account);
        result.ShouldHaveValidationErrorFor(a => a.EmailAddress)
            .WithErrorMessage("'Email Address' is not a valid email address.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Account_Is_Valid()
    {
        var provider = new Provider { Name = "TestProvider" };
        var account = new Account 
        { 
            UserId = "test-user-id", 
            EmailAddress = "test@example.com",
            Provider = provider,
            ProviderId = provider.Id
        };
        var result = _validator.TestValidate(account);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.name@domain.com")]
    [InlineData("user+tag@domain.co.uk")]
    public void Should_Accept_Valid_Email_Formats(string emailAddress)
    {
        // Arrange
        var account = new Account 
        { 
            UserId = "test-user-id", 
            EmailAddress = emailAddress,
            Provider = new Provider { Name = "TestProvider" }
        };

        // Act
        var result = _validator.TestValidate(account);

        // Assert
        result.ShouldNotHaveValidationErrorFor(a => a.EmailAddress);
    }
}
