using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Papermail.UI.Tests;

/// <summary>
/// End-to-end UI tests for PaperMail web application using Playwright.
/// These tests verify user workflows including login, inbox navigation, and email composition.
/// Set RUN_UI_TESTS=true environment variable to enable these tests.
/// Requires Docker environment to be running (docker-compose up -d).
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class EmailWorkflowTests : PageTest
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

    [SetUp]
    public async Task Setup()
    {
        if (!_runTests) return;

        // Ensure user is logged out before each test
        await EnsureLoggedOut();
        
        // Configure browser context to ignore SSL errors for self-signed certificates
        await Context.GrantPermissionsAsync(new[] { "clipboard-read", "clipboard-write" });
        
        // Navigate to the application, ignoring SSL errors
        await Page.GotoAsync(BaseUrl, new() { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000 // 30 second timeout
        });
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
        catch
        {
            // If anything fails, just clear cookies and continue
            await Context.ClearCookiesAsync();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true // Ignore self-signed certificate errors
        };
    }

    [Test]
    public async Task CanLoginSuccessfully()
    {
        // Arrange - Navigate to protected page, which will redirect to OIDC login
        await Page.GotoAsync($"{BaseUrl}/inbox", new() { 
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000 
        });
        
        // Wait for OIDC login page to load after redirect
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });

        // Act - Fill in credentials and submit
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(AdminEmail);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // Assert - Check we're redirected to inbox
        await Page.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanViewInboxAfterLogin()
    {
        // Arrange - Login first
        await LoginAsAdmin();

        // Act - Navigate to inbox using header link (more specific than sidebar)
        await Page.Locator("header").GetByRole(AriaRole.Link, new() { Name = "Inbox" }).ClickAsync();

        // Assert - Verify inbox elements are visible
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Table)).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanComposeAndSendEmail()
    {
        // Arrange - Login first
        await LoginAsAdmin();

        // Act - Navigate to compose page
        await Page.GetByRole(AriaRole.Link, new() { Name = "Compose" }).ClickAsync();

        // Fill in email form
        await Page.GetByLabel("To:").FillAsync("user@papermail.local");
        await Page.GetByLabel("Subject:").FillAsync("Test Email from Playwright");
        await Page.GetByLabel("Message:").FillAsync("This is a test email sent via Playwright UI automation.");

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Email" }).ClickAsync();

        // Assert - Check for success message
        await Expect(Page.GetByText("Email sent successfully!")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanNavigateBetweenFolders()
    {
        // Arrange - Login first
        await LoginAsAdmin();

        // Act & Assert - Navigate through different folders using header links
        await Page.Locator("header").GetByRole(AriaRole.Link, new() { Name = "Drafts" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Drafts" })).ToBeVisibleAsync();

        await Page.Locator("header").GetByRole(AriaRole.Link, new() { Name = "Sent" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Sent Items" })).ToBeVisibleAsync();

        await Page.Locator("header").GetByRole(AriaRole.Link, new() { Name = "Inbox" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ComposePageValidationWorks()
    {
        // Arrange - Login first
        await LoginAsAdmin();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Compose" }).ClickAsync();

        // Act - Try to send without filling required fields
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Email" }).ClickAsync();

        // Assert - Check HTML5 validation prevents submission
        var toInput = Page.GetByLabel("To:");
        var isInvalid = await toInput.EvaluateAsync<bool>("el => !el.validity.valid");
        Assert.That(isInvalid, Is.True, "Expected validation error on empty To field");
    }

    private async Task LoginAsAdmin()
    {
        // Navigate to protected page (inbox) which will redirect to OIDC
        await Page.GotoAsync($"{BaseUrl}/inbox", new() { 
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000 
        });
        
        // Wait for OIDC login page to load (it redirects to oidc.papermail.local)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        
        // Fill in credentials on OIDC provider login page
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync(AdminEmail);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        
        // Wait for redirect back to papermail and then to inbox
        await Page.WaitForURLAsync(new Regex(".*/inbox"), new() { Timeout = 60000 });
    }
}
