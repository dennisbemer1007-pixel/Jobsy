using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>Real Chromium check for the pupil and school privacy footers.</summary>
[Collection("PlaywrightSmoke")]
public class SchoolRun7PlaywrightTests
{
    [Fact]
    public async Task Legal_footer_links_are_visible_on_pupil_and_school_shells()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        var cssPath = Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "scholen.css");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("""
            <!DOCTYPE html>
            <html lang="nl">
            <body>
            <div class="ll-shell">
              <main class="ll-main"></main>
              <footer class="ll-legal">
                <a href="/privacy">Privacyverklaring</a>
                <a href="/toegankelijkheid">Toegankelijkheid</a>
              </footer>
            </div>
            <div class="sch-shell">
              <footer class="sch-legal">
                <a href="/privacy">Privacyverklaring</a>
                <a href="/toegankelijkheid">Toegankelijkheid</a>
              </footer>
            </div>
            </body>
            </html>
            """);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                * { box-sizing: border-box; }
                html, body { margin: 0; }
                :root {
                  --space-3: 12px;
                  --space-4: 16px;
                  --text-sm: 14px;
                  --muted: #3d4d63;
                }
                """
        });
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = cssPath });

        var links = page.Locator("footer a");
        Assert.Equal(4, await links.CountAsync());
        for (var i = 0; i < 4; i++)
        {
            var link = links.Nth(i);
            await ExpectVisible(link);
            var box = await link.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Height >= 44, $"link {i} height {box.Height}");
        }

        Assert.Equal("/privacy", await links.Nth(0).GetAttributeAsync("href"));
        Assert.Equal("/toegankelijkheid", await links.Nth(1).GetAttributeAsync("href"));
    }

    private static async Task ExpectVisible(ILocator locator)
    {
        Assert.True(await locator.IsVisibleAsync());
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
