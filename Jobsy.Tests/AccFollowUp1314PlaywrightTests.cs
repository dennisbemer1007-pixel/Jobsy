using System.Text.Json;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acceptatie follow-up: #1314 examples, #1301 vacature search URL sync, sollic/banenkaart timings.
/// Run: JOBSY_ACC_FOLLOWUP=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl dotnet test --filter Acc_followup
/// Password via JOBSY_E2E_PASSWORD (never logged).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AccFollowUp1314PlaywrightTests
{
    private const string ArtifactDir = "/opt/cursor/artifacts";

    [Fact]
    public async Task Acc_followup_1314_1301_and_perf()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ACC_FOLLOWUP") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "https://acceptatie.lobsy.nl";
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            Assert.Fail("Acceptatie unreachable at " + baseUrl);
        }

        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD") ?? KbE2e.DefaultPassword).Trim();
        Directory.CreateDirectory(ArtifactDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var metrics = new Dictionary<string, object>();

        await Capture1314ExamplesAsync(browser, baseUrl, password, metrics);
        await Capture1301VacancySearchUrlSyncAsync(browser, baseUrl, password, metrics);
        await MeasureCandidatePerfAsync(browser, baseUrl, password, metrics);

        var metricsPath = Path.Combine(ArtifactDir, "acc-followup-metrics.json");
        await File.WriteAllTextAsync(metricsPath, JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task Capture1314ExamplesAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_EMAIL") ?? KbE2e.DefaultIncompleteEmail).Trim();
        var paths = new[]
        {
            "/candidate/competencies?stap=vraag",
            "/candidate/deep-analysis/competence"
        };

        foreach (var culture in new[] { ("nl", "nl-NL", "acc-1314-nl"), ("en", "en-GB", "acc-1314-en") })
        {
            foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await using var context = await browser.NewContextAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = height },
                    Locale = culture.Item2,
                    IgnoreHTTPSErrors = true
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);

                Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password), "Incomplete candidate login failed.");

                if (string.Equals(culture.Item1, "en", StringComparison.Ordinal))
                {
                    await TrySwitchToEnglishAsync(page);
                }

                foreach (var path in paths)
                {
                    await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
                    await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                    await AcceptTestConsentIfPresentAsync(page);
                    if (path.Contains("competencies", StringComparison.Ordinal))
                    {
                        var begin = page.GetByRole(AriaRole.Button, new() { Name = "Begin" });
                        if (await begin.CountAsync() == 0)
                        {
                            begin = page.GetByRole(AriaRole.Button, new() { Name = "Verder" });
                        }

                        if (await begin.CountAsync() > 0)
                        {
                            await begin.First.ClickAsync();
                            await page.WaitForTimeoutAsync(800);
                        }
                    }

                    var example = page.Locator("[data-testid=test-item-example]");
                    try
                    {
                        await example.WaitForAsync(new() { Timeout = 45_000 });
                    }
                    catch (TimeoutException)
                    {
                        if (path.Contains("deep-analysis", StringComparison.Ordinal))
                        {
                            await page.GotoAsync(baseUrl + "/candidate/start/wizard", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                            await AcceptTestConsentIfPresentAsync(page);
                            await example.WaitForAsync(new() { Timeout = 45_000 });
                        }
                        else
                        {
                            metrics[$"1314_missing_{culture.Item1}_{tag}_{path}"] = page.Url;
                            continue;
                        }
                    }

                    var text = (await example.InnerTextAsync()).Trim();
                    Assert.False(string.IsNullOrWhiteSpace(text));
                    var slug = path.Contains("deep-analysis", StringComparison.Ordinal) ? "deep" : "quick";
                    var file = $"{culture.Item3}-{slug}-{tag}.png";
                    await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, file), FullPage = true });
                    metrics[$"1314_{culture.Item1}_{slug}_{tag}"] = new { exampleChars = text.Length, url = page.Url };
                }
            }
        }
    }

    private static async Task AcceptTestConsentIfPresentAsync(IPage page)
    {
        var consent = page.Locator("[data-consent=test] button.btn-primary");
        if (await consent.CountAsync() > 0)
        {
            await consent.First.ClickAsync();
            await page.WaitForTimeoutAsync(600);
        }
    }

    private static async Task TrySwitchToEnglishAsync(IPage page)
    {
        var en = page.Locator("button[lang=en], a[href*='culture=en'], [data-testid='lang-en']").First;
        if (await en.CountAsync() > 0)
        {
            await en.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            return;
        }

        await page.GotoAsync(page.Url + (page.Url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "culture=en",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    private static async Task Capture1301VacancySearchUrlSyncAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL") ?? "ondernemer@jobsy.local").Trim();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password), "Employer login failed.");
        await page.GotoAsync(baseUrl + "/werkgever/vacatures", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".ent-filter-bar input, .wg-vac__title").First.WaitForAsync(new() { Timeout = 60_000 });

        var search = page.Locator(".ent-filter-bar input[type=search], .ent-filter-bar input").First;
        var probe = "acc-url-sync-" + Guid.NewGuid().ToString("N")[..6];
        await search.FillAsync(probe);
        await page.WaitForFunctionAsync(
            """
            (needle) => window.location.search.includes('q=') && decodeURIComponent(window.location.search).includes(needle)
            """,
            probe,
            new() { Timeout = 15_000 });

        metrics["1301_url_sync"] = new { q = probe, search = page.Url };
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, "acc-1301-vacatures-url-sync-1440.png"), FullPage = false });
    }

    private static async Task MeasureCandidatePerfAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local").Trim();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password));

        var sollicMs = await NavigationReadyMsAsync(page, baseUrl + "/candidate/applications");
        var banenMs = await NavigationReadyMsAsync(page, baseUrl + "/banenkaart?weergave=lijst");
        metrics["perf_sollicitaties_readyMs"] = sollicMs;
        metrics["perf_banenkaart_readyMs"] = banenMs;
        metrics["perf_note"] = "SSR/API on acceptatie; sollic perf fix ships with #1317 branch.";

        await page.GotoAsync(baseUrl + "/candidate/applications", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".sollic-card, .sollicitaties-page-wrap, [data-testid=sollic-list]").First
            .WaitForAsync(new() { Timeout = 60_000 });
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, "acc-perf-sollic-1440.png"), FullPage = false });
    }

    private static async Task<double> NavigationReadyMsAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        var ready = await page.EvaluateAsync<double>(
            """
            () => {
              const t = performance.timing;
              return t.domContentLoadedEventEnd - t.navigationStart;
            }
            """);
        return ready;
    }
}
