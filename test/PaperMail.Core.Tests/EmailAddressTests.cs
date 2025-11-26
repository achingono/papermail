using FluentAssertions;
using PaperMail.Core.Entities;

namespace PaperMail.Core.Tests;

public class EmailAddressTests
{
    [Fact]
    public void Create_ShouldNormalizeAndStoreValue()
    {
        var addr = EmailAddress.Create(" User@Example.COM ");
        addr.Value.Should().Be("User@Example.COM".Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    [InlineData("@nope")]
    [InlineData("also@")]
    public void Create_InvalidInput_ShouldThrow(string input)
    {
        Action act = () => EmailAddress.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TooLongEmail_ShouldThrow()
    {
        var longEmail = new string('a', 250) + "@example.com";
        Action act = () => EmailAddress.Create(longEmail);
        act.Should().Throw<ArgumentException>().WithMessage("*too long*");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var email = EmailAddress.Create("test@example.com");
        email.ToString().Should().Be("test@example.com");
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("TEST@EXAMPLE.COM");
        
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void Equals_SameEmail_ShouldReturnTrue()
    {
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("test@example.com");
        
        email1.Equals(email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentCase_ShouldReturnTrue()
    {
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("TEST@EXAMPLE.COM");
        
        email1.Equals(email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentEmail_ShouldReturnFalse()
    {
        var email1 = EmailAddress.Create("test1@example.com");
        var email2 = EmailAddress.Create("test2@example.com");
        
        email1.Equals(email2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        var email = EmailAddress.Create("test@example.com");
        
        email.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ShouldReturnFalse()
    {
        var email = EmailAddress.Create("test@example.com");
        
        email.Equals("test@example.com").Should().BeFalse();
    }

    [Fact]
    public void Create_ValidEmailWithSpaces_ShouldTrim()
    {
        var email = EmailAddress.Create("  user@example.com  ");
        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.name@domain.com")]
    [InlineData("user+tag@domain.co.uk")]
    [InlineData("first.last@sub.domain.com")]
    public void Create_ValidEmailFormats_ShouldSucceed(string validEmail)
    {
        var email = EmailAddress.Create(validEmail);
        email.Value.Should().Be(validEmail);
    }

    [Fact]
    public void Create_NullEmail_ShouldThrow()
    {
        Action act = () => EmailAddress.Create(null!);
        act.Should().Throw<ArgumentException>();
    }
}
