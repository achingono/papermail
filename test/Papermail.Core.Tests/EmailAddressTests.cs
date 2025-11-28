using System;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("u@d.com")]
    public void Create_ValidEmail_ReturnsInstance(string email)
    {
        var address = EmailAddress.Create(email);
        Assert.Equal(email, address.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("userdomain.com")]
    public void Create_InvalidEmail_ThrowsArgumentException(string email)
    {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(email));
    }

    [Fact]
    public void Create_TooLongEmail_ThrowsArgumentException()
    {
        var longEmail = new string('a', 255) + "@example.com";
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(longEmail));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var email = "user@example.com";
        var address = EmailAddress.Create(email);
        Assert.Equal(email, address.ToString());
    }

    [Fact]
    public void Equals_SameValue_IsTrue()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("USER@example.com");
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GetHashCode_CaseInsensitive_IsEqual()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("USER@example.com");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
