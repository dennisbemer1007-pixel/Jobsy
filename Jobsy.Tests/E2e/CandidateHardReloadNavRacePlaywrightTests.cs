using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Repro: hard reload on Ontdekkingsreis then immediate header nav — URL vs DOM vs CSS on Acceptatie.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class CandidateHardReloadNavRacePlaywrightTests
{
    private const string AccUrl = "https://acceptatie.lobsy.nl";
    private const string Email = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    [Theory]
    [InlineData(1280, 900, 10)]
    [InlineData(390, 844, 10)]
    public async Task Hard_reload_reis_then_immediate_header_nav(int width, int height, int iterations)
    {
        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "hard-reload-nav-race");
        Directory.CreateDirectory(outDir);
        var tag = width >= 500 ? "1280" : "390";

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
            {
                consoleErrors.Add($"{msg.Type}: {msg.Text}");
            }
        };

        await page.GotoAsync(AccUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.FillAsync("input[name='email']", Email);
        await page.FillAsync("input[name='password']", Password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(u => !u.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });

        await page.GotoAsync(AccUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".journey-overview, .career-page").First.WaitForAsync(new() { Timeout = 60_000 });

        var results = new List<object>();
        var targets = new[] { ("/carriere", ".career-page, .career-card"), ("/candidate/paspoort", ".passport-page"), ("/banenkaart", ".jobsy-discovery, .bk-page") };

        for (var i = 0; i < iterations; i++)
        {
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            var (targetHref, targetProbe) = targets[i % targets.Length];

            var circuitAtClick = await page.EvaluateAsync<string>(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') || 'none'");

            var link = width >= 500
                ? page.Locator($".candidate-header-nav a[href='{targetHref}']").First
                : page.Locator($"nav.bottom-nav a[href='{targetHref}']").First;
            await link.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await link.ClickAsync();

            await page.WaitForTimeoutAsync(400);

            var snapshot = await page.EvaluateAsync<JsonElement>(
                """
                ({ targetHref, targetProbe }) => {
                  const probes = targetProbe.split(',').map(s => s.trim());
                  const journey = !!document.querySelector('.journey-overview');
                  const targetEl = probes.map(p => document.querySelector(p)).find(Boolean);
                  const sheets = [...document.querySelectorAll('link[rel=stylesheet]')].map(l => l.href || '');
                  const routeCss = sheets.filter(h => /ontdekkingsreis|carriere|paspoort|banenkaart|app\\.min/i.test(h));
                  const sun = document.querySelector('.journey-scene__sun');
                  let sunFill = '';
                  if (sun) sunFill = getComputedStyle(sun).fill || '';
                  return {
                    href: location.pathname,
                    circuit: document.documentElement.getAttribute('data-lobsy-circuit'),
                    journeyVisible: journey,
                    targetVisible: !!targetEl,
                    urlMatchesTarget: location.pathname.replace(/\/$/, '') === targetHref.replace(/\/$/, ''),
                    contentMismatch: location.pathname.replace(/\/$/, '') === targetHref.replace(/\/$/, '') && journey && !targetEl,
                    routeCssCount: routeCss.length,
                    hasOntdekkingsreisCss: sheets.some(h => h.includes('ontdekkingsreis.css')),
                    blackSun: sunFill === 'rgb(0, 0, 0)' || sunFill === 'black',
                    sunFill
                  };
                }
                """,
                new { targetHref, targetProbe });

            results.Add(new
            {
                iteration = i,
                target = targetHref,
                circuitAtClick,
                snapshot = JsonSerializer.Deserialize<object>(snapshot.GetRawText())
            });

            if (snapshot.TryGetProperty("contentMismatch", out var mismatch) && mismatch.GetBoolean())
            {
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"mismatch-{tag}-iter{i}-{targetHref.Trim('/').Replace('/', '-')}.png"),
                    FullPage = false
                });
            }
        }

        var report = new
        {
            width,
            iterations,
            consoleErrors = consoleErrors.Take(40).ToList(),
            results
        };
        await File.WriteAllTextAsync(
            Path.Combine(outDir, $"report-{tag}.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        var mismatches = results.Count(r =>
        {
            var json = JsonSerializer.Serialize(r);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("snapshot").TryGetProperty("contentMismatch", out var m) && m.GetBoolean();
        });

        Assert.True(mismatches == 0, $"URL/content mismatch in {mismatches}/{iterations} iterations ({tag}). See artifacts/hard-reload-nav-race/report-{tag}.json");
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
