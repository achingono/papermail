using Microsoft.Playwright;
using Xunit;

namespace PaperMail.EndToEnd.Tests.Docker;

/// <summary>
/// Tests OAuth authentication flow with the docker-hosted OIDC provider.
/// Validates login, logout, and token handling.
/// </summary>
[Collection(nameof(DockerEnvironmentCollection))]
public class AuthenticationFlowTests
{
    private readonly DockerEnvironmentFixture _fixture;
    private readonly PlaywrightFixture _playwright;

    public AuthenticationFlowTests(DockerEnvironmentFixture fixture)
    {
        _fixture = fixture;
        _playwright = new PlaywrightFixture();
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();

            // Navigate to the application
            await page.GotoAsync(_fixture.BaseUrl);

            // Should redirect to OIDC login
            await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local"), 
                new() { Timeout = 10000 });

            // Fill in login form with admin credentials
            await page.FillAsync("input[name=username], input[name=email], input[type=email]", _fixture.AdminUser.Email);
            await page.FillAsync("input[name=password], input[type=password]", _fixture.AdminUser.Password);
            
            // Submit login form
            await page.ClickAsync("button[type=submit], input[type=submit]");

            // Should redirect back to application
            await page.WaitForURLAsync(url => url.Contains("papermail.local") && !url.Contains("oidc"), 
                new() { Timeout = 10000 });

            // Verify we're logged in (should see inbox or main page content)
            var content = await page.ContentAsync();
            Assert.True(content.Contains("Inbox") || content.Contains("inbox") || content.Contains("mail"),
                "After login, should see email-related content");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();

            // Navigate to the application
            await page.GotoAsync(_fixture.BaseUrl);

            // Should redirect to OIDC login
            await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local"), 
                new() { Timeout = 10000 });

            // Fill in login form with invalid credentials
            await page.FillAsync("input[name=username], input[name=email], input[type=email]", "invalid@papermail.local");
            await page.FillAsync("input[name=password], input[type=password]", "WrongPassword");
            
            // Submit login form
            await page.ClickAsync("button[type=submit], input[type=submit]");

            // Should show error or stay on login page
            await Task.Delay(2000); // Wait for error message to appear
            
            var content = await page.ContentAsync();
            Assert.True(
                content.Contains("error") || 
                content.Contains("invalid") || 
                content.Contains("incorrect") ||
                page.Url.Contains("oidc.papermail.local"),
                "Should show error message or remain on login page");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task Login_WithRegularUser_ShouldSucceed()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();

            // Navigate to the application
            await page.GotoAsync(_fixture.BaseUrl);

            // Should redirect to OIDC login
            await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local"), 
                new() { Timeout = 10000 });

            // Fill in login form with regular user credentials
            await page.FillAsync("input[name=username], input[name=email], input[type=email]", _fixture.RegularUser.Email);
            await page.FillAsync("input[name=password], input[type=password]", _fixture.RegularUser.Password);
            
            // Submit login form
            await page.ClickAsync("button[type=submit], input[type=submit]");

            // Should redirect back to application
            await page.WaitForURLAsync(url => url.Contains("papermail.local") && !url.Contains("oidc"), 
                new() { Timeout = 10000 });

            // Verify we're logged in
            var content = await page.ContentAsync();
            Assert.True(content.Contains("Inbox") || content.Contains("inbox") || content.Contains("mail"),
                "Regular user should be able to log in and access the application");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task AuthenticationFlow_ShouldIncludeOAuthCallback()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            var callbackSeen = false;

            // Listen for navigation events to track OAuth callback
            page.FrameNavigated += (_, e) =>
            {
                if (e.Url.Contains("/oauth/callback"))
                {
                    callbackSeen = true;
                }
            };

            // Navigate to the application
            await page.GotoAsync(_fixture.BaseUrl);

            // Login
            await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local"), 
                new() { Timeout = 10000 });
            await page.FillAsync("input[name=username], input[name=email], input[type=email]", _fixture.AdminUser.Email);
            await page.FillAsync("input[name=password], input[type=password]", _fixture.AdminUser.Password);
            await page.ClickAsync("button[type=submit], input[type=submit]");

            // Wait for redirect back to app
            await page.WaitForURLAsync(url => url.Contains("papermail.local") && !url.Contains("oidc"), 
                new() { Timeout = 10000 });

            Assert.True(callbackSeen, "OAuth callback URL should be visited during authentication flow");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task Logout_ShouldClearSessionAndRedirectToLogin()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();

            // First, login
            await LoginAsync(page);

            // Find and click logout button/link
            var logoutSelector = "a[href*='logout'], button:has-text('Logout'), a:has-text('Logout'), a:has-text('Sign out')";
            var logoutElement = await page.QuerySelectorAsync(logoutSelector);
            
            if (logoutElement != null)
            {
                await logoutElement.ClickAsync();

                // Should redirect to OIDC or back to login page
                await Task.Delay(2000);
                
                var currentUrl = page.Url;
                Assert.True(
                    currentUrl.Contains("oidc") || 
                    currentUrl.Contains("login") || 
                    currentUrl.Contains("logout"),
                    "After logout, should redirect to login or OIDC logout page");
            }
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact(Skip = "Requires docker-compose environment")]
    public async Task UnauthenticatedAccess_ShouldRedirectToLogin()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();

            // Try to access protected route directly
            await page.GotoAsync($"{_fixture.BaseUrl}/inbox");

            // Should redirect to OIDC login
            await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local") || url.Contains("login"), 
                new() { Timeout = 10000 });

            var content = await page.ContentAsync();
            Assert.True(
                content.Contains("username") || 
                content.Contains("email") || 
                content.Contains("login"),
                "Should show login form when accessing protected route without authentication");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    private async Task<IPage> CreatePageAsync()
    {
        var context = await _playwright.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        });
        return await context.NewPageAsync();
    }

    private async Task LoginAsync(IPage page)
    {
        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForURLAsync(url => url.Contains("oidc.papermail.local"), 
            new() { Timeout = 10000 });
        await page.FillAsync("input[name=username], input[name=email], input[type=email]", _fixture.AdminUser.Email);
        await page.FillAsync("input[name=password], input[type=password]", _fixture.AdminUser.Password);
        await page.ClickAsync("button[type=submit], input[type=submit]");
        await page.WaitForURLAsync(url => url.Contains("papermail.local") && !url.Contains("oidc"), 
            new() { Timeout = 10000 });
    }
}
