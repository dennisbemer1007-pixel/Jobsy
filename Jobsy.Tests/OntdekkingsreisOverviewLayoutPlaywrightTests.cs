using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class OntdekkingsreisOverviewLayoutPlaywrightTests
{
    [Fact]
    public async Task Overview_grid_screenshots_desktop_and_mobile()
    {
        var root = FindRepoRoot();
        var cssPath = Path.Combine(root, "Jobsy.Web/wwwroot/css/features/ontdekkingsreis.css");
        var css = await File.ReadAllTextAsync(cssPath);
        var html = $$"""
            <!DOCTYPE html>
            <html lang="nl"><head><meta charset="utf-8"/><style>
            :root { --border:#ddd5cc; --radius-sm:10px; --text-sm:0.9rem; --text-xs:0.78rem; --muted:#5a6578; --brand:#0f2d5c; }
            body { font-family: system-ui, sans-serif; margin: 24px; background: #f4f7fb; }
            .btn { padding: 0.45rem 0.85rem; border-radius: 8px; border: 1px solid var(--border); background: #fff; cursor: pointer; }
            .status-pill { padding: 0.25rem 0.55rem; border-radius: 999px; font-size: 0.75rem; background: #e8f5ec; color: #1b5e34; }
            {{css}}
            </style></head><body>
            <div class="journey-overview">
              <h1>Verder ontdekken</h1>
              <ul class="journey-overview__list">
                <li class="journey-overview__row"><div class="journey-overview__main"><b>Over jou</b><small class="is-done">Klaar</small></div><div class="journey-overview__actions"><button class="btn">Bekijken</button></div></li>
                <li class="journey-overview__row"><div class="journey-overview__main"><b>Wanneer en hoe</b><small class="is-done">Klaar</small></div><div class="journey-overview__actions"><button class="btn">Bekijken</button></div></li>
                <li class="journey-overview__row"><div class="journey-overview__main"><b>Wat ik leuk vind</b><small class="is-done">Klaar</small></div><div class="journey-overview__actions"><button class="btn">Bekijken</button></div></li>
              </ul>
              <ul class="journey-overview__list">
                <li class="journey-overview__row journey-overview__row--test"><div class="journey-overview__main"><b>Hoe je werkt</b><small>25 van 25 vragen</small></div><div class="journey-overview__actions"><span class="status-pill">Helemaal gedaan</span><button class="btn">Uitgebreid rapport</button></div></li>
              </ul>
            </div></body></html>
            """;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var artifacts = "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifacts);
        var before = Path.Combine(root, "bug/verder-ontdekken.png");
        if (File.Exists(before))
        {
            File.Copy(before, Path.Combine(artifacts, "ontdekkingsreis-before-desktop.png"), overwrite: true);
        }

        foreach (var (width, height, name) in new[] { (1280, 800, "desktop"), (390, 844, "mobile") })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(html);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, $"ontdekkingsreis-after-{name}.png"),
                FullPage = true
            });
        }
    }

    private static string FindRepoRoot()
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
