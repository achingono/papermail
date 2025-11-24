using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace PaperMail.EndToEnd.Tests;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("PAPERMAIL_BASE_URL") ?? "http://localhost:8080";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null) await Browser.CloseAsync();
        Playwright?.Dispose();
    }

    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        });
        return await context.NewPageAsync();
    }

    public async Task LoginAsync(IPage page)
    {
        // Navigate to login page in app
        await page.GotoAsync(BaseUrl + "/oauth/login");
        // The app should redirect to OIDC authorize endpoint served by simple-oidc-provider.
        // simple-oidc-provider login form fields typically: name="username" and name="password" then a submit button.
        // Attempt generic selectors; if provider differs, adjust accordingly in later refinement.
        if (await page.QuerySelectorAsync("input[name=username]") is not null)
        {
            await page.FillAsync("input[name=username]", "user1");
            await page.FillAsync("input[name=password]", "Password123!");
            await page.ClickAsync("button, input[type=submit]");
        }
        // Wait until redirected back to app inbox or callback completes.
        await page.WaitForURLAsync(url => url.Contains("/inbox") || url.Contains("/oauth/callback"), new() { Timeout = 10000 });
        if (!page.Url.Contains("/inbox"))
        {
            // If on callback, follow potential redirect.
            await page.WaitForURLAsync(u => u.Contains("/inbox"), new() { Timeout = 10000 });
        }
    }
}

[CollectionDefinition(nameof(E2ECollection))]
public sealed class E2ECollection : ICollectionFixture<PlaywrightFixture> { }
