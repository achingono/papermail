using System.Threading.Tasks;
using Xunit;

namespace PaperMail.EndToEnd.Tests;

[Collection(nameof(E2ECollection))]
public class ShortcutsTests
{
    private readonly PlaywrightFixture _fx;
    public ShortcutsTests(PlaywrightFixture fx) => _fx = fx;

    [Fact]
    public async Task KeyboardShortcut_I_NavigatesInbox()
    {
        var page = await _fx.NewPageAsync();
        await _fx.LoginAsync(page);
        await page.GotoAsync(_fx.BaseUrl + "/compose");
        await page.Keyboard.PressAsync("i");
        await page.WaitForURLAsync(u => u.Contains("/inbox"));
        Assert.Contains("/inbox", page.Url);
    }
}
