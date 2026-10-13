using Jobsy.Tests.Uat;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Candidate run 13: at 390px the last passport tab stays out of the coach button.
/// Static page, no live site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateRun13PlaywrightTests
{
    [Fact]
    public async Task Mobile_career_tab_does_not_overlap_the_coach_button()
    {
        await using var page = await OpenAsync(390, 844, PageHtml());
        var tab = await page.Locator("#career-tab").BoundingBoxAsync();
        var link = await page.Locator("#empty-link").BoundingBoxAsync();
        var button = await page.Locator("#lobsy-coach-btn").BoundingBoxAsync();
        Assert.NotNull(tab);
        Assert.NotNull(button);
        Assert.False(Overlaps(tab, button), $"Carrière tab ({tab!.X},{tab.Y},{tab.Width}x{tab.Height}) overlaps the coach button.");
        if (link is not null && link.Width > 0 && link.Height > 0)
        {
            Assert.False(Overlaps(link, button), "The values link overlaps the coach button.");
        }
    }

    private static bool Overlaps(Microsoft.Playwright.LocatorBoundingBoxResult? left, Microsoft.Playwright.LocatorBoundingBoxResult? right)
        => left is not null
           && right is not null
           && left.X < right.X + right.Width
           && left.X + left.Width > right.X
           && left.Y < right.Y + right.Height
           && left.Y + left.Height > right.Y;

    private static async Task<IPage> OpenAsync(int width, int height, string html)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            HasTouch = true,
            IsMobile = true
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(html);
        return page;
    }

    private static string PageHtml()
    {
        var root = RepoRoot.Find();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + css + "</style></head><body>"
               + "<div class=\"app-shell\" data-lobsy-coach>"
               + "<main class=\"app-main\">"
               + "<div class=\"passport-tabs\" style=\"position:fixed;left:0;right:0;bottom:12px;display:flex;justify-content:flex-end;align-items:center;height:56px\">"
               + "<button type=\"button\" class=\"passport-tab\" id=\"career-tab\">Carrière</button>"
               + "</div>"
               + "<div class=\"test-page\" style=\"position:fixed;left:0;right:0;bottom:12px;height:56px;display:flex;align-items:center;justify-content:flex-end\">"
               + "<a id=\"empty-link\" href=\"/profiel/tests/values\" style=\"display:inline-block;width:40px;height:40px\"></a>"
               + "</div></main>"
               + "<div class=\"lobsy-coach-dock\" data-lobsy-coach>"
               + "<button type=\"button\" id=\"lobsy-coach-btn\" class=\"lobsy-coach-dock__btn\" aria-label=\"Coach\"></button>"
               + "</div></div></body></html>";
    }
}
