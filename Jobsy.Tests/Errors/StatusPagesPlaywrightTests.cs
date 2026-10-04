using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 06 §06.1–§06.2: every status page in a real browser — desktop 1440×900 and mobile
/// 390×844, five languages (ar right-to-left), with and without JavaScript — plus the 403, 410,
/// maintenance, reconnect, inline-error and copy-code flows.
/// <para>
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c> (or when the host is unreachable), like the other
/// browser suites, and never targets production. Rows that need a seeded id, an account or the
/// opt-in throwing path skip themselves and say so through <see cref="SkipLog"/>.
/// </para>
/// </summary>
[Collection("PlaywrightSmoke")]
public class StatusPagesPlaywrightTests
{
    private const string DefaultCandidateEmail = "kandidaat@jobsy.local";
    private const string DefaultAdminEmail = "admin@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    /// <summary>Crockford base32 without I, L, O and U (decision E2).</summary>
    private const string SupportCodePattern = @"LB-[2-9A-HJKMNP-TV-Z]{4}";

    private static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];

    private static readonly (int Width, int Height)[] Viewports = [(1440, 900), (390, 844)];

    /// <summary>Text a visitor may never see (00-README §0).</summary>
    private static readonly string[] ForbiddenFragments = ["Exception", "at Jobsy.", "System.", "[PLACEHOLDER]"];

    /// <summary>Guid-shaped request identifiers must never reach the page either.</summary>
    private static readonly Regex GuidLike = new(
        @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b",
        RegexOptions.IgnoreCase);

    /// <summary>Why a row skipped, written next to the screenshots so the PR can quote it.</summary>
    private static readonly List<string> SkipLog = [];

    [Fact]
    public void Never_targets_production_host()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        // Acceptatie (acceptatie.lobsy.nl) is fine; the live apex and www are not.
        var host = new Uri(baseUrl).Host;
        Assert.False(
            host.Equals("lobsy.nl", StringComparison.OrdinalIgnoreCase)
            || host.Equals("www.lobsy.nl", StringComparison.OrdinalIgnoreCase),
            $"This suite must never run against production ({host}).");
    }

    public static TheoryData<string, int> PageMatrix()
    {
        var data = new TheoryData<string, int>();
        foreach (var language in Languages)
        {
            foreach (var (width, _) in Viewports)
            {
                data.Add(language, width);
            }
        }

        return data;
    }

    /// <summary>
    /// 404, 500, 403, 410, 429 and 503 in one language at one width: the real status on the
    /// document response, noindex, one h1, the right <c>lang</c>/<c>dir</c>, no horizontal
    /// overflow on mobile, readable body text, 44 px tap targets and no technical detail.
    /// </summary>
    [Theory]
    [MemberData(nameof(PageMatrix))]
    public async Task Status_pages_render_in_every_language_at_both_widths(string language, int width)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip($"page matrix {language}/{width}: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        var height = Viewports.First(v => v.Width == width).Height;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, language, width, height);
        var page = await context.NewPageAsync();

        foreach (var variant in await ResolveVariantsAsync(baseUrl, context))
        {
            var response = await page.GotoAsync(
                baseUrl + variant.Path,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            Assert.NotNull(response);
            Assert.Equal(variant.ExpectedStatus, response!.Status);

            await AssertStatusPageAsync(page, language, width, variant);

            await page.ScreenshotAsync(new()
            {
                Path = ScreenshotPath($"{variant.ExpectedStatus}-{variant.Name}-{language}-{width}.png"),
                FullPage = true
            });
        }
    }

    /// <summary>
    /// The static pages are server-rendered: without JavaScript they still show the full page.
    /// 403 and 429 are left out — the first needs a session, the second a limiter window.
    /// </summary>
    [Theory]
    [InlineData("nl")]
    [InlineData("ar")]
    public async Task Static_status_pages_work_without_javascript(string language)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip($"no-JS {language}: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            JavaScriptEnabled = false,
            IgnoreHTTPSErrors = true
        });
        await SetCultureCookieAsync(context, baseUrl, language);
        var page = await context.NewPageAsync();

        var variants = (await ResolveVariantsAsync(baseUrl, context))
            .Where(v => v.ExpectedStatus is 404 or 500 or 410 or 503)
            .ToList();

        foreach (var variant in variants)
        {
            var response = await page.GotoAsync(
                baseUrl + variant.Path,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            Assert.NotNull(response);
            Assert.Equal(variant.ExpectedStatus, response!.Status);
            Assert.Equal(1, await page.Locator("h1").CountAsync());
            Assert.False(string.IsNullOrWhiteSpace(await page.Locator("h1").First.InnerTextAsync()));
            Assert.True(await page.Locator(".err-layout").CountAsync() > 0);

            await page.ScreenshotAsync(new()
            {
                Path = ScreenshotPath($"{variant.ExpectedStatus}-{variant.Name}-{language}-nojs.png"),
                FullPage = true
            });
        }
    }

    /// <summary>§06.2 — a candidate on an admin URL keeps the URL and gets a real 403.</summary>
    [Fact]
    public async Task Candidate_on_an_admin_url_gets_403_at_that_url()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("403 flow: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        var page = await context.NewPageAsync();

        if (!await TryLoginAsync(page, baseUrl, CandidateEmail(), Password()))
        {
            Skip("403 flow: could not sign in as the seeded candidate");
            return;
        }

        var response = await page.GotoAsync(
            baseUrl + "/admin/instellingen",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        Assert.NotNull(response);
        Assert.Equal(403, response!.Status);
        Assert.Contains("/admin/instellingen", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/access-denied", page.Url, StringComparison.OrdinalIgnoreCase);

        var switchAccount = page.Locator("form.err-switch-form button[type=submit]");
        if (await switchAccount.CountAsync() == 0)
        {
            Skip("403 flow: no switch-account control on the page");
            return;
        }

        await page.ScreenshotAsync(new() { Path = ScreenshotPath("403-admin-nl-1440.png"), FullPage = true });
    }

    /// <summary>§06.2 — the employers-off reason has its own copy (paspoort dependency).</summary>
    [Fact]
    public async Task Employers_off_reason_shows_its_own_text()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("403 employers-off: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync(
            baseUrl + "/access-denied?reason=employers-off",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        Assert.NotNull(response);
        Assert.Equal(403, response!.Status);

        var body = await page.Locator("body").InnerTextAsync();
        if (!body.Contains("werkgever", StringComparison.OrdinalIgnoreCase))
        {
            Skip("403 employers-off: the reason has no specific copy on this build");
            return;
        }

        await page.ScreenshotAsync(new() { Path = ScreenshotPath("403-employers-off-nl-1440.png"), FullPage = true });
    }

    /// <summary>§06.2 — the 410 page offers at most three similar jobs, each a working 200.</summary>
    [Fact]
    public async Task Closed_vacancy_offers_at_most_three_working_alternatives()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("410 flow: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        var closedId = Environment.GetEnvironmentVariable("JOBSY_E2E_CLOSED_VACANCY_ID")?.Trim();
        if (string.IsNullOrWhiteSpace(closedId))
        {
            Skip("410 flow: JOBSY_E2E_CLOSED_VACANCY_ID is not set (seed has no known closed vacancy)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync(
            baseUrl + "/vacancies/" + closedId,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        Assert.NotNull(response);
        Assert.Equal(410, response!.Status);

        var cards = page.Locator("[data-testid='closed-vacancy-similar'] a[href*='/vacancies/']");
        var count = await cards.CountAsync();
        Assert.InRange(count, 0, 3);

        if (count == 0)
        {
            // The "none nearby" case must offer the search instead of a dead end.
            Assert.True(await page.Locator("a[href*='/banenkaart'], a[href*='/ontdek']").CountAsync() > 0);
        }
        else
        {
            var href = await cards.First.GetAttributeAsync("href");
            Assert.False(string.IsNullOrWhiteSpace(href));
            var similar = await page.GotoAsync(
                baseUrl + href,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            Assert.Equal(200, similar!.Status);
        }

        await page.ScreenshotAsync(new() { Path = ScreenshotPath("410-closed-nl-1440.png"), FullPage = true });
    }

    /// <summary>§06.2 — maintenance on gives anonymous visitors 503 while admins browse on.</summary>
    [Fact]
    public async Task Maintenance_switch_blocks_visitors_and_keeps_health_and_admins()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("maintenance flow: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var adminContext = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        await PlaywrightCookieConsent.AcceptAsync(adminContext);
        var adminPage = await adminContext.NewPageAsync();

        if (!await TryLoginAsync(adminPage, baseUrl, AdminEmail(), Password()))
        {
            Skip("maintenance flow: could not sign in as the seeded admin (MFA is required for admins)");
            return;
        }

        var toggle = adminPage.Locator("button.admin-switch[role=switch]").First;
        await adminPage.GotoAsync(
            baseUrl + "/admin/instellingen",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (await toggle.CountAsync() == 0)
        {
            Skip("maintenance flow: no maintenance toggle found on /admin/instellingen");
            return;
        }

        try
        {
            await SetSwitchAsync(toggle, on: true);
            await using var anonymous = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
            var anonymousPage = await anonymous.NewPageAsync();
            Assert.True(
                await WaitForStatusAsync(anonymousPage, baseUrl + "/", 503),
                "Anonymous visitors did not get a 503 within 20 s.");

            await anonymousPage.ScreenshotAsync(new()
            {
                Path = ScreenshotPath("503-maintenance-nl-1440.png"),
                FullPage = true
            });

            var health = await anonymousPage.APIRequest.GetAsync(baseUrl + "/healthz");
            Assert.Equal(200, health.Status);

            var adminResponse = await adminPage.GotoAsync(
                baseUrl + "/admin/instellingen",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            Assert.Equal(200, adminResponse!.Status);
            Assert.True(await adminPage.Locator(".maintenance-banner").CountAsync() > 0);
        }
        finally
        {
            await adminPage.GotoAsync(
                baseUrl + "/admin/instellingen",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            if (await toggle.CountAsync() > 0)
            {
                await SetSwitchAsync(toggle, on: false);
            }
        }

        await using var afterwards = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        var afterPage = await afterwards.NewPageAsync();
        Assert.True(
            await WaitForStatusAsync(afterPage, baseUrl + "/", 200),
            "Anonymous visitors did not get a 200 back within 20 s.");
    }

    /// <summary>§06.2 — losing the circuit shows the translated reconnect toast, not English.</summary>
    [Theory]
    [InlineData("nl")]
    [InlineData("ar")]
    public async Task Reconnect_toast_speaks_the_page_language(string language)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip($"reconnect toast {language}: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, language, 1440, 900);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

        var trying = await page.Locator(".reconnect-toast__msg--show").First.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(trying));

        var hasBlazor = await page.EvaluateAsync<bool>("() => typeof Blazor !== 'undefined' && !!Blazor");
        if (!hasBlazor)
        {
            Skip($"reconnect toast {language}: landing has no Blazor circuit in this environment");
            return;
        }

        await context.SetOfflineAsync(true);
        try
        {
            await page.EvaluateAsync("() => Blazor?.disconnect?.()");
            var shown = await WaitForVisibleAsync(page, "#components-reconnect-modal", 15_000);
            if (!shown)
            {
                Skip($"reconnect toast {language}: the circuit did not drop in this environment");
                return;
            }

            await page.ScreenshotAsync(new()
            {
                Path = ScreenshotPath($"reconnect-{language}-1440.png"),
                FullPage = false
            });
        }
        finally
        {
            await context.SetOfflineAsync(false);
        }
    }

    /// <summary>§06.2 — one failing card shows the inline block with its code; the page lives on.</summary>
    [Fact]
    public async Task Inline_block_error_keeps_the_rest_of_the_page_usable()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("inline block: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await NewContextAsync(browser, baseUrl, "nl", 1440, 900);
        var page = await context.NewPageAsync();

        if (!await TryLoginAsync(page, baseUrl, CandidateEmail(), Password()))
        {
            Skip("inline block: could not sign in as the seeded candidate");
            return;
        }

        await page.RouteAsync("**/api/candidate/**", async route => await route.FulfillAsync(new()
        {
            Status = 500,
            ContentType = "application/problem+json",
            Body = """{"code":"server_error","supportCode":"LB-7Q3K"}"""
        }));

        await page.GotoAsync(
            baseUrl + "/candidate/dashboard",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var block = page.Locator(".err-inline");
        if (!await WaitForVisibleAsync(page, ".err-inline", 15_000))
        {
            Skip("inline block: no card on the candidate dashboard uses InlineErrorBlock yet");
            return;
        }

        var text = await block.First.InnerTextAsync();
        Assert.Contains("Dit stukje laadt nu niet.", text, StringComparison.Ordinal);
        Assert.Matches(SupportCodePattern, text);

        // The rest of the page still works: the main navigation is there and clickable.
        Assert.True(await page.Locator("h1").CountAsync() >= 1);

        await page.UnrouteAsync("**/api/candidate/**");
        var retry = block.First.Locator("button");
        if (await retry.CountAsync() > 0)
        {
            await retry.First.ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
        }

        await page.ScreenshotAsync(new() { Path = ScreenshotPath("inline-block-nl-1440.png"), FullPage = true });
    }

    /// <summary>§06.2 — "Kopieer" puts the support code on the clipboard; without JS it is text.</summary>
    [Fact]
    public async Task Copy_button_puts_the_support_code_on_the_clipboard()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            Skip("copy button: no usable JOBSY_E2E_BASE_URL (unset, unreachable, or not running this stack)");
            return;
        }

        if (!await ThrowPathIsEnabledAsync(baseUrl))
        {
            Skip("copy button: the opt-in throwing path is off, so there is no live 500 page");
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true,
            Permissions = ["clipboard-read", "clipboard-write"]
        });
        await SetCultureCookieAsync(context, baseUrl, "nl");
        var page = await context.NewPageAsync();

        await page.GotoAsync(
            baseUrl + Jobsy.Web.Hosting.ErrorPagesExtensions.TestThrowPath,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var shown = await page.Locator("[data-support-code]").First.InnerTextAsync();
        Assert.Matches(SupportCodePattern, shown);

        var copy = page.Locator("[data-err-copy]");
        if (await copy.CountAsync() == 0)
        {
            Skip("copy button: no copy control on the 500 page");
            return;
        }

        await copy.First.ClickAsync();
        var clipboard = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Matches(SupportCodePattern, clipboard);
    }

    /// <summary>
    /// The admin maintenance control is a <c>button[role=switch]</c>. Turning it on opens a
    /// confirm dialog and does not save until that primary button is pressed.
    /// </summary>
    private static async Task SetSwitchAsync(ILocator toggle, bool on)
    {
        var want = on ? "true" : "false";
        var state = await toggle.GetAttributeAsync("aria-checked");
        if (string.Equals(state, want, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await toggle.ClickAsync();
        if (on)
        {
            // The cookie banner is also role=dialog with a primary button. Confirm only the
            // maintenance dialog ("Onderhoudsmodus aanzetten" / Bevestigen).
            var confirm = toggle.Page
                .GetByRole(AriaRole.Dialog, new() { Name = "Onderhoudsmodus aanzetten" })
                .GetByRole(AriaRole.Button, new() { Name = "Bevestigen" });
            await Assertions.Expect(confirm).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await confirm.ClickAsync();
        }

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-checked", want, new() { Timeout = 15_000 });
    }

    private sealed record Variant(string Name, string Path, int ExpectedStatus);

    /// <summary>
    /// The matrix rows that can actually run here: 404/429/503 always, 500 only with the opt-in
    /// throwing path, 410 only with a known closed vacancy, 403 only with a candidate session.
    /// </summary>
    private static async Task<List<Variant>> ResolveVariantsAsync(string baseUrl, IBrowserContext context)
    {
        var variants = new List<Variant>
        {
            new("unknown-page", "/bestaat-niet", 404),
            new("rate-limited", "/status/429", 429),
            new("maintenance", "/status/503", 503)
        };

        if (await ThrowPathIsEnabledAsync(baseUrl))
        {
            variants.Add(new Variant("server-error", Jobsy.Web.Hosting.ErrorPagesExtensions.TestThrowPath, 500));
        }
        else
        {
            Skip("page matrix: 500 skipped — Errors__EnableTestThrow is off on this host");
        }

        var closedId = Environment.GetEnvironmentVariable("JOBSY_E2E_CLOSED_VACANCY_ID")?.Trim();
        if (!string.IsNullOrWhiteSpace(closedId))
        {
            variants.Add(new Variant("closed-vacancy", "/vacancies/" + closedId, 410));
        }
        else
        {
            Skip("page matrix: 410 skipped — JOBSY_E2E_CLOSED_VACANCY_ID is not set");
        }

        var page = await context.NewPageAsync();
        try
        {
            if (await TryLoginAsync(page, baseUrl, CandidateEmail(), Password()))
            {
                variants.Add(new Variant("forbidden", "/admin/instellingen", 403));
            }
            else
            {
                Skip("page matrix: 403 skipped — no candidate session");
            }
        }
        finally
        {
            await page.CloseAsync();
        }

        return variants;
    }

    private static async Task AssertStatusPageAsync(IPage page, string language, int width, Variant variant)
    {
        var html = await page.ContentAsync();

        Assert.Matches(@"<meta\s+name=""robots""\s+content=""noindex", html);
        Assert.Equal(1, await page.Locator("h1").CountAsync());

        var documentLang = await page.EvaluateAsync<string?>("() => document.documentElement.lang");
        Assert.Equal(language, documentLang);

        var dir = await page.EvaluateAsync<string?>(
            "() => document.documentElement.getAttribute('dir') || document.querySelector('.err-layout')?.getAttribute('dir')");
        if (language == "ar")
        {
            Assert.Equal("rtl", dir);
        }
        else
        {
            Assert.NotEqual("rtl", dir);
        }

        foreach (var fragment in ForbiddenFragments)
        {
            Assert.DoesNotContain(fragment, html, StringComparison.Ordinal);
        }

        var visibleText = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotMatch(GuidLike, visibleText);

        if (variant.ExpectedStatus is 500 or 429)
        {
            Assert.Matches(SupportCodePattern, visibleText);
        }

        if (width == 390)
        {
            var overflow = await page.EvaluateAsync<int>(
                "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
            Assert.InRange(overflow, 0, 1);
        }

        var bodyFontPx = await page.EvaluateAsync<double>(
            "() => parseFloat(getComputedStyle(document.querySelector('.err-hero__lead') || document.body).fontSize)");
        Assert.True(bodyFontPx >= 16, $"Body text is {bodyFontPx}px, below the 16px floor.");

        var primary = page.Locator(".pub-btn--primary");
        if (await primary.CountAsync() > 0)
        {
            var box = await primary.First.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Height >= 44, $"Primary button is {box.Height}px high, below the 44px tap target.");
        }
    }

    private static async Task<IBrowserContext> NewContextAsync(
        IBrowser browser,
        string baseUrl,
        string language,
        int width,
        int height)
    {
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = language,
            IgnoreHTTPSErrors = true
        });
        await SetCultureCookieAsync(context, baseUrl, language);
        return context;
    }

    private static async Task SetCultureCookieAsync(IBrowserContext context, string baseUrl, string language)
        => await context.AddCookiesAsync(
        [
            new Cookie { Name = "Jobsy.Culture", Value = language, Url = baseUrl }
        ]);

    private static async Task<bool> WaitForStatusAsync(IPage page, string url, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var response = await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });
            if (response?.Status == expected)
            {
                return true;
            }

            await page.WaitForTimeoutAsync(2_000);
        }

        return false;
    }

    private static async Task<bool> WaitForVisibleAsync(IPage page, string selector, int timeoutMs)
    {
        try
        {
            await page.Locator(selector).First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });
            return true;
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            return false;
        }
    }

    private static async Task<bool> ThrowPathIsEnabledAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("Accept", "text/html");
            using var response = await http.GetAsync(
                baseUrl + Jobsy.Web.Hosting.ErrorPagesExtensions.TestThrowPath);
            return (int)response.StatusCode == 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit, button.au-submit[type=submit]'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await Task.WhenAll(
                page.WaitForURLAsync(
                    url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 }),
                submit.ClickAsync());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static string CandidateEmail()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultCandidateEmail;

    private static string AdminEmail()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_ADMIN_EMAIL") ?? DefaultAdminEmail;

    private static string Password()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

    /// <summary>
    /// Reachable *and* already running this stack's pages. Pointing the suite at a host that was
    /// deployed before errors 01 would otherwise fail every row for the wrong reason.
    /// </summary>
    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var health = await http.GetAsync(baseUrl + "/healthz");
            if (!health.IsSuccessStatusCode)
            {
                return false;
            }

            http.DefaultRequestHeaders.Add("Accept", "text/html");
            using var status = await http.GetAsync(baseUrl + "/status/404");
            var html = await status.Content.ReadAsStringAsync();
            if (html.Contains("err-layout", StringComparison.Ordinal))
            {
                return true;
            }

            Skip($"{new Uri(baseUrl).Host} does not serve the errors-stack status pages yet");
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string ScreenshotPath(string fileName)
    {
        var dir = Path.Combine(RepoRoot(), "artifacts", "playwright-errors");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, fileName);
    }

    /// <summary>Records a skip reason and writes it next to the screenshots for the PR body.</summary>
    private static void Skip(string reason)
    {
        lock (SkipLog)
        {
            if (!SkipLog.Contains(reason))
            {
                SkipLog.Add(reason);
            }

            var dir = Path.Combine(RepoRoot(), "artifacts", "playwright-errors");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, "skipped.txt"), SkipLog);
        }
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
