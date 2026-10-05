using Jobsy.Tests.Uat;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Candidate run 11: at 390px the coach reserves space, and a focused field hides only the tip.
/// Static page, no live site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateRun11PlaywrightTests
{
    [Fact]
    public async Task Mobile_content_clears_the_coach_and_a_focused_field_hides_only_the_tip()
    {
        await using var page = await OpenAsync(390, 844, PageHtml());
        var padding = await page.EvalOnSelectorAsync<double>(
            ".app-main",
            "el => parseFloat(getComputedStyle(el).paddingBottom)");
        Assert.True(padding >= 56 + 88 + 16, $"content padding-bottom was {padding}px");

        var tipVisible = await page.Locator(".lobsy-coach-dock__tip").IsVisibleAsync();
        Assert.True(tipVisible);
        var coachVisible = await page.Locator("#lobsy-coach-btn").IsVisibleAsync();
        Assert.True(coachVisible);

        await page.Locator("#search").FocusAsync();
        Assert.False(await page.Locator(".lobsy-coach-dock__tip").IsVisibleAsync());
        Assert.True(await page.Locator("#lobsy-coach-btn").IsVisibleAsync());

        await page.Locator("#lobsy-assistant-input").FocusAsync();
        Assert.True(await page.Locator(".lobsy-coach-dock__tip").IsVisibleAsync());

        var chat = await page.Locator("[data-testid=new-chat]").BoundingBoxAsync();
        Assert.NotNull(chat);
        Assert.True(chat!.Width >= 24 && chat.Height >= 24);
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
        var css = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/wwwroot/css/app.css"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + css + "</style></head><body>"
               + "<div class=\"app-shell\" data-lobsy-coach>"
               + "<main class=\"app-main\"><input id=\"search\" type=\"search\" aria-label=\"Zoeken\"></main>"
               + "<div class=\"lobsy-coach-dock\" data-lobsy-coach>"
               + "<div class=\"lobsy-coach-dock__tip\"><p>Ik help je zoeken.</p></div>"
               + "<button type=\"button\" id=\"lobsy-coach-btn\" class=\"lobsy-coach-dock__btn\" aria-label=\"Coach\"></button>"
               + "</div>"
               + "<section class=\"lobsy-assistant\">"
               + "<button type=\"button\" class=\"btn btn-ghost\" data-testid=\"new-chat\">Nieuw gesprek</button>"
               + "<textarea id=\"lobsy-assistant-input\" aria-label=\"Bericht\"></textarea>"
               + "</section></div></body></html>";
    }
}
