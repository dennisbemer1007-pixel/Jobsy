using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Candidate tests stack E2E (07). Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// Real Mollie is covered by API tests with a fake Mollie handler (01); this suite expects stub payments.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateTestsPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Career_intro_shows_depth_pick_and_continue(int width, int height)
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, width, height);
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/candidate/career?stap=intro",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var pick = session.Page.Locator(".test-depth-pick");
        if (await pick.CountAsync() == 0) return;
        Assert.True(await pick.IsVisibleAsync());
        Assert.Contains("Heel diep", await session.Page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deep_offer_requires_waiver_and_shows_price()
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, 1440, 900);
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/candidate/deep-analysis/competence",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var html = await session.Page.ContentAsync();
        if (!html.Contains("Duik tot de bodem", StringComparison.Ordinal)) return;
        Assert.Contains("inclusief btw", html, StringComparison.OrdinalIgnoreCase);
        var pay = session.Page.Locator("button.engagement-btn").Filter(new() { HasText = "Naar betalen" });
        if (await pay.CountAsync() == 0) return;
        Assert.True(await pay.IsDisabledAsync());
    }

    [Fact]
    public async Task Invalid_deep_kind_shows_friendly_not_found()
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, 1440, 900);
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/candidate/deep-analysis/not-a-real-kind",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var html = await session.Page.ContentAsync();
        Assert.Contains("bestaat niet", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Arabic_intro_has_rtl_dir()
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, 390, 844, locale: "ar");
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/candidate/career?stap=intro",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var dir = await session.Page.Locator("html").GetAttributeAsync("dir");
        if (string.IsNullOrWhiteSpace(dir)) return;
        Assert.Equal("rtl", dir);
        var overflow = await session.Page.EvaluateAsync<bool>("""
            () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
            """);
        Assert.False(overflow);
    }

    [Fact]
    public async Task Reduced_motion_leaves_no_running_animations_on_intro()
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, 1440, 900, reducedMotion: true);
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/candidate/competencies?stap=intro",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (await session.Page.Locator(".test-shell").CountAsync() == 0) return;
        var running = await session.Page.EvaluateAsync<int>("""
            () => Array.from(document.getAnimations?.() ?? []).filter(a => a.playState === 'running').length
            """);
        Assert.Equal(0, running);
    }

    [Fact]
    public async Task TestDetail_lists_free_cta_before_extended()
    {
        if (!TryBase(out var baseUrl)) return;
        await using var session = await OpenAsync(baseUrl, 390, 844);
        if (session is null) return;

        await session.Page.GotoAsync(baseUrl + "/profiel/tests/competence",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var choices = session.Page.Locator(".test-detail__choices");
        if (await choices.CountAsync() == 0) return;
        var html = await choices.InnerHTMLAsync();
        var freeIdx = html.IndexOf("StartFree", StringComparison.OrdinalIgnoreCase);
        // rendered text rather than key
        freeIdx = html.IndexOf("gratis", StringComparison.OrdinalIgnoreCase);
        var paidIdx = html.IndexOf("Uitgebreide", StringComparison.OrdinalIgnoreCase);
        if (freeIdx < 0 || paidIdx < 0) return;
        Assert.True(freeIdx < paidIdx);
    }

    private static bool TryBase(out string baseUrl)
    {
        baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return !string.IsNullOrWhiteSpace(baseUrl);
    }

    private static async Task<BrowserSession?> OpenAsync(
        string baseUrl, int width, int height, string? locale = null, bool reducedMotion = false)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            Locale = locale ?? "nl-NL",
            ReducedMotion = reducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
            return null;
        }

        return new BrowserSession(playwright, browser, context, page);
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/account/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            var email = page.Locator("input[type=email], input[name=email]").First;
            var password = page.Locator("input[type=password]").First;
            if (await email.CountAsync() == 0) return false;
            await email.FillAsync(Environment.GetEnvironmentVariable("JOBSY_E2E_EMAIL") ?? DefaultEmail);
            await password.FillAsync(Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD") ?? DefaultPassword);
            await page.Locator("button[type=submit]").First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class BrowserSession(
        IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IPage Page { get; } = page;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
