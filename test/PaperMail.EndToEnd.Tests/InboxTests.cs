using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace PaperMail.EndToEnd.Tests;

[Collection(nameof(E2ECollection))]
public class InboxTests
{
    private readonly PlaywrightFixture _fx;
    public InboxTests(PlaywrightFixture fx) => _fx = fx;

    [Fact]
    public async Task LoginAndLoadInbox_ShouldShowInboxTitle()
    {
        var page = await _fx.NewPageAsync();
        await _fx.LoginAsync(page);
        await page.WaitForSelectorAsync("h1,header,body");
        var content = await page.ContentAsync();
        Assert.Contains("Inbox", content);
    }

    [Fact]
    public async Task KeyboardShortcut_C_OpensCompose()
    {
        var page = await _fx.NewPageAsync();
        await _fx.LoginAsync(page);
        await page.Keyboard.PressAsync("c");
        await page.WaitForURLAsync(u => u.Contains("/compose"));
        Assert.Contains("/compose", page.Url);
    }
}
