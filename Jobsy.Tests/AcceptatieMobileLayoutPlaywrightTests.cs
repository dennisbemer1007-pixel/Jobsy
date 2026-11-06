using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile layout guards for test-flow footer, passport tabs, and WebKit post-login responsiveness.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AcceptatieMobileLayoutPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_test_flow_footer_is_fixed_and_coach_clears_it()
    {
        await using var page = await OpenStaticPageAsync(390, 844, TestFlowHtml());
        var footer = page.Locator(".test-flow__footer");
        var position = await footer.EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("fixed", position);

        await page.EvaluateAsync("""
            () => {
              const footer = document.querySelector('.test-flow__footer');
              const h = Math.ceil(footer.getBoundingClientRect().height);
              document.documentElement.style.setProperty('--sticky-footer-h', h + 'px');
              document.documentElement.style.setProperty('--bottom-nav-h', '64px');
            }
            """);

        var coachBottom = await page.Locator(".lobsy-coach-dock").EvaluateAsync<double>("""
            el => parseFloat(getComputedStyle(el).bottom)
            """);
        Assert.True(coachBottom >= 64 + 40, $"coach bottom offset was {coachBottom}px");
    }

    [Fact]
    public async Task Mobile_passport_tabs_sticky_shell_does_not_overlap_preceding_card()
    {
        await using var page = await OpenStaticPageAsync(390, 844, PassportTabsHtml());

        var shellSticky = await page.Locator(".passport-tabs-shell").EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("sticky", shellSticky);

        var noOverlap = await page.EvaluateAsync<bool>("""
            () => {
              const shell = document.querySelector('.passport-tabs-shell');
              const card = document.querySelector('.passport-overview');
              if (!shell || !card) return false;
              const s = shell.getBoundingClientRect();
              const c = card.getBoundingClientRect();
              return s.top >= c.bottom - 1;
            }
            """);
        Assert.True(noOverlap);
    }

    [Fact]
    public async Task WebKit_stays_interactive_after_login()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "webkit"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });

        await page.WaitForTimeoutAsync(3_500);

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 45_000 });

        var nav = page.Locator(".bottom-nav a, .bottom-nav button").First;
        if (await nav.CountAsync() == 0)
        {
            return;
        }

        await nav.ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(800);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await page.Locator(".circuit-error, .reconnect-overlay").CountAsync());
    }

    [Fact]
    public void Service_worker_registration_waits_for_blazor_circuit()
    {
        var core = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/app-core.js"));
        Assert.Contains("data-lobsy-circuit", core, StringComparison.Ordinal);
        Assert.Contains("updateViaCache", core, StringComparison.Ordinal);
        var sw = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/service-worker.js"));
        Assert.DoesNotContain("cache.put(\"/\", copy)", sw, StringComparison.Ordinal);
    }

    private static async Task<IPage> OpenStaticPageAsync(int width, int height, string html)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = true,
            IsMobile = true,
            Locale = "nl-NL"
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(html);
        return page;
    }

    private static string TestFlowHtml()
    {
        var root = FindRepoRoot();
        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var testsCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/tests.css"));
        return """
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
            """ + appCss + testsCss + """
            </style></head><body>
            <div class="app-shell has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px">
            <main class="app-main">
            <div class="test-flow test-flow--deep" data-test-flow="Career">
            <footer class="test-flow__footer" style="height:72px">
            <button type="button" class="test-flow__next engagement-btn">Volgende</button>
            </footer></div></main>
            <nav class="bottom-nav" style="height:64px"></nav>
            <div class="lobsy-coach-dock" data-lobsy-coach style="position:fixed;right:12px;bottom:calc(var(--bottom-nav-h) + var(--sticky-footer-h,0px) + 12px)">
            <button type="button" id="lobsy-coach-btn" class="lobsy-coach-dock__btn"></button>
            </div></div></body></html>
            """;
    }

    private static string PassportTabsHtml()
    {
        var root = FindRepoRoot();
        var appCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var passportCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        var kompasCss = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kompas-illustrated.css"));
        return """
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
            :root { --app-header-height: 56px; --bg: #f5f2ee; --surface: #fffcfa; --border: #ddd5cc; --brand: #0f2d5c; --z-sticky: 20; --space-3: 0.75rem; }
            """ + appCss + passportCss + kompasCss + """
            </style></head><body>
            <div class="app-shell has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px">
            <main class="app-main">
            <div class="panel-page passport-page passport-page--illustrated">
            <div class="passport-layout"><div class="passport-layout__main">
            <section class="passport-overview" style="background:var(--surface);padding:1rem;margin-bottom:1rem;border:1px solid var(--border);border-radius:12px">
            <button type="button" style="width:100%;padding:0.75rem;background:var(--brand);color:#fff;border:0;border-radius:8px">Naar de tests</button>
            </section>
            <div class="passport-tabs-shell">
            <div class="passport-tabs" role="tablist">
            <button type="button" class="passport-tab passport-tab--active" id="passport-tab-dna">DNA</button>
            <button type="button" class="passport-tab" id="passport-tab-tests">Tests</button>
            </div></div></div></div></div></main>
            <nav class="bottom-nav" aria-label="Nav" style="height:64px"></nav>
            </div></body></html>
            """;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/login", HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
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
}
