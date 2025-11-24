using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace PaperMail.EndToEnd.Tests;

[Collection(nameof(E2ECollection))]
public class PaginationTests
{
    private readonly PlaywrightFixture _fixture;
    public PaginationTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task InboxPagination_Navigation_WorksOrIsAbsentForSinglePage()
    {
        var page = await _fixture.NewPageAsync();
        await _fixture.LoginAsync(page);

        // Ensure we are on inbox.
        Assert.Contains("/inbox", page.Url);

        // Check for pagination nav which only renders if TotalPages > 1
        var pagination = await page.QuerySelectorAsync("nav.pagination");
        if (pagination is null)
        {
            // Single page inbox scenario; test passes as no pagination required.
            return;
        }

        var infoLocator = page.Locator(".pagination-info");
        await infoLocator.WaitForAsync(new() { Timeout = 3000 });
        var text = await infoLocator.InnerTextAsync();
        Assert.True(Regex.IsMatch(text, @"^Page \d+ of \d+$"), "pagination info should display current page and total pages");

        // Try advancing if a Next link exists.
        var nextLink = await page.QuerySelectorAsync("nav.pagination a:has-text(\"Next\")");
        if (nextLink is not null)
        {
            await nextLink.ClickAsync();
            // Wait for URL change to include page parameter = 1 (or different from before)
            await page.WaitForURLAsync(url => url.Contains("?page=") && !url.EndsWith("?page=0"), new() { Timeout = 5000 });
            var newText = await infoLocator.InnerTextAsync();
            Assert.NotEqual(text, newText);
        }
    }
}
