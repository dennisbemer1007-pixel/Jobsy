using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Landing "/" smoke: no map/Blazor assets, hero CTA, keyboard menu, viewport margins.
/// Soft-skips when JOBSY_E2E_BASE_URL is unset/unreachable.
/// </summary>
[Collection("PlaywrightSmoke")]
public class LandingPlaywrightTests
{
    [Fact]
    public async Task Landing_has_no_map_or_blazor_and_hero_cta_works()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (w, h, label) in new[] { (1440, 900, "desktop"), (390, 844, "mobile") })
        {
            var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                Locale = "nl-NL"
            });
            var page = await context.NewPageAsync();
            var banned = new List<string>();
            page.Request += (_, req) =>
            {
                var u = req.Url;
                if (u.Contains("maplibre", StringComparison.OrdinalIgnoreCase)
                    || u.Contains("jobMap", StringComparison.OrdinalIgnoreCase)
                    || u.Contains("tiles.openfreemap.org", StringComparison.OrdinalIgnoreCase)
                    || u.Contains("api/vacancies", StringComparison.OrdinalIgnoreCase)
                    || u.Contains("_blazor", StringComparison.OrdinalIgnoreCase)
                    || u.Contains("blazor.web.js", StringComparison.OrdinalIgnoreCase))
                {
                    banned.Add(u);
                }
            };

            await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            Assert.Empty(banned);

            var cta = page.Locator("a[data-kpi='LandingCtaTest']").First;
            await Assertions.Expect(cta).ToBeVisibleAsync();
            Assert.Contains("/ontdek", await cta.GetAttributeAsync("href") ?? "");

            // No horizontal overflow
            var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflow);

            var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "landing-playwright");
            Directory.CreateDirectory(artifactDir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"landing-{label}.png"), FullPage = true });
            await context.CloseAsync();
        }

        // Keyboard menu open/close at 390
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 390, Height = 844 }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var summary = page.Locator("details.pub-menu > summary");
            if (await summary.CountAsync() > 0)
            {
                await summary.FocusAsync();
                await page.Keyboard.PressAsync("Enter");
                await Assertions.Expect(page.Locator("details.pub-menu[open]")).ToBeVisibleAsync();
                await page.Keyboard.PressAsync("Escape");
                await Assertions.Expect(page.Locator("details.pub-menu[open]")).ToHaveCountAsync(0);
            }
        }
    }

    [Fact]
    public async Task Cookie_banner_does_not_cover_hero_cta_at_360x640()
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
            ViewportSize = new() { Width = 360, Height = 640 }
        });
        // Clear consent so banner shows
        await context.ClearCookiesAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.EvaluateAsync("() => { try { localStorage.removeItem('Jobsy.CookieConsent'); document.documentElement.classList.remove('cookie-consent-known'); } catch(e){} }");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var banner = page.Locator(".cookie-consent");
        var cta = page.Locator(".pub-landing__ctas a[data-kpi='LandingCtaTest']").First;
        if (await banner.CountAsync() == 0 || await cta.CountAsync() == 0)
        {
            return;
        }

        var bannerBox = await banner.BoundingBoxAsync();
        var ctaBox = await cta.BoundingBoxAsync();
        Assert.NotNull(bannerBox);
        Assert.NotNull(ctaBox);
        // CTA bottom should sit above banner top (not covered).
        Assert.True(ctaBox!.Y + ctaBox.Height <= bannerBox!.Y + 4,
            $"CTA overlaps cookie banner (ctaBottom={ctaBox.Y + ctaBox.Height}, bannerTop={bannerBox.Y})");
    }

    [Fact]
    public async Task Hero_scene_chips_stay_inside_viewport_both_variants()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var width in new[] { 360, 390, 414 })
            foreach (var variant in new[] { "on", "zw" })
                foreach (var lang in new[] { "nl", "ar" })
                {
                    await using var context = await browser.NewContextAsync(new()
                    {
                        ViewportSize = new() { Width = width, Height = 844 },
                        Locale = lang == "ar" ? "ar" : "nl-NL"
                    });
                    var page = await context.NewPageAsync();
                    await page.GotoAsync(
                        $"{baseUrl}/?_variant={variant}&lang={lang}",
                        new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

                    var ok = await page.EvaluateAsync<bool>("""
                () => {
                  const scene = document.querySelector('.pub-landing__hero-scene');
                  if (!scene) return false;
                  const vw = window.innerWidth;
                  const nodes = [scene, ...scene.querySelectorAll('*')];
                  for (const el of nodes) {
                    if (!(el instanceof HTMLElement)) continue;
                    const r = el.getBoundingClientRect();
                    if (r.width < 1 || r.height < 1) continue;
                    if (r.left < 12 - 0.5 || r.right > vw - 12 + 0.5) return false;
                  }
                  for (const chip of scene.querySelectorAll('.pub-landing__ppc-chip, .pub-chip, .pub-landing__float')) {
                    if (chip.scrollWidth > chip.clientWidth + 1) return false;
                  }
                  return true;
                }
                """);
                    Assert.True(ok, $"hero scene overflow at {width}px variant={variant} lang={lang}");
                }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently;
        }
        catch
        {
            return false;
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

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
