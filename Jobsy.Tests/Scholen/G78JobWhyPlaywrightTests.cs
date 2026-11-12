using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

[Collection("PlaywrightSmoke")]
public sealed class G78JobWhyPlaywrightTests
{
    [Theory]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task Job_why_panel_opens_on_tap(int viewportWidth)
    {
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        var root = RepoRoot();
        var jsPath = Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = viewportWidth, Height = 900 } });
        await page.SetContentAsync("""
            <!DOCTYPE html><html lang="nl"><body class="ll-g78-shell">
            <article class="lp-g78__card lp-g78__job">
            <div class="g78-job-why-wrap">
            <button type="button" class="ll-info-btn g78-job-why-btn" aria-label="Waarom past dit bij jou?" aria-expanded="false" aria-controls="g78-job-why-fotograaf">i</button>
            <div id="g78-job-why-fotograaf" class="ll-info g78-job-why-panel" hidden>
            <button type="button" class="ll-info__close" aria-label="Sluiten">×</button>
            <p class="g78-job-why-panel__body">Je gaf aan dat je tekenen leuk vindt.</p>
            </div></div>
            <h4>Fotograaf</h4>
            </article>
            </body></html>
            """);
        await page.AddScriptTagAsync(new() { Path = Path.Combine(root, "Jobsy.Web/wwwroot/js/jobsy-dom-safe.js") });
        await page.AddScriptTagAsync(new() { Path = jsPath });
        await page.Locator(".g78-job-why-btn").ClickAsync();
        Assert.Equal("true", await page.Locator(".g78-job-why-btn").GetAttributeAsync("aria-expanded"));
        Assert.True(await page.Locator(".g78-job-why-panel.is-open").IsVisibleAsync());

        if (string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory("/opt/cursor/artifacts");
            var suffix = viewportWidth <= 400 ? "390" : "1440";
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/g78-job-why-panel-{suffix}.png",
                FullPage = true
            });
        }
    }

    private static string RepoRoot()
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
