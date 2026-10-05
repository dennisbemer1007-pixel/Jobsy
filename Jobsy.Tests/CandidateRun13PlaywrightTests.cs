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
        var button = await page.Locator("#lobsy-coach-btn").BoundingBoxAsync();
        Assert.NotNull(tab);
        Assert.NotNull(button);
        var overlaps = tab!.X < button!.X + button.Width
                       && tab.X + tab.Width > button.X
                       && tab.Y < button.Y + button.Height
                       && tab.Y + tab.Height > button.Y;
        Assert.False(overlaps, $"Carrière tab ({tab.X},{tab.Y},{tab.Width}x{tab.Height}) overlaps the coach button.");
    }

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
               + "<div class=\"passport-tabs\" style=\"position:fixed;left:0;right:0;bottom:88px;display:flex;gap:8px\">"
               + "<button type=\"button\">Overzicht</button>"
               + "<button type=\"button\" id=\"career-tab\">Carrière</button>"
               + "</div></main>"
               + "<div class=\"lobsy-coach-dock\" data-lobsy-coach style=\"position:fixed;right:12px;bottom:88px\">"
               + "<button type=\"button\" id=\"lobsy-coach-btn\" class=\"lobsy-coach-dock__btn\" style=\"width:56px;height:56px\" aria-label=\"Coach\"></button>"
               + "</div></div></body></html>";
    }
}
