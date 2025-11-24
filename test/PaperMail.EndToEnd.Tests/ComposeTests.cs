using System.Threading.Tasks;
using Xunit;

namespace PaperMail.EndToEnd.Tests;

[Collection(nameof(E2ECollection))]
public class ComposeTests
{
    private readonly PlaywrightFixture _fx;
    public ComposeTests(PlaywrightFixture fx) => _fx = fx;

    [Fact]
    public async Task ComposePage_ShouldContainFormFields()
    {
        var page = await _fx.NewPageAsync();
        await _fx.LoginAsync(page);
        await page.GotoAsync(_fx.BaseUrl + "/compose");
        await page.WaitForSelectorAsync("form");
        Assert.NotNull(await page.QuerySelectorAsync("input[name=To]"));
        Assert.NotNull(await page.QuerySelectorAsync("input[name=Subject]"));
        Assert.NotNull(await page.QuerySelectorAsync("textarea[name=BodyPlain]"));
    }
}
