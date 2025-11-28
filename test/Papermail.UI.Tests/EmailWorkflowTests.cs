using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Papermail.UI.Tests;

/// <summary>
/// End-to-end UI tests for PaperMail web application using Playwright.
/// These tests verify user workflows including login, inbox navigation, and email composition.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class EmailWorkflowTests : PageTest
{
    private const string BaseUrl = "https://papermail.local";
    private const string AdminEmail = "admin@papermail.local";
    private const string AdminPassword = "P@ssw0rd";

    [SetUp]
    public async Task Setup()
    {
        // Navigate to the application
        await Page.GotoAsync(BaseUrl);
    }

    [Test]
    public async Task CanLoginSuccessfully()
    {
        // Arrange - Click login link
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();

        // Act - Fill in credentials and submit
        await Page.GetByLabel("Email").FillAsync(AdminEmail);
        await Page.GetByLabel("Password").FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

        // Assert - Check we're redirected to inbox
        await Expect(Page).ToHaveURLAsync(new Regex(".*/inbox"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanViewInboxAfterLogin()
    {
        // Arrange - Login first
        await LoginAsAdmin();

        // Act - Navigate to inbox
        await Page.GetByRole(AriaRole.Link, new() { Name = "Inbox" }).ClickAsync();

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

        // Act & Assert - Navigate through different folders
        await Page.GetByRole(AriaRole.Link, new() { Name = "Drafts" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Drafts" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Sent Items" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Sent Items" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Inbox" }).ClickAsync();
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
        await Page.GotoAsync($"{BaseUrl}/auth/oidc");
        await Page.GetByLabel("Email").FillAsync(AdminEmail);
        await Page.GetByLabel("Password").FillAsync(AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
        
        // Wait for redirect to complete
        await Page.WaitForURLAsync(new Regex(".*/inbox"));
    }
}
