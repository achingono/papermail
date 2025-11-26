using Microsoft.Playwright;
using Xunit;

namespace PaperMail.EndToEnd.Tests.Docker;

/// <summary>
/// End-to-end UI tests for email workflows in the docker-hosted environment.
/// Tests compose, send, receive, and read operations through the web interface.
/// </summary>
[Collection(nameof(DockerEnvironmentCollection))]
public class EmailWorkflowUITests
{
    private readonly DockerEnvironmentFixture _fixture;
    private readonly PlaywrightFixture _playwright;

    public EmailWorkflowUITests(DockerEnvironmentFixture fixture)
    {
        _fixture = fixture;
        _playwright = new PlaywrightFixture();
    }

    [Fact]
    public async Task ComposeAndSendEmail_ShouldSucceed()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to compose page
            await page.GotoAsync($"{_fixture.BaseUrl}/compose");
            await page.WaitForSelectorAsync("form", new() { Timeout = 10000 });

            // Fill in email form
            await page.FillAsync("input[name=To], input[name=to], input[id*='to' i]", _fixture.RegularUser.Email);
            await page.FillAsync("input[name=Subject], input[name=subject], input[id*='subject' i]", "Test Email from UI Test");
            await page.FillAsync("textarea[name=BodyPlain], textarea[name=body], textarea[id*='body' i]", "This is a test email sent from an automated UI test.");

            // Submit the form
            var submitButton = await page.QuerySelectorAsync("button[type=submit], input[type=submit], button:has-text('Send')");
            Assert.NotNull(submitButton);
            await submitButton.ClickAsync();

            // Wait for success message or redirect
            await Task.Delay(3000);

            // Verify we're redirected or see success message
            var content = await page.ContentAsync();
            Assert.True(
                content.Contains("sent") || 
                content.Contains("success") || 
                page.Url.Contains("/inbox") ||
                !page.Url.Contains("/compose"),
                "Should show success or redirect after sending email");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task Inbox_ShouldLoadAndDisplayEmails()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to inbox
            await page.GotoAsync($"{_fixture.BaseUrl}/inbox");
            
            // Wait for content to load
            await page.WaitForSelectorAsync("body", new() { Timeout = 10000 });
            await Task.Delay(2000); // Allow time for email list to populate

            var content = await page.ContentAsync();
            
            // Verify inbox page is loaded
            Assert.True(
                content.Contains("Inbox") || 
                content.Contains("inbox") ||
                content.Contains("email") ||
                content.Contains("message"),
                "Inbox page should be displayed");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task ComposeEmail_RequiredFieldValidation_ShouldWork()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to compose page
            await page.GotoAsync($"{_fixture.BaseUrl}/compose");
            await page.WaitForSelectorAsync("form", new() { Timeout = 10000 });

            // Try to submit without filling required fields
            var submitButton = await page.QuerySelectorAsync("button[type=submit], input[type=submit], button:has-text('Send')");
            Assert.NotNull(submitButton);
            await submitButton.ClickAsync();

            // Should show validation errors or remain on compose page
            await Task.Delay(1000);
            
            Assert.True(
                page.Url.Contains("/compose"),
                "Should remain on compose page when validation fails");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task ComposeEmail_WithSubject_ShouldPreserveInput()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to compose page
            await page.GotoAsync($"{_fixture.BaseUrl}/compose");
            await page.WaitForSelectorAsync("form", new() { Timeout = 10000 });

            // Fill in subject
            var testSubject = "Test Subject for Validation";
            await page.FillAsync("input[name=Subject], input[name=subject], input[id*='subject' i]", testSubject);

            // Verify the value is preserved
            var subjectValue = await page.InputValueAsync("input[name=Subject], input[name=subject], input[id*='subject' i]");
            
            Assert.Equal(testSubject, subjectValue);
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task Navigation_BetweenInboxAndCompose_ShouldWork()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Start at inbox
            await page.GotoAsync($"{_fixture.BaseUrl}/inbox");
            await Task.Delay(1000);
            Assert.Contains("inbox", page.Url.ToLower());

            // Navigate to compose
            await page.GotoAsync($"{_fixture.BaseUrl}/compose");
            await Task.Delay(1000);
            Assert.Contains("compose", page.Url.ToLower());

            // Navigate back to inbox
            await page.GotoAsync($"{_fixture.BaseUrl}/inbox");
            await Task.Delay(1000);
            Assert.Contains("inbox", page.Url.ToLower());
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task ComposeEmail_KeyboardShortcut_C_ShouldOpenCompose()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to inbox
            await page.GotoAsync($"{_fixture.BaseUrl}/inbox");
            await Task.Delay(1000);

            // Press 'c' to open compose
            await page.Keyboard.PressAsync("c");
            await Task.Delay(1000);

            // Should navigate to compose page
            Assert.True(
                page.Url.Contains("/compose"),
                "Pressing 'c' should navigate to compose page");
        }
        finally
        {
            await _playwright.DisposeAsync();
        }
    }

    [Fact]
    public async Task SendEmail_ToMultipleRecipients_ShouldSucceed()
    {
        await _playwright.InitializeAsync();
        try
        {
            var page = await CreatePageAsync();
            await LoginAsync(page);

            // Navigate to compose page
            await page.GotoAsync($"{_fixture.BaseUrl}/compose");
            await page.WaitForSelectorAsync("form", new() { Timeout = 10000 });

            // Fill in email form with multiple recipients
            var recipients = $"{_fixture.RegularUser.Email},{_fixture.AdminUser.Email}";
            await page.FillAsync("input[name=To], input[name=to], input[id*='to' i]", recipients);
            await page.FillAsync("input[name=Subject], input[name=subject], input[id*='subject' i]", "Test to Multiple Recipients");
            await page.FillAsync("textarea[name=BodyPlain], textarea[name=body], textarea[id*='body' i]", "Testing multiple recipients.");

            // Submit the form
            var submitButton = await page.QuerySelectorAsync("button[type=submit], input[type=submit], button:has-text('Send')");
            Assert.NotNull(submitButton);
            await submitButton.ClickAsync();

            // Wait for success
            await Task.Delay(3000);

            var content = await page.ContentAsync();
            Assert.True(
                content.Contains("sent") || 
                content.Contains("success") || 
                !page.Url.Contains("/compose"),
                "Should successfully send email to multiple recipients");
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
