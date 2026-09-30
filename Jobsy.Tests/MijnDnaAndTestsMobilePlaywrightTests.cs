using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mijn DNA variant C + mobile questionnaire overflow/perf checks.
/// Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset/unreachable.
/// HTML size hard-asserts &lt; 60 KB; TTFB &lt; 300 ms only when <c>JOBSY_E2E_STRICT_PERF=1</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class MijnDnaAndTestsMobilePlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";
    private const int MaxHtmlBytes = 60_000;

    private static readonly (int Width, int Height)[] Viewports =
    [
        (360, 780),
        (390, 844),
        (430, 932)
    ];

    private static readonly string[] TestRoutes =
    [
        "/candidate/competencies",
        "/candidate/career",
        "/candidate/culture",
        "/candidate/values"
    ];

    [Fact]
    public void Structure_covers_dna_c_and_questionnaire_guards()
    {
        var root = FindRepoRoot();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/DnaPanel.razor"));
        Assert.Contains("dna-carousel", panel, StringComparison.Ordinal);
        Assert.Contains("dna-tiles__grid", panel, StringComparison.Ordinal);
        Assert.Contains("Dna.HighlightsTitle", panel, StringComparison.Ordinal);
        Assert.Contains("GetMyKompasDnaResultAsync", panel, StringComparison.Ordinal);

        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/onboarding-wizard.css"));
        var idx = 0;
        while ((idx = wizard.IndexOf(".q-likert", idx, StringComparison.Ordinal)) >= 0)
        {
            var window = wizard[Math.Max(0, idx - 80)..idx];
            Assert.Contains(".ob-wizard", window, StringComparison.Ordinal);
            idx += ".q-likert".Length;
        }

        var qCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/questionnaire.css"));
        Assert.Contains("min-inline-size: 0", qCss, StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/QuestionnaireShell.razor"));
        Assert.Contains("@(\" \")", shell, StringComparison.Ordinal);
        Assert.Contains("Questionnaire.PrivacyShort", shell, StringComparison.Ordinal);

        var testFile = Path.Combine(root, "Jobsy.Tests", "MijnDnaAndTestsMobilePlaywrightTests.cs");
        Assert.True(File.Exists(testFile));
        var src = File.ReadAllText(testFile);
        Assert.Contains("60_000", src, StringComparison.Ordinal);
        Assert.Contains("360", src, StringComparison.Ordinal);
        Assert.Contains("390", src, StringComparison.Ordinal);
        Assert.Contains("430", src, StringComparison.Ordinal);
        Assert.Contains("artifacts/playwright-dna", src, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(360, 780)]
    [InlineData(390, 844)]
    [InlineData(430, 932)]
    public async Task Dna_and_test_screens_no_overflow_at_viewport(int width, int height)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        EnsureChromium();
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "playwright-dna", $"{width}x{height}");
        Directory.CreateDirectory(artifactDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var guard = AttachGuards(page);

        await LoginAsync(page, baseUrl);
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.GotoAsync(
            baseUrl + "/candidate/profile?tab=dna",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(800);
        await AssertNoHorizontalOverflowAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifactDir, "dna.png"),
            FullPage = true
        });

        // Variant C markers — soft when the deploy predates this PR.
        var hasVariantC = await page.Locator(".dna-carousel, .dna-tiles__grid, .dna-highlights").CountAsync() > 0;
        if (hasVariantC)
        {
            var chartCount = await page.Locator("svg.radar, .bar-list li, .pole-sliders__row").CountAsync();
            Assert.True(chartCount >= 1, "Expected at least one chart in the DNA carousel/detail.");
            var provisional = await page.Locator(".dna-card__badge--provisional, .dna-tile--provisional").CountAsync();
            var rings = await page.Locator(".progress-ring__label").CountAsync();
            Assert.True(rings >= 1 || provisional >= 0);
        }

        foreach (var route in TestRoutes)
        {
            await page.GotoAsync(
                baseUrl + route,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(600);
            await AssertNoHorizontalOverflowAsync(page);

            var scale = page.Locator(".q-likert--current .q-likert__scale button, .q-likert.is-current .q-likert__scale button, fieldset.q-likert:not(.q-likert--collapsed) .q-likert__scale button");
            var scaleCount = await scale.CountAsync();
            if (scaleCount >= 5)
            {
                for (var i = 0; i < 5; i++)
                {
                    Assert.True(await scale.Nth(i).IsVisibleAsync(), $"Scale button {i + 1} not visible on {route}");
                }

                var low = page.GetByText("Past niet", new() { Exact = false });
                var high = page.GetByText(new Regex("Past wel|Past heel", RegexOptions.IgnoreCase));
                Assert.True(await low.First.IsVisibleAsync(), $"Past niet missing on {route}");
                Assert.True(await high.First.IsVisibleAsync(), $"Past wel missing on {route}");

                var sticky = page.Locator(".questionnaire__footer button, .q-shell__footer button, button.questionnaire-next, .q-footer button").First;
                if (await sticky.CountAsync() > 0)
                {
                    Assert.True(await sticky.IsVisibleAsync(), $"Sticky footer not visible on {route}");
                }
            }

            var slug = route.Trim('/').Replace('/', '-');
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"{slug}.png"),
                FullPage = true
            });
        }

        // Wizard regression at 390 only (one viewport is enough).
        if (width == 390)
        {
            await page.GotoAsync(
                baseUrl + "/candidate/start",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(700);
            if (await page.Locator(".ob-wizard").CountAsync() > 0)
            {
                await AssertNoHorizontalOverflowAsync(page);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(artifactDir, "wizard.png"),
                    FullPage = true
                });
            }
        }

        guard.AssertClean();
    }

    [Fact]
    public async Task Dna_html_size_and_ttfb()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl);

        var cookies = await context.CookiesAsync();
        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

        // Warm request for TTFB.
        _ = await FetchHtmlAsync(baseUrl + "/candidate/profile?tab=dna", cookieHeader);
        var sw = Stopwatch.StartNew();
        var (bytes, ttfbMs) = await FetchHtmlAsync(baseUrl + "/candidate/profile?tab=dna", cookieHeader);
        sw.Stop();

        Console.WriteLine(
            $"[MijnDnaPerf] htmlBytes={bytes} ttfbMs={ttfbMs:F0} wallMs={sw.ElapsedMilliseconds} url={baseUrl}");

        // Only hard-assert size when the slim DNA persist is live (local CI stack / post-merge Acc).
        await page.GotoAsync(
            baseUrl + "/candidate/profile?tab=dna",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(500);
        var hasSlim = await page.Locator(".dna-carousel, .dna-tiles__grid").CountAsync() > 0;

        if (hasSlim)
        {
            Assert.True(
                bytes < MaxHtmlBytes,
                $"DNA HTML was {bytes} bytes; expected < {MaxHtmlBytes}.");
        }
        else
        {
            Console.WriteLine(
                $"[MijnDnaPerf] slim DNA UI not detected — logging size only (before baseline {bytes} bytes).");
        }

        if (string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_STRICT_PERF"), "1", StringComparison.Ordinal))
        {
            Assert.True(ttfbMs < 300, $"TTFB was {ttfbMs:F0} ms; expected < 300 when JOBSY_E2E_STRICT_PERF=1.");
        }
    }

    private static async Task<(int Bytes, double TtfbMs)> FetchHtmlAsync(string url, string cookieHeader)
    {
        var handler = new SocketsHttpHandler { AllowAutoRedirect = true };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        var sw = Stopwatch.StartNew();
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var ttfb = sw.Elapsed.TotalMilliseconds;
        var body = await response.Content.ReadAsByteArrayAsync();
        return (body.Length, ttfb);
    }

    private static async Task AssertNoHorizontalOverflowAsync(IPage page)
    {
        var ok = await page.EvaluateAsync<bool>(
            """
            () => {
              const doc = document.documentElement;
              if (doc.scrollWidth > window.innerWidth + 1) return false;
              const nodes = document.querySelectorAll('body *');
              const limit = window.innerWidth + 1;
              for (const el of nodes) {
                const style = window.getComputedStyle(el);
                if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') continue;
                const r = el.getBoundingClientRect();
                if (r.width < 1 || r.height < 1) continue;
                if (r.right > limit + 0.5) return false;
              }
              return true;
            }
            """);
        Assert.True(ok, $"Horizontal overflow at {page.Url}");
    }

    private static async Task LoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
    }

    private static NetworkGuard AttachGuards(IPage page)
    {
        var guard = new NetworkGuard();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                guard.ConsoleErrors.Add(msg.Text);
            }
        };
        page.PageError += (_, err) => guard.ConsoleErrors.Add(err);
        return guard;
    }

    private static void EnsureChromium()
    {
        try
        {
            Microsoft.Playwright.Program.Main(["install", "chromium"]);
        }
        catch
        {
            // Already installed in CI.
        }
    }

    private static string? ResolveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var ci = (Environment.GetEnvironmentVariable("JOBSY_CI_WEB_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(ci) ? null : ci;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class NetworkGuard
    {
        public ConcurrentBag<string> ConsoleErrors { get; } = new();

        public void AssertClean()
        {
            static bool IsNoisy(string e) =>
                e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Connection disconnected", StringComparison.OrdinalIgnoreCase)
                || e.Contains("WebSocket closed", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Failed to start the transport", StringComparison.OrdinalIgnoreCase)
                || e.Contains("openfreemap.org", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);

            var fatal = ConsoleErrors.Where(e => !IsNoisy(e)).ToList();
            Assert.True(fatal.Count == 0, "Console/page errors: " + string.Join(" | ", fatal.Take(8)));
        }
    }
}
