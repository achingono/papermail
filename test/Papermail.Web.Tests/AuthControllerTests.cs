using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Papermail.Data.Services;
using Papermail.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace Papermail.Web.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        
        _controller = new AuthController(
            _mockAccountService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_controller);
    }

    [Fact]
    public void Controller_HasCorrectRouteAttribute()
    {
        // Arrange
        var controllerType = typeof(AuthController);

        // Act
        var routeAttribute = (RouteAttribute?)Attribute.GetCustomAttribute(
            controllerType, 
            typeof(RouteAttribute)
        );

        // Assert
        Assert.NotNull(routeAttribute);
        Assert.Equal("auth", routeAttribute.Template);
    }

    [Fact]
    public void Controller_HasApiControllerAttribute()
    {
        // Arrange
        var controllerType = typeof(AuthController);

        // Act
        var hasApiController = Attribute.IsDefined(
            controllerType, 
            typeof(ApiControllerAttribute)
        );

        // Assert
        Assert.True(hasApiController);
    }
}
