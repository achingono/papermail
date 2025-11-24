using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Authentication;
using PaperMail.Infrastructure.Configuration;
using System.Net;
using System.Text.Json;

namespace PaperMail.Infrastructure.Tests;

public class OAuthServiceTests
{
    private readonly Mock<ITokenStorage> _tokenStorageMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly OAuthSettings _settings;
    private readonly OAuthService _service;

    public OAuthServiceTests()
    {
        _tokenStorageMock = new Mock<ITokenStorage>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        
        _settings = new OAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Scopes = new[] { "https://mail.google.com/" },
            RedirectUri = "https://localhost:5001/oauth/callback"
        };

        _service = new OAuthService(
            Options.Create(_settings),
            _tokenStorageMock.Object,
            _httpClientFactoryMock.Object);
    }

    [Fact]
    public void GetAuthorizationUrl_ReturnsValidUrl()
    {
        // Act
        var (authUrl, codeVerifier, state) = _service.GetAuthorizationUrl();

        // Assert
        authUrl.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        authUrl.Should().Contain("client_id=test-client-id");
        authUrl.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%3A5001%2Foauth%2Fcallback");
        authUrl.Should().Contain("response_type=code");
        authUrl.Should().Contain("scope=https%3A%2F%2Fmail.google.com%2F");
        authUrl.Should().Contain("code_challenge_method=S256");
        authUrl.Should().Contain("access_type=offline");
        authUrl.Should().Contain("prompt=consent");
        authUrl.Should().Contain($"state={state}");
        
        codeVerifier.Should().NotBeNullOrEmpty();
        state.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAuthorizationUrl_GeneratesUniqueStateEachTime()
    {
        // Act
        var (_, _, state1) = _service.GetAuthorizationUrl();
        var (_, _, state2) = _service.GetAuthorizationUrl();

        // Assert
        state1.Should().NotBe(state2);
    }

    [Fact]
    public void GetAuthorizationUrl_IncludesCodeChallengeInUrl()
    {
        // Act
        var (authUrl, _, _) = _service.GetAuthorizationUrl();

        // Assert
        authUrl.Should().Contain("code_challenge=");
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_ValidResponse_ReturnsTokens()
    {
        // Arrange
        var tokenResponse = new OAuthTokenResponse
        {
            AccessToken = "access-token-123",
            RefreshToken = "refresh-token-456",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    access_token = "access-token-123",
                    refresh_token = "refresh-token-456",
                    expires_in = 3600,
                    token_type = "Bearer"
                }))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _service.ExchangeCodeForTokensAsync("auth-code", "code-verifier");

        // Assert
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-456");
        result.ExpiresIn.Should().Be(3600);
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ValidResponse_ReturnsNewAccessToken()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    access_token = "new-access-token",
                    expires_in = 3600,
                    token_type = "Bearer"
                }))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _service.RefreshAccessTokenAsync("refresh-token");

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("refresh-token"); // Preserved from input
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task StoreUserTokensAsync_WithRefreshToken_StoresInTokenStorage()
    {
        // Arrange
        var tokens = new OAuthTokenResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        // Act
        await _service.StoreUserTokensAsync("user123", tokens);

        // Assert
        _tokenStorageMock.Verify(
            t => t.StoreTokenAsync("user123", "refresh", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreUserTokensAsync_WithoutRefreshToken_DoesNotStore()
    {
        // Arrange
        var tokens = new OAuthTokenResponse
        {
            AccessToken = "access",
            RefreshToken = null
        };

        // Act
        await _service.StoreUserTokensAsync("user123", tokens);

        // Assert
        _tokenStorageMock.Verify(
            t => t.StoreTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserRefreshTokenAsync_ReturnsTokenFromStorage()
    {
        // Arrange
        _tokenStorageMock
            .Setup(t => t.GetTokenAsync("user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-refresh-token");

        // Act
        var result = await _service.GetUserRefreshTokenAsync("user123");

        // Assert
        result.Should().Be("stored-refresh-token");
    }

    [Fact]
    public async Task GetUserRefreshTokenAsync_NoToken_ReturnsNull()
    {
        // Arrange
        _tokenStorageMock
            .Setup(t => t.GetTokenAsync("user123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetUserRefreshTokenAsync("user123");

        // Assert
        result.Should().BeNull();
    }
}
