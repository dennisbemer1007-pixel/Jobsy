using System.Text.Json;
using Jobsy.Infrastructure.Ops;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acceptatie follow-up: #1314 examples, #1301 vacature search URL sync, sollic/banenkaart timings.
/// Run: JOBSY_ACC_FOLLOWUP=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl dotnet test --filter Acc_followup
/// Password via JOBSY_E2E_PASSWORD (never logged). Artifacts also copied to agent store artifacts/.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AccFollowUp1314PlaywrightTests
{
    private const string ArtifactDir = "/opt/cursor/artifacts";
    private const string AgentStoreDir = "/cursor/stores/self/artifacts";

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
        Directory.CreateDirectory(AgentStoreDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var accHead = await ResolveAcceptatieCommitAsync(baseUrl);
        var artifactPrefix = $"acc-{accHead}-1314";
        var metrics = new Dictionary<string, object> { ["acc_head"] = accHead, ["artifact_prefix"] = artifactPrefix };

        await Capture1314ExamplesAsync(browser, baseUrl, password, artifactPrefix, metrics);
        await TryCaptureAsync(metrics, "1301_url_sync", () => Capture1301VacancySearchUrlSyncAsync(browser, baseUrl, password, accHead, metrics));
        await TryCaptureAsync(metrics, "1323_werkgever_search_capture", () => CaptureWerkgeverSearchFieldAsync(browser, baseUrl, password, accHead, metrics));
        await TryCaptureAsync(metrics, "perf", () => MeasurePerfAsync(browser, baseUrl, password, metrics));

        var metricsPath = Path.Combine(ArtifactDir, "acc-followup-metrics.json");
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metricsPath, metricsJson);
        await File.WriteAllTextAsync(Path.Combine(AgentStoreDir, "acc-followup-metrics.json"), metricsJson);

        if (metrics.TryGetValue("1314_failures", out var failuresObj)
            && failuresObj is List<string> failures
            && failures.Count > 0)
        {
            Assert.Fail("Acc #1314 capture failures: " + string.Join("; ", failures));
        }
    }

    private static async Task TryCaptureAsync(
        Dictionary<string, object> metrics,
        string key,
        Func<Task> capture)
    {
        try
        {
            await capture();
            metrics[$"{key}_status"] = "ok";
        }
        catch (Exception ex)
        {
            metrics[$"{key}_status"] = "fail";
            metrics[$"{key}_error"] = ex.Message;
            if (!metrics.TryGetValue("1314_failures", out var listObj)
                || listObj is not List<string> list)
            {
                list = [];
                metrics["1314_failures"] = list;
            }

            list.Add($"{key}: {ex.GetType().Name}");
        }
    }

    private static async Task<string> ResolveAcceptatieCommitAsync(string baseUrl)
    {
        var env = (Environment.GetEnvironmentVariable("JOBSY_ACC_HEAD") ?? "").Trim();
        if (env.Length is >= 7 and <= 40)
        {
            return env.Length > 12 ? env[..12] : env;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var html = await http.GetStringAsync(baseUrl.TrimEnd('/') + "/login");
        const string marker = "name=\"lobsy-commit\" content=\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "unknown";
        }

        start += marker.Length;
        var end = html.IndexOf('"', start);
        if (end <= start)
        {
            return "unknown";
        }

        var full = html[start..end].Trim();
        return full.Length >= 12 ? full[..12] : full;
    }

    private static async Task Capture1314ExamplesAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        string artifactPrefix,
        Dictionary<string, object> metrics)
    {
        foreach (var (lang, locale) in new[] { ("nl", "nl-NL"), ("en", "en-GB") })
        {
            foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await TryCaptureAsync(
                    metrics,
                    $"1314_{lang}_quick_{tag}",
                    () => CaptureQuickScanExampleAsync(browser, baseUrl, password, metrics, lang, locale, artifactPrefix, width, height, tag));
                await TryCaptureAsync(
                    metrics,
                    $"1314_{lang}_mini_{tag}",
                    () => CaptureOnboardingMiniExampleAsync(browser, baseUrl, password, metrics, lang, locale, artifactPrefix, width, height, tag));
                await TryCaptureAsync(
                    metrics,
                    $"1314_{lang}_deep_{tag}",
                    () => CaptureDeepAnalysisExampleAsync(browser, baseUrl, password, metrics, lang, locale, artifactPrefix, width, height, tag));
            }
        }
    }

    private static async Task CaptureQuickScanExampleAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics,
        string lang,
        string locale,
        string artifactPrefix,
        int width,
        int height,
        string tag)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_EMAIL") ?? KbE2e.DefaultIncompleteEmail).Trim();
        await using var context = await NewContextAsync(browser, width, height, locale);
        var page = await NewLoggedInPageAsync(context, baseUrl, email, password);
        await SetCultureAsync(page, lang);

        await page.GotoAsync(baseUrl + "/candidate/competencies?stap=vraag", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await EnsureTestFlowReadyAsync(page);

        var example = page.Locator("[data-testid=test-item-example]");
        await example.WaitForAsync(new() { Timeout = 90_000 });
        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{artifactPrefix}-{lang}-quick-{tag}.png";
        await SaveScreenshotAsync(page, file, fullPage: true);
        metrics[$"1314_{lang}_quick_{tag}"] = new { exampleChars = text.Length, url = page.Url };
    }

    private static async Task CaptureOnboardingMiniExampleAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics,
        string lang,
        string locale,
        string artifactPrefix,
        int width,
        int height,
        string tag)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? KbE2e.DefaultCompleteEmail).Trim();
        await using var context = await NewContextAsync(browser, width, height, locale);
        var page = await NewLoggedInPageAsync(context, baseUrl, email, password);
        await SetCultureAsync(page, lang);

        await page.GotoAsync(baseUrl + "/candidate/start/wizard", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await AcceptTestConsentIfPresentAsync(page);

        var example = page.Locator("[data-testid=test-item-example]");
        if (await example.CountAsync() == 0)
        {
            await TryAdvanceWizardToMiniBlockAsync(page);
        }

        await EnsureTestFlowReadyAsync(page);
        await example.WaitForAsync(new() { Timeout = 90_000 });
        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{artifactPrefix}-{lang}-onboarding-mini-{tag}.png";
        await SaveScreenshotAsync(page, file, fullPage: true);
        metrics[$"1314_{lang}_mini_{tag}"] = new { exampleChars = text.Length, url = page.Url };
    }

    private static async Task CaptureDeepAnalysisExampleAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics,
        string lang,
        string locale,
        string artifactPrefix,
        int width,
        int height,
        string tag)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_EMAIL") ?? KbE2e.DefaultIncompleteEmail).Trim();
        await using var context = await NewContextAsync(browser, width, height, locale);
        var page = await NewLoggedInPageAsync(context, baseUrl, email, password);
        await SetCultureAsync(page, lang);

        await page.GotoAsync(baseUrl + "/candidate/deep-analysis/competence", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await AcceptTestConsentIfPresentAsync(page);

        var example = page.Locator("[data-testid=test-item-example]");
        try
        {
            await EnsureTestFlowReadyAsync(page);
            await example.WaitForAsync(new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            await page.GotoAsync(baseUrl + "/candidate/competencies?stap=vraag", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await EnsureTestFlowReadyAsync(page);
            await example.WaitForAsync(new() { Timeout = 60_000 });
            metrics[$"1314_{lang}_deep_fallback_quick_{tag}"] = page.Url;
        }

        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{artifactPrefix}-{lang}-deep-{tag}.png";
        await SaveScreenshotAsync(page, file, fullPage: true);
        metrics[$"1314_{lang}_deep_{tag}"] = new { exampleChars = text.Length, url = page.Url };
    }

    private static async Task TryAdvanceWizardToMiniBlockAsync(IPage page)
    {
        for (var i = 0; i < 12; i++)
        {
            if (await page.Locator("[data-testid=test-item-example]").CountAsync() > 0)
            {
                return;
            }

            var volgende = page.GetByRole(AriaRole.Button, new() { Name = "Volgende" });
            if (await volgende.CountAsync() == 0)
            {
                volgende = page.Locator("[data-ob-nav='next'], button.ob-wizard__next, .ob-wizard__footer .btn-primary");
            }

            if (await volgende.CountAsync() == 0)
            {
                break;
            }

            await volgende.First.ClickAsync(new() { Timeout = 10_000 });
            await page.WaitForTimeoutAsync(800);
        }
    }

    private static async Task EnsureTestFlowReadyAsync(IPage page)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            await AcceptTestConsentIfPresentAsync(page);
            await ClickBeginOrContinueAsync(page);

            if (await page.Locator("[data-testid=test-item-example]").CountAsync() > 0)
            {
                return;
            }

            var circuitReady = await page.EvaluateAsync<bool>(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'");
            if (circuitReady && await page.Locator(".test-flow__statement, .test-flow").CountAsync() > 0)
            {
                return;
            }

            await page.WaitForTimeoutAsync(750);
        }

        var debugName = $"acc-1314-debug-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
        await SaveScreenshotAsync(page, debugName, fullPage: true);
    }

    private static async Task ClickBeginOrContinueAsync(IPage page)
    {
        var begin = page.GetByRole(AriaRole.Button, new() { Name = "Begin" });
        if (await begin.CountAsync() == 0)
        {
            begin = page.GetByRole(AriaRole.Button, new() { Name = "Verder" });
        }

        if (await begin.CountAsync() == 0)
        {
            begin = page.GetByRole(AriaRole.Button, new() { Name = "Continue" });
        }

        if (await begin.CountAsync() > 0)
        {
            await begin.First.ClickAsync();
            await page.WaitForTimeoutAsync(800);
        }
    }

    private static async Task<IBrowserContext> NewContextAsync(IBrowser browser, int width, int height, string locale)
    {
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = locale,
            IgnoreHTTPSErrors = true,
            IsMobile = width < 600,
            HasTouch = width < 600
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        return context;
    }

    private static async Task<IPage> NewLoggedInPageAsync(IBrowserContext context, string baseUrl, string email, string password)
    {
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password));
        return page;
    }

    private static async Task SetCultureAsync(IPage page, string lang)
    {
        if (string.Equals(lang, "en", StringComparison.Ordinal))
        {
            await TrySwitchToEnglishAsync(page);
        }
    }

    private static async Task SaveScreenshotAsync(IPage page, string fileName, bool fullPage)
    {
        Directory.CreateDirectory(ArtifactDir);
        Directory.CreateDirectory(AgentStoreDir);
        var artifactPath = Path.Combine(ArtifactDir, fileName);
        await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = fullPage });
        var storePath = Path.Combine(AgentStoreDir, fileName);
        try
        {
            File.Copy(artifactPath, storePath, overwrite: true);
        }
        catch (IOException)
        {
            var bytes = await File.ReadAllBytesAsync(artifactPath);
            await File.WriteAllBytesAsync(storePath, bytes);
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

    private static async Task CaptureWerkgeverSearchFieldAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        string accHead,
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

        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password))
        {
            metrics["1323_werkgever_search"] = "employer_login_failed";
            return;
        }

        await page.GotoAsync(baseUrl + "/werkgever/vacatures", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var search = page.Locator(".ent-filter-bar input[type=search], .ent-filter-bar input").First;
        await search.WaitForAsync(new() { Timeout = 60_000 });
        await search.FocusAsync();
        var file = $"acc-{accHead}-1323-werkgever-search-1440.png";
        await SaveScreenshotAsync(page, file, fullPage: false);
        metrics["1323_werkgever_search"] = new { file, url = page.Url };
    }

    private static async Task Capture1301VacancySearchUrlSyncAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        string accHead,
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
        await SaveScreenshotAsync(page, $"acc-{accHead}-1301-vacatures-url-sync-1440.png", fullPage: false);
    }

    private static async Task MeasurePerfAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics)
    {
        var employerEmail = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL") ?? "ondernemer@jobsy.local").Trim();
        await using var employerCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(employerCtx);
        var employerPage = await employerCtx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(employerPage, baseUrl, employerEmail, password));

        var wgSollicMs = await NavigationReadyMsAsync(employerPage, baseUrl + "/werkgever/sollicitaties");
        metrics["perf_werkgever_sollicitaties_readyMs"] = wgSollicMs;
        metrics["perf_werkgever_sollic_targetMs"] = 1000;
        metrics["perf_werkgever_sollic_maxMs"] = 3000;

        var candidateEmail = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? KbE2e.DefaultCompleteEmail).Trim();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, candidateEmail, password));

        var banenMs = await NavigationReadyMsAsync(page, baseUrl + "/banenkaart?weergave=lijst");
        metrics["perf_banenkaart_readyMs"] = banenMs;

        await employerPage.GotoAsync(baseUrl + "/werkgever/sollicitaties", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await employerPage.Locator(
                ".ent-data-table, .wg-sollic, .sollicitaties-page-wrap, .werkgever-sollic, main, h1")
            .First.WaitForAsync(new() { Timeout = 60_000 });
        await SaveScreenshotAsync(employerPage, "acc-perf-werkgever-sollic-1440.png", fullPage: false);

        await page.GotoAsync(baseUrl + "/banenkaart?weergave=lijst", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=kb-list-row], .banenkaart-page-wrap, .map-pane").First
            .WaitForAsync(new() { Timeout = 60_000 });
        await SaveScreenshotAsync(page, "acc-perf-banenkaart-1440.png", fullPage: false);
    }

    private static async Task<double> NavigationReadyMsAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        return await page.EvaluateAsync<double>(
            """
            () => {
              const t = performance.timing;
              return t.domContentLoadedEventEnd - t.navigationStart;
            }
            """);
    }
}
