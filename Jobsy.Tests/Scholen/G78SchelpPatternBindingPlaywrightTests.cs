using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>Regression: schelpen pattern puzzle choice buttons must select and enable continue.</summary>
[Collection("PlaywrightSmoke")]
public sealed class G78SchelpPatternBindingPlaywrightTests
{
    [Fact]
    public async Task Schelp_pattern_choice_binds_directly_on_play_section()
    {
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));

        var root = RepoRoot();
        var html = """
            <!DOCTYPE html>
            <html lang="nl"><body class="ll-g78-shell">
            <form data-ll-puzzle-form="1"><input type="hidden" name="Outcome" value="" />
            <div class="card ll-puzzle ll-puzzle-g78" data-ll-puzzle-kind="schelpenrij" data-ll-puzzle-mode="pattern" data-ll-puzzle-answer="2">
              <div class="ll-puzzle-play" data-ll-puzzle-phase="play">
                <section data-ll-puzzle-play-section="1">
                  <div class="answers ll-puzzle-schelp-choices">
                    <button type="button" class="ans" data-ll-schelp-choice="1">1</button>
                    <button type="button" class="ans" data-ll-schelp-choice="2">2</button>
                  </div>
                  <p data-ll-puzzle-success hidden>Goed!</p>
                  <button type="button" data-ll-puzzle-continue disabled>Verder</button>
                </section>
              </div>
            </div></form></body></html>
            """;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.AddScriptTagAsync(new() { Path = Path.Combine(root, "Jobsy.Web/wwwroot/js/jobsy-dom-safe.js") });
        await page.AddScriptTagAsync(new() { Path = Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js") });

        await page.Locator("[data-ll-schelp-choice=\"1\"]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-schelp-choice=\"1\"]")).ToHaveClassAsync(new Regex(".*\\bsel\\b.*"));
        await Assertions.Expect(page.Locator("[data-ll-puzzle-continue]")).ToBeDisabledAsync();

        await page.Locator("[data-ll-schelp-choice=\"2\"]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-ll-schelp-choice=\"2\"]")).ToHaveClassAsync(new Regex(".*\\bsel\\b.*"));
        await Assertions.Expect(page.Locator("[data-ll-puzzle-continue]")).ToBeEnabledAsync();
        await Assertions.Expect(page.Locator("[data-ll-puzzle-success]")).ToBeVisibleAsync();
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
