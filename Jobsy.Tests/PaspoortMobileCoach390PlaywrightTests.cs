using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class PaspoortMobileCoach390PlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string OutDir = "/opt/cursor/artifacts/pr1488-mobile-coach";

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun())
        {
            return;
        }

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(OutDir);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private static bool ShouldRun()
        => string.Equals(Environment.GetEnvironmentVariable("JOBSY_PASPOORT_MOBILE_CAPTURE"), "1", StringComparison.Ordinal);

    [Fact(Timeout = 600_000)]
    public async Task Capture_paspoort_mobile_coach_evidence()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun())
        {
            return;
        }

        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", "1");
        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1280, 800, "1280") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                IgnoreHTTPSErrors = true,
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await SignInAsync(page, root, "/candidate/paspoort?tab=dna");
            await page.GotoAsync($"{root}/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(".lobsy-coach-dock__lobster").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(500);
            if (w < 1024)
            {
                await AssertCoachAboveNavAsync(page);
            }

            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-top-{tag}.png"), FullPage = false });

            if (w < 1024)
            {
                await page.Locator("[data-testid=passport-footer-nav]").WaitForAsync(
                    new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await page.WaitForTimeoutAsync(350);
                await AssertCoachAboveNavAsync(page);
                await AssertLastContentToTabBarGapAsync(page, maxGapPx: 140);
                await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-bottom-{tag}.png"), FullPage = false });
            }

            await page.GotoAsync($"{root}/candidate/paspoort?tab=proof", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator("[data-testid=passport-cv-proposal]").First
                .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
            await page.Locator("[data-testid=passport-cv-proposal]").First.ScrollIntoViewIfNeededAsync();
            await page.WaitForTimeoutAsync(300);
            if (w < 1024)
            {
                await AssertCoachAboveNavAsync(page);
            }

            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-bewijzen-cv-checklist-{tag}.png"), FullPage = false });

            await page.GotoAsync($"{root}/candidate/paspoort?tab=data", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator(".passport-data, .passport-page--cv-plain").First
                .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
            if (w < 1024)
            {
                await AssertCoachAboveNavAsync(page);
            }

            if (w < 1024)
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await page.WaitForTimeoutAsync(350);
                await AssertLastContentToTabBarGapAsync(page, maxGapPx: 140);
                await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-gegevens-bottom-{tag}.png"), FullPage = false });
            }
            else
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-gegevens-{tag}.png"), FullPage = false });
            }
        }
    }

    private static async Task SignInAsync(IPage page, string root, string returnPath)
    {
        var url =
            $"{root.TrimEnd('/')}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(returnPath)}";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    private static async Task AssertCoachAboveNavAsync(IPage page)
    {
        var ok = await page.EvaluateAsync<bool>(
            """
            () => {
              const dock = document.querySelector('.lobsy-coach-dock--variant-b');
              const nav = document.querySelector('.bottom-nav');
              if (!dock || !nav) return false;
              const dr = dock.getBoundingClientRect();
              const nr = nav.getBoundingClientRect();
              const rightGap = window.innerWidth - dr.right;
              return rightGap >= -1 && rightGap <= 16.5 && dr.bottom <= nr.top + 1;
            }
            """);
        Assert.True(ok, "Coach must sit bottom-right within 16px and above the tab bar");
    }

    private static async Task AssertLastContentToTabBarGapAsync(IPage page, double maxGapPx)
    {
        var gap = await page.EvaluateAsync<double>(
            """
            () => {
              const nav =
                document.querySelector('.bottom-nav')
                ?? document.querySelector('[data-testid="candidate-bottom-nav-ssr"]');
              if (!nav) return 9999;
              const nr = nav.getBoundingClientRect();
              const visibleBottom = (el) => {
                const r = el.getBoundingClientRect();
                if (r.height < 1) return 0;
                const s = getComputedStyle(el);
                if (s.display === 'none' || s.visibility === 'hidden') return 0;
                return r.bottom;
              };
              let contentBottom = 0;
              for (const el of document.querySelectorAll(
                '.passport-ssr-footer-nav, .passport-ssr-nav-links')) {
                contentBottom = Math.max(contentBottom, visibleBottom(el));
              }
              if (contentBottom <= 0) return 9999;
              return nr.top - contentBottom;
            }
            """);
        Assert.True(
            gap <= maxGapPx,
            $"Gap from last content to tab bar top must be <= {maxGapPx}px (was {gap:F1}px)");
    }
}
