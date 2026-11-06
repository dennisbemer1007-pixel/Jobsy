using Jobsy.Tests.Uat;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Candidate run 12: at 390px the coach tip hides when it covers a button, and the coach button stays.
/// Static page, no live site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateRun12PlaywrightTests
{
    [Fact]
    public async Task Mobile_tip_hides_when_it_covers_a_button()
    {
        await using var page = await OpenAsync(390, 844, PageHtml());
        var padding = await page.EvalOnSelectorAsync<double>(
            ".app-main",
            "el => parseFloat(getComputedStyle(el).paddingBottom)");
        Assert.True(padding >= 56 + 88 + 16 + 96, $"content padding-bottom was {padding}px");

        await page.EvaluateAsync("() => window.jobsyCoachDock.placeTip(document.querySelector('[data-lobsy-coach].lobsy-coach-dock'))");
        Assert.False(await page.Locator(".lobsy-coach-dock__tip").IsVisibleAsync());
        Assert.True(await page.Locator("#lobsy-coach-btn").IsVisibleAsync());
        Assert.True(await page.Locator("#go-on").IsVisibleAsync());
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
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + css + "</style></head><body>"
               + "<div class=\"app-shell\" data-lobsy-coach>"
               + "<main class=\"app-main\">"
               + "<button type=\"button\" id=\"go-on\" style=\"position:fixed;left:8px;right:8px;bottom:70px;height:48px;z-index:5\">Ga verder</button>"
               + "</main>"
               + "<div class=\"lobsy-coach-dock\" data-lobsy-coach>"
               + "<div class=\"lobsy-coach-dock__tip\"><p>Tip voor je volgende stap.</p></div>"
               + "<button type=\"button\" id=\"lobsy-coach-btn\" class=\"lobsy-coach-dock__btn\" aria-label=\"Coach\"></button>"
               + "</div></div>"
               + "<script>" + js + "</script></body></html>";
    }
}
