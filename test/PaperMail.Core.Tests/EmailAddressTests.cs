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
}
