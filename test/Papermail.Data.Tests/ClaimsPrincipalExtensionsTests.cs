using System.Security.Claims;
using Papermail.Data;

namespace Papermail.Data.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void Id_WithSubClaim_ReturnsId()
    {
        var claims = new[] { new Claim("sub", "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Id();

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void Id_WithSidClaim_ReturnsId()
    {
        var claims = new[] { new Claim(ClaimTypes.Sid, "user-456") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Id();

        Assert.Equal("user-456", result);
    }

    [Fact]
    public void Id_WithNameIdentifierClaim_ReturnsId()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-789") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Id();

        Assert.Equal("user-789", result);
    }

    [Fact]
    public void Name_WithNameClaim_ReturnsName()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "John Doe") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Name();

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void Name_WithGivenNameAndSurname_ReturnsFullName()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.GivenName, "Jane"),
            new Claim(ClaimTypes.Surname, "Smith")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Name();

        Assert.Contains("Jane", result);
        Assert.Contains("Smith", result);
    }

    [Fact]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "user@example.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Email();

        Assert.Equal("user@example.com", result);
    }

    [Fact]
    public void Email_WithUpnClaim_ReturnsEmail()
    {
        var claims = new[] { new Claim(ClaimTypes.Upn, "user@domain.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Email();

        Assert.Equal("user@domain.com", result);
    }

    [Fact]
    public void Email_WithInvalidEmail_ReturnsEmpty()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "not-an-email") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Email();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Email_WithNoEmailClaim_ReturnsEmpty()
    {
        var claims = new[] { new Claim("sub", "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = principal.Email();

        Assert.Equal(string.Empty, result);
    }
}
