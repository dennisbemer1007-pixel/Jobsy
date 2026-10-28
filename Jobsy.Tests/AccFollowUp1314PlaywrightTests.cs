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

        var metrics = new Dictionary<string, object> { ["acc_head"] = "82e9f64" };

        await Capture1314ExamplesAsync(browser, baseUrl, password, metrics);
        await Capture1301VacancySearchUrlSyncAsync(browser, baseUrl, password, metrics);
        await MeasureCandidatePerfAsync(browser, baseUrl, password, metrics);

        var metricsPath = Path.Combine(ArtifactDir, "acc-followup-metrics.json");
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metricsPath, metricsJson);
        await File.WriteAllTextAsync(Path.Combine(AgentStoreDir, "acc-followup-metrics.json"), metricsJson);
    }

    private static async Task Capture1314ExamplesAsync(
        IBrowser browser,
        string baseUrl,
        string password,
        Dictionary<string, object> metrics)
    {
        foreach (var (lang, locale, prefix) in new[] { ("nl", "nl-NL", "acc-1314-nl"), ("en", "en-GB", "acc-1314-en") })
        {
            foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await CaptureQuickScanExampleAsync(browser, baseUrl, password, metrics, lang, locale, prefix, width, height, tag);
                await CaptureOnboardingMiniExampleAsync(browser, baseUrl, password, metrics, lang, locale, prefix, width, height, tag);
                await CaptureDeepAnalysisExampleAsync(browser, baseUrl, password, metrics, lang, locale, prefix, width, height, tag);
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
        string prefix,
        int width,
        int height,
        string tag)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_ONBOARDING_EMAIL") ?? OnboardingE2eProfileEnsurer.LoginEmail).Trim();
        await using var context = await NewContextAsync(browser, width, height, locale);
        var page = await NewLoggedInPageAsync(context, baseUrl, email, password);
        await SetCultureAsync(page, lang);

        await NavigateToQuickScanQuestionAsync(page, baseUrl);

        var example = page.Locator("[data-testid=test-item-example]");
        await example.WaitForAsync(new() { Timeout = 90_000 });
        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{prefix}-quick-{tag}.png";
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
        string prefix,
        int width,
        int height,
        string tag)
    {
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_ONBOARDING_EMAIL") ?? OnboardingE2eProfileEnsurer.LoginEmail).Trim();
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

        await example.WaitForAsync(new() { Timeout = 90_000 });
        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{prefix}-onboarding-mini-{tag}.png";
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
        string prefix,
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
            await example.WaitForAsync(new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            await page.GotoAsync(baseUrl + "/candidate/competencies?stap=vraag", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await AcceptTestConsentIfPresentAsync(page);
            await ClickBeginOrContinueAsync(page);
            await example.WaitForAsync(new() { Timeout = 60_000 });
            metrics[$"1314_{lang}_deep_fallback_quick_{tag}"] = page.Url;
        }

        var text = (await example.InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var file = $"{prefix}-deep-{tag}.png";
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

    private static async Task NavigateToQuickScanQuestionAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        var testsTab = page.Locator(".passport-tab", new() { HasText = "Mijn tests" });
        if (await testsTab.CountAsync() == 0)
        {
            testsTab = page.GetByRole(AriaRole.Tab, new() { Name = "Mijn tests" });
        }

        if (await testsTab.CountAsync() > 0)
        {
            await testsTab.First.ClickAsync();
            await page.WaitForTimeoutAsync(500);
        }

        var startCompetence = page.Locator("a[href*='/candidate/competencies'], button:has-text('Hoe je werkt')").First;
        if (await startCompetence.CountAsync() > 0)
        {
            await startCompetence.ClickAsync();
            await page.WaitForURLAsync("**/candidate/competencies**", new() { Timeout = 60_000 });
        }
        else
        {
            await page.GotoAsync(baseUrl + "/candidate/competencies?stap=vraag", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        await WaitForInteractiveAppAsync(page);
        await AcceptTestConsentIfPresentAsync(page);
        await ClickBeginOrContinueAsync(page);
        await WaitForInteractiveAppAsync(page);
    }

    private static async Task WaitForInteractiveAppAsync(IPage page)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  if (document.querySelector('[data-testid=test-item-example]')) return true;
                  return document.documentElement.getAttribute('data-lobsy-circuit') === 'ready';
                }
                """,
                null,
                new() { Timeout = 120_000 });
        }
        catch (TimeoutException)
        {
            // fall through — example wait will surface failure
        }
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
        var artifactPath = Path.Combine(ArtifactDir, fileName);
        await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = fullPage });
        File.Copy(artifactPath, Path.Combine(AgentStoreDir, fileName), overwrite: true);
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
        await SaveScreenshotAsync(page, "acc-1301-vacatures-url-sync-1440.png", fullPage: false);
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
        metrics["perf_sollic_targetMs"] = 1000;
        metrics["perf_sollic_maxMs"] = 3000;

        await page.GotoAsync(baseUrl + "/candidate/applications", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".sollic-card, .sollicitaties-page-wrap, [data-testid=sollic-list]").First
            .WaitForAsync(new() { Timeout = 60_000 });
        await SaveScreenshotAsync(page, "acc-perf-sollic-1440.png", fullPage: false);

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
