using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// G78 reis puzzels (S2a+S2b): browser load at 1440×900 and 390×844 is covered by
/// <see cref="PupilCircuitPlaywrightTests.Groep78_schelpenrij_puzzle_loads_under_3_seconds"/>.
/// This file keeps a fast Chromium check that themed puzzle markup fits both viewports.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class G78ReisPuzzlePlaywrightTests
{
    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Puzzle_shell_renders_without_horizontal_overflow(int width, int height)
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var cssPaths = new[]
        {
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-kies.css"),
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-vraag.css"),
            Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "leerling-werelden-puzzel-play.css"),
        };
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl"><body class="ll-g78-shell t-zee">
            <section class="band pp" data-testid="ll-zee-puzzel-play-band">
              <div class="pp-journey vr-journey"><div class="vr-jrow"></div></div>
            </section>
            <div class="wrap pp-overlap">
              <div class="pp-layout">
                <div class="card pp-card" data-testid="ll-puzzle-root">
                  <div class="pp-shell-row">
                    <div class="pp-slot"><button type="button" class="pp-shell" data-testid="ll-puzzle-shell"><img src="/x.png" alt=""></button></div>
                    <div class="pp-slot"><button type="button" class="pp-shell" data-testid="ll-puzzle-shell"><img src="/x.png" alt=""></button></div>
                    <div class="pp-slot"><button type="button" class="pp-shell" data-testid="ll-puzzle-shell"><img src="/x.png" alt=""></button></div>
                    <div class="pp-slot"><button type="button" class="pp-shell" data-testid="ll-puzzle-shell"><img src="/x.png" alt=""></button></div>
                    <div class="pp-slot"><button type="button" class="pp-shell" data-testid="ll-puzzle-shell"><img src="/x.png" alt=""></button></div>
                    <div class="pp-slot pp-slot--q"><span>?</span></div>
                  </div>
                  <div class="pp-foot"><button type="button" class="btn btn-quiet">Overslaan</button></div>
                </div>
              </div>
            </div>
            </body></html>
            """);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                * { box-sizing: border-box; }
                html, body { margin: 0; }
                """
        });
        foreach (var cssPath in cssPaths)
        {
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });
        }

        var scroll = await page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.True(scroll <= 1, $"horizontal overflow {scroll} at {width}x{height}");
        Assert.Equal(5, await page.Locator("[data-testid=ll-puzzle-shell]").CountAsync());
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
