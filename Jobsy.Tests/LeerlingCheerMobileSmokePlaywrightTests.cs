using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile + desktop cheer layout: compact toast at 390px must not cover answers;
/// desktop 1440px keeps the sidebar bubble. Soft-skips without JOBSY_E2E_BASE_URL.
/// Named *MobileSmokePlaywrightTests so PR CI already picks it up.
/// </summary>
[Collection("PlaywrightSmoke")]
public class LeerlingCheerMobileSmokePlaywrightTests
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(1440, 900)]
    public async Task Cheer_visible_without_covering_answers(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(
            FixtureHtml(baseUrl),
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        // "0px" is truthy, so wait until scholen.css has actually applied.
        await page.WaitForFunctionAsync(
            "() => parseFloat(getComputedStyle(document.querySelector('.ll-answer')).minHeight) >= 40",
            null,
            new() { Timeout = 15_000 });

        if (width < 900)
        {
            var toast = page.Locator("[data-testid=ll-cheer-toast]");
            await Assertions.Expect(toast).ToBeVisibleAsync();
            var desktop = page.Locator("[data-testid=ll-cheer-desktop]");
            await Assertions.Expect(desktop).ToBeHiddenAsync();

            var layout = await page.EvaluateAsync<bool>("""
                () => {
                  const toast = document.querySelector('[data-testid=ll-cheer-toast]');
                  const answers = [...document.querySelectorAll('.ll-answer')];
                  if (!toast || answers.length < 5) return false;
                  const tr = toast.getBoundingClientRect();
                  if (tr.height <= 0 || tr.bottom > window.innerHeight) return false;
                  for (const a of answers) {
                    const r = a.getBoundingClientRect();
                    if (r.height < 43.5) return false;
                    if (r.bottom > window.innerHeight + 1) return false;
                    if (r.top < tr.bottom - 1) return false;
                    const x = r.left + r.width / 2;
                    const y = r.top + r.height / 2;
                    const hit = document.elementFromPoint(x, y);
                    if (hit && hit.closest('[data-testid=ll-cheer-toast]')) return false;
                  }
                  return document.documentElement.scrollWidth <= window.innerWidth + 1;
                }
                """);
            Assert.True(layout, "Mobile cheer toast must sit above the answers without covering them or overflowing.");
        }
        else
        {
            var desktop = page.Locator("[data-testid=ll-cheer-desktop] .ll-bubble");
            await Assertions.Expect(desktop).ToBeVisibleAsync();
            var toastDisplay = await page.Locator("[data-testid=ll-cheer-toast]")
                .EvaluateAsync<string>("el => getComputedStyle(el).display");
            Assert.Equal("none", toastDisplay);
        }
    }

    [Fact]
    public async Task Reduced_motion_disables_cheer_animation()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            ReducedMotion = ReducedMotion.Reduce,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(FixtureHtml(baseUrl), new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var animation = await page.Locator("[data-testid=ll-cheer-toast]")
            .EvaluateAsync<string>("el => getComputedStyle(el).animationName");
        Assert.True(
            string.IsNullOrEmpty(animation) || string.Equals(animation, "none", StringComparison.OrdinalIgnoreCase),
            $"prefers-reduced-motion must disable cheer animation, was '{animation}'.");
    }

    private static string FixtureHtml(string baseUrl) => $$"""
        <!DOCTYPE html>
        <html lang="nl">
        <head>
          <link rel="stylesheet" href="{{baseUrl}}/css/app.min.css" />
          <link rel="stylesheet" href="{{baseUrl}}/css/features/scholen.css" />
        </head>
        <body>
          <div class="ll-reis">
            <section class="ll-card ll-question">
              <p class="ll-cheer-toast" role="status" aria-live="polite" data-testid="ll-cheer-toast">Elke 10 vragen valt er een schaaltje af.</p>
              <p class="ll-question__meta">Vraag 11 van 100</p>
              <h1 class="ll-question__text">Ik werk graag samen met anderen.</h1>
              <div class="ll-answers">
                <button type="button" class="ll-answer"><span class="ll-answer__dot"></span>Nee</button>
                <button type="button" class="ll-answer"><span class="ll-answer__dot"></span>Niet echt</button>
                <button type="button" class="ll-answer"><span class="ll-answer__dot"></span>Soms</button>
                <button type="button" class="ll-answer"><span class="ll-answer__dot"></span>Best wel</button>
                <button type="button" class="ll-answer"><span class="ll-answer__dot"></span>Ja!</button>
              </div>
            </section>
            <div class="ll-lob-zone" data-testid="ll-cheer-desktop">
              <p class="ll-bubble">Elke 10 vragen valt er een schaaltje af.</p>
            </div>
          </div>
        </body>
        </html>
        """;

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
