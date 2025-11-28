using Microsoft.AspNetCore.Http;
using Moq;
using Papermail.Web.Security;
using System.Security.Claims;

namespace Papermail.Web.Tests;

public class PrincipalAccessorTests
{
    [Fact]
    public void Principal_WhenUserExists_ReturnsPrincipal()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new[] { new Claim("sub", "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var httpContext = new DefaultHttpContext { User = principal };
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var accessor = new PrincipalAccessor(mockHttpContextAccessor.Object);

        Assert.NotNull(accessor.Principal);
        Assert.Equal(principal, accessor.Principal);
    }

    [Fact]
    public void Principal_WhenNoHttpContext_ReturnsNull()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var accessor = new PrincipalAccessor(mockHttpContextAccessor.Object);

        Assert.Null(accessor.Principal);
    }
}
