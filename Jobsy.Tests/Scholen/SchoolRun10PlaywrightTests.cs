using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>Mobile pupil coach hint keeps a read-aloud control when the bubble is hidden.</summary>
[Collection("PlaywrightSmoke")]
public class SchoolRun10PlaywrightTests
{
    [Fact]
    public async Task Inline_coach_hint_shows_lees_voor_at_390()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            Locale = "nl-NL"
        });
        await page.SetContentAsync(Html());

        var button = page.Locator(".ll-coach-inline .read-aloud");
        await Assertions.Expect(button).ToBeVisibleAsync();
        Assert.Contains("Lees voor", await button.InnerTextAsync(), StringComparison.Ordinal);
        var box = await button.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Width >= 44 && box.Height >= 44);

        await page.SetViewportSizeAsync(1280, 800);
        await Assertions.Expect(button).ToBeHiddenAsync();
    }

    private static string Html()
    {
        var css = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/wwwroot/css/features/scholen.css"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">"
               + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
               + "<style>" + css + "</style></head><body>"
               + "<div class=\"ll-shell\"><div class=\"ll-coach-inline\" role=\"status\">"
               + "<p>Hoi! Fijn dat je er bent.</p>"
               + "<button type=\"button\" class=\"read-aloud\" aria-label=\"Lees voor\">"
               + "<span class=\"read-aloud__label\">Lees voor</span></button>"
               + "</div></div></body></html>";
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
