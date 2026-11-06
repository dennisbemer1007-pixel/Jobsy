using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// G78 reis puzzels (S2a+S2b): browser load at 1440×900 and 390×844 is covered by
/// <see cref="PupilCircuitPlaywrightTests.Groep78_schelpenrij_puzzle_loads_under_3_seconds"/>.
/// This file keeps a fast Chromium check that puzzle markup and scholen.css fit both viewports.
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

        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl"><body>
            <section class="ll-card ll-puzzle" data-testid="ll-puzzle-root">
              <h1 class="ll-question__text">Schelpen op een rij</h1>
              <div class="ll-puzzle__shells">
                <button type="button" class="ll-puzzle__shell" data-testid="ll-puzzle-shell"><span class="ll-puzzle__shell-icon" data-size="1"></span></button>
                <button type="button" class="ll-puzzle__shell" data-testid="ll-puzzle-shell"><span class="ll-puzzle__shell-icon" data-size="3"></span></button>
              </div>
              <div class="ll-puzzle__actions">
                <button type="button" class="btn-secondary">Overslaan</button>
              </div>
            </section>
            </body></html>
            """);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                * { box-sizing: border-box; }
                html, body { margin: 0; }
                :root {
                  --space-2: 8px; --space-3: 12px; --space-4: 16px; --space-5: 20px;
                  --text-sm: 14px; --text-lg: 18px; --text-xl: 22px;
                  --muted: #5c5348; --text: #3d342b; --surface: #fffaf3; --border: #e6d7c3;
                  --brand: #c4552a; --radius-md: 8px;
                }
                """
        });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

        var scroll = await page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.True(scroll <= 1, $"horizontal overflow {scroll} at {width}x{height}");
        Assert.Equal(2, await page.Locator("[data-testid=ll-puzzle-shell]").CountAsync());
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
