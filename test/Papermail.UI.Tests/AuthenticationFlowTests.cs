using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Papermail.UI.Tests;

/// <summary>
/// UI tests focused on authentication flows and security.
/// Set RUN_UI_TESTS=true to enable.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class AuthenticationFlowTests : PageTest
{
    private const string BaseUrl = "https://papermail.local";
    private const string AdminEmail = "admin@papermail.local";
    private const string AdminPassword = "P@ssw0rd";
    private bool _runTests;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _runTests = Environment.GetEnvironmentVariable("RUN_UI_TESTS") == "true";
        if (!_runTests)
        {
            Assert.Ignore("UI tests are disabled. Set RUN_UI_TESTS=true to enable.");
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true // Ignore self-signed certificate errors
        };
    }

    [SetUp]
    public async Task Setup()
    {
        if (!_runTests) return;
        
        // Ensure user is logged out before each test
        await EnsureLoggedOut();
    }

    /// <summary>
    /// Ensures the user is logged out by checking for authenticated state and logging out if needed.
    /// </summary>
    private async Task EnsureLoggedOut()
    {
        try
        {
            // Navigate to home page to check authentication state
            await Page.GotoAsync(BaseUrl, new() { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 10000
            });
            
            // Check if Logout link exists (indicates user is authenticated)
            var logoutLink = Page.Locator("a[href='/logout'], a[href='/auth/logout']");
            var isAuthenticated = await logoutLink.CountAsync() > 0;
            
            if (isAuthenticated)
            {
                // Click logout
                await logoutLink.First.ClickAsync();
                
                // Wait for OIDC logout confirmation page
                await Page.WaitForURLAsync(new Regex("oidc\\.papermail\\.local/session/end"), new() { Timeout = 10000 });
                
                // Click "Yes, sign me out" button if present
                var signOutButton = Page.Locator("button").Filter(new() { HasTextRegex = new Regex("yes.*sign.*out", RegexOptions.IgnoreCase) });
                var buttonCount = await signOutButton.CountAsync();
                
                if (buttonCount > 0)
                {
                    await signOutButton.First.ClickAsync();
                }
                
                // Wait for redirect back to home page
                await Page.WaitForURLAsync(new Regex("^https://papermail\\.local/?$"), new() { Timeout = 10000 });
                
                // Clear all cookies to ensure clean state
                await Context.ClearCookiesAsync();
            }
        }
        catch (Exception)
        {
            // If anything fails, just clear cookies and continue
            await Context.ClearCookiesAsync();
        }
    }

    [Test]
    public async Task UnauthenticatedUser_RedirectsToLogin()
    {
        if (!_runTests) return;

        // Attempt to access protected page
        await Page.GotoAsync($"{BaseUrl}/inbox");

        // Should redirect to OIDC provider or show login prompt
        await Page.WaitForURLAsync(new Regex("(oidc\\.papermail\\.local|/auth/)"));
        
        var url = Page.Url;
        Assert.That(url, Does.Contain("oidc").Or.Contains("auth"));
    }

    [Test]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        if (!_runTests) return;

        // Navigate to protected page, which will redirect to OIDC login
        await Page.GotoAsync($"{BaseUrl}/inbox", new() { 
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000 
        });
        
        // Wait for OIDC login page to load after redirect
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(AdminEmail);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // Wait for redirect and check we're logged in
        await Page.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });
        
        var url = Page.Url;
        Assert.That(url, Does.Contain("inbox"));
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        if (!_runTests) return;

        // Navigate to protected page, which will redirect to OIDC login
        await Page.GotoAsync($"{BaseUrl}/inbox", new() { 
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000 
        });
        
        // Wait for OIDC login page to load after redirect
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(AdminEmail);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("WrongPassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // Should show error message (wait a bit for error to appear)
        await Page.WaitForTimeoutAsync(2000);
        
        var errorVisible = await Page.GetByText(new Regex("invalid|error|failed|incorrect", RegexOptions.IgnoreCase))
            .IsVisibleAsync()
            .ContinueWith(t => t.Result, TaskScheduler.Default);

        Assert.That(errorVisible, Is.True, "Expected error message for invalid credentials");
    }

    [Test]
    public async Task Logout_ClearsSessionAndRedirects()
    {
        if (!_runTests) return;

        // Login first - navigate to protected page which will redirect to OIDC
        await Page.GotoAsync($"{BaseUrl}/inbox", new() { 
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000 
        });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(AdminEmail);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await Page.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });

        // Logout
        var logoutButton = Page.Locator("button, a").Filter(new() { 
            HasTextRegex = new Regex("logout|sign out", RegexOptions.IgnoreCase) 
        }).First;
        await logoutButton.ClickAsync();

        // Wait for OIDC confirmation page if it appears
        try 
        {
            await Page.WaitForURLAsync(new Regex("/session/end"), new() { Timeout = 5000 });
            // Click the confirmation button
            await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("yes|confirm|sign out", RegexOptions.IgnoreCase) }).ClickAsync();
        }
        catch (TimeoutException)
        {
            // If no OIDC confirmation, that's fine - we might be already redirected
        }

        // Should redirect to home or login page
        await Page.WaitForURLAsync(new Regex("papermail\\.local/?$"), new() { Timeout = 10000 });
        
        // Try to access protected page - should redirect to login
        await Page.GotoAsync($"{BaseUrl}/inbox");
        await Page.WaitForURLAsync(new Regex("(oidc\\.papermail\\.local|/auth/)"));
        
        Assert.That(Page.Url, Does.Contain("oidc").Or.Contains("auth"));
    }

    [Test]
    public async Task MultipleUsers_CanLoginSimultaneously()
    {
        if (!_runTests) return;

        // Create two browser contexts for two different users
        var context1 = await Browser!.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true
        });
        var page1 = await context1.NewPageAsync();

        var context2 = await Browser!.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true
        });
        var page2 = await context2.NewPageAsync();

        try
        {
            // Login as admin in first context - navigate to /inbox which triggers OIDC redirect
            await page1.GotoAsync($"{BaseUrl}/inbox", new() { 
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000 
            });
            await page1.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
            await page1.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("admin@papermail.local");
            await page1.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("P@ssw0rd");
            await page1.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
            await page1.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });

            // Login as user in second context - navigate to /inbox which triggers OIDC redirect
            await page2.GotoAsync($"{BaseUrl}/inbox", new() { 
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000 
            });
            await page2.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
            await page2.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("user@papermail.local");
            await page2.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("P@ssw0rd");
            await page2.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
            await page2.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });

            // Both should be logged in to their respective sessions
            Assert.That(page1.Url, Does.Contain("inbox"));
            Assert.That(page2.Url, Does.Contain("inbox"));
        }
        finally
        {
            await context1.CloseAsync();
            await context2.CloseAsync();
        }
    }
}
