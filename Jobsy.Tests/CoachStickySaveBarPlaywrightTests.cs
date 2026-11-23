using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CoachStickySaveBarPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string OutDir = "/opt/cursor/artifacts/coach-sticky-save-bar";

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
        => string.Equals(Environment.GetEnvironmentVariable("JOBSY_COACH_STICKY_BAR_CAPTURE"), "1", StringComparison.Ordinal);

    [Fact(Timeout = 600_000)]
    public async Task Capture_coach_above_sticky_save_bars()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun())
        {
            return;
        }

        await CaptureGeboortedatumAsync();
        await CapturePaspoortGegevensAsync();
    }

    private async Task CaptureGeboortedatumAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", null);
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE_DOB", "1");

        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1280, 800, "1280") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await SignInAsync(page, root, "/candidate/geboortedatum");
            await page.GotoAsync($"{root}/candidate/geboortedatum", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("[data-testid=candidate-dob-save]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await page.Locator(".lobsy-coach-dock__lobster, [data-ssr-coach-open]").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(600);
            if (w < 1024)
            {
                await AssertCoachDoesNotIntersectStickyBarAsync(page);
            }

            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"geboortedatum-{tag}.png"), FullPage = false });
        }

        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE_DOB", null);
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", "1");
    }

    private async Task CapturePaspoortGegevensAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", "1");
        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1280, 800, "1280") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await SignInAsync(page, root, "/candidate/paspoort?tab=data");
            await page.GotoAsync($"{root}/candidate/paspoort?tab=data", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(".passport-data, [data-testid=passport-data-save]").First
                .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
            await page.Locator(".lobsy-coach-dock__lobster, [data-ssr-coach-open]").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(400);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"paspoort-gegevens-{tag}.png"), FullPage = false });
        }
    }

    private async Task<IBrowserContext> NewContextAsync(int w, int h)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        return context;
    }

    private static async Task SignInAsync(IPage page, string root, string returnPath)
    {
        var url = $"{root.TrimEnd('/')}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(returnPath)}";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    private static async Task AssertCoachDoesNotIntersectStickyBarAsync(IPage page)
    {
        var ok = await page.EvaluateAsync<bool>(
            """
            () => {
              const dock =
                document.querySelector('.lobsy-coach-dock--variant-b')
                ?? document.querySelector('.lobsy-coach-dock');
              const bar = document.querySelector('.profile-save-bar');
              if (!dock || !bar) return false;
              const barStyle = getComputedStyle(bar);
              if (barStyle.position !== 'fixed') return true;
              const union = (a, b) => ({
                left: Math.min(a.left, b.left),
                top: Math.min(a.top, b.top),
                right: Math.max(a.right, b.right),
                bottom: Math.max(a.bottom, b.bottom),
                width: Math.max(a.right, b.right) - Math.min(a.left, b.left),
                height: Math.max(a.bottom, b.bottom) - Math.min(a.top, b.top),
              });
              let dr = dock.getBoundingClientRect();
              const lobster = dock.querySelector('.lobsy-coach-dock__lobster');
              if (lobster) {
                dr = union(dr, lobster.getBoundingClientRect());
              }
              const btn = dock.querySelector('[data-ssr-coach-open], .lobsy-coach-dock__btn--lobster-b');
              if (btn) {
                dr = union(dr, btn.getBoundingClientRect());
              }
              const intersects = (a, b) =>
                a.width > 0 && a.height > 0 && b.width > 0 && b.height > 0
                && !(a.right <= b.left || a.left >= b.right || a.bottom <= b.top || a.top >= b.bottom);
              const br = bar.getBoundingClientRect();
              const saveBtn = bar.querySelector('.login-submit, .btn-primary, button[type=submit]');
              if (saveBtn && intersects(dr, saveBtn.getBoundingClientRect())) return false;
              return !intersects(dr, br);
            }
            """);
        Assert.True(ok, "B2 coach must not intersect the sticky save bar or its primary button at 390");
    }
}
